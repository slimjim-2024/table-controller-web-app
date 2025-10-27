from enum import Enum
import logging

logger = logging.getLogger(__name__)

class UserType(Enum):
    """Enumeration for user types."""
    SEATED = "seated"
    STANDING = "standing"
    ACTIVE = "active"

class UserBehavior:
    """Base class for user behaviors."""
    def __init__(self, desk):
        self.desk = desk

    def simulate(self, time_delta_s):
        """Simulate user behavior. Override in subclasses."""
        pass

    def __repr__(self):
        return f"{self.__class__.__name__}(desk_id={self.desk.desk_id})"

class SeatedUser(UserBehavior):
    """User who always keeps the desk in a seated position."""
    def __init__(self, desk, preffered_position=0):
        super().__init__(desk)
        if preffered_position < desk.min_position or preffered_position > desk.max_position:
            preffered_position = desk.min_position
            
        self.preffered_position = preffered_position
    
    def simulate(self, time_delta_s):
        if self.desk.state["position_mm"] > self.preffered_position:
            logger.info(f"SeatedUser adjusting desk {self.desk.desk_id} to seated position {self.preferred_position}.")
            self.desk.set_target_position(self.preffered_position)

class StandingUser(UserBehavior):
    """User who always keeps the desk in a standing position."""
    def __init__(self, desk, preffered_position=0):
        super().__init__(desk)
        if preffered_position < desk.min_position or preffered_position > desk.max_position:
            preffered_position = desk.max_position

        self.preffered_position = preffered_position

    def simulate(self, time_delta_s):
        if self.desk.state["position_mm"] < self.preffered_position:
            logger.info(f"StandingUser adjusting desk {self.desk.desk_id} to standing position {self.preferred_position}.")
            self.desk.set_target_position(self.preffered_position)

class ActiveUser(UserBehavior):
    """User who moves between seated and standing positions a few times a day."""
    def __init__(self, desk, position_cycle_time_s=3600, seated_position=0, standing_position=0):
        super().__init__(desk)

        self.position_cycle_time_s = position_cycle_time_s

        if seated_position < desk.min_position or seated_position > desk.max_position:
            seated_position = desk.min_position        

        self.seated_position = seated_position

        if standing_position < desk.min_position or standing_position > desk.max_position:
            standing_position = desk.max_position

        self.standing_position = standing_position
        self.next_position = self.seated_position
        self.cycle_timer = 0

    def simulate(self, time_delta_s):
        self.cycle_timer = (self.cycle_timer + time_delta_s) % self.position_cycle_time_s

        if self.cycle_timer < time_delta_s:
            self.next_position = (
                self.standing_position if self.desk.state["position_mm"] <= self.seated_position else self.seated_position
            )
            logger.info(f"ActiveUser adjusting desk {self.desk.desk_id} to {'standing' if self.next_position == self.standing_position else 'seated'} position {self.next_position}.")
            self.desk.set_target_position(self.next_position)
