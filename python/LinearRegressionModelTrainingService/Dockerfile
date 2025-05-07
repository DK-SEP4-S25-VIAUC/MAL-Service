# LinearRegressionModelTrainingService/Dockerfile
FROM python:3.11-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --upgrade pip
RUN pip install --no-cache-dir -r requirements.txt

COPY training.py run_scheduler.py upload_model.py ./

CMD ["python", "main.py"]