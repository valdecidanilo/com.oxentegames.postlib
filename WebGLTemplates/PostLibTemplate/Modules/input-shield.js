// Modules/input-shield.js
(function () {
  "use strict";

  const state = {
    enabled: false,
    sidebarId: "sidebar",
    toggleId: "sidebarToggle",
    unityContainerId: "unity-container",
    unityCanvasId: "unity-canvas",
  };

  function $(id) {
    return document.getElementById(id);
  }

  function pathContains(e, el) {
    if (!el) return false;
    const p = typeof e.composedPath === "function" ? e.composedPath() : null;
    if (p && Array.isArray(p)) return p.includes(el);
    // fallback
    return el.contains(e.target);
  }

  function isInsideSidebarTarget(e) {
    const sidebar = $(state.sidebarId);
    const toggle = $(state.toggleId);

    if (sidebar && pathContains(e, sidebar)) return true;
    if (toggle && pathContains(e, toggle)) return true;
    return false;
  }

  function isUnityTarget(e) {
    const canvas = $(state.unityCanvasId);
    const container = $(state.unityContainerId);

    // bloqueia só se o evento está indo para o canvas/container do unity
    if (canvas && pathContains(e, canvas)) return true;
    if (container && pathContains(e, container)) return true;
    return false;
  }

  function shieldKeyboard(e) {
    if (!state.enabled) return;

    // Se está digitando/clicando dentro do sidebar/toggle: deixa o browser agir,
    // mas impede o Unity de ouvir.
    if (isInsideSidebarTarget(e)) {
      e.stopImmediatePropagation();
      return; // NÃO preventDefault -> não quebra digitação
    }

    // Se painel aberto, corta teclado pro Unity mesmo fora do sidebar
    e.stopImmediatePropagation();

    // Evita scroll com espaço/setas quando painel aberto (opcional)
    const k = e.key;
    if (k === " " || k === "ArrowUp" || k === "ArrowDown" || k === "PageUp" || k === "PageDown") {
      e.preventDefault();
    }
  }

  function shieldPointer(e) {
    if (!state.enabled) return;

    // Nunca bloqueia cliques dentro do sidebar/toggle
    if (isInsideSidebarTarget(e)) return;

    // Só bloqueia se o alvo é Unity
    if (!isUnityTarget(e)) return;

    e.stopImmediatePropagation();
    e.preventDefault();
  }

  function shieldWheel(e) {
    if (!state.enabled) return;

    // scroll do sidebar funciona
    if (isInsideSidebarTarget(e)) return;

    // só bloqueia wheel se estiver em cima do Unity
    if (!isUnityTarget(e)) return;

    e.stopImmediatePropagation();
    e.preventDefault();
  }

  function shieldFocusIn(e) {
    if (!state.enabled) return;

    // se foco foi para algo fora do sidebar, traz de volta pro textarea
    if (isInsideSidebarTarget(e)) return;

    const ta = document.getElementById("postMessageJson");
    if (ta) ta.focus();
  }

  window.OpaInputShield = {
    enable() {
      state.enabled = true;

      // solta pointer lock se existir
      try { document.exitPointerLock?.(); } catch {}

      // se existir WebGLInput, desliga capture total do teclado
      try {
        if (window.WebGLInput && typeof window.WebGLInput.captureAllKeyboardInput !== "undefined") {
          window.WebGLInput.captureAllKeyboardInput = false;
        }
      } catch {}
    },
    disable() {
      state.enabled = false;

      // opcional: reabilitar capture total quando voltar pro jogo
      try {
        if (window.WebGLInput && typeof window.WebGLInput.captureAllKeyboardInput !== "undefined") {
          window.WebGLInput.captureAllKeyboardInput = true;
        }
      } catch {}
    },
    setIds(opts) {
      if (!opts) return;
      if (opts.sidebarId) state.sidebarId = opts.sidebarId;
      if (opts.toggleId) state.toggleId = opts.toggleId;
      if (opts.unityContainerId) state.unityContainerId = opts.unityContainerId;
      if (opts.unityCanvasId) state.unityCanvasId = opts.unityCanvasId;
    },
    isEnabled() {
      return state.enabled;
    },
  };

  // KEYBOARD (capture)
  window.addEventListener("keydown", shieldKeyboard, true);
  window.addEventListener("keyup", shieldKeyboard, true);
  window.addEventListener("keypress", shieldKeyboard, true);

  // IME/composição (acentos/celular)
  window.addEventListener("compositionstart", shieldKeyboard, true);
  window.addEventListener("compositionupdate", shieldKeyboard, true);
  window.addEventListener("compositionend", shieldKeyboard, true);

  // POINTER/WHEEL (capture)
  document.addEventListener("pointerdown", shieldPointer, true);
  document.addEventListener("mousedown", shieldPointer, true);
  document.addEventListener("touchstart", shieldPointer, true);
  document.addEventListener("wheel", shieldWheel, { capture: true, passive: false });

  // FOCUS
  document.addEventListener("focusin", shieldFocusIn, true);
})();
