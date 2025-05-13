# training.py

"""
Train a linear (Ridge) regression baseline that predicts
“minutes until soil humidity drops below a certain threshold %”.
"""
import logging
import os
import re
import json
from datetime import datetime
import numpy as np
import pandas as pd
from sklearn.linear_model import Ridge
from sklearn.model_selection import TimeSeriesSplit, GridSearchCV
from sklearn.pipeline import make_pipeline
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import r2_score
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType

from upload_model import upload_to_blob

logger = logging.getLogger(__name__)

# Helper: derive the target variable
def add_minutes_to_dry(df: pd.DataFrame, threshold: float) -> pd.DataFrame:
    soil = df["soil_humidity"].to_numpy()

    ts_minutes = df["timestamp"].values.astype("datetime64[m]").view("int")
    below = np.where(soil < threshold)[0]

    if below.size == 0:
        logger.warning("No samples below threshold %.2f found in data. minutes_to_dry cannot be calculated.", threshold)
        return df.assign(minutes_to_dry=np.nan, threshold=threshold)

    next_idx = np.full(len(df), np.nan, dtype=float)

    for i in range(len(df) - 1):
        j = below[below > i]
        if j.size:
            next_idx[i] = ts_minutes[j[0]] - ts_minutes[i]

    df["minutes_to_dry"] = next_idx
    df["threshold"] = threshold

    return df


# Main training entry point
def train_model(json_samples: str, json_threshold: str) -> dict:

    # Parse the incoming JSON samples
    parsed_samples = json.loads(json_samples)

    if parsed_samples and isinstance(parsed_samples[0], dict):
        if "SampleDTO" in parsed_samples[0]:
            sample_data = [item["SampleDTO"] for item in parsed_samples]
        else:
            sample_data = parsed_samples
    else:
        raise ValueError("Unexpected JSON structure from /sensor/data")

    logger.info("Received %d samples", len(sample_data))

    unique_keys = set()
    for sample in sample_data:
        unique_keys.update(sample.keys())
    logger.debug("Unique keys in sample data: %s", unique_keys)
    df = pd.DataFrame(sample_data)

    rename_map = {
        "soilhumidity": "soil_humidity",
        "airhumidity": "air_humidity",
        "airtemperature": "temperature",
        "lightvalue": "light",
        "timestamp": "timestamp"
    }

    def normalize_col(col: str) -> str:
        return re.sub(r"[^a-z0-9]", "", col.lower())

    df.rename(columns={col: rename_map.get(normalize_col(col), col) for col in df.columns}, inplace=True)

    logger.debug("DataFrame columns after rename: %s", df.columns.tolist())

    required_cols = ["soil_humidity", "air_humidity", "temperature", "light", "timestamp"]
    missing = set(required_cols) - set(df.columns)
    if missing:
        raise ValueError(f"Missing required columns in sample data: {missing}")


    # Parse incoming JSON threshold
    threshold = json.loads(json_threshold)
    logger.info("Threshold value received: %s", threshold)

    # Auto-fix threshold if unreachable
    if df["soil_humidity"].min() >= threshold:
        new_threshold = df["soil_humidity"].quantile(0.10)
        logger.warning(
            "Threshold %.2f is too low (min soil_humidity = %.2f). Adjusting threshold to 10th percentile: %.2f",
            threshold, df["soil_humidity"].min(), new_threshold
        )
        threshold = new_threshold

    # Data pre-processing
    df["timestamp"] = pd.to_datetime(df["timestamp"])
    df.sort_values("timestamp", inplace=True)

    # Create target variable
    df = add_minutes_to_dry(df, threshold)
    df.dropna(subset=["minutes_to_dry"], inplace=True)

    # Early exit if no data remains
    if df.empty:
        logger.error("No data remains after filtering minutes_to_dry. Skipping model training.")
        return {
            "message": "No valid training samples found after applying threshold.",
            "model_file": None,
            "metadata_file": None,
            "rmse_cv": None,
            "r2_insample": None
        }

    # Feature engineering
    df["soil_delta"] = df["soil_humidity"].diff().fillna(0) # Calculating the slope (Hældningskoefficient) between the soil-humidity and the one immediately before.
    # Using Sin and Cos, while linear regression would view 0 and 23, as being far away from each other,
    # while it is actually "close" to each other in the cycle of the day. By using the unit circle, we don't get a leap in "time" at midnight
    df["hour_sin"]   = np.sin(df["timestamp"].dt.hour / 24 * 2 * np.pi)
    df["hour_cos"]   = np.cos(df["timestamp"].dt.hour / 24 * 2 * np.pi)

    feature_cols = [
        "soil_humidity",
        "soil_delta",
        "air_humidity",
        "temperature",
        "light",
        "hour_sin",
        "hour_cos",
        "threshold",
    ]

    X = df[feature_cols].astype(float)
    y = df["minutes_to_dry"].astype(float)

    # Build a pipeline so scaler + model are saved together
    pipe = make_pipeline(StandardScaler(), Ridge())

    # Time-series cross-validation
    # By using time-series split instead of train-test-split, we omit breaking the chronologically order, which we need in order to predict minutes to dry
    # In a running / production flow, we need the entire historical dataset to train the actual model, CV-folds acts as our train/validation process, but still keeping the chronologically order
    tscv = TimeSeriesSplit(n_splits=5)
    param_grid = {"ridge__alpha": np.logspace(-4, 3, 20)}

    gscv = GridSearchCV(
        estimator=pipe,
        param_grid=param_grid,
        cv=tscv,
        scoring="neg_root_mean_squared_error",
        n_jobs=-1,
    )
    gscv.fit(X, y)

    # Evaluate baseline performance
    rmse = -gscv.best_score_
    r2   = r2_score(y, gscv.predict(X))

    # ONNX export
    initial_type = [("input", FloatTensorType([None, len(feature_cols)]))]
    onnx_model = convert_sklearn(gscv.best_estimator_, initial_types=initial_type)

    # Build timestamped filename
    now = datetime.now()
    ts_str = now.strftime("%Y%m%d%H%M%S")
    base_name = "soil_humidity_baseline_ridge"
    model_fname = f"{base_name}_{ts_str}.onnx"
    meta_fname  = f"{base_name}_{ts_str}.metadata.json"

    local_models_dir = os.path.join(os.path.dirname(__file__), "models")
    os.makedirs(local_models_dir, exist_ok=True)

    model_path = os.path.join(local_models_dir, model_fname)
    with open(model_path, "wb") as f:
        f.write(onnx_model.SerializeToString())

    # Produce metadata
    metadata = {
        "model_type": "Ridge (linear)",
        "target": f"minutes_to_dry (<{threshold}% soil humidity)",
        "feature_names": feature_cols,
        "alpha": gscv.best_params_["ridge__alpha"],
        "cross_val_splits": tscv.n_splits,
        "training_timestamp_utc": now.isoformat(),
        "rmse_cv": round(rmse, 2),
        "r2_insample": round(r2, 2),
    }
    meta_path = os.path.join(local_models_dir, meta_fname)
    with open(meta_path, "w") as f:
        json.dump(metadata, f, indent=4)

    # Upload to Azure Blob Storage
    upload_to_blob(model_path,  model_fname)
    upload_to_blob(meta_path,   meta_fname)

    logger.info("Model and metadata uploaded: %s, %s", model_fname, meta_fname)

    # Return a short summary to caller
    return {
        "message":        "Model and metadata uploaded successfully.",
        "model_file":     model_fname,
        "metadata_file":  meta_fname,
        "rmse_cv":        round(rmse, 2),
        "r2_insample":    round(r2, 2),
    }
