import json
import logging
import time
from datetime import datetime

from apscheduler.schedulers.background import BackgroundScheduler
from pytz import timezone

from config_rf import TIMEZONE, SCHEDULE_CRON
from data.io import fetch_sensor_data, fetch_threshold
from models.randomforest import train_model_rf

logger = logging.getLogger(__name__)


def job():
    ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    logger.info(f"[{ts}] Starting RandomForest model-training via scheduler...")

    try:
        data = fetch_sensor_data()
        threshold = fetch_threshold()
        result = train_model_rf(
            json.dumps(data),
            json.dumps(threshold),
        )
        logger.info(f"Result: RMSE={result['rmse_cv']} R2={result['r2_insample']}")
    except Exception as e:
        logger.exception("Scheduler-job error: %s", e)


def start_scheduler():
    tz = timezone(TIMEZONE)
    sched = BackgroundScheduler(timezone=tz)

    minute, hour, day, month, weekday = SCHEDULE_CRON.split()
    sched.add_job(job, trigger="cron", minute=minute, hour=hour)
    sched.start()
    logger.info("Scheduler is running: cron=%s %s", TIMEZONE, SCHEDULE_CRON)

    try:
        while True:
            time.sleep(1)
    except (KeyboardInterrupt, SystemExit):
        sched.shutdown()
        logger.info("Scheduler shutdown and exiting.")
