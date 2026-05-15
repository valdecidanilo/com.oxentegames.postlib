window.UnityLoader = (function () {
  let unityStarting = false;
  let unloadCleanupRegistered = false;

  const SAFARI_STORAGE_CLEANUP_BYTES = 512 * 1024 * 1024;
  const SAFARI_STORAGE_CLEANUP_RATIO = 0.75;

  function isIOSWebKit() {
    const ua = navigator.userAgent || "";
    return /iPhone|iPad|iPod/i.test(ua) || (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1);
  }

  function isSafariLikeMemoryTarget() {
    const ua = navigator.userAgent || "";
    const isSafari = /Safari/i.test(ua) && !/Chrome|CriOS|Chromium|FxiOS|EdgiOS|OPiOS/i.test(ua);
    return isSafari || isIOSWebKit();
  }

  function loseCanvasContext(canvas) {
    if (!canvas) return;

    try {
      const gl = canvas.getContext("webgl2") || canvas.getContext("webgl") || canvas.getContext("experimental-webgl");
      const loseContext = gl?.getExtension?.("WEBGL_lose_context");
      loseContext?.loseContext();
    } catch {}
  }

  function releaseUnityMemory(canvas) {
    const unityInstance = window.unityInstance || window.UnityModules?.unityInstance;

    loseCanvasContext(canvas);

    if (unityInstance) {
      try {
        const quitResult = unityInstance.Quit?.();
        quitResult?.catch?.(() => {});
      } catch {}
    }

    try { window.UnityModules.unityInstance = null; } catch {}
    try { window.unityInstance = null; } catch {}
  }

  function registerUnloadCleanup(canvas) {
    if (unloadCleanupRegistered) return;
    unloadCleanupRegistered = true;

    const cleanup = () => releaseUnityMemory(canvas);
    window.addEventListener("pagehide", cleanup, { once: true });
    window.addEventListener("beforeunload", cleanup, { once: true });
  }

  function shouldForceStorageCleanup() {
    try {
      const params = new URLSearchParams(window.location.search);
      return params.has("clearUnityCache") || params.has("clearWebGLCache") || params.has("clearBrowserCache");
    } catch {
      return false;
    }
  }

  function shouldDisableUnityCache() {
    try {
      const params = new URLSearchParams(window.location.search);
      if (params.has("useUnityCache")) return false;
      return isSafariLikeMemoryTarget() || params.has("noUnityCache");
    } catch {
      return isSafariLikeMemoryTarget();
    }
  }

  async function getStorageEstimate() {
    try {
      if (!navigator.storage?.estimate) return null;
      return await navigator.storage.estimate();
    } catch {
      return null;
    }
  }

  async function clearSameOriginCacheStorage() {
    if (!window.caches?.keys) return 0;

    const cacheNames = await caches.keys();
    await Promise.all(cacheNames.map((name) => caches.delete(name)));
    return cacheNames.length;
  }

  async function deleteIndexedDBDatabase(name) {
    return await new Promise((resolve) => {
      try {
        const request = indexedDB.deleteDatabase(name);
        request.onsuccess = () => resolve(true);
        request.onerror = () => resolve(false);
        request.onblocked = () => resolve(false);
      } catch {
        resolve(false);
      }
    });
  }

  async function clearSameOriginIndexedDB() {
    if (!window.indexedDB || typeof indexedDB.databases !== "function") return 0;

    const databases = await indexedDB.databases();
    let deleted = 0;

    for (const database of databases) {
      if (!database?.name) continue;
      if (await deleteIndexedDBDatabase(database.name)) deleted++;
    }

    return deleted;
  }

  async function prepareSafariStorageForUnity() {
    const forceCleanup = shouldForceStorageCleanup();

    if (!forceCleanup && !isSafariLikeMemoryTarget()) return;

    try {
      if (!forceCleanup && sessionStorage.getItem("unitySafariStorageCleaned") === "1") return;
    } catch {}

    const estimate = await getStorageEstimate();
    const usage = estimate?.usage ?? 0;
    const quota = estimate?.quota ?? 0;
    const usageRatio = quota > 0 ? usage / quota : 0;
    const shouldCleanup = forceCleanup || usage >= SAFARI_STORAGE_CLEANUP_BYTES || usageRatio >= SAFARI_STORAGE_CLEANUP_RATIO;

    if (!shouldCleanup) return;

    try { sessionStorage.setItem("unitySafariStorageCleaned", "1"); } catch {}

    const deletedCaches = await clearSameOriginCacheStorage();
    const deletedDatabases = await clearSameOriginIndexedDB();

    console.info("[UnityLoader] Same-origin browser storage cleanup completed.", {
      forced: forceCleanup,
      usage,
      quota,
      deletedCaches,
      deletedDatabases,
    });
  }

  function showMessage(text, kind = "error") {
    const box = document.getElementById("unityPopup");
    const label = document.getElementById("unityPopupText");

    if (!text) {
      if (box) box.style.display = "none";
      return;
    }

    if (!box || !label) {
      alert(text);
      return;
    }

    label.textContent = text;
    box.style.display = "block";

    if (kind === "success") {
      box.style.background = "#0f2b16";
      box.style.color = "#d7ffe0";
      box.style.border = "1px solid #5bff7a";
    } else {
      box.style.background = "#2b0f12";
      box.style.color = "#ffd7db";
      box.style.border = "1px solid #ff5b6e";
    }
  }

  function showStartButton() {
    const btnStartGame = document.getElementById("btnStartGame");
    const loadingBar = document.getElementById("unity-loading-bar");

    window.LoadingManager?.complete();

    setTimeout(() => {
      if (loadingBar) {
        loadingBar.style.opacity = "0";
        loadingBar.style.pointerEvents = "none";
      }
      
      if (btnStartGame) {
        btnStartGame.style.display = "block";
        setTimeout(() => {
          btnStartGame.classList.add("visible");
        }, 50);
      }
    }, 500);
  }

  window.OnUnityGameReady = function () {
    //console.log("[WebGL] Unity confirmou Game Ready ✅");
    showMessage("", "success");
    showStartButton();
  };

  function start() {
    if (unityStarting) return;
    unityStarting = true;

    const canvas = document.getElementById("unity-canvas");
    const overlay = document.getElementById("overlay-content");
    const loadingBar = document.getElementById("unity-loading-bar");

    const btnStartGame = document.getElementById("btnStartGame");
    const logo = document.getElementById("unity-logo");
    const prod = document.getElementById("production-name");

    if (!canvas || !overlay) {
      showMessage("Erro: elementos do loader não encontrados no HTML.", "error");
      return;
    }

    window.LoadingManager?.init();

    try { window.CoverManager?.apply(); } catch {}
    overlay.style.display = "flex";

    if (loadingBar) {
      loadingBar.style.display = "block";
      loadingBar.style.opacity = "1";
    }
    if (btnStartGame) {
      btnStartGame.style.display = "none";
      btnStartGame.classList.remove("visible");
    }
    showMessage("", "success");

    if (btnStartGame) {
      btnStartGame.onclick = () => {
        overlay.style.display = "none";
        try { window.CoverManager?.remove(); } catch {}
        try { 
          window.UnityModules?.sendToUnity("LoaderSettings", "OnStartClicked", "");
         } catch {}
      };
    }

    const buildUrl = "Build";
    const loaderUrl = buildUrl + "/{{{ LOADER_FILENAME }}}";

    const config = {
      dataUrl: buildUrl + "/{{{ DATA_FILENAME }}}",
      frameworkUrl: buildUrl + "/{{{ FRAMEWORK_FILENAME }}}",
      codeUrl: buildUrl + "/{{{ CODE_FILENAME }}}",
      streamingAssetsUrl: "StreamingAssets",
      companyName: {{{ JSON.stringify(COMPANY_NAME) }}},
      productName: {{{ JSON.stringify(PRODUCT_NAME) }}},
      productVersion: {{{ JSON.stringify(PRODUCT_VERSION) }}},
      showBanner: null,
    };

    if (shouldDisableUnityCache()) {
      config.cacheControl = function () {
        return "no-store";
      };
    }

    if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
      const meta = document.createElement("meta");
      meta.name = "viewport";
      meta.content =
        "width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes";
      document.head.appendChild(meta);

      canvas.className = "unity-mobile";
      config.devicePixelRatio = 2;
    } else {
      canvas.style.width = "100%";
      canvas.style.height = "100%";
    }

    const script = document.createElement("script");
    script.src = loaderUrl;

    script.onload = async () => {
      if (typeof createUnityInstance !== "function") {
        showMessage("Loader carregou, mas createUnityInstance não existe. Confira Build/ e LOADER_FILENAME.", "error");
        return;
      }

      try {
        await prepareSafariStorageForUnity();
      } catch (err) {
        console.warn("[UnityLoader] Browser storage cleanup skipped:", err);
      }

      createUnityInstance(canvas, config, (progress) => {
        window.LoadingManager?.setProgress(progress);

        if (progress >= 0.9) {
          window.LoadingManager?.setInitializing();
        }
      })
          .then((unityInstance) => {
            try { window.unityInstance = unityInstance; } catch {}
            try { window.UnityModules?.init(unityInstance); } catch {}
            registerUnloadCleanup(canvas);

            window.LoadingManager?.setInitializing("Connecting to server...");

            try {
              if (typeof unityInstance.SendMessage === "function" && window.regulation !== undefined) {
                unityInstance.SendMessage("LoaderSettings", "ReceivedRegulation", window.regulation);
              }
            } catch {}

            try {
              unityInstance.SendMessage("UserAgent", "GetUser");
            } catch {}
          })
          .catch((message) => {
            showMessage(String(message || "Falha ao iniciar Unity WebGL."), "error");
            releaseUnityMemory(canvas);
          });
    };

    script.onerror = () => {
      showMessage("Falha ao carregar o Unity Loader. Verifique Build/ e rode por HTTP (não file://).", "error");
    };

    document.body.appendChild(script);
  }
  function hideWebGLLoadingOverlay() {
    const overlay = document.getElementById("overlay-content");
    const loadingBar = document.getElementById("unity-loading-bar");
    const spinner = document.getElementById("unity-spinner");
    const cover = document.getElementById("cover-overlay");
    const popup = document.getElementById("unityPopup");

    if (overlay) {
      overlay.style.opacity = "0";
      overlay.style.pointerEvents = "none";
      overlay.style.display = "none";
    }

    if (loadingBar) {
      loadingBar.style.opacity = "0";
      loadingBar.style.pointerEvents = "none";
      loadingBar.style.display = "none";
    }

    if (spinner) {
      spinner.style.opacity = "0";
      spinner.style.display = "none";
    }

    if (popup) {
      popup.style.display = "none";
    }

    if (cover) {
      cover.classList.remove("cover-active");
      cover.classList.add("cover-hidden");
      cover.style.opacity = "0";
      cover.style.pointerEvents = "none";
    }

    try { window.CoverManager?.remove(); } catch {}
  }

  window.WebGLLoading_HideOverlayForUnityPopup = hideWebGLLoadingOverlay;
  
  return { start };
})();
