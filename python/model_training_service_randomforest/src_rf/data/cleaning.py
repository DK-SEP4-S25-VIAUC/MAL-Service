import logging

import pandas as pd

logger = logging.getLogger(__name__)


def clean_sensor_data(df: pd.DataFrame, expected_interval_minutes=20, gap_drop_threshold=60) -> pd.DataFrame:
    """
    Cleans sensor data by:
    - Filtering out physically impossible values
    - Detecting and handling gaps in data
    - Adjusting soil_delta where needed
    """

    logger.info("Starting sensor data cleaning. Initial samples: %d", len(df))

    # Outliers and spikes filter
    df_clean = df[
        (df["soil_humidity"].between(0, 100)) &
        (df["air_humidity"].between(20, 90)) &
        (df["temperature"].between(0, 50)) &
        (df["light"].between(0, 1023))
        ].copy()


    logger.info("Samples after hard limits filter: %d", len(df_clean))

    # Sort by timestamp
    df_clean.sort_values("timestamp", inplace=True)

    # Compute time gaps
    df_clean["gap_minutes"] = df_clean["timestamp"].diff().dt.total_seconds() / 60
    df_clean["gap_minutes"] = df_clean["gap_minutes"].fillna(0)

    # Drop rows after large gaps (e.g., sensor offline > gap_drop_threshold minutes)
    rows_before = len(df_clean)
    df_clean = df_clean[df_clean["gap_minutes"] <= gap_drop_threshold]
    logger.info("Dropped %d samples after large gaps (> %d min). Remaining: %d",
                rows_before - len(df_clean), gap_drop_threshold, len(df_clean))

    # Compute soil_delta (slope)
    df_clean["soil_delta"] = df_clean["soil_humidity"].diff().fillna(0)

    # Set soil_delta to 0 if gap is too large (above expected_interval_minutes)
    df_clean.loc[df_clean["gap_minutes"] > expected_interval_minutes, "soil_delta"] = 0

    # Drop helper columns if you don't want them in features later
    df_clean.drop(columns=["gap_minutes"], inplace=True)

    logger.info("Data cleaning complete. Final samples: %d", len(df_clean))

    return df_clean
