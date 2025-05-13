# Endpoints (hardcoded)
SENSOR_BASE_URL = "https://mal-api.whitebush-734a9017.northeurope.azurecontainerapps.io"
DATA_ENDPOINT = "https://mal-api.whitebush-734a9017.northeurope.azurecontainerapps.io/sensor/data"
THRESHOLD_ENDPOINT = "https://mal-api.whitebush-734a9017.northeurope.azurecontainerapps.io/sensor/soilhumiditythreshold"

# HTTP-server & scheduler (hardcoded)
HEALTH_PORT = 8081
TIMEZONE = "Europe/Copenhagen"
# Cron expression for scheduling jobs: minute hour day month weekday
SCHEDULE_CRON = "0 0 * * *"
