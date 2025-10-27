# Desk Management REST API

This REST API provides access to a collection of desks, each with configurable properties and a state that includes position, speed, and other operational data. The API allows retrieving and updating desk data and is secured by API keys. **For development and testing purposes only.**

## Usage

The API server can be started in either HTTP or HTTPS mode.

### Starting the Server

#### 1. **HTTP Mode** (default):  
To start the server in HTTP mode on the default port (8000), run:

```bash
python simulator/main.py --port 8000
```

- Options:
   - __--port__: (Optional) Specify the port to use for the server. The default is 8000

#### 2. **HTTPS Mode**:
To enable HTTPS, you need to provide paths to the SSL certificate and key files. Here’s an example of starting the server on port 8443 in HTTPS mode:

```bash
python simulator/main.py --port 8443 --https --certfile config/cert.pem --keyfile config/key.pem
```

- Options:
   - __--https__: Enable HTTPS. Requires --certfile and --keyfile
   - __--certfile__: Path to the SSL certificate file (required for HTTPS)
   - __--keyfile__: Path to the SSL private key file (required for HTTPS)
   - __--port__: (Optional) Specify the port to use for the server. The default is 8000

#### 3. **Other Options**:
**Desks:** to generate the minimum required amount of desks:

```bash
python simulator/main.py --desks 100
```
- Option:
   - __--desks__: Minimum number of desks to simulate (default: 2)

**Speed:** To control speed of the clock simulation of the attached desks:

```bash
python simulator/main.py --speed 60
```
- Option:
   - __--speed__: Simulation speed (default: 60)

**Log Level**: To control logging level of the simulator modules:

```bash
python simulator/main.py --log-level INFO
```
- Option:
   - __--log-level__: Logging level (DEBUG, INFO, WARNING, ERROR, CRITICAL)


## Data Persistence

The server automatically loads the desk data on startup and saves it upon shutdown. Desk data, including configurations, state (position, speed, etc.), usage counters, and any errors, are saved to a JSON file named `desks_state.json` in `data` folder.

- **Loading Data**:
  When the server starts, it checks if `desks_state.json` exists. If found, it loads the saved data to restore each desk's previous state. If no file is found or if the file is invalid, the server starts with default desk settings.

- **Saving Data**:
  When the server shuts down (e.g., via keyboard interrupt), it automatically saves the current state of all desks to `desks_state.json`. This ensures that any changes to desk states are preserved across server restarts.

This feature allows for seamless persistence of desk data, enabling the server to resume from the last known state without data loss.

## Base URL

All endpoints are based on the following format: 
```
/api/v2/<api_key>/desks
```

Replace `<api_key>` with a valid API key from `api_keys.json`.

## Endpoints

### 1. Get All Desks

- **Endpoint**: `GET /api/v2/<api_key>/desks`
- **Description**: Retrieve a list of all desk IDs available in the system.
- **Response**:
  - **Status**: `200 OK`
  - **Body**: Array of desk IDs.
    ```json
    ["cd:fb:1a:53:fb:e6", "ee:62:5b:b8:73:1d"]
    ```
- **Errors**:
  - `401 Unauthorized`: Invalid API key.
  - `400 Bad Request`: Incorrect endpoint format or version mismatch.

### 2. Get Specific Desk Data

- **Endpoint**: `GET /api/v2/<api_key>/desks/<desk_id>`
- **Description**: Retrieve detailed data for a specific desk by its ID.
- **Path Parameters**:
  - `desk_id`: The ID of the desk to retrieve.
- **Response**:
  - **Status**: `200 OK`
  - **Body**: JSON object with desk configuration, state, usage, and errors.
    ```json
    {
      "config": {
        "name": "DESK 4486",
        "manufacturer": "Desk-O-Matic Co."
      },
      "state": {
        "position_mm": 680,
        "speed_mms": 0,
        "status": "Normal",
        "isPositionLost": false,
        "isOverloadProtectionUp": false,
        "isOverloadProtectionDown": false,
        "isAntiCollision": false
      },
      "usage": {
        "activationsCounter": 25,
        "sitStandCounter": 1
      },
      "lastErrors": [
        {
          "time_s": 120,
          "errorCode": 93
        }
      ]
    }
    ```
- **Errors**:
  - `404 Not Found`: Desk not found.
  - `401 Unauthorized`: Invalid API key.
  - `400 Bad Request`: Incorrect endpoint format or version mismatch.

### 3. Get Specific Category Data of a Desk

- **Endpoint**: `GET /api/v2/<api_key>/desks/<desk_id>/<category>`
- **Description**: Retrieve a specific category (`config`, `state`, `usage`, or `lastErrors`) of a desk's data.
- **Path Parameters**:
  - `desk_id`: The ID of the desk.
  - `category`: The category of data to retrieve (e.g., `config`, `state`, `usage`, `lastErrors`).
- **Response**:
  - **Status**: `200 OK`
  - **Body**: JSON object with the requested category data.
- **Errors**:
  - `404 Not Found`: Desk or category not found.
  - `401 Unauthorized`: Invalid API key.
  - `400 Bad Request`: Incorrect endpoint format or version mismatch.

### 4. Update Specific Category Data of a Desk

- **Endpoint**: `PUT /api/v2/<api_key>/desks/<desk_id>/<category>`
- **Description**: Update a specific category of a desk, such as setting a new `position_mm` in the `state` category.
- **Path Parameters**:
  - `desk_id`: The ID of the desk.
  - `category`: The category of data to update (only `state` category is currently updatable).
- **Request Body**:
  - **Content-Type**: `application/json`
  - **Body**: JSON object with the data to update. Example for updating position:
    ```json
    {
      "position_mm": 1000
    }
    ```
- **Response**:
  - **Status**: `200 OK`
  - **Body**: JSON object indicating the allowed position after the update.
    ```json
    {
        "position_mm": 1000
    }
    ```
- **Errors**:
  - `404 Not Found`: Desk or category not found.
  - `401 Unauthorized`: Invalid API key.
  - `400 Bad Request`: Incorrect endpoint format or invalid data type in the request body.

## Error Responses

For all endpoints, the API may return the following standard error responses:

- **400 Bad Request**: Returned if the request path or parameters are incorrect.
  - **Example**:
    ```json
    {
      "error": "Invalid endpoint"
    }
    ```

- **401 Unauthorized**: Returned if an invalid API key is provided.
  - **Example**:
    ```json
    {
      "error": "Unauthorized"
    }
    ```

- **404 Not Found**: Returned if a specified desk or category is not found.
  - **Example**:
    ```json
    {
      "error": "Desk not found"
    }
    ```

- **405 Method Not Allowed**: Returned if an unsupported HTTP method is used (e.g., `POST`, `DELETE`, `PATCH`).
  - **Example**:
    ```json
    {
      "error": "Method Not Allowed"
    }
    ```

## Authentication

All endpoints require a valid API key in the URL path to authorize access. API keys are loaded from the `api_keys.json` file.

---

This API allows clients to retrieve detailed information about desks, update specific categories within a desk, and view real-time adjustments in position. It includes authentication through API keys and provides clear error handling for invalid or unauthorized requests.
