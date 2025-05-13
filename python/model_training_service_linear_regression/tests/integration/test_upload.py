# tests/integration/test_job_flow.py
import src.scheduler as scheduler_mod


def test_job_flow(monkeypatch, caplog):
    caplog.set_level("INFO")
    called = {}

    # 1) Stub fetch_sensor_data and fetch_threshold in scheduler_mod
    dummy_data = {"response": {"list": [{"SampleDTO": {
        "soil_humidity": 10,
        "air_humidity": 50,
        "temperature": 20,
        "light": 100,
        "timestamp": "2025-01-01T00:00:00"
    }}]}}
    dummy_threshold = 5

    monkeypatch.setattr(
        scheduler_mod,
        "fetch_sensor_data",
        lambda timeout=...: dummy_data
    )
    monkeypatch.setattr(
        scheduler_mod,
        "fetch_threshold",
        lambda timeout=...: dummy_threshold
    )

    # 2) Stub train_model in scheduler_mod
    def fake_train(json_samples, json_threshold):
        # marker at vi lander her
        called["trained"] = True
        return {"rmse_cv": 1.23, "r2_insample": 0.45}

    monkeypatch.setattr(
        scheduler_mod,
        "train_model",
        fake_train
    )

    # 3) Run job()
    scheduler_mod.job()

    # 4) Assert that fake_train got called
    assert called.get("trained", False), "train_model blev ikke kaldt af job()"

    # 5) There needs to be a log for start and end
    assert any("Starting model-training" in rec.message for rec in caplog.records)
    assert any("Result:" in rec.message for rec in caplog.records)
