(function () {
  "use strict";

  const DebugPanel = {
    panel: null,
    isVisible: false,
    isEnabled: false,
    commands: [],
    history: [],
    historyIndex: -1,
    _unity: null,

    mount: function () {
      if (this.panel) return;

      const panel = document.createElement("div");
      panel.id = "debug-panel";
      panel.classList.add("opa-widget");
      panel.style.cssText = `
        position: fixed;
        top: 10px;
        right: 10px;
        width: 380px;
        max-height: 85vh;
        display: none;
        z-index: 12001;
        pointer-events: auto;
        overflow: hidden;
      `;

      panel.innerHTML = `
        <div class="opa-header">
          <div class="opa-title">🔧 DEBUG CONSOLE</div>
          <div style="display:flex; gap:8px; align-items:center;">
            <button id="debug-minimize" class="opa-close" style="color:#fff;border-color:rgba(255,255,255,0.25);">_</button>
            <button id="debug-close" class="opa-close">✕</button>
          </div>
        </div>

        <div id="debug-content" style="padding: 10px 12px 12px 12px;">
          <div style="display:flex; align-items:center; gap:10px; margin-bottom: 10px;">
            <div style="font-weight:900; color: rgba(255,255,255,0.75);">></div>
            <input type="text" id="debug-input" class="opa-input" placeholder="Digite comando ou 'help'..." style="margin-top:0; flex: 1;" />
            <button id="debug-execute" class="opa-btn" style="width:auto; margin-top:0; padding:10px 14px;">▶</button>
          </div>

          <div style="background: rgba(0,0,0,0.18); border: 1px solid rgba(255,255,255,0.10); border-radius: 10px; padding: 10px; margin-bottom: 10px;">
            <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom: 8px;">
              <strong style="font-size: 12px; color: rgba(255,255,255,0.9);">📋 COMANDOS</strong>
              <button id="debug-toggle-commands" class="opa-close" style="color:#fff;border-color:rgba(255,255,255,0.25);">▼</button>
            </div>
            <div id="debug-commands" style="max-height: 150px; overflow-y: auto; font-size: 12px;"></div>
          </div>

          <div style="background: rgba(0,0,0,0.22); border: 1px solid rgba(255,255,255,0.10); border-radius: 10px; padding: 10px;">
            <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom: 8px;">
              <strong style="font-size: 12px; color: rgba(255,255,255,0.9);">📺 OUTPUT</strong>
              <button id="debug-clear" class="opa-close">CLEAR</button>
            </div>
            <div id="debug-output" style="max-height: 260px; overflow-y: auto; font-size: 12px; line-height: 1.45;"></div>
          </div>
        </div>
      `;

      document.body.appendChild(panel);
      this.panel = panel;

      this.setupEventListeners();
      this.setupHistoryNavigation();
      this.addDefaultCommands();
      this.updateCommandsList();
    },

    init: function (unityInstance) {
      this._unity = unityInstance;
    },

    enable: function () {
      this.isEnabled = true;
      // não abre automaticamente, só habilita a possibilidade
    },

    disable: function () {
      this.isEnabled = false;
      this.hide();
    },

    show: function () {
      this.isVisible = true;
      if (this.panel) this.panel.style.display = "block";
      const input = document.getElementById("debug-input");
      if (input) input.focus();
    },

    hide: function () {
      this.isVisible = false;
      if (this.panel) this.panel.style.display = "none";
    },

    toggle: function () {
      (this.panel && this.panel.style.display === "none") ? this.show() : this.hide();
    },

    setupEventListeners: function () {
      const closeBtn = document.getElementById("debug-close");
      const minimizeBtn = document.getElementById("debug-minimize");
      const executeBtn = document.getElementById("debug-execute");
      const clearBtn = document.getElementById("debug-clear");
      const toggleCmds = document.getElementById("debug-toggle-commands");
      const input = document.getElementById("debug-input");

      closeBtn.onclick = () => this.hide();
      clearBtn.onclick = () => this.clearOutput();

      minimizeBtn.onclick = () => {
        const content = document.getElementById("debug-content");
        const isMin = content.style.display === "none";
        content.style.display = isMin ? "block" : "none";
        minimizeBtn.textContent = isMin ? "_" : "□";
      };

      toggleCmds.onclick = () => {
        const cmds = document.getElementById("debug-commands");
        const isVisible = cmds.style.display !== "none";
        cmds.style.display = isVisible ? "none" : "block";
        toggleCmds.textContent = isVisible ? "▶" : "▼";
      };

      executeBtn.onclick = () => {
        this.executeCommand(input.value);
        input.value = "";
      };

      input.addEventListener("keydown", (e) => {
        if (e.key === "Enter") {
          this.executeCommand(input.value);
          input.value = "";
        } else if (e.key === "ArrowUp") {
          e.preventDefault();
          this.navigateHistory(-1);
        } else if (e.key === "ArrowDown") {
          e.preventDefault();
          this.navigateHistory(1);
        }
      });
    },

    setupHistoryNavigation: function () {
      this.history = [];
      this.historyIndex = -1;
    },

    navigateHistory: function (direction) {
      if (this.history.length === 0) return;

      this.historyIndex += direction;
      if (this.historyIndex < 0) this.historyIndex = 0;
      if (this.historyIndex >= this.history.length) this.historyIndex = this.history.length - 1;

      const input = document.getElementById("debug-input");
      input.value = this.history[this.historyIndex] || "";
    },

    addCommand: function (name, description, callback) {
      this.commands.push({ name, description, callback });
      this.updateCommandsList();
    },

    updateCommandsList: function () {
      const list = document.getElementById("debug-commands");
      if (!list) return;

      list.innerHTML = "";
      this.commands.forEach((cmd) => {
        const row = document.createElement("div");
        row.style.cssText = `
          margin: 6px 0;
          padding: 8px 10px;
          border-radius: 10px;
          background: rgba(255,255,255,0.05);
          border: 1px solid rgba(255,255,255,0.08);
          cursor: pointer;
        `;
        row.innerHTML = `
          <div style="display:flex; gap:8px; align-items:baseline;">
            <span style="font-weight:900; color: rgba(255,255,255,0.95);">${cmd.name}</span>
            <span style="color: rgba(255,255,255,0.55);">—</span>
            <span style="color: rgba(255,255,255,0.75);">${cmd.description}</span>
          </div>
        `;
        row.onclick = () => {
          const input = document.getElementById("debug-input");
          input.value = cmd.name + " ";
          input.focus();
        };
        list.appendChild(row);
      });
    },

    executeCommand: function (input) {
      input = (input || "").trim();
      if (!input) return;

      this.history.unshift(input);
      if (this.history.length > 50) this.history.pop();
      this.historyIndex = -1;

      const parts = input.split(" ");
      const cmdName = parts[0];
      const args = parts.slice(1);

      this.log(`> ${input}`, "cmd");

      const command = this.commands.find((c) => c.name === cmdName);
      if (!command) {
        this.log(`Comando não encontrado: "${cmdName}". Digite "help" para ver comandos.`, "error");
        return;
      }

      try { command.callback(args); }
      catch (err) { this.log(`Erro: ${err.message}`, "error"); }
    },

    log: function (message, type = "ok") {
      const output = document.getElementById("debug-output");
      if (!output) return;

      const line = document.createElement("div");
      line.style.cssText = `
        margin-bottom: 6px;
        padding-bottom: 6px;
        border-bottom: 1px solid rgba(255,255,255,0.08);
        color: ${this._colorFor(type)};
      `;

      const timestamp = new Date().toLocaleTimeString();
      line.innerHTML = `<span style="color: rgba(255,255,255,0.45);">[${timestamp}]</span> ${this._escapeHtml(message)}`;
      output.appendChild(line);
      output.scrollTop = output.scrollHeight;
    },

    clearOutput: function () {
      const out = document.getElementById("debug-output");
      if (out) out.innerHTML = "";
      this.log("Console limpo.", "muted");
    },

    _colorFor: function (type) {
      if (type === "error") return "#ff4d4d";
      if (type === "warn") return "#fbbf24";
      if (type === "info") return "#60a5fa";
      if (type === "cmd") return "rgba(255,255,255,0.92)";
      if (type === "muted") return "rgba(255,255,255,0.60)";
      return "rgba(255,255,255,0.85)";
    },

    _escapeHtml: function (s) {
      return String(s)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
    },

    addDefaultCommands: function () {
      this.addCommand("help", "Lista todos os comandos disponíveis", () => {
        this.log("═══ COMANDOS DISPONÍVEIS ═══", "info");
        this.commands.forEach((cmd) => this.log(`  ${cmd.name} - ${cmd.description}`, "muted"));
      });

      this.addCommand("clear", "Limpa o console de output", () => this.clearOutput());

      this.addCommand("fps", "Toggle contador de FPS no Unity", () => {
        this.log("Comando FPS enviado ao Unity", "info");
        window.UnityModules?.sendToUnity?.("GameManager", "ToggleFPS", "");
      });

      this.addCommand("timescale", "Altera velocidade (ex: timescale 2)", (args) => {
        const scale = parseFloat(args[0]);
        if (isNaN(scale)) return this.log("Uso: timescale <número>", "warn");
        this.log(`TimeScale alterado para: ${scale}x`, "info");
        window.UnityModules?.sendToUnity?.("GameManager", "SetTimeScale", scale.toString());
      });

      this.addCommand("reload", "Recarrega a cena atual", () => {
        this.log("Recarregando cena...", "warn");
        window.UnityModules?.sendToUnity?.("GameManager", "ReloadScene", "");
      });

      this.addCommand("info", "Mostra informações do sistema", () => {
        this.log("═══ INFORMAÇÕES ═══", "info");
        this.log(`Navegador: ${navigator.userAgent}`, "muted");
        this.log(`Resolução: ${window.innerWidth}x${window.innerHeight}`, "muted");
        this.log(`Unity: ${window.UnityModules?.unityInstance ? "Conectado" : "Desconectado"}`, "muted");
      });
    },
  };

  if (window.UnityModules) {
    window.UnityModules.register("debugPanel", DebugPanel);
  }
  window.DebugPanel = DebugPanel;
})();
