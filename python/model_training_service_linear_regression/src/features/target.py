import logging

import numpy as np
import pandas as pd

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
