import logging
import threading
import time
from http.server import HTTPServer, BaseHTTPRequestHandler

import onnx
import onnxconverter_common
import skl2onnx
from src_rf.config_rf import HEALTH_PORT
from src_rf.scheduler import start_scheduler

logger = logging.getLogger(__name__)


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


def run_scheduler():
    logger.info("Starting scheduler in background thread...")
    start_scheduler()


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)

    logger.info(f"ONNX version: {onnx.__version__}")
    logger.info(f"skl2onnx version: {skl2onnx.__version__}")
    logger.info(f"onnxconverter_common version: {onnxconverter_common.__version__}")

    # Start scheduler i en baggrundstråd
    scheduler_thread = threading.Thread(target=run_scheduler, daemon=True)
    scheduler_thread.start()

    # Start HTTP-server (main thread)
    server = HTTPServer(("", HEALTH_PORT), HealthHandler)
    logger.info(f"Health endpoint listening on port {HEALTH_PORT}")
    server.serve_forever()
