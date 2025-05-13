# serve.py

import logging
import time
from http.server import HTTPServer, BaseHTTPRequestHandler

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
    server.serve_forever()
