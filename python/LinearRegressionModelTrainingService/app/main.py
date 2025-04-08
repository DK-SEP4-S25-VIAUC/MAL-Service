from fastapi import FastAPI, Query
from training import train_model

app = FastAPI()

@app.post("/train")
def train(data_url: str = Query(..., description="URL to fetch training data from")):
    result = train_model(data_url)
    return result
