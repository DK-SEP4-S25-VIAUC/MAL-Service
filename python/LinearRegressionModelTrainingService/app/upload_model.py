# upload_model.py
import logging
from azure.identity import DefaultAzureCredential
from azure.storage.blob import BlobServiceClient

logger = logging.getLogger(__name__)

def upload_to_blob(local_path: str, blob_name: str):
    try:
        account_url = "https://modelregistrymal.blob.core.windows.net/"
        container_name = "models"

        credential = DefaultAzureCredential()
        blob_service_client = BlobServiceClient(account_url=account_url, credential=credential)
        blob_client = blob_service_client.get_blob_client(container=container_name, blob=blob_name)

        with open(local_path, "rb") as data:
            blob_client.upload_blob(data, overwrite=True)

        logger.info(f"Uploaded '{blob_name}' to container '{container_name}' using Managed Identity.")

    except Exception as e:
        logger.exception("Upload failed for %s", blob_name)
        raise

