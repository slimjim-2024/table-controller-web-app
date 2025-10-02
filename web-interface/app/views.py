"""
Definition of views.
"""

from datetime import datetime
from django.shortcuts import redirect, render
from django.http import HttpRequest, HttpResponse
from django.contrib.auth import authenticate, login as auth_login
from .forms import UserLoginForm
from hashlib import sha256
from .models import Users


def login(request):
    form = UserLoginForm()
    if request.method == 'POST':
        username = request.POST.get('username')
        password = request.POST.get('password')
        remember = request.POST.get('rememberPass')
        print(f"{username} {password} {remember}")
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

            
    return render(request, 'app/login.html', {'form': form, 'title': "Table Control"})

def home(request):
    """Renders the home page."""
    assert isinstance(request, HttpRequest)
    return HttpResponse("Hello")

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
