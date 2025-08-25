function getURLParameter(name) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(name);
}

const regulation = (getURLParameter("regulation") || "asian").toLowerCase();
window.regulation = regulation;

const chosenLogo =
regulation === "brazilian"
    ? "TemplateData/logo_business.png"
    : "TemplateData/logo_business_2.png";

const logoEl = document.getElementById("unity-logo");
const prodEl = document.getElementById("production-name");

const preloader = new Image();
preloader.onload = () => {
    logoEl.src = chosenLogo;
    logoEl.style.visibility = "visible";

    if (prodEl) {
        prodEl.style.display = regulation === "brazilian" ? "none" : "";
        prodEl.style.visibility = regulation === "brazilian" ? "" : "visible";
    }
};
preloader.src = chosenLogo;

window.addEventListener("load", function () {
if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register("ServiceWorker.js");
}
});