import requests
from src_rf.config_rf import DATA_ENDPOINT, THRESHOLD_ENDPOINT


def fetch_sensor_data(timeout=120):
    r = requests.get(DATA_ENDPOINT, timeout=timeout)
    r.raise_for_status()
    return r.json()


def fetch_threshold(timeout=120):
    r = requests.get(THRESHOLD_ENDPOINT, timeout=timeout)
    r.raise_for_status()
    return r.json()
