window.LoadingManager = (function () {
  const UNITY_PROGRESS_MAX = 0.8;
  const BOOT_PROGRESS_START = 0.8;
  const BOOT_PROGRESS_RANGE = 0.2;

  const state = {
    currentProgress: 0,
    targetProgress: 0,

    unityProgress: 0,
    bootProgress: 0,

    isInitializing: false,
    isComplete: false,

    smoothingSpeed: 0.08,
    lastUpdate: performance.now(),

    customStatus: null,
  };

  let animationFrame = null;
  let elements = null;

  function clamp01(value) {
    value = Number(value);
    if (Number.isNaN(value)) return 0;
    return Math.max(0, Math.min(1, value));
  }

  function initElements() {
    elements = {
      progressBar: document.getElementById("unity-progress-bar-full"),
      progressText: document.getElementById("unity-progress-text"),
      spinner: document.getElementById("unity-spinner"),
      loadingBar: document.getElementById("unity-loading-bar"),
      statusText: document.getElementById("unity-status-text"),
    };
  }

  function ensureAnimation() {
    if (!animationFrame && !state.isComplete) {
      state.lastUpdate = performance.now();
      animationFrame = requestAnimationFrame(updateProgress);
    }
  }

  function setTargetProgress(value) {
    const next = clamp01(value);

    // Nunca deixa a barra voltar para trás.
    state.targetProgress = Math.max(state.targetProgress, next);

    ensureAnimation();
    updateDisplay();
  }

  function updateProgress() {
    const now = performance.now();
    state.lastUpdate = now;

    if (state.currentProgress < state.targetProgress) {
      const diff = state.targetProgress - state.currentProgress;
      const step = Math.max(diff * state.smoothingSpeed, 0.0015);
      state.currentProgress = Math.min(state.currentProgress + step, state.targetProgress);
    }

    updateDisplay();

    if (!state.isComplete && state.currentProgress < 1) {
      animationFrame = requestAnimationFrame(updateProgress);
    } else {
      animationFrame = null;
    }
  }

  function getDefaultStatus() {
    if (state.customStatus) {
      return state.customStatus;
    }

    if (state.currentProgress < 0.25) {
      return "Loading assets...";
    }

    if (state.currentProgress < 0.55) {
      return "Loading resources...";
    }

    if (state.currentProgress < UNITY_PROGRESS_MAX) {
      return "Loading Unity...";
    }

    if (state.currentProgress < 1) {
      return "Preparing game...";
    }

    return "Ready!";
  }

  function updateDisplay() {
    if (!elements?.progressBar) return;

    const percentage = Math.floor(state.currentProgress * 100);

    elements.progressBar.style.width = `${percentage}%`;

    if (elements.progressText) {
      elements.progressText.textContent = `${percentage}%`;
    }

    if (elements.statusText) {
      elements.statusText.textContent = getDefaultStatus();
    }
  }

  // Unity loader progress: 0 até 80%.
  function setProgress(progress) {
    state.unityProgress = clamp01(progress);

    const mappedProgress = state.unityProgress * UNITY_PROGRESS_MAX;

    setTargetProgress(mappedProgress);
  }

  // Quando createUnityInstance chegou no fim, segura em 80%.
  function setInitializing(status) {
    state.isInitializing = true;

    if (status) {
      state.customStatus = status;
    } else {
      state.customStatus = "Initializing...";
    }

    setTargetProgress(UNITY_PROGRESS_MAX);
  }

  // Checklist do C#: 0 até 1 vira 80% até 100%.
  function setBootProgress(progress, status) {
    state.isInitializing = true;
    state.bootProgress = clamp01(progress);

    if (status) {
      state.customStatus = status;
    }

    const mappedProgress =
        BOOT_PROGRESS_START + state.bootProgress * BOOT_PROGRESS_RANGE;

    setTargetProgress(mappedProgress);
  }

  function setStatus(status) {
    state.customStatus = status || null;
    updateDisplay();
  }

  function complete() {
    state.isComplete = true;
    state.targetProgress = 1;
    state.currentProgress = 1;
    state.customStatus = "Ready!";

    updateDisplay();

    if (animationFrame) {
      cancelAnimationFrame(animationFrame);
      animationFrame = null;
    }

    setTimeout(() => {
      if (elements?.spinner) {
        elements.spinner.style.opacity = "0";
      }
    }, 300);
  }

  function reset() {
    state.currentProgress = 0;
    state.targetProgress = 0;
    state.unityProgress = 0;
    state.bootProgress = 0;
    state.isInitializing = false;
    state.isComplete = false;
    state.customStatus = null;

    if (animationFrame) {
      cancelAnimationFrame(animationFrame);
      animationFrame = null;
    }

    if (elements?.loadingBar) {
      elements.loadingBar.style.opacity = "1";
      elements.loadingBar.style.pointerEvents = "auto";
    }

    if (elements?.spinner) {
      elements.spinner.style.opacity = "1";
    }

    updateDisplay();
  }

  function init() {
    initElements();
    reset();
  }

  return {
    init,
    setProgress,
    setInitializing,
    setBootProgress,
    setStatus,
    complete,
    reset,
  };
})();