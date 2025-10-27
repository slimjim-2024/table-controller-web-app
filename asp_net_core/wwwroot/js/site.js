darkModeSlider.addEventListener("click", makeDarkmode);

function makeDarkmode()
{
    var body = document.body;
    body.classList.toggle("darkMode");
}