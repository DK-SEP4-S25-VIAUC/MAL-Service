# upload_model.py
import os
from azure.storage.blob import BlobServiceClient

def upload_to_blob(local_path: str, blob_name: str):
    try:
        connect_str = os.environ["AZURE_STORAGE_CONNECTION_STRING"]
        container_name = os.environ.get("AZURE_CONTAINER_NAME", "model-registry")

        blob_service_client = BlobServiceClient.from_connection_string(connect_str)
        blob_client = blob_service_client.get_blob_client(container=container_name, blob=blob_name)

        with open(local_path, "rb") as data:
            blob_client.upload_blob(data, overwrite=True)

        print(f"Uploaded '{blob_name}' to Azure Blob Storage in container '{container_name}'")
    except Exception as e:
        print(f"Upload failed: {e}")

