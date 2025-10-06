"""
Definition of urls for web_interface.
"""

from datetime import datetime
from django.urls import path, re_path
from django.contrib import admin
from django.contrib.auth.views import LogoutView
from app import forms, views

urlpatterns = [
    path('', views.home, name='home'),
    path('login/', views.login, name='login'),
    path('logout/', LogoutView.as_view(next_page='/'), name='logout'),
    path('test/', views.test, name='test'),
    path('desks/all/', views.allTables),
    path('<str:desk>/', views.home, name='home'),
    #re_path(r"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$", views.home),
]
