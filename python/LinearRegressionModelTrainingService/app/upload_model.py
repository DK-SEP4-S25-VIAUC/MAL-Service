# upload_model.py
import os
from azure.storage.blob import BlobServiceClient

def upload_to_blob(local_path: str, blob_name: str):
    try:
        connect_str = "DefaultEndpointsProtocol=https;AccountName=modelregistrymal;AccountKey=DIN_HEMMELIGE_NØGLE;EndpointSuffix=core.windows.net"
        container_name = "models"

        blob_service_client = BlobServiceClient.from_connection_string(connect_str)
        blob_client = blob_service_client.get_blob_client(container=container_name, blob=blob_name)

        with open(local_path, "rb") as data:
            blob_client.upload_blob(data, overwrite=True)

        print(f"Uploaded '{blob_name}' to Azure Blob Storage in container '{container_name}'")
    except Exception as e:
        print(f"Upload failed: {e}")

