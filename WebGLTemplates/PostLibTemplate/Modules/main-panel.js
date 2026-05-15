// Modules/main-panel.js
(function () {
  "use strict";

  const $ = (id) => document.getElementById(id);

  let keyShieldBound = false;
  let unityKeyboardCapturePrev = null;

  function isSidebarOpen() {
    const sidebar = $("sidebar");
    return !!sidebar && sidebar.classList.contains("open");
  }

  function isEditableTarget(el) {
    if (!el) return false;
    const tag = (el.tagName || "").toUpperCase();
    return tag === "TEXTAREA" || tag === "INPUT" || el.isContentEditable === true;
  }

  function getWebGLInput() {
    // Unity costuma expor WebGLInput global
    if (window.WebGLInput) return window.WebGLInput;

    // fallback: tenta pegar via unityInstance (se já existir)
    const ui = window.UnityModules?.unityInstance;
    if (ui && ui.Module && ui.Module.WebGLInput) return ui.Module.WebGLInput;

    return null;
  }

  function setUnityKeyboardCapture(enabled) {
    const wgi = getWebGLInput();
    if (!wgi || typeof wgi.captureAllKeyboardInput === "undefined") return;

    // salva o valor original na primeira vez que mexer
    if (unityKeyboardCapturePrev === null) {
      unityKeyboardCapturePrev = !!wgi.captureAllKeyboardInput;
    }

    wgi.captureAllKeyboardInput = !!enabled;
  }

  function setCanvasInputLocked(locked) {
    const canvas = $("unity-canvas");
    if (!canvas) return;

    canvas.style.pointerEvents = locked ? "none" : "auto";
    if (locked) {
      try { canvas.blur(); } catch {}
      try { canvas.setAttribute("tabindex", "-1"); } catch {}
    }
  }

  function bindKeyboardShieldOnce() {
    if (keyShieldBound) return;
    keyShieldBound = true;

    const shield = (e) => {
      if (!isSidebarOpen()) return;

      // Se o foco está em algo editável (textarea/input) dentro do sidebar,
      // corta o evento antes do Unity (SEM preventDefault)
      const sidebar = $("sidebar");
      const t = e.target || document.activeElement;

      if (sidebar && sidebar.contains(t) && isEditableTarget(t)) {
        e.stopImmediatePropagation();
        return;
      }

      // Se sidebar aberto e o foco não está em editável, ainda assim bloqueia o Unity
      // (mas sem matar comportamento normal do browser em outros elementos do painel)
      if (sidebar && sidebar.contains(t)) {
        e.stopImmediatePropagation();
        return;
      }
    };

    // capture=true pra ganhar do Unity
    window.addEventListener("keydown", shield, true);
    window.addEventListener("keyup", shield, true);
    window.addEventListener("keypress", shield, true);
  }

  function stopToUnity(e) {
    e.stopPropagation();
  }

  function bindAntiUnitySteal(sidebar, toggleBtn) {
    // Não use keydown aqui, deixa o teclado fluir pro textarea
    ["pointerdown", "mousedown", "touchstart", "click", "wheel"].forEach((evt) => {
      sidebar.addEventListener(evt, stopToUnity, false);
    });

    ["pointerdown", "mousedown", "touchstart"].forEach((evt) => {
      toggleBtn.addEventListener(evt, stopToUnity, true);
    });
  }
  function microtask(fn) {
    if (window.queueMicrotask) queueMicrotask(fn);
    else Promise.resolve().then(fn);
  }
  function focusPostMessage() {
    const ta = $("postMessageJson");
    if (!ta) return;

    // foco imediato (ainda dentro do clique)
    ta.focus({ preventScroll: true });

    // reforço em microtask (continua valendo como gesto do usuário em geral)
    microtask(() => ta.focus({ preventScroll: true }));

    // opcional: posiciona cursor no fim
    try {
      const len = ta.value?.length ?? 0;
      ta.setSelectionRange(len, len);
    } catch {}
  }

  function setSidebarOpen(isOpen) {
    const sidebar = $("sidebar");
    if (!sidebar) return;

    sidebar.classList.toggle("open", isOpen);
    sidebar.setAttribute("aria-hidden", isOpen ? "false" : "true");
    document.body.classList.toggle("sidebar-open", isOpen);

    setCanvasInputLocked(isOpen);

    if (isOpen) {
      setUnityKeyboardCapture(false);
      bindKeyboardShieldOnce();

      focusPostMessage();

      requestAnimationFrame(focusPostMessage);
      requestAnimationFrame(focusPostMessage);
    } else {
      if (unityKeyboardCapturePrev !== null) setUnityKeyboardCapture(unityKeyboardCapturePrev);
    }
  }

  function toggleSidebar() {
    setSidebarOpen(!isSidebarOpen());
  }

  function bindButtons() {
    const toggleBtn = $("sidebarToggle");
    const sidebar = $("sidebar");
    if (!toggleBtn || !sidebar) return;

    toggleBtn.style.pointerEvents = "auto";
    toggleBtn.style.zIndex = "10000";
    toggleBtn.style.position = "fixed";
    sidebar.style.zIndex = "9999";

    bindAntiUnitySteal(sidebar, toggleBtn);

    toggleBtn.addEventListener(
      "click",
      (e) => {
        e.stopPropagation();
        toggleSidebar();
      },
      true
    );
    

    // Fecha clicando fora
    document.addEventListener("click", (e) => {
      if (!isSidebarOpen()) return;
      const clickedInside = sidebar.contains(e.target) || toggleBtn.contains(e.target);
      if (!clickedInside) setSidebarOpen(false);
    });

    // ESC fecha
    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape" && isSidebarOpen()) setSidebarOpen(false);
    });

    // Botões do painel
    $("btnOpenDebug")?.addEventListener("click", () => {
      window.UnityModules?.enable("debugPanel");
      const mod = window.UnityModules?.registry?.debugPanel;
      if (mod && typeof mod.toggle === "function") mod.toggle();
      else if (mod && typeof mod.show === "function") mod.show();
    });

    $("btnTogglePerf")?.addEventListener("click", () => {
      window.UnityModules?.toggle("performanceMonitor");
    });

    $("btnToggleHud")?.addEventListener("click", () => {
      window.UnityModules?.toggle("customUI");
      if (window.CustomUI && typeof window.CustomUI.toggleHUD === "function") window.CustomUI.toggleHUD();
    });

    $("btnSendPostMessage")?.addEventListener("click", () => {
      const ta = $("postMessageJson");
      if (!ta) return;
      window.UnityModules?.sendToUnity("GameManager", "OnDevPostMessage", ta.value);
    });

    $("pauseBtn")?.addEventListener("click", () => {
      window.UnityModules?.sendToUnity("GameManager", "TogglePause", "");
    });

    $("autoBtn")?.addEventListener("click", () => {
      window.UnityModules?.sendToUnity("GameManager", "StopAutoplay", "");
    });

    $("resolutionSelect")?.addEventListener("change", (e) => {
      const canvas = $("unity-canvas");
      if (!canvas) return;

    canvas?.addEventListener("focus", () => {
      if (isSidebarOpen()) {
        try { canvas.blur(); } catch {}
        focusPostMessage();
      }
    }, true);

      const value = e.target.value;
      if (value === "auto") {
        canvas.style.width = "100%";
        canvas.style.height = "100%";
        canvas.style.margin = "0";
        canvas.style.display = "block";
        return;
      }

      const [w, h] = value.split("x").map((n) => parseInt(n, 10));
      if (!w || !h) return;

      canvas.style.width = w + "px";
      canvas.style.height = h + "px";
      canvas.style.margin = "0 auto";
      canvas.style.display = "block";
    });

    // começa fechado
    setSidebarOpen(false);
  }
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", bindButtons);
  } else {
    bindButtons();
  }
  window.__setSidebarOpen = setSidebarOpen;
})();
