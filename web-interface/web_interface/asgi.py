#This code initializes your Django application with ASGI support.

import os
from django.core.asgi import get_asgi_application

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'web_interface.settings')
application = get_asgi_application()

