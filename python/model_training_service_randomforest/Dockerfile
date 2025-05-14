# model_training_service_randomforest/Dockerfile
FROM python:3.11-slim
ENV PYTHONUNBUFFERED=1

WORKDIR /app
COPY pyproject.toml .
COPY src_rf/ src_rf/
COPY cli/ cli/

RUN pip install --upgrade pip && \
    pip install --force-reinstall --no-cache-dir onnx==1.17.0 skl2onnx==1.18.0 && \
    pip install -e .

EXPOSE 8081
CMD ["python", "-m", "cli.serve"]
