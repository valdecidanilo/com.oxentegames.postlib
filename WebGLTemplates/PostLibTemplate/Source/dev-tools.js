(function () {
  const GAME_FEATURES = [
    "gamestatus",
    ["openpage", { delegatePages: ["deposit", "game_history"] }],
    ["sound", { mute: false, level: 50 }]
  ];

  // Listener mínimo que SEMPRE escuta mensagens
  window.addEventListener("message", (event) => {
    const data = event.data;
    if (!data || typeof data !== "object" || !data._type) return;

    if (data._type === "ucip.basic.g2wInitializationRequest") {
      const response = {
        _type: "ucip.basic.w2gInitializationResponse",
        version: data.version || "1.0.0",
        features: GAME_FEATURES.map(f => Array.isArray(f) ? f[0] : f)
      };
      event.source.postMessage(response, "*");
      console.log("[DevTools] Initialization enviado:", response);
    }
  });

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
        setTimeout(applyCanvasLayout, 300);
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
      window.postMessage(msg, "*");
      pauseBtn.textContent = isPaused ? "⏸️ Pause" : "▶️ Resume";
      console.log("[DevTools] Toggle pause:", msg);
    };

    window.interruptAutoplay = () => {
      const msg = { _type: "ucip.autoplay.w2gInterruptGameplayCommand" };
      window.postMessage(msg, "*");
      console.log("[DevTools] Autoplay interrompido:", msg);
    };

    window.changeResolution = (value) => {
      if (!canvas) return;
      if (value === "auto") {
        canvas.style.width = "100%";
        canvas.style.height = "100%";
      } else {
        const [w, h] = value.split("x");
        canvas.style.width = `${w}px`;
        canvas.style.height = `${h}px`;
      }
    };

    window.sendCustomMessage = () => {
      try {
        const raw = document.getElementById("postMessageJson").value;
        const msg = JSON.parse(raw);
        window.postMessage(msg, "*");
        console.log("[DevTools] postMessage enviado:", msg);
      } catch (e) {
        alert("Erro no JSON: " + e.message);
      }
    };
  });
})();
