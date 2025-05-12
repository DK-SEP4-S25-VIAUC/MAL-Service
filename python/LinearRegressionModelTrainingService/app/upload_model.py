# upload_model.py
import os
import logging
from azure.storage.blob import BlobServiceClient

logger = logging.getLogger(__name__)

def upload_to_blob(local_path: str, blob_name: str):
    try:
        connect_str = "DefaultEndpointsProtocol=https;AccountName=modelregistrymal;AccountKey=DIN_HEMMELIGE_NØGLE;EndpointSuffix=core.windows.net"
        container_name = "models"

        blob_service_client = BlobServiceClient.from_connection_string(connect_str)
        blob_client = blob_service_client.get_blob_client(container=container_name, blob=blob_name)

        with open(local_path, "rb") as data:
            blob_client.upload_blob(data, overwrite=True)
            logger.info("Uploaded %s (%s bytes)", blob_name, os.path.getsize(local_path))
    except Exception as e:
        logger.exception("Upload failed for %s", blob_name)
        raise

