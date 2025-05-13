import json
import pandas as pd
from models.randomforest import train_model_rf

df = pd.read_csv("src_rf/testdata.csv", parse_dates=["timestamp"])
threshold = 20


sample_list = df.copy()
sample_list["timestamp"] = sample_list["timestamp"].dt.strftime("%Y-%m-%dT%H:%M:%S")
sample_list = sample_list.to_dict(orient="records")
wrapped = {
    "response": {
        "list": [{"SampleDTO": row} for row in sample_list]
    }
}


result = train_model_rf(json.dumps(wrapped), json.dumps(threshold))

print(json.dumps(result, indent=2))
