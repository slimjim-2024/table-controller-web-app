"""
Definition of views.
"""

from datetime import datetime
from django.shortcuts import render
from django.http import HttpRequest
from django.contrib.auth import authenticate, login as auth_login
from .forms import UserLoginForm
from hashlib import sha256
from .models import Users


def login(request):
    form = UserLoginForm()
    if request.method == 'POST':
        username = request.POST.get('username')
        password = request.POST.get('password')
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
            return render(request, 'app/hello.html', {'username': user})

            
    return render(request, 'app/login.html', {'form': form})

def home(request):
    """Renders the home page."""
    assert isinstance(request, HttpRequest)
    return render(
        request,
        'app/index.html',
        {
            'title':'Home Page',
            'year':datetime.now().year,
        }
    )

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

def about(request):
    """Renders the about page."""
    assert isinstance(request, HttpRequest)
    return render(
        request,
        'app/about.html',
        {
            'title':'About',
            'message':'Your application description page.',
            'year':datetime.now().year,
        }
    )

def test(request):
    assert isinstance(request, HttpRequest)
    return render(
        request,
        'app/test.html'
    )
