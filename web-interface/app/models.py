"""
Definition of models.
"""

from hashlib import sha256
from turtle import speed
from django.db import models
from django.contrib.auth.models import AbstractUser
from idna import encode



class Users(AbstractUser):
    userID = models.BigAutoField(unique=True, primary_key=True)
    username = models.CharField(max_length=128, unique=True, null=False, default='')
    password = models.BinaryField(max_length=32, blank=False, default=0)
    email = models.EmailField(max_length=256, unique=True, null=False, default='')
    date_joined = None
    last_login = None
    is_active = None
    is_staff = None
    is_superuser = None

    def check_password(self, raw_password):
        return self.password.hex() == sha256(encode(raw_password, 'UTF-8')).digest().hex()


    adminStatus = models.BinaryField(max_length=1, default=1) # binary field to set up to 8 admin status types

class UserSettings(models.Model):
    MultipleObjectsReturned = models.BigAutoField(primary_key=True) #Auto incrementing primary key field, not explicitly set.
    userID = models.ForeignKey(Users, on_delete=models.CASCADE) #sets the ID to the primary key in the Users model, the CASCADE option deletes the user settings if user is deleted.
    preferredPosition = models.PositiveSmallIntegerField()
    timeFrom = models.DateTimeField()
    timeTo = models.DateTimeField()
    profileName = models.CharField(max_length=32)
    
class Tables(models.Model) :
    tableID = models.IntegerField(unique=True, primary_key=True)
    #beingUsed = models.BooleanField()
    name = models.TextField(max_length=100)
    manufacturer = models.TextField(max_length=100)

    position = models.PositiveSmallIntegerField()
    speed = models.FloatField()
    status = models.CharField(max_length=16)
    anomalies = models.BinaryField(max_length=1)

    activationCounter = models.PositiveIntegerField()
    sitStandCounter = models.PositiveIntegerField()

class ErrorMessages(models.Model):
    errorCode = models.SmallIntegerField(unique=True, primary_key=True)
    message = models.TextField(max_length=256)

class Errors(models.Model):
    id = models.AutoField(primary_key=True) #Auto incrementing primary key field, not explicitly set.
    tableID = models.ForeignKey(Tables, on_delete=models.CASCADE) #sets the ID to the primary key in the Table model, the CASCADE option deletes the error if table is deleted.
    errorCode = models.ForeignKey(ErrorMessages, on_delete=models.SET_DEFAULT, default=00)
    errorTime = models.DateTimeField(auto_now_add=True)

    

# Create your models here.
