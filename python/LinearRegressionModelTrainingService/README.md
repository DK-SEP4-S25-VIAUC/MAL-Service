# 📈 LinearRegressionModelTrainingService

A lightweight microservice built with **FastAPI** that trains a **Ridge Regression** model based on external data, evaluates its performance, exports it to **ONNX**, and uploads it to **Azure Blob Storage**. The service supports **CI/CD** using **Docker** and **GitHub Actions**.

---

## 🔧 Features

- Fetches training data from a given URL (expects JSON format).
- Splits data into training, validation, and test sets.
- Scales features using `StandardScaler`.
- Trains a Ridge Regression model with hyperparameter tuning using `GridSearchCV`.
- Evaluates performance using RMSE and R².
- Exports the trained model in ONNX format.
- Automatically uploads the exported model to **Azure Blob Storage** after training.
- Supports automated build and deployment with **Docker** and **GitHub Actions**.

---

## 📆 Technologies Used

- Python 3.11
- FastAPI
- Scikit-learn
- Pandas / NumPy
- skl2onnx
- Azure Storage SDK
- Docker
- GitHub Actions

---

## 🚀 API Endpoint

### `POST /train`

**Description:** Trains a regression model using data from the provided URL, exports it to ONNX format, and uploads it to Azure Blob Storage.

**Query Parameter:**

| Name       | Type   | Description                       |
|------------|--------|-----------------------------------|
| `data_url` | string | URL to the JSON data for training |

**Example:**

```http
POST /train?data_url=https://example.com/data.json
```

**Response:**

```json
{
  "message": "Model trained, saved, and uploaded successfully.",
  "rmse": 1.23,
  "r2": 0.87,
  "best_alpha": 10
}
```

---

## 🐳 Docker

### Build the Docker image

```bash
docker build -t linear-regression-service .
```

### Run the container

```bash
docker run -p 8001:8001 linear-regression-service
```

API will be available at `http://localhost:8001/train`.

---

## 🔁 CI/CD with GitHub Actions

On each `push` to the `main` branch:

- ✅ Tests are run (if available)
- 🐳 Docker image is built and pushed to Docker Hub
- ☁️ Trained model (if it exists) is uploaded to Azure Blob Storage

### Example workflow

See `.github/workflows/main.yml` for the full CI/CD pipeline.

---

## Contact

Developed as part of the SEP4 project @ VIA University College.

---

*This README was generated with help from [ChatGPT](https://chat.openai.com), April 2025.*