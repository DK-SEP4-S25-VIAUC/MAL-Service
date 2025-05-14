import json

from src_rf.models.randomforest import train_model_rf

if __name__ == "__main__":
    with open("sample_data.json") as f:
        data = f.read()
    with open("sample_threshold.json") as f:
        thresh = f.read()
    result = train_model_rf(data, thresh)
    print(json.dumps(result, indent=2))
