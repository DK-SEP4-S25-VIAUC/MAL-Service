# serve.py
import logging
import time
from http.server import HTTPServer, BaseHTTPRequestHandler

import onnx
import onnxconverter_common
import skl2onnx
from src.config import HEALTH_PORT, TIMEZONE

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


if __name__ == "__main__":
    server = HTTPServer(("", HEALTH_PORT), HealthHandler)
    print(f"Health endpoint listening on port {HEALTH_PORT}")

    logging.basicConfig(level=logging.INFO)

    logging.info(f"ONNX version: {onnx.__version__}")
    logging.info(f"skl2onnx version: {skl2onnx.__version__}")
    logging.info(f"onnxconverter_common version: {onnxconverter_common.__version__}")

    server.serve_forever()
