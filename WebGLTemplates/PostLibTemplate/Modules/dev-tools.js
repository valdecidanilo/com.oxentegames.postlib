(function () {
    if (!IS_DEV_BUILD) return;

    function loadScript(src, defer = true) {
        const s = document.createElement("script");
        s.src = src;
        if (defer) s.defer = true;
        document.head.appendChild(s);
    }
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

    function boot() {
        mountDevPanel();

        loadScript("Modules/input-shield.js", false);
        loadScript("Modules/debug-panel.js", true);
        loadScript("Modules/perf-monitor.js", true);

        loadScript("Modules/main-panel.js", true);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", boot);
    } else {
        boot();
    }
})();
