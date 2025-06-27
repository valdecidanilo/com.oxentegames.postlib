function getURLParameter(name) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(name);
}

const regulation = (getURLParameter("regulation") || "EN").toUpperCase();

//const logoImg = document.getElementById("unity-logo");
//const productionText = document.getElementById("production-name");

if (regulation === "BR") {
    window.logoImg.src = "TemplateData/logo_business.png";
    if (window.productionText) {
        window.productionText.style.display = "none";
    }
}else{
    window.logoImg.src = "TemplateData/logo_business_2.png";
}

window.regulation = regulation;

window.addEventListener("load", function () {
    if ("serviceWorker" in navigator) {
        navigator.serviceWorker.register("ServiceWorker.js");
    }
});