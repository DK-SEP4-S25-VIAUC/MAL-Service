from src_rf.data.io import fetch_sensor_data, fetch_threshold
from src_rf.scheduler import job

if __name__ == "__main__":
    data = fetch_sensor_data()
    threshold = fetch_threshold()
    result = job()
    print("Jobs done. View logs for details")