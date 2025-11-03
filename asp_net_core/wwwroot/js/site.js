darkModeCheck.addEventListener("click", makeDarkmode);

let interacted = false;

function makeDarkmode()
{
    document.body.classList.toggle("darkMode");

    var sliderImg = document.getElementById("sliderIMG");

    if (!interacted) {
        sliderImg.classList.add("animate");
        interacted = true;
    }
    if (document.getElementById("darkModeCheck").checked) {
        sliderImg.src = "/moony.png";
    }
    else {
        sliderImg.src = "/sunny.png";
    }
}