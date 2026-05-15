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

function initLogo() {
    const logoEl = document.getElementById("unity-logo");
    const prodEl = document.getElementById("production-name");

    if (!logoEl) {
        console.warn("[url-search-params] Logo element not found");
        return;
    }

    const preloader = new Image();
    preloader.onload = () => {
        logoEl.src = chosenLogo;
        
        if (prodEl) {
            if (regulation === "brazilian") {
                prodEl.style.display = "none";
            }
        }
    };
    preloader.onerror = () => {
        console.warn("[PostLib] Failed to preload logo:", chosenLogo);
        logoEl.src = chosenLogo;
    };
    preloader.src = chosenLogo;
}

if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initLogo);
} else {
    initLogo();
}