# model_training_service_linear_regression/Dockerfile
FROM python:3.11-sl
ENV PYTHONUNBUFFERED=1

WORKDIR /app
COPY pyproject.toml .
COPY src/ src/
COPY cli/ cli/

RUN pip install --upgrade pip && pip install -e .

EXPOSE 8081
CMD ["python", "-m", "cli.schedule"]
