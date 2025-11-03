document.addEventListener("DOMContentLoaded", () => {
    if (window.matchMedia("(prefers-color-scheme: dark)").matches) {
        document.body.classList.toggle("darkMode");

        document.getElementById("darkModeCheck").checked = true;

        var sliderImg = document.getElementById("sliderIMG");

        sliderImg.src = "/moony.svg";
        sliderImg.classList.add("animate");
        interacted = true;
    }
});

darkModeCheck.addEventListener("click", () => {
    document.body.classList.toggle("darkMode");

    var sliderImg = document.getElementById("sliderIMG");

    if (document.getElementById("darkModeCheck").checked) {
        sliderImg.src = "/moony.svg";
    }
    else {
        sliderImg.src = "/sunny.svg";
    }

    if (!interacted) {
        sliderImg.classList.add("animate");
        interacted = true;
    }
});

let interacted = false;