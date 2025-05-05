import json
from dotenv import load_dotenv
load_dotenv()
from pathlib import Path
import pandas as pd
from fastapi import FastAPI, Query
from pydantic import BaseModel
from app.training import train_model

app = FastAPI()

class TrainInput(BaseModel):
    json_data: str

@app.post("/train")
def train(input: TrainInput):
    result = train_model(input.json_data)
    return result


print("Hello from main ")


def testingTrain():
    """
    Read local CSV test data, map column names to what train_model expects,
    build the JSON payload, and call train_model(...) to verify everything works.
    """
    # 1) Read your CSV
    base = Path(__file__).parent
    test_csv = base / "testdata.csv"
    df = pd.read_csv(test_csv)

    # 2) Rename CSV columns so they match train_model's feature names
    df = df.rename(columns={
        "soil_humidity": "soil_humidity",
        "air_humidity": "air_humidity",
        "air_temperature": "temperature",
        "light_value": "light",
    })

    # 3) Build the list of SampleDTO entries
    sample_list = []
    for i, row in df.iterrows():
        sample = {
            "soil_humidity":   row["soil_humidity"],
            "air_humidity":    row["air_humidity"],
            "temperature":     row["temperature"],
            "light":           row["light"],
            "timestamp":       row["timestamp"],
        }
        # You still need an "id", even if it's unused by train_model
        sample_list.append({"SampleDTO": {"id": i, **sample}})

    # 4) Wrap in the same top-level shape your API expects
    payload = {
        "response": {
            "list": sample_list
        }
    }
    json_string = json.dumps(payload)

    # 5) Call the training function
    result = train_model(json_string)
    print("Training result:", result)


