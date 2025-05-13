import logging
import os
import re
import json
from datetime import datetime

import numpy as np
import pandas as pd
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import TimeSeriesSplit, GridSearchCV
from sklearn.pipeline import make_pipeline
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import r2_score
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType

from upload_model import upload_to_blob

logger = logging.getLogger(__name__)

def add_minutes_to_dry(df: pd.DataFrame, threshold: float) -> pd.DataFrame:
    soil = df["soil_humidity"].to_numpy()
    ts_minutes = df["timestamp"].values.astype("datetime64[m]").view("int")
    below = np.where(soil < threshold)[0]
    next_idx = np.full(len(df), np.nan, dtype=float)

    for i in range(len(df) - 1):
        j = below[below > i]
        if j.size:
            next_idx[i] = ts_minutes[j[0]] - ts_minutes[i]

    df["minutes_to_dry"] = next_idx
    df["threshold"] = threshold
    return df

def clean_sensor_data(df: pd.DataFrame, expected_interval_minutes=10, gap_drop_threshold=60) -> pd.DataFrame:
    logger.info("Starting sensor data cleaning. Initial samples: %d", len(df))

    df_clean = df[
        (df["soil_humidity"].between(0, 100)) &
        (df["air_humidity"].between(0, 100)) &
        (df["temperature"].between(-30, 60)) &
        (df["light"] >= 0)
    ].copy()

    logger.info("Samples after hard limits filter: %d", len(df_clean))

    df_clean.sort_values("timestamp", inplace=True)

    df_clean["gap_minutes"] = df_clean["timestamp"].diff().dt.total_seconds() / 60
    df_clean["gap_minutes"].fillna(0, inplace=True)

    rows_before = len(df_clean)
    df_clean = df_clean[df_clean["gap_minutes"] <= gap_drop_threshold]
    logger.info("Dropped %d samples after large gaps (> %d min). Remaining: %d",
                rows_before - len(df_clean), gap_drop_threshold, len(df_clean))

    df_clean["soil_delta"] = df_clean["soil_humidity"].diff().fillna(0)
    df_clean.loc[df_clean["gap_minutes"] > expected_interval_minutes, "soil_delta"] = 0
    df_clean.drop(columns=["gap_minutes"], inplace=True)

    logger.info("Data cleaning complete. Final samples: %d", len(df_clean))
    return df_clean

def train_model(json_samples: str, json_threshold: str) -> dict:
    parsed_samples = json.loads(json_samples)

    if isinstance(parsed_samples, dict) and "response" in parsed_samples and "list" in parsed_samples["response"]:
        logger.info("Detected nested response/list/SampleDTO structure.")
        sample_data = [item["SampleDTO"] for item in parsed_samples["response"]["list"]]
    elif isinstance(parsed_samples, list) and parsed_samples and isinstance(parsed_samples[0], dict):
        if "SampleDTO" in parsed_samples[0]:
            logger.info("Detected list of SampleDTO wrappers.")
            sample_data = [item["SampleDTO"] for item in parsed_samples]
        else:
            logger.info("Detected direct list of SampleDTO dicts.")
            sample_data = parsed_samples
    else:
        raise ValueError("Unexpected JSON structure from /sensor/data")

    logger.info("Parsed %d samples", len(sample_data))

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

    required_cols = ["soil_humidity", "air_humidity", "temperature", "light", "timestamp"]
    missing = set(required_cols) - set(df.columns)
    if missing:
        raise ValueError(f"Missing required columns in sample data: {missing}")

    df["timestamp"] = pd.to_datetime(df["timestamp"])

    df = clean_sensor_data(df)

    if df.empty:
        logger.error("No valid samples after data cleaning. Skipping model training.")
        return {
            "message": "No valid training samples found after cleaning.",
            "model_file": None,
            "metadata_file": None,
            "rmse_cv": None,
            "r2_insample": None
        }

    threshold = json.loads(json_threshold)
    logger.info("Threshold value received: %s", threshold)

    if df["soil_humidity"].min() >= threshold:
        new_threshold = df["soil_humidity"].quantile(0.10)
        logger.warning(
            "Threshold %.2f is too low (min soil_humidity = %.2f). Adjusting threshold to 10th percentile: %.2f",
            threshold, df["soil_humidity"].min(), new_threshold
        )
        threshold = new_threshold

    df = add_minutes_to_dry(df, threshold)
    df.dropna(subset=["minutes_to_dry"], inplace=True)

    if df.empty:
        logger.error("No data remains after filtering minutes_to_dry. Skipping model training.")
        return {
            "message": "No valid training samples found after threshold filtering.",
            "model_file": None,
            "metadata_file": None,
            "rmse_cv": None,
            "r2_insample": None
        }

    df["hour_sin"] = np.sin(df["timestamp"].dt.hour / 24 * 2 * np.pi)
    df["hour_cos"] = np.cos(df["timestamp"].dt.hour / 24 * 2 * np.pi)

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

    pipe = make_pipeline(StandardScaler(), RandomForestRegressor(random_state=42))

    tscv = TimeSeriesSplit(n_splits=5)
    param_grid = {
        "randomforestregressor__n_estimators": [100, 200],
        "randomforestregressor__max_depth": [None, 10, 20]
    }

    gscv = GridSearchCV(
        estimator=pipe,
        param_grid=param_grid,
        cv=tscv,
        scoring="neg_root_mean_squared_error",
        n_jobs=-1,
    )
    gscv.fit(X, y)

    rmse = -gscv.best_score_
    r2 = r2_score(y, gscv.predict(X))

    initial_type = [("input", FloatTensorType([None, len(feature_cols)]))]
    onnx_model = convert_sklearn(gscv.best_estimator_, initial_types=initial_type)

    now = datetime.now()
    ts_str = now.strftime("%Y%m%d%H%M%S")
    base_name = "soil_humidity_rf"
    model_fname = f"{base_name}_{ts_str}.onnx"
    meta_fname = f"{base_name}_{ts_str}.metadata.json"

    local_models_dir = os.path.join(os.path.dirname(__file__), "models")
    os.makedirs(local_models_dir, exist_ok=True)

    model_path = os.path.join(local_models_dir, model_fname)
    with open(model_path, "wb") as f:
        f.write(onnx_model.SerializeToString())

    metadata = {
        "model_type": "RandomForest (nonlinear)",
        "target": f"minutes_to_dry (<{threshold}% soil humidity)",
        "feature_names": feature_cols,
        "best_params": gscv.best_params_,
        "cross_val_splits": tscv.n_splits,
        "training_timestamp_utc": now.isoformat(),
        "rmse_cv": round(rmse, 2),
        "r2_insample": round(r2, 2),
    }

    meta_path = os.path.join(local_models_dir, meta_fname)
    with open(meta_path, "w") as f:
        json.dump(metadata, f, indent=4)

    upload_to_blob(model_path, model_fname)
    upload_to_blob(meta_path, meta_fname)

    logger.info("Model and metadata uploaded: %s, %s", model_fname, meta_fname)

    return {
        "message": "Model and metadata uploaded successfully.",
        "model_file": model_fname,
        "metadata_file": meta_fname,
        "rmse_cv": round(rmse, 2),
        "r2_insample": round(r2, 2),
    }
