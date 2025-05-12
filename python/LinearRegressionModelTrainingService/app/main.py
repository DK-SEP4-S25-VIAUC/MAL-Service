import json
import logging
import sys
import threading
import time
from datetime import datetime
from http.server import BaseHTTPRequestHandler, HTTPServer

import requests
from apscheduler.schedulers.background import BackgroundScheduler
from pytz import timezone

from training import train_model

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    handlers=[logging.StreamHandler(sys.stdout)],
)
logger = logging.getLogger(__name__)
logging.getLogger("apscheduler").setLevel(logging.WARNING)
logging.getLogger("azure").setLevel(logging.WARNING)

# Get endpoints
SENSOR_BASE_URL = "https://mal-api.whitebush-734a9017.northeurope.azurecontainerapps.io"
DATA_ENDPOINT = SENSOR_BASE_URL.rstrip("/") + "/sensor/data"
THRESHOLD_ENDPOINT = SENSOR_BASE_URL.rstrip("/") + "/sensor/soilhumiditythreshold"

HEALTH_PORT = 8081


# --- Health endpoint setup ---
class HealthHandler(BaseHTTPRequestHandler):
    LOG_EVERY = 600
    _last_log = 0.0

    def do_GET(self):
        self.send_response(200)
        self.end_headers()
        self.wfile.write(b"OK")

    def log_message(self, _format, *args):
        now = time.time()
        if now - HealthHandler._last_log >= self.LOG_EVERY:
            logger.info("Health probe OK  (client %s)", self.client_address[0])
            HealthHandler._last_log = now


def start_health_server(port: int | str = HEALTH_PORT):
    port = int(port)
    server = HTTPServer(('', port), HealthHandler)
    print(f"[{datetime.now()}] Health endpoint listening on port {port}")
    server.serve_forever()


def job():
    start = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    logger.info(f"[{start}] starting model training")

    try:
        url = DATA_ENDPOINT
        logger.debug("Requesting sensor data from %s", DATA_ENDPOINT)

        resp_data = requests.get(DATA_ENDPOINT, timeout=120)
        resp_threshold = requests.get(THRESHOLD_ENDPOINT, timeout=120)

        result = train_model(
            json.dumps(resp_data.json()),
            json.dumps(resp_threshold.json())
        )

        logger.info(
            f"[{datetime.now()}] Done. RMSE={result.get('rmse_cv', 'N/A')} R2={result.get('r2_insample', 'N/A')}")
    except Exception as e:
        logger.exception(f"[{datetime.now()}] Error during training: {e}")


def main():
    # Starting health server in the background
    t = threading.Thread(target=start_health_server, args=(HEALTH_PORT,), daemon=True)
    t.start()

    tz = timezone("Europe/Copenhagen")
    scheduler = BackgroundScheduler(timezone=tz)

    # Scheduling daily job
    scheduler.add_job(job, trigger="cron", hour=0, minute=0)
    scheduler.start()
    logger.info("Scheduler is running, next training at 00:00 CET")

    # Manual test-call
    logger.info("Running manual job")
    job()

    try:
        while True:
            time.sleep(1)
    except (KeyboardInterrupt, SystemExit):
        scheduler.shutdown()
        logger.info("Shutting down scheduler and exits")


if __name__ == "__main__":
    main()
