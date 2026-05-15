window.CoverManager = (function() {
  function pickCoverImage() {
    const isMobile =
      /iPhone|iPad|iPod|Android/i.test(navigator.userAgent) ||
      window.matchMedia("(max-width: 768px)").matches;

    return isMobile ? "TemplateData/capa_mobile.webp" : "TemplateData/capa_desktop.webp";
  }

  function applyCover() {
    const coverImage = document.getElementById("cover-image");
    const coverOverlay = document.getElementById("cover-overlay");
    
    if (!coverImage || !coverOverlay) return;
    
    coverImage.src = pickCoverImage();
    coverOverlay.classList.add("cover-active");
    coverOverlay.classList.remove("cover-hidden");
  }

  function removeCover() {
    const coverOverlay = document.getElementById("cover-overlay");
    if (!coverOverlay) return;
    
    coverOverlay.classList.remove("cover-active");
    coverOverlay.classList.add("cover-hidden");
    
    setTimeout(() => {
      if (coverOverlay.parentNode) {
        coverOverlay.parentNode.removeChild(coverOverlay);
      }
    }, 500);
  }

  return {
    apply: applyCover,
    remove: removeCover
  };
})();