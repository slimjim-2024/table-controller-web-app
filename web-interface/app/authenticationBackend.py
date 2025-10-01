# app/backends.py
from django.contrib.auth.backends import BaseBackend
from .models import Users

class MyCustomBackend(BaseBackend):
    def authenticate(self, request, username=None, password=None, **kwargs):
        try:
            user = Users.objects.get(username=username)
            # Use your own password check logic here:
            if user.check_password(password):
                return user
        except Users.DoesNotExist:
            return None

    def get_user(self, user_id):
        try:
            return Users.objects.get(userID=user_id)
        except Users.DoesNotExist:
            return None