"""
Definition of models.
"""

from turtle import speed
from django.db import models


class Users(models.Model):
    userID = models.IntegerField(unique=True, primary_key=True)
    userName = models.CharField(max_length=128)
    passwordHash = models.CharField(max_length=64) #Sha256

class UserSettings(models.Model):
    MultipleObjectsReturned = models.AutoField(primary_key=True) #Auto incrementing primary key field, not explicitly set.
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

class Errors(models.Model):
    id = models.AutoField(primary_key=True) #Auto incrementing primary key field, not explicitly set.
    tableID = models.ForeignKey(Tables, on_delete=models.CASCADE) #sets the ID to the primary key in the Table model, the CASCADE option deletes the error if table is deleted.
    errorCode = models.SmallIntegerField()
    errorTime = models.DateTimeField(auto_now_add=True)

class ErrorMessages(models.Model):
    errorCode = models.SmallIntegerField(unique=True, primary_key=True)
    message = models.TextField(max_length=256)
    

# Create your models here.
