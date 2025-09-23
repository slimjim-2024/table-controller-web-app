from django.shortcuts import render
from django.http import HttpRequest, HttpResponse

def test(request):
    assert isinstance(request, HttpRequest)
    return render(
        request,
        'app/test.html'
        )
    #return HttpResponse("Test page")