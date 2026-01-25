(function () {
  document.addEventListener("DOMContentLoaded", () => {
    const sidebar = document.getElementById("sidebar");
    const toggleBtn = document.getElementById("sidebarToggle");
    const canvas = document.getElementById("unity-canvas");
    const container = document.getElementById("unity-container");
    const applyCanvasLayout = () => {
      const isOpen = sidebar?.classList.contains("open");
      const sidebarWidth = isOpen ? sidebar.offsetWidth : 0;
      container.style.left = `${sidebarWidth}px`;
      container.style.width = `${window.innerWidth - sidebarWidth}px`;
    };

    if (toggleBtn) {
      toggleBtn.addEventListener("click", () => {
        sidebar.classList.toggle("open");
        setTimeout(() => {
          applyCanvasLayout();
          if (sidebar.classList.contains("open")) {
            //document.getElementById("postMessageJson")?.focus();
            const firstInput = sidebar.querySelector("textarea, input, select, button");
            if (firstInput) firstInput.focus();
          }
        }, 300);
      });
    }

    window.addEventListener("resize", applyCanvasLayout);
    applyCanvasLayout();

    // Funções globais
    window.togglePause = () => {
      const pauseBtn = document.getElementById("pauseBtn");
      const isPaused = pauseBtn.textContent.includes("Resume");

      const msg = {
        _type: "ucip.pause.w2gPauseCommand",
        pause: !isPaused
      };
      window.postMessage(JSON.stringify(msg), "*");
      pauseBtn.textContent = isPaused ? "⏸️ Pause" : "▶️ Resume";
      console.log("[PostLib JS] Toggle pause:", msg);
    };

    window.interruptAutoplay = () => {
      const msg = { _type: "ucip.autoplay.w2gInterruptGameplayCommand" };
      window.postMessage(msg, "*");
      console.log("[PostLib JS] Autoplay interrompido:", msg);
    };

    window.changeResolution = (value) => {
      const canvas = document.getElementById("unity-canvas");
      const match = value.match(/^(\d+)x(\d+)$/);
      if (match) {
        const width = parseInt(match[1], 10);
        const height = parseInt(match[2], 10);
        canvas.style.width = width + "px";
        canvas.style.height = height + "px";
      } else if (value === "") {
        canvas.style.width = "100%";
        canvas.style.height = "auto";
      }
    };

    window.sendCustomMessage = () => {
      try {
        const raw = document.getElementById("postMessageJson").value.trim();
        const msg = JSON.parse(raw);
        window.postMessage(msg, "*");
        console.log("[PostLib JS] postMessage enviado:", msg);
      } catch (e) {
        alert("Erro no JSON: " + e.message);
      }
    };
  });
})();
