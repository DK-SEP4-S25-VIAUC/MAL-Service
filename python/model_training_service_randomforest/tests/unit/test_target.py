import pandas as pd

from src_rf.features.target import add_minutes_to_dry


def test_add_minutes_to_dry_all_above_threshold():
    df = pd.DataFrame({
        "timestamp": pd.date_range("2025-01-01", periods=5, freq="10min"),
        "soil_humidity": [80, 82, 85, 90, 95],
    })

    thresh = 50.0
    out = add_minutes_to_dry(df.copy(), threshold=thresh)

    # minutes_to_dry should exist and be all NaN
    assert "minutes_to_dry" in out.columns
    assert out["minutes_to_dry"].isna().all()

    # threshold column should exist and match the input
    assert "threshold" in out.columns
    assert (out["threshold"] == thresh).all()
