function getURLParameter(name) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(name);
}

const regulation = (getURLParameter("regulation") || "en").toLowerCase();

if (regulation === "br") {
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