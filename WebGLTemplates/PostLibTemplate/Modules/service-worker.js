(function () {
      if (location.protocol === "file:") {
        console.warn("[PWA] Manifest desativado em file:// (CORS origin null).");
        return;
      }
      const link = document.createElement("link");
      link.rel = "manifest";
      link.href = "manifest.webmanifest";
      document.head.appendChild(link);
    })();