"""
Definition of urls for web_interface.
"""

from datetime import datetime
from django.urls import path
from django.contrib import admin
from django.contrib.auth.views import LoginView, LogoutView
from app import forms, views

urlpatterns = [
    path('', views.home, name='home'),
    path('login', views.login, name='login'),
    path('logout', LogoutView.as_view(next_page='/'), name='logout'),
    path('test', views.test, name='test'),
]
