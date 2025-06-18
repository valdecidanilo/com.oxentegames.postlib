/* ===================================================================== */
/*  BLOCO 1 — Listener mínimo (roda assim que o script é executado)      */
/* ===================================================================== */
(function () {
  const GAME_FEATURES = [
    "gamestatus",
    ["openpage", { delegatePages: ["deposit", "game_history"] }],
    ["sound",    { mute: false, level: 50 }]
  ];

  // Deixe acessível ao restante do arquivo, se precisar:
  window.__GAME_FEATURES = GAME_FEATURES;

  window.addEventListener("message", function unityInitListener(event) {
    const data = event.data;
    if (!data || typeof data !== "object" || !data._type) return;

    // --- Responde apenas ao pedido de inicialização --------------------
    if (data._type === "ucip.basic.g2wInitializationRequest") {
      console.log("[DEV TOOLS] InitializationRequest recebido");

      const response = {
        _type: "ucip.basic.w2gInitializationResponse",
        version: data.version || "1.0.0",
        features: GAME_FEATURES.map(f => Array.isArray(f) ? f[0] : f)
      };

      event.source.postMessage(response, "*");

      /* Se o build Unity está num <iframe>, use: */
      // event.source.postMessage(response, "*");

      console.log("[DEV TOOLS] Initialization enviado:", response);
    }
  });
})();

/* ===================================================================== */
/*  BLOCO 2 — Tudo que depende do DOM (sidebar, resolução, etc.)         */
/* ===================================================================== */
document.addEventListener("DOMContentLoaded", () => {
  const sidebar   = document.getElementById("sidebar");
  const toggleBtn = document.getElementById("sidebarToggle");
  const canvas    = document.getElementById("unity-canvas");
  const container = document.getElementById("unity-container");

  /* -------- Mapeamento de mensagens que o PRÓPRIO jogo trata ---------- */
  const w2gMappings = {
    "ucip.basic.w2gInitializationResponse":       initializeResponseHandler,
    "ucip.autoplay.w2gInterruptGameplayCommand":  interruptGameplayHandler,
    "ucip.pause.w2gPauseCommand":                 gamePauseHandler,
    "ucip.displaystatus.w2gVisibilityChangeNotification": visibilityChangeHandler,
    "ucip.sound.w2gSoundUpdateNotification":      soundUpdateHandler,
    "ucip.balancerefresh.w2gRefreshBalanceCommand": refreshBalanceHandler,
    "ucip.closegame.w2gCloseGameRequest":         closeGameHandler
  };

  let isPaused   = false;
  let autoStopped = false;

  function initializeResponseHandler(msg) {}
  function interruptGameplayHandler(msg) {}
  function gamePauseHandler(msg) {}
  function visibilityChangeHandler(msg) {}
  function soundUpdateHandler(msg) {}
  function refreshBalanceHandler(msg) {}
  function closeGameHandler(msg) {}

  /* --------------------- Layout do canvas x sidebar ------------------- */
  function applyCanvasLayout() {
    const isOpen       = sidebar.classList.contains("open");
    const sidebarWidth = isOpen ? sidebar.offsetWidth : 0;
    container.style.left  = `${sidebarWidth}px`;
    container.style.width = `${window.innerWidth - sidebarWidth}px`;
  }

  if (toggleBtn) {
    toggleBtn.addEventListener("click", () => {
      sidebar.classList.toggle("open");
      setTimeout(applyCanvasLayout, 300); // espera animação CSS
    });
  }
  window.addEventListener("resize", applyCanvasLayout);
  applyCanvasLayout();
  /* -------------------------- Botões de controle ----------------------- */
  window.togglePause = () => {
    isPaused = !isPaused;

    const msg = {
      _type: "ucip.pause.w2gPauseCommand",
      pause: isPaused
    };
    window.postMessage(msg, "*");

    // Atualiza rótulo do botão
    document.getElementById("pauseBtn").textContent = isPaused ? "▶️ Resume" : "⏸️ Pause";

    console.log("[DEV TOOLS] Pause toggled:", msg);
  };

  window.interruptAutoplay = () => {
    const msg = {
      _type: "ucip.autoplay.w2gInterruptGameplayCommand",
    };
    window.postMessage(msg, "*");
    console.log("[DEV TOOLS] Autoplay interrompido:", msg);
  };


  /* ---------------------------- Resolução ----------------------------- */
  window.changeResolution = (value) => {
    if (!canvas) return;

    if (value === "auto") {
      canvas.style.width = "100%";
      canvas.style.height = "100%";
    } else {
      const [w, h] = value.split("x");
      canvas.style.width  = `${w}px`;
      canvas.style.height = `${h}px`;
    }
  };

  /* ----------------------- Enviar postMessage manual ------------------ */
  window.sendCustomMessage = () => {
    try {
      const raw = document.getElementById("postMessageJson").value;
      const msg = JSON.parse(raw);
      window.postMessage(msg, "*");
      console.log("[DEV TOOLS] postMessage enviado:", msg);
    } catch (e) {
      alert("Erro no JSON: " + e.message);
    }
  };

  /* --------- Qualquer mensagem NÃO tratada pelo próprio jogo ---------- */
  window.addEventListener("message", (event) => {
    const data = event.data;
    if (!data || typeof data !== "object" || !data._type) return;

    // se cair aqui é porque não está no w2gMappings (e não é request g2w)
    if (!Object.prototype.hasOwnProperty.call(w2gMappings, data._type)) {
      console.log("[DEV TOOLS] 📥 Mensagem recebida do Game (não mapeada):", data);
    }
  });
});
