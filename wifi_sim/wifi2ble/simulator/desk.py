import threading
import random
import logging

logger = logging.getLogger(__name__)

class Desk:
    DEFAULT_SPEED_MMS = 32
    COLLISION_CHANCE = 0.03
    MAX_ERROR_COUNT = 10
    ERROR_CODE_E93 = 93

    def __init__(self, desk_id, name, manufacturer, initial_position=680, min_position=680, max_position=1320):
        self.desk_id = desk_id
        self.config = {
            "name": name,
            "manufacturer": manufacturer,
        }
        self.state = {
            "position_mm": initial_position,
            "speed_mms": 0,
            "status": "Normal",
            "isPositionLost": False,
            "isOverloadProtectionUp": False,
            "isOverloadProtectionDown": False,
            "isAntiCollision": False,
        }
        self.usage = {
            "activationsCounter": 25,
            "sitStandCounter": 1,
        }
        self.lastErrors = [
            {"time_s": 120, "errorCode": 93},
        ]
        self.lock = threading.RLock()
        self.target_position_mm = initial_position
        self.min_position = min_position
        self.max_position = max_position
        self.sit_stand_position = (max_position - min_position) / 2 + min_position
        self.clock_s = 180
        self.collision_occurred = False

        logger.info(f"Desk initialized: ID={desk_id}, Name={name}, Manufacturer={manufacturer}, "
            f"Position={initial_position}, Min={min_position}, Max={max_position}")


    def get_target_position(self):
        """Get the target position to move towards"""
        with self.lock:
            return self.target_position_mm

    def set_target_position(self, position_mm):
        """Set the target position to move towards, respecting min and max limits."""
        with self.lock:
            self.target_position_mm = max(self.min_position, min(position_mm, self.max_position))
            logger.info(f"Desk target position set: ID={self.desk_id}, Requested={position_mm}, Accepted={self.target_position_mm}")
            if position_mm != self.state["position_mm"]:
                self.usage["activationsCounter"] += 1
                logger.info(f"Desk activated: ID={self.desk_id}, ActivationCounter={self.usage["activationsCounter"]}")

    def _generate_error(self):
        """Generate an error during movement."""
        with self.lock:
            self.lastErrors.insert(0, {"time_s": self.clock_s, "errorCode": self.ERROR_CODE_E93})
            if len(self.lastErrors) > self.MAX_ERROR_COUNT:
                self.lastErrors.pop()

            self.state["isAntiCollision"] = True
            self.state["status"] = "Collision"
            self.collision_occurred = True

            logger.error(f"Desk collision detected: ID={self.desk_id}, Time={self.clock_s}, Position={self.state['position_mm']}")
    
    def update(self):
        """Update clock and position gradually toward target_position_mm within limits, increment sitStandCounter on crossing."""
        """Must be called every 1s"""	
        with self.lock:
            self.clock_s += 1

            successful_movement = False

            if self.collision_occurred:
                self.collision_occurred = False
                return

            previous_position = self.state["position_mm"]
            if self.state["position_mm"] < self.target_position_mm:
                self.state["position_mm"] += min(self.DEFAULT_SPEED_MMS, self.target_position_mm - self.state["position_mm"])
                self.state["position_mm"] = min(self.state["position_mm"], self.max_position)
                self.state["speed_mms"] = self.DEFAULT_SPEED_MMS
                successful_movement = True
                logger.info(f"Desk moving up: ID={self.desk_id}, Position={self.state['position_mm']}")
            elif self.state["position_mm"] > self.target_position_mm:
                self.state["position_mm"] -= min(self.DEFAULT_SPEED_MMS, self.state["position_mm"] - self.target_position_mm)
                self.state["position_mm"] = max(self.state["position_mm"], self.min_position)
                self.state["speed_mms"] = -self.DEFAULT_SPEED_MMS
                successful_movement = True
                logger.info(f"Desk moving down: ID={self.desk_id}, Position={self.state['position_mm']}")
            else:
                self.state["speed_mms"] = 0

            if (previous_position < self.sit_stand_position <= self.state["position_mm"]) or \
               (previous_position > self.sit_stand_position >= self.state["position_mm"]):
                self.usage["sitStandCounter"] += 1
                logger.info(f"Desk crossed sit/stand position: ID={self.desk_id}, SitStandCounter={self.usage['sitStandCounter']}")


            if successful_movement:
                if self.state["isAntiCollision"]:
                    self.state["isAntiCollision"] = False
                    self.state["status"] = "Normal"
                    logger.info(f"Desk reset from collision: ID={self.desk_id}, Time={self.clock_s}, Position={self.state['position_mm']}")
                elif random.random() < self.COLLISION_CHANCE:
                    self._generate_error()
                    if self.state["speed_mms"] > 0:
                        self.state["position_mm"] = max(self.state["position_mm"] - 10, self.min_position)
                    elif self.state["speed_mms"] < 0:
                        self.state["position_mm"] = min(self.state["position_mm"] + 10, self.max_position)

                    self.target_position_mm = self.state["position_mm"]
                    self.state["speed_mms"] = 0

    def get_data(self):
        """Get a snapshot of the desk's data."""
        with self.lock:
            return {
                "config": self.config,
                "state": self.state,
                "usage": self.usage,
                "lastErrors": self.lastErrors,
            }

    def update_category(self, category, data):
        """Update a specific category of the desk."""
        with self.lock:
            if category == "state" and "position_mm" in data:
                self.set_target_position(data["position_mm"])
                return True

            return False
