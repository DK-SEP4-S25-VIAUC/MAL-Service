import pandas as pd
import requests
from sklearn import Ridge
from sklearn.model_selection import train_test_split, GridSearchCV
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import mean_squared_error, r2_score
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType
import os
from upload_model import upload_model_to_blob

def train_model(data_url: str) -> dict:
    # 1. Fetch training data
    response = requests.get(data_url)
    data_json = response.json()
    data = pd.DataFrame(data_json)

    # 2. Split into train/val/test sets
    X = data.drop('y', axis=1)
    y = data['y']
    X_temp, X_test, y_temp, y_test = train_test_split(X, y, test_size=0.2, random_state=42)
    X_train, X_val, y_train, y_val = train_test_split(X_temp, y_temp, test_size=0.25, random_state=42)

    # 3. Scale the features
    scaler = StandardScaler()
    X_train_scaled = scaler.fit_transform(X_train)
    X_val_scaled = scaler.transform(X_val)
    X_test_scaled = scaler.transform(X_test)

    # 4. Train Ridge regression with hyperparameter tuning
    param_grid = {'alpha': [0.01, 0.1, 1, 10, 100]}
    grid_search = GridSearchCV(Ridge(), param_grid, scoring='neg_mean_squared_error', cv=5)
    grid_search.fit(X_train_scaled, y_train)
    best_model = grid_search.best_estimator_

    # 5. Evaluate on test set
    y_test_pred = best_model.predict(X_test_scaled)
    rmse = mean_squared_error(y_test, y_test_pred, squared=False)
    r2 = r2_score(y_test, y_test_pred)

    # 6. Export as ONNX
    initial_type = [('input', FloatTensorType([None, X_train_scaled.shape[1]]))]
    onnx_model = convert_sklearn(best_model, initial_types=initial_type)
    model_path = os.path.join(os.path.dirname(__file__), "model.onnx")
    with open(model_path, "wb") as f:
        f.write(onnx_model.SerializeToString())

    # 7. Upload model to Azure
    upload_model_to_blob(model_path, "linear-regression-model-latest.onnx")

    return {
        "message": "Model trained, saved and uploaded successfully.",
        "rmse": round(rmse, 2),
        "r2": round(r2, 2),
        "best_alpha": grid_search.best_params_["alpha"]
    }
