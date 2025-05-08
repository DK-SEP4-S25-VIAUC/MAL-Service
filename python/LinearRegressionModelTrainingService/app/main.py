import threading
import time
from datetime import datetime
import os
import asyncio
from http.server import BaseHTTPRequestHandler, HTTPServer

import requests
from pytz import timezone
from apscheduler.schedulers.background import BackgroundScheduler

from training import train_model

# Get endpoints
SENSOR_BASE_URL = os.environ.get("SENSOR_API_BASE_URL")
if not SENSOR_BASE_URL:
    raise RuntimeError("Du skal sætte miljø‐variablen SENSOR_API_BASE_URL")

DATA_ENDPOINT      = SENSOR_BASE_URL.rstrip("/") + "/sensor/data"
THRESHOLD_ENDPOINT = SENSOR_BASE_URL.rstrip("/") + "/sensor/threshold"

HEALTH_PORT = int(os.getenv("HEALTH_PORT", "8081"))

# --- Health endpoint setup ---
class HealthHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        self.send_response(200)
        self.end_headers()
        self.wfile.write(b"OK")

def start_health_server(port: int = 80):
    server = HTTPServer(('', port), HealthHandler)
    print(f"[{datetime.now()}] Health endpoint listening on port {port}")
    server.serve_forever()

def job():
    # Defining parameters for period of time
    from_ts = "2025-04-30T00:00:00Z"
    now_utc = datetime.now(timezone("UTC")).isoformat(timespec="seconds")
    to_ts = now_utc.replace("+00:00", "Z")
    params = {"from" : from_ts, "to" : to_ts}

    start = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    print(f"[{start}] starting model training")

    try:
        # Get samples
        resp_data = requests.get(DATA_ENDPOINT, params=params, timeout=60)
        resp_data.raise_for_status()
        json_data = resp_data.json()

        # Get threshold
        resp_threshold = requests.get(THRESHOLD_ENDPOINT, timeout=60)
        resp_threshold.raise_for_status()
        json_thr = resp_threshold.json()

        result = train_model(json_data, json_thr)
        print(f"[{datetime.now()}] Done. RMSE={result['rmse']} R2={result['r2']}")
    except Exception as e:
        print(f"[{datetime.now()}] Error during training: {e}")


def main():
    # Starting health server in the background
    t = threading.Thread(target=start_health_server, args=(HEALTH_PORT,), daemon=True)
    t.start()

    tz = timezone("Europe/Copenhagen")
    scheduler = BackgroundScheduler(timezone=tz)

    # Scheduling daily job
    scheduler.add_job(job, trigger="cron", hour=0, minute=0)
    scheduler.start()
    print("Scheduler is running, next training begins at kl. 00.00 Copenhagen-time")


    # Manual test-call
    print("Testing job() manually")
    job()

    print(f"[{datetime.now()}] Scheduler is running. Health endpoint at port {HEALTH_PORT}.")

    try:
        while True:
            time.sleep(1)
    except (KeyboardInterrupt, SystemExit):
        scheduler.shutdown()
        print("Shutdown scheduler og exit.")

if __name__ == "__main__":
    main()




