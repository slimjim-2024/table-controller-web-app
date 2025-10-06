import json
import logging
from http.server import BaseHTTPRequestHandler, HTTPServer
from desk_manager import DeskManager

logger = logging.getLogger(__name__)

class SimpleRESTServer(BaseHTTPRequestHandler):
    VERSION = "v2"
    API_KEYS_FILE = "config/api_keys.json"
    API_KEYS = []

    def __init__(self, desk_manager: DeskManager, *args, **kwargs):
        self.desk_manager = desk_manager
        self.path_parts = []
        super().__init__(*args, **kwargs)

    @staticmethod
    def load_api_keys(api_keys_file):
        """Static method to load API keys from a file."""
        try:
            with open(api_keys_file, "r") as f:
                keys = json.load(f)
                logger.info(f"API keys successfully loaded from {api_keys_file}.")
                return keys
        except FileNotFoundError:
            logger.error(f"API keys file not found at {api_keys_file}.")
            return []
        except json.JSONDecodeError as e:
            logger.error(f"Error decoding API keys file {api_keys_file}: {e}")
            return []

    @classmethod
    def initialize_api_keys(cls):
        """Class method to initialize the API_KEYS static attribute."""
        cls.API_KEYS = cls.load_api_keys(cls.API_KEYS_FILE)
    
    def _send_response(self, status_code, data):
        response_body = json.dumps(data).encode("utf-8")
        self.send_response(status_code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(response_body)))
        self.end_headers()
        self.wfile.write(response_body)
        logger.info(f"Response sent: {status_code} - {data}")
    
    def _is_valid_path(self):
        # Path format: /api/<version>/<api_key>/desks[/<desk_id>]
        self.path_parts = self.path.strip("/").split("/")
    
        if len(self.path_parts) < 4 or self.path_parts[0] != "api":
            logger.warning(f"Invalid endpoint: {self.path}")
            self._send_response(400, {"error": "Invalid endpoint"})
            return False
    
        version = self.path_parts[1]
        api_key = self.path_parts[2]
    
        if api_key not in self.API_KEYS:
            logger.warning(f"Unauthorized API key: {api_key}")
            self._send_response(401, {"error": "Unauthorized"})
            return False
    
        if version != self.VERSION:
            logger.warning(f"Invalid API version: {version}")
            self._send_response(400, {"error": "Invalid API version"})
            return False
        
        logger.info(f"Valid API request: {self.path}")
        return True
    
    def do_GET(self):
        if not self._is_valid_path():
            return
        
        logger.info(f"Handling GET request for {self.path}")
        if self.path_parts[3] == "desks":
            if len(self.path_parts) == 4:
                desk_ids = self.desk_manager.get_desk_ids()
                self._send_response(200, desk_ids)
            elif len(self.path_parts) == 5:
                desk_id = self.path_parts[4]
                desk = self.desk_manager.get_desk_data(desk_id)
                if desk:
                    self._send_response(200, desk)
                else:
                    logger.warning(f"Desk not found: {desk_id}")
                    self._send_response(404, {"error": "Desk not found"})
            elif len(self.path_parts) == 6:
                desk_id = self.path_parts[4]
                category = self.path_parts[5]
                data = self.desk_manager.get_desk_category(desk_id, category)
                if data:
                    self._send_response(200, data)
                else:
                    logger.warning(f"Category not found: {category} for desk {desk_id}")
                    self._send_response(404, {"error": "Category not found"})
            else:
                logger.warning(f"Invalid path structure for GET: {self.path}")
                self._send_response(400, {"error": "Invalid path"})
        else:
            logger.warning(f"Invalid endpoint for GET: {self.path}")
            self._send_response(400, {"error": "Invalid endpoint"})
    
    def do_PUT(self):
        if not self._is_valid_path():
            return

        logger.info(f"Handling PUT request for {self.path}")
        if self.path_parts[3] == "desks":
            if len(self.path_parts) == 6:
                # Update a specific category of a specific desk
                try:
                    content_length = int(self.headers["Content-Length"])
                    post_data = self.rfile.read(content_length)
                    update_data = json.loads(post_data)
                    desk_id = self.path_parts[4]
                    category = self.path_parts[5]
                    success = self.desk_manager.update_desk_category(desk_id, category, update_data)
                    if success:
                        current_target_position = self.desk_manager.get_desk(desk_id).get_target_position()
                        response_data = {
                            "position_mm": current_target_position
                        }
                        self._send_response(200, response_data)
                    else:
                        logger.warning(f"Update failed: Category {category} or desk {desk_id} not found.")
                        self._send_response(404, {"error": "Category not found or desk not found"})
                except ValueError:
                    logger.error(f"Invalid data format for PUT: {self.path}")
                    self._send_response(400, {"error": "Invalid data"})
                except TypeError:
                    logger.error(f"Invalid data type for PUT: {self.path}")
                    self._send_response(400, {"error": "Invalid type"})
            else:
                logger.warning(f"Invalid path structure for PUT: {self.path}")
                self._send_response(400, {"error": "Invalid path"})
        else:
            logger.warning(f"Invalid endpoint for PUT: {self.path}")
            self._send_response(400, {"error": "Invalid endpoint"})

    def do_POST(self):
        """Handle unsupported POST method."""
        logger.warning(f"POST method not allowed: {self.path}")
        self._send_response(405, {"error": "Method Not Allowed"})
    
    def do_DELETE(self):
        """Handle unsupported DELETE method."""
        logger.warning(f"DELETE method not allowed: {self.path}")
        self._send_response(405, {"error": "Method Not Allowed"})
    
    def do_PATCH(self):
        """Handle unsupported PATCH method."""
        logger.warning(f"PATCH method not allowed: {self.path}")
        self._send_response(405, {"error": "Method Not Allowed"})
