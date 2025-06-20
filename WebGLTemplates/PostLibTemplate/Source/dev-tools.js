/* ===================================================================== */
/*  Dev‑Tools – carregado somente em Development                         */
/* ===================================================================== */
document.addEventListener("DOMContentLoaded", () => {
  /* ------------------- DOM ------------------------------------------------ */
  const sidebar     = document.getElementById("sidebar");
  const toggleBtn   = document.getElementById("sidebarToggle");
  const canvas      = document.getElementById("unity-canvas");
  const container   = document.getElementById("unity-container");
  const pauseBtn    = document.getElementById("pauseBtn");
  const autoBtn     = document.getElementById("autoBtn");
  const msgTextarea = document.getElementById("postMessageJson");

  /* ------------------- Estado -------------------------------------------- */
  let isPaused    = false;

  /* ------------------- Layout / Sidebar ---------------------------------- */
  function applyCanvasLayout() {
    const isOpen       = sidebar.classList.contains("open");
    const sidebarWidth = isOpen ? sidebar.offsetWidth : 0;
    container.style.left  = `${sidebarWidth}px`;
    container.style.width = `${window.innerWidth - sidebarWidth}px`;
  }

  if (toggleBtn) {
    toggleBtn.addEventListener("click", () => {
      sidebar.classList.toggle("open");
      setTimeout(applyCanvasLayout, 300);
    });
  }
  window.addEventListener("resize", applyCanvasLayout);
  applyCanvasLayout();               // primeira execução

  /* ------------------- Botões de controle -------------------------------- */
  window.togglePause = () => {
    isPaused = !isPaused;
    PostLib.send({
      _type: "ucip.pause.w2gPauseCommand",
      pause: isPaused
    });

    pauseBtn.textContent = isPaused ? "▶️ Resume" : "⏸️ Pause";
    console.log("[Dev‑Tools] Pause toggled:", isPaused);
  };

  window.interruptAutoplay = () => {
    PostLib.send({ _type: "ucip.autoplay.w2gInterruptGameplayCommand" });
    console.log("[Dev‑Tools] Autoplay interrompido");
  };

  /* ------------------- Resolução canvas ---------------------------------- */
  window.changeResolution = (value) => {
    if (!canvas) return;

    if (value === "auto") {
      canvas.style.width  = "100%";
      canvas.style.height = "100%";
    } else {
      const [w, h] = value.split("x");
      canvas.style.width  = `${w}px`;
      canvas.style.height = `${h}px`;
    }
  };

  /* ------------------- Enviar JSON manual -------------------------------- */
  window.sendCustomMessage = () => {
    try {
      const raw = msgTextarea.value;
      const msg = JSON.parse(raw);
      PostLib.send(msg);
      console.log("[Dev‑Tools] postMessage enviado:", msg);
    } catch (e) {
      alert("Erro no JSON: " + e.message);
    }
  };

  /* ------------------- Handlers vindos do jogo --------------------------- */
  const w2gMappings = {
    "ucip.basic.w2gInitializationResponse": (msg) => {
      console.log("[Dev‑Tools] InitializationResponse", msg);
    },
    "ucip.autoplay.w2gInterruptGameplayCommand": (msg) => {
      console.log("[Dev‑Tools] Autoplay interrupt recebido", msg);
    },
    "ucip.pause.w2gPauseCommand": (msg) => {
      console.log("[Dev‑Tools] Pause command recebido", msg);
    },
    // ... adicione outros handlers se precisar ...
  };

  /* Inscreve‑se no dispatcher do core */
  PostLib.onMessage.add((msg) => {
    if (w2gMappings[msg._type])
      w2gMappings[msg._type](msg);
    else
      console.log("[Dev‑Tools] 📥 Mensagem não mapeada:", msg);
  });
});
