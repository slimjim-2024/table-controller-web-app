"""
Definition of forms.
"""

from django import forms
from django.contrib.auth.forms import AuthenticationForm
from django.utils.translation import gettext_lazy as _


class BootstrapAuthenticationForm(AuthenticationForm):
    """Authentication form which uses boostrap CSS."""
    username = forms.CharField(max_length=254,
                               widget=forms.TextInput({
                                   'class': 'form-control',
                                   'placeholder': 'User name'}))
    password = forms.CharField(label=_("Password"),
                               widget=forms.PasswordInput({
                                   'class': 'form-control',
                                   'placeholder':'Password'}))


class UserLoginForm(forms.Form):
    username = forms.CharField(max_length=100, 
                               widget=forms.TextInput(attrs={'class': 'textInput', 'placeholder': ''}))
    password = forms.CharField(label=_('Password'),
                              widget=forms.PasswordInput(attrs={'class': 'textInput',
                                                               'placeholder': ''}))
    rememberPass = forms.BooleanField(required=False, widget=forms.CheckboxInput(attrs={'type': 'checkbox', 'name':'rememberMe', 'id':'rememberMe'}))
    
