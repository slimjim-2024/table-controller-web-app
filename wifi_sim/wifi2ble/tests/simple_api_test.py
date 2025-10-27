import http.client
import json
import argparse
import ssl

# Constants for the API
API_VERSION = "v2"
API_KEY = "E9Y2LxT4g1hQZ7aD8nR3mWx5P0qK6pV7"  # Replace with a valid API key
DESK_ID = "cd:fb:1a:53:fb:e6"  # Replace with an actual desk ID from your server
CATEGORY = "state"  # Category to update, e.g., "state"

def get_connection(use_https, host, port):
    if use_https:
        context = ssl._create_unverified_context()  # For testing only; consider using verified SSL in production
        return http.client.HTTPSConnection(host, port, context=context)
    else:
        return http.client.HTTPConnection(host, port)

def make_request(connection, method, endpoint, data=None):
    headers = {"Content-Type": "application/json"}
    if data is not None:
        json_data = json.dumps(data)
        connection.request(method, endpoint, body=json_data, headers=headers)
    else:
        connection.request(method, endpoint, headers=headers)
    
    response = connection.getresponse()
    print(f"Status: {response.status} {response.reason}")
    response_data = response.read().decode()
    
    try:
        json_response = json.loads(response_data)
        print("Response JSON:")
        print(json.dumps(json_response, indent=2))
    except json.JSONDecodeError:
        print("Response Text:")
        print(response_data)

def get_all_desks(connection, base_url):
    print("Fetching all desks...")
    make_request(connection, "GET", base_url)

def get_desk_data(connection, base_url, desk_id):
    print(f"Fetching data for desk ID: {desk_id}...")
    endpoint = f"{base_url}/{desk_id}"
    make_request(connection, "GET", endpoint)

def get_desk_category(connection, base_url, desk_id, category):
    print(f"Fetching '{category}' category data for desk ID: {desk_id}...")
    endpoint = f"{base_url}/{desk_id}/{category}"
    make_request(connection, "GET", endpoint)

def update_desk_category(connection, base_url, desk_id, category, data):
    print(f"Updating '{category}' category data for desk ID: {desk_id}...")
    endpoint = f"{base_url}/{desk_id}/{category}"
    make_request(connection, "PUT", endpoint, data)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Test Desk Management REST API using HTTP or HTTPS.")
    parser.add_argument("--https", action="store_true", help="Use HTTPS for requests")
    parser.add_argument("--host", type=str, default="localhost", help="Server host (default: localhost)")
    parser.add_argument("--port", type=int, default=8000, help="Server port (default: 8000)")

    args = parser.parse_args()
    protocol = "https" if args.https else "http"
    base_url = f"/api/{API_VERSION}/{API_KEY}/desks"

    # Establish connection
    connection = get_connection(args.https, args.host, args.port)

    # Run test functions
    try:
        get_all_desks(connection, base_url)
        get_desk_data(connection, base_url, DESK_ID)
        update_desk_category(connection, base_url, DESK_ID, CATEGORY, {"position_mm": 1000})
        get_desk_category(connection, base_url, DESK_ID, CATEGORY)
    finally:
        connection.close()
