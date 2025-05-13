# tests/unit/test_target.py
import pandas as pd

from src.features.target import add_minutes_to_dry


def test_add_minutes_to_dry_all_above_threshold():
    df = pd.DataFrame({
        "timestamp": pd.date_range("2025-01-01", periods=5, freq="10T"),
        "soil_humidity": [80, 82, 85, 90, 95],
    })

    thresh = 50.0
    out = add_minutes_to_dry(df.copy(), threshold=thresh)

    # minutes_to_dry needs to exist and needs to be NaN
    assert "minutes_to_dry" in out.columns
    assert out["minutes_to_dry"].isna().all()

    # threshold-col needs to be in output and be corrects
    assert "threshold" in out.columns
    assert (out["threshold"] == thresh).all()
