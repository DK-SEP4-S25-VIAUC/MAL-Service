import logging
from azure.identity import DefaultAzureCredential
from azure.storage.blob import BlobServiceClient

logger = logging.getLogger(__name__)

def upload_to_blob(local_path: str, blob_name: str):
    try:
        account_url = "https://modelregistrymal.blob.core.windows.net/"
        container_name = "models"

        try:
            logger.info("Attempting upload with Managed Identity (DefaultAzureCredential)...")
            credential = DefaultAzureCredential()
            _token = credential.get_token("https://storage.azure.com/.default")
            logger.info("Successfully acquired token via Managed Identity.")
        except Exception as mi_error:
            logger.warning("Failed to acquire token via Managed Identity: %s", mi_error)

        blob_service_client = BlobServiceClient(account_url=account_url, credential=credential)
        blob_client = blob_service_client.get_blob_client(container=container_name, blob=blob_name)

        with open(local_path, "rb") as data:
            blob_client.upload_blob(data, overwrite=True)

        logger.info(f"Uploaded '{blob_name}' to container '{container_name}' using Managed Identity.")
    except Exception as e:
        logger.exception("Upload failed for %s : %s", blob_name, e)
        raise
