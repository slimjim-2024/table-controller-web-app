"""
Definition of views.
"""

from ast import List
from datetime import datetime
from typing import Any
from django.shortcuts import redirect, render
from django.http import HttpRequest, HttpResponse, JsonResponse
from django.contrib.auth import authenticate, login as auth_login
from .forms import UserLoginForm
from hashlib import sha256
import urllib.request as requests
import json
from .models import Users


requestUrlBase='http://localhost:6000/api/v2/F7H1vM3kQ5rW8zT9xG2pJ6nY4dL0aZ3K/desks'

def getTableData(tableIds: Any):
    print(tableIds)
    for tableId in tableIds:
        resp = requests.Request(f'{requestUrlBase}/{tableId}', method='GET')
        data = {}
        try:    
            with requests.urlopen(resp, timeout=10) as response:
                data = json.loads(response.read().decode())
                yield data
        except Exception as e:
            yield f"Big problem baoss: {e}"


def login(request):
    #print("Login view called")
    if request.user.is_authenticated: 
        return redirect('home')
    #print("User not authenticated, proceeding with login")
    form = UserLoginForm()

    if request.method == 'POST':
        #print ("POST request received")
        username = request.POST.get('username')
        password = request.POST.get('password')
        remember = request.POST.get('rememberPass')
        #print(f"{username} {password} {remember}")
        try:
            user = Users.objects.get(username=username)
        except Users.DoesNotExist:
            errors = 'User does not exist'
            return render(request, 'app/login.html', {'form': form, 'errors': errors})
        else:
            if not user.check_password(password):
                 errors = 'Invalid username or password'
                 return render(request, 'app/login.html', {'form': form, 'errors': errors})              
            auth_login(request, user)
            if not remember:
                request.session.set_expiry(0)
            return render(request, 'app/hello.html', {'username': user})
            
    return render(request, 'app/login.html', {'form': form, 'errors': 'IDK'})

def allTables(request):
    assert isinstance(request, HttpRequest)
    resp = requests.Request(f"{requestUrlBase}/", method='GET')
    data: Any = []
    tables: Any = None
    try:    
        with requests.urlopen(resp, timeout=10) as response:
            #print("Response received")
            tables = json.loads(response.read().decode())
        for i in getTableData(tables):
            data.append(i)


    except Exception as e:
        data.append(f"Big problem baossuu: {e}")
    return JsonResponse(data=data, safe=False)


def home(request, desk = None):
    """Renders the home page."""
    assert isinstance(request, HttpRequest)
    resp = requests.Request(f'{requestUrlBase}{f"/{desk}/" if desk is not None else ''}', method='GET')
    data = {}
    try:    
        with requests.urlopen(resp, timeout=10) as response:
            data = json.loads(response.read().decode())
            return JsonResponse(data=data, safe=False)

    except Exception as e:
        return HttpResponse(f"Big problem baossu: {e}")




def contact(request):
    """Renders the contact page."""
    assert isinstance(request, HttpRequest)
    return render(
        request,
        'app/contact.html',
        {
            'title':'Contact',
            'message':'Your contact page.',
            'year':datetime.now().year,
        }
    )

def test(request):
    assert isinstance(request, HttpRequest)
    return render(
        request,
        'app/test.html'
    )
