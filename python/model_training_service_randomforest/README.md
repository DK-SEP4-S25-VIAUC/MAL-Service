# RandomForestModelTrainingService

A lightweight microservice that trains a Random Forest model on time-series sensor data, evaluates its performance, exports it to ONNX, and uploads it to Azure Blob Storage. The service supports CI/CD using Docker and GitHub Actions.

---

## Features

- Fetches training data and thresholds from external endpoints
- Cleans sensor data and handles time gaps
- Creates a time-to-threshold target (minutes_to_dry)
- Trains a RandomForestRegressor model
- Evaluates performance using RMSE and R²
- Exports the trained model to ONNX format
- Uploads model and metadata to Azure Blob Storage
- Supports scheduled retraining via apscheduler
- Containerized and ready for deployment

---

## Technologies Used

- Python 3.11
- scikit-learn
- pandas / numpy
- skl2onnx
- onnx
- Azure Storage SDK
- Docker
- GitHub Actions
- apscheduler

---

## Health & Scheduler Service

This service runs a background scheduler (via apscheduler) and exposes a /health endpoint on port 8081. The scheduler runs the training job daily at the configured time.

To run locally:
    python -m cli.serve

To manually trigger a training job:
    python -m cli.run_job

---

## Docker

Build the Docker image:
    docker build -t randomforest-training-service .

Run the container:
    docker run -p 8081:8081 randomforest-training-service

---

## CI/CD with GitHub Actions

On each push to the main branch:
- Tests are run (if available)
- Docker image is built and pushed
- Trained model and metadata are uploaded to Azure Blob Storage

Refer to .github/workflows/main.yml for implementation details.

---

## Contact

Developed as part of the SEP4 project at VIA University College.
