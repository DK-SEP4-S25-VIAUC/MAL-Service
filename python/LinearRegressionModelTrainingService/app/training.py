"""
Train a linear (Ridge) regression baseline that predicts
“minutes until soil humidity drops below 20 %”.
"""

import os
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

from LinearRegressionModelTrainingService.app.upload_model import upload_to_blob

# Helper: derive the target variable
def add_minutes_to_dry(df: pd.DataFrame, threshold: float) -> pd.DataFrame:
    """
    Adds a 'minutes_to_dry' column to *df*.

    For every row i, the value is the number of minutes until the next
    measurement where soil_humidity < `threshold`.
    If that never happens, NaN is returned (row will be dropped later).

    """
    soil = df["soil_humidity"].to_numpy()

    # Convert timestamps to a NumPy datetime64[m] array *then* to ints
    # so that each entry is "minutes since epoch".

    ts_minutes = df["timestamp"].values.astype("datetime64[m]").view("int")

    below = np.where(soil < threshold)[0]
    next_idx = np.full(len(df), np.nan, dtype=float) # handle NaNs

    for i in range(len(df) - 1):
        j = below[below > i] # first future index < 40 %
        if j.size:
            next_idx[i] = ts_minutes[j[0]] - ts_minutes[i]

    df["minutes_to_dry"] = next_idx
    df["threshold"] = threshold

    return df

# Main training entry point
def train_model(json_samples: str, json_threshold: str) -> dict:
    # Parse the incoming JSON
    parsed_samples = json.loads(json_samples)
    samples = parsed_samples["response"]["list"]
    sample_data = [item["SampleDTO"] for item in samples]
    df = pd.DataFrame(sample_data)

    # Parse incoming JSON threshold
    parsed_threshold = json.loads(json_threshold)
    threshold = parsed_threshold["threshold"]

    # Data pre-processing
    df["timestamp"] = pd.to_datetime(df["timestamp"])
    df.sort_values("timestamp", inplace=True)

    # Create target variable
    df = add_minutes_to_dry(df, threshold)
    df.dropna(subset=["minutes_to_dry"], inplace=True)

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
        "target": "minutes_to_dry (<20 % soil humidity)",
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

    # Return a short summary to caller
    return {
        "message":        "Model and metadata uploaded successfully.",
        "model_file":     model_fname,
        "metadata_file":  meta_fname,
        "rmse_cv":        round(rmse, 2),
        "r2_insample":    round(r2, 2),
    }
