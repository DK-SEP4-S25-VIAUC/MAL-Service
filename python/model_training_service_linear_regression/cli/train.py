import json

from src.models.ridge import train_model

if __name__ == "__main__":
    with open("sample_data.json") as f:
        data = f.read()
    with open("sample_threshold.json") as f:
        thresh = f.read()
    result = train_model(data, thresh)
    print(json.dumps(result, indent=2))
