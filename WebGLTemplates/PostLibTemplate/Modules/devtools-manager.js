window.DevToolsManager = (function() {
  let devToolsLoaded = false;

  function mountDevPanel() {
    if (document.getElementById("sidebar") || document.getElementById("sidebarToggle")) return;

    const toggle = document.createElement("button");
    toggle.id = "sidebarToggle";
    toggle.textContent = "☰ Painel Principal";
    document.body.appendChild(toggle);

    const sidebar = document.createElement("div");
    sidebar.id = "sidebar";
    sidebar.setAttribute("aria-hidden", "true");

    sidebar.innerHTML = `
      <div class="menu-section">
        <h3>🧩 Módulos</h3>
        <button class="opa-btn" id="btnOpenDebug">Abrir Debug Panel</button>
        <button class="opa-btn" id="btnTogglePerf">Mostrar/Ocultar Perf</button>
        <button class="opa-btn" id="btnToggleHud">Mostrar/Ocultar HUD</button>
      </div>

      <div class="menu-section">
        <h3>📤 Enviar Mensagem</h3>
        <textarea id="postMessageJson" class="opa-textarea" rows="8">{
  "_type": "ucip.pause.w2gPauseCommand",
  "pause": true
}</textarea>
        <button class="opa-btn" id="btnSendPostMessage">Enviar postMessage</button>
      </div>

      <div class="menu-section">
        <h3>🎮 Controles de Jogo</h3>
        <button class="opa-btn" id="pauseBtn">⏸️ Pause</button>
        <button class="opa-btn" id="autoBtn">⏹️ Parar Autoplay</button>
      </div>

      <div class="menu-section">
        <h3>🖥️ Resolução Canvas</h3>
        <select id="resolutionSelect" class="opa-select">
          <option value="auto">Auto (100%)</option>
          <option value="1920x1080">1920x1080</option>
          <option value="1280x720">1280x720</option>
          <option value="800x600">800x600</option>
          <option value="360x640">Mobile Portrait</option>
        </select>
      </div>
    `;

    document.body.appendChild(sidebar);
  }

  async function ensureLoaded() {
    if (devToolsLoaded) return;
    devToolsLoaded = true;

    mountDevPanel();

    const root = document.getElementById("devtools-root");
    if (root) root.style.display = "block";

    const { safeLoadScript } = window.LoaderUtils;

    await safeLoadScript("Modules/input-shield.js");
    await safeLoadScript("Modules/perf-monitor.js");
    await safeLoadScript("Modules/custom-ui.js");
    await safeLoadScript("Modules/debug-panel.js");
    await safeLoadScript("Modules/main-panel.js");
  }

  function openAndFocus() {
    try {
      window.__setSidebarOpen?.(true);
    } catch {}
    
    const ta = document.getElementById("postMessageJson");
    if (ta) {
      ta.focus({ preventScroll: true });
      setTimeout(() => ta.focus({ preventScroll: true }), 0);
    }
  }

  return {
    ensureLoaded,
    openAndFocus
  };
})();