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

// Pré-carrega a imagem e depois mostra
const preloader = new Image();
preloader.onload = () => {
    logoEl.src = chosenLogo;
    
    // SOLUÇÃO 1: Usar a classe CSS definida
    logoEl.classList.add("logo-ready");
    
    if (prodEl) {
        if (regulation === "brazilian") {
            prodEl.style.display = "none";
        } else {
            prodEl.classList.add("logo-ready");
        }
    }
};
preloader.src = chosenLogo;