# LinearRegressionModelTrainingService/Dockerfile
FROM python:3.11-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --upgrade pip
RUN pip install --no-cache-dir -r requirements.txt

COPY app/ .

EXPOSE 80
CMD ["python", "main.py"]