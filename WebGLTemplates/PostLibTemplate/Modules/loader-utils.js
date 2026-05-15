window.LoaderUtils = (function() {
  function loadCss(href) {
    return new Promise((resolve, reject) => {
      const link = document.createElement("link");
      link.rel = "stylesheet";
      link.href = href;
      link.onload = resolve;
      link.onerror = reject;
      document.head.appendChild(link);
    });
  }

  function loadScript(src, defer = true) {
    return new Promise((resolve, reject) => {
      const s = document.createElement("script");
      s.src = src;
      if (defer) s.defer = true;
      s.onload = resolve;
      s.onerror = reject;
      document.head.appendChild(s);
    });
  }

  async function safeLoadCss(path) {
    try {
      await loadCss(path);
      return true;
    } catch {
      console.warn("[CSS] Falhou:", path);
      return false;
    }
  }

  async function safeLoadScript(path) {
    try {
      await loadScript(path, true);
      return true;
    } catch {
      console.warn("[JS] Falhou:", path);
      return false;
    }
  }

  return {
    loadCss,
    loadScript,
    safeLoadCss,
    safeLoadScript
  };
})();