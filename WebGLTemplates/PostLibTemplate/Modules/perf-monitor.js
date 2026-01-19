(function () {
  "use strict";

  const PerformanceMonitor = {
    widget: null,
    isVisible: false,
    isEnabled: false,
    stats: { fps: 0, frameTime: 0, memory: 0, maxMemory: 0, cpu: 0 },
    frames: [],
    cpuSamples: [],
    frameTimeSamples: [], // Para calcular CPU corretamente
    lastTime: performance.now(),
    cpuCheckTime: performance.now(),
    graphCanvas: null,
    graphCtx: null,
    cpuGraphCanvas: null,
    cpuGraphCtx: null,
    _raf: 0,
    _updateCounter: 0,
    _displayUpdateCounter: 0,

    mount: function () {
      if (this.widget) return;

      const widget = document.createElement("div");
      widget.id = "perf-monitor";
      widget.classList.add("opa-widget");
      widget.style.cssText = `
        position: fixed;
        top: 10px;
        right: 10px;
        z-index: 12000;
        min-width: 220px;
        max-width: 260px;
        pointer-events: auto;
        display: none;
        overflow: hidden;
      `;

      widget.innerHTML = `
        <div class="opa-header">
          <div class="opa-title">⚡ PERFORMANCE</div>
          <button id="perf-close" class="opa-close">✕</button>
        </div>

        <div style="padding: 10px 12px 12px 12px;">
          <div style="display:flex; justify-content:space-between; font-size:12px; color: rgba(255,255,255,0.7);">
            <span>FPS</span>
            <span>
              <span id="perf-fps" style="font-weight:900; color:#fff;">--</span>
              <span id="perf-fps-indicator" style="margin-left:6px;">●</span>
            </span>
          </div>

          <div style="display:flex; justify-content:space-between; margin-top:6px; font-size:12px; color: rgba(255,255,255,0.7);">
            <span>Frame</span>
            <span><span id="perf-frametime" style="font-weight:900; color:#fff;">--</span> ms</span>
          </div>

          <div style="display:flex; justify-content:space-between; margin-top:6px; font-size:12px; color: rgba(255,255,255,0.7);">
            <span>CPU</span>
            <span>
              <span id="perf-cpu" style="font-weight:900; color:#fff;">--</span>%
              <span id="perf-cpu-indicator" style="margin-left:6px;">●</span>
            </span>
          </div>

          <div id="perf-memory-section" style="display:none; justify-content:space-between; margin-top:6px; font-size:12px; color: rgba(255,255,255,0.7);">
            <span>Memory</span>
            <span><span id="perf-memory" style="font-weight:900; color:#fff;">--</span> MB</span>
          </div>

          <div style="margin-top: 10px; font-size: 11px; font-weight: 600; color: rgba(255,255,255,0.5); text-transform: uppercase; letter-spacing: 0.5px;">FPS Graph</div>
          <div id="perf-graph-fps" style="
            margin-top: 6px;
            height: 50px;
            background: rgba(0,0,0,0.25);
            border: 1px solid rgba(61,120,255,0.15);
            border-radius: 8px;
            overflow: hidden;
          "></div>

          <div style="margin-top: 10px; font-size: 11px; font-weight: 600; color: rgba(255,255,255,0.5); text-transform: uppercase; letter-spacing: 0.5px;">CPU Graph</div>
          <div id="perf-graph-cpu" style="
            margin-top: 6px;
            height: 50px;
            background: rgba(0,0,0,0.25);
            border: 1px solid rgba(255,120,61,0.15);
            border-radius: 8px;
            overflow: hidden;
          "></div>
        </div>
      `;

      document.body.appendChild(widget);
      this.widget = widget;

      document.getElementById("perf-close").onclick = () => this.hide();

      this.createGraph();

      if (performance.memory) {
        const sec = document.getElementById("perf-memory-section");
        if (sec) sec.style.display = "flex";
      }
    },

    init: function (unityInstance) {
      // não precisa do unityInstance para medir FPS do browser
    },

    enable: function () {
      this.isEnabled = true;
      this.show();
      this.start();
    },

    disable: function () {
      this.isEnabled = false;
      this.stop();
      this.hide();
    },

    show: function () {
      this.isVisible = true;
      if (this.widget) this.widget.style.display = "block";
    },

    hide: function () {
      this.isVisible = false;
      if (this.widget) this.widget.style.display = "none";
    },

    toggleVisibility: function () {
      if (!this.widget) return;
      (this.widget.style.display === "none") ? this.show() : this.hide();
    },

    createGraph: function () {
      // FPS Graph
      const containerFPS = document.getElementById("perf-graph-fps");
      const canvasFPS = document.createElement("canvas");
      const dpr = window.devicePixelRatio || 1;
      
      canvasFPS.width = (containerFPS.clientWidth || 240) * dpr;
      canvasFPS.height = 50 * dpr;
      canvasFPS.style.width = "100%";
      canvasFPS.style.height = "100%";
      containerFPS.appendChild(canvasFPS);

      this.graphCanvas = canvasFPS;
      this.graphCtx = canvasFPS.getContext("2d");

      // CPU Graph
      const containerCPU = document.getElementById("perf-graph-cpu");
      const canvasCPU = document.createElement("canvas");
      
      canvasCPU.width = (containerCPU.clientWidth || 240) * dpr;
      canvasCPU.height = 50 * dpr;
      canvasCPU.style.width = "100%";
      canvasCPU.style.height = "100%";
      containerCPU.appendChild(canvasCPU);

      this.cpuGraphCanvas = canvasCPU;
      this.cpuGraphCtx = canvasCPU.getContext("2d");

      window.addEventListener("resize", () => {
        const dpr = window.devicePixelRatio || 1;
        
        if (this.graphCanvas) {
          this.graphCanvas.width = (containerFPS.clientWidth || 240) * dpr;
          this.graphCanvas.height = 50 * dpr;
        }
        
        if (this.cpuGraphCanvas) {
          this.cpuGraphCanvas.width = (containerCPU.clientWidth || 240) * dpr;
          this.cpuGraphCanvas.height = 50 * dpr;
        }
      });
    },

    start: function () {
      if (this._raf) return;

      // Inicia medição de idle time
      this.startIdleCallback();

      const loop = () => {
        this._raf = requestAnimationFrame(loop);
        this.calculateFPS();
        
        // Atualiza display a cada 10 frames (~166ms em 60fps)
        this._displayUpdateCounter++;
        if (this._displayUpdateCounter >= 10) {
          this._displayUpdateCounter = 0;
          if (this.isVisible) {
            this.updateDisplay();
          }
        }
        
        // Atualiza gráfico a cada 3 frames
        this._updateCounter++;
        if (this._updateCounter >= 3) {
          this._updateCounter = 0;
          if (this.isVisible) {
            this.updateFPSGraph();
            this.updateCPUGraph();
          }
        }
      };
      this._raf = requestAnimationFrame(loop);
    },

    stop: function () {
      if (!this._raf) return;
      cancelAnimationFrame(this._raf);
      this._raf = 0;
      this.stopIdleCallback();
    },

    startIdleCallback: function () {
      if (!window.requestIdleCallback) return;
      
      // Garante que o array existe
      if (!this.idleTimes) this.idleTimes = [];
      
      const self = this; // Captura o contexto correto
      
      const measureIdle = (deadline) => {
        const idleTime = deadline.timeRemaining();
        self.idleTimes.push(idleTime);
        if (self.idleTimes.length > 30) self.idleTimes.shift();
        
        if (self.isEnabled) {
          self._idleCallbackId = requestIdleCallback(measureIdle);
        }
      };
      
      this._idleCallbackId = requestIdleCallback(measureIdle);
    },

    stopIdleCallback: function () {
      if (this._idleCallbackId) {
        cancelIdleCallback(this._idleCallbackId);
        this._idleCallbackId = null;
      }
    },

    calculateFPS: function () {
      const now = performance.now();
      const delta = now - this.lastTime;
      
      // Mede tempo de execução real de JS usando performance.now() de alta precisão
      const executionStart = performance.now();
      
      this.stats.fps = Math.round(1000 / delta);
      this.stats.frameTime = delta.toFixed(2);

      // Adiciona frame time ao buffer
      this.frameTimeSamples.push(delta);
      if (this.frameTimeSamples.length > 60) this.frameTimeSamples.shift();

      // Método 1: Usa PerformanceObserver para long tasks (>50ms)
      // Método 2: Calcula baseado em idle time disponível
      // Método 3: Estima pelo tempo de frame vs tempo ideal
      
      let cpuUsage = 0;
      
      // Se temos dados de idle time (mais preciso)
      if (this.idleTimes && this.idleTimes.length > 0) {
        const avgIdle = this.idleTimes.reduce((a, b) => a + b, 0) / this.idleTimes.length;
        const maxIdle = 16.67; // Máximo idle time possível em 60fps
        // Se idle é baixo, CPU está alta
        cpuUsage = Math.max(0, Math.min(100, 100 - (avgIdle / maxIdle * 100)));
      } else {
        // Fallback: usa frame time
        const avgFrameTime = this.frameTimeSamples.reduce((a, b) => a + b, 0) / this.frameTimeSamples.length;
        const targetFrameTime = 16.67; // 60 FPS
        cpuUsage = Math.min(100, Math.max(0, (avgFrameTime / targetFrameTime) * 100));
      }

      this.stats.cpu = Math.round(cpuUsage);
      
      // Tempo de execução desta função
      const executionTime = performance.now() - executionStart;
      if (!this.jsExecutionTimes) this.jsExecutionTimes = [];
      this.jsExecutionTimes.push(executionTime);
      if (this.jsExecutionTimes.length > 60) this.jsExecutionTimes.shift();

      this.lastTime = now;

      if (this._updateCounter === 0) {
        this.frames.push(this.stats.fps);
        if (this.frames.length > 100) this.frames.shift();
        
        this.cpuSamples.push(this.stats.cpu);
        if (this.cpuSamples.length > 100) this.cpuSamples.shift();
      }

      if (performance.memory) {
        this.stats.memory = (performance.memory.usedJSHeapSize / 1048576).toFixed(1);
        this.stats.maxMemory = (performance.memory.jsHeapSizeLimit / 1048576).toFixed(0);
      }
    },

    updateDisplay: function () {
      const fpsEl = document.getElementById("perf-fps");
      const frameTimeEl = document.getElementById("perf-frametime");
      const cpuEl = document.getElementById("perf-cpu");
      const memoryEl = document.getElementById("perf-memory");
      const indicatorEl = document.getElementById("perf-fps-indicator");
      const cpuIndicatorEl = document.getElementById("perf-cpu-indicator");
      if (!fpsEl || !frameTimeEl || !indicatorEl) return;

      fpsEl.textContent = this.stats.fps;
      frameTimeEl.textContent = this.stats.frameTime;

      if (this.stats.fps >= 55) indicatorEl.style.color = "#4ade80";
      else if (this.stats.fps >= 30) indicatorEl.style.color = "#fbbf24";
      else indicatorEl.style.color = "#ff4d4d";

      if (cpuEl && cpuIndicatorEl) {
        cpuEl.textContent = this.stats.cpu;
        
        if (this.stats.cpu <= 50) cpuIndicatorEl.style.color = "#4ade80";
        else if (this.stats.cpu <= 80) cpuIndicatorEl.style.color = "#fbbf24";
        else cpuIndicatorEl.style.color = "#ff4d4d";
      }

      if (performance.memory && memoryEl) {
        memoryEl.textContent = `${this.stats.memory} / ${this.stats.maxMemory}`;
      }
    },

    updateFPSGraph: function () {
      if (!this.graphCtx) return;

      const ctx = this.graphCtx;
      const width = this.graphCanvas.width;
      const height = this.graphCanvas.height;

      ctx.fillStyle = "rgba(0,0,0,0.25)";
      ctx.fillRect(0, 0, width, height);

      ctx.strokeStyle = "rgba(255,255,255,0.06)";
      ctx.lineWidth = 1;
      for (let i = 1; i < 4; i++) {
        const y = (height / 4) * i;
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(width, y);
        ctx.stroke();
      }

      // Determina escala dinâmica baseada no FPS máximo recente
      const maxRecentFPS = Math.max(...this.frames, 120);
      const maxScale = maxRecentFPS > 200 ? 600 : (maxRecentFPS > 120 ? 240 : 120);
      
      // Linha de referência em 60 FPS
      const fps60Y = height - (60 / maxScale) * height;
      ctx.strokeStyle = "rgba(61,120,255,0.4)";
      ctx.setLineDash([4, 4]);
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(0, fps60Y);
      ctx.lineTo(width, fps60Y);
      ctx.stroke();
      ctx.setLineDash([]);

      // Label para mostrar a escala
      ctx.fillStyle = "rgba(255,255,255,0.4)";
      ctx.font = `${10 * (window.devicePixelRatio || 1)}px monospace`;
      ctx.fillText(`60`, 5, fps60Y - 5);
      ctx.fillText(`${maxScale}`, 5, 15);

      if (this.frames.length > 1) {
        ctx.strokeStyle = "rgba(61,120,255,0.9)";
        ctx.lineWidth = 2.5;
        ctx.lineCap = "round";
        ctx.lineJoin = "round";
        ctx.beginPath();

        const step = width / (this.frames.length - 1);
        this.frames.forEach((fps, i) => {
          const x = i * step;
          const clampedFPS = Math.max(0, Math.min(fps, maxScale));
          const y = height - (clampedFPS / maxScale) * height;
          
          if (i === 0) ctx.moveTo(x, y);
          else ctx.lineTo(x, y);
        });

        ctx.stroke();

        ctx.globalAlpha = 0.15;
        ctx.fillStyle = "rgba(61,120,255,1)";
        ctx.lineTo(width, height);
        ctx.lineTo(0, height);
        ctx.closePath();
        ctx.fill();
        ctx.globalAlpha = 1.0;
      }
    },

    updateCPUGraph: function () {
      if (!this.cpuGraphCtx) return;

      const ctx = this.cpuGraphCtx;
      const width = this.cpuGraphCanvas.width;
      const height = this.cpuGraphCanvas.height;

      ctx.fillStyle = "rgba(0,0,0,0.25)";
      ctx.fillRect(0, 0, width, height);

      ctx.strokeStyle = "rgba(255,255,255,0.06)";
      ctx.lineWidth = 1;
      for (let i = 1; i < 4; i++) {
        const y = (height / 4) * i;
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(width, y);
        ctx.stroke();
      }

      // Linha de referência em 50% CPU
      const cpu50Y = height - (50 / 100) * height;
      ctx.strokeStyle = "rgba(255,120,61,0.4)";
      ctx.setLineDash([4, 4]);
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(0, cpu50Y);
      ctx.lineTo(width, cpu50Y);
      ctx.stroke();
      ctx.setLineDash([]);

      if (this.cpuSamples.length > 1) {
        ctx.strokeStyle = "rgba(255,120,61,0.9)";
        ctx.lineWidth = 2.5;
        ctx.lineCap = "round";
        ctx.lineJoin = "round";
        ctx.beginPath();

        const step = width / (this.cpuSamples.length - 1);
        this.cpuSamples.forEach((cpu, i) => {
          const x = i * step;
          const clampedCPU = Math.max(0, Math.min(cpu, 100));
          const y = height - (clampedCPU / 100) * height;
          
          if (i === 0) ctx.moveTo(x, y);
          else ctx.lineTo(x, y);
        });

        ctx.stroke();

        ctx.globalAlpha = 0.15;
        ctx.fillStyle = "rgba(255,120,61,1)";
        ctx.lineTo(width, height);
        ctx.lineTo(0, height);
        ctx.closePath();
        ctx.fill();
        ctx.globalAlpha = 1.0;
      }
    },
  };

  if (window.UnityModules) {
    window.UnityModules.register("performanceMonitor", PerformanceMonitor);
  }
  window.PerformanceMonitor = PerformanceMonitor;
})();