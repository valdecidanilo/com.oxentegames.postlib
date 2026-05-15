window.BootManager = (function () {
  let isProcessing = false;

  function setButtonsEnabled(enabled) {
    const btnGame = document.getElementById("btnLoadGame");
    const btnDev = document.getElementById("btnLoadDev");
    if (btnGame) btnGame.disabled = !enabled;
    if (btnDev) btnDev.disabled = !enabled;
  }

  function showMessage(text) {
    const el = document.getElementById("gateMessage");
    if (el) {
      el.textContent = text;
      el.style.display = text ? "block" : "none";
    } else if (text) {
      alert(text);
    }
  }

  function getPlayerName() {
    const input = document.getElementById("playerNameInput");
    return (input?.value ?? "").trim();
  }

  async function safeReadJson(res) {
    try {
      return await res.json();
    } catch {
      return null;
    }
  }

  async function getOrCreatePlayer(playerName) {
    const baseUrl = "http://localhost:8000";
    const encoded = encodeURIComponent(playerName);

    const getUrl = `${baseUrl}/operator/get-player/${encoded}`;
    const getRes = await fetch(getUrl, {
      method: "GET",
      headers: { Accept: "application/json" },
    });

    if (getRes.ok) {
      const data = await safeReadJson(getRes);
      window.playerData = data ?? null;
      return true;
    }

    if (getRes.status !== 404) {
      const errBody = await safeReadJson(getRes);
      console.warn("[BootManager] get-player failed:", getRes.status, errBody);
      return false;
    }

    const createUrl = `${baseUrl}/operator/create-player/${encoded}`;
    const createRes = await fetch(createUrl, {
      method: "GET",
      headers: { Accept: "application/json" },
    });

    if (!createRes.ok) {
      const errBody = await safeReadJson(createRes);
      console.warn("[BootManager] create-player failed:", createRes.status, errBody);
      return false;
    }

    const confirmRes = await fetch(getUrl, {
      method: "GET",
      headers: { Accept: "application/json" },
    });

    if (confirmRes.ok) {
      const data = await safeReadJson(confirmRes);
      window.playerData = data ?? null;
    } else {
      window.playerData = null;
    }

    return true;
  }

  async function ensurePlayerReady() {
    const playerName = getPlayerName();

    if (!playerName) {
      showMessage("Preencha seu nome antes de iniciar.");
      return false;
    }

    if (isProcessing) return false;

    isProcessing = true;
    setButtonsEnabled(false);
    showMessage("Verificando player...");

    try {
      const ok = await getOrCreatePlayer(playerName);

      if (!ok) {
        showMessage("NÃ£o foi possÃ­vel validar/criar o player. Tente novamente.");
        return false;
      }

      window.playerName = playerName;
      showMessage("");
      return true;
    } catch (err) {
      console.error("[BootManager] ensurePlayerReady error:", err);
      showMessage("Erro de conexÃ£o ao validar/criar o player. Tente novamente.");
      return false;
    } finally {
      isProcessing = false;
      setButtonsEnabled(true);
    }
  }

  async function startGameFlow({ withDevTools }) {
    const select = document.getElementById("apiVersionSelect");
    const GameIDSelect = document.getElementById("gameIDSelect");

    if (select) window.selectedApiVersion = select.value;
    if (GameIDSelect) window.selectedGame = GameIDSelect.value;

    const ok = await ensurePlayerReady();
    if (!ok) return;

    window.GateManager.hide();

    if (withDevTools) {
      await window.DevToolsManager.ensureLoaded();
    }

    window.UnityLoader.start();

    if (withDevTools) {
      setTimeout(() => {
        window.DevToolsManager.openAndFocus();
      }, 500);
    }
  }

  function init() {
    const playerNameInput = document.getElementById("playerNameInput");
    const btnGame = document.getElementById("btnLoadGame");
    const btnDev = document.getElementById("btnLoadDev");

    if (!window.IS_DEV_BUILD) {
      window.GateManager.hide();
      window.UnityLoader.start();
      return;
    }

    window.GateManager.show();
    window.GateManager.populateApiVersionSelect();
    window.GateManager.populateGameIDSelect();

    if (playerNameInput) {
      playerNameInput.addEventListener("input", (event) => {
        const input = event.target;
        window.playerName = (input.value ?? "").trim();
        showMessage("");
      });
    }

    btnGame?.addEventListener("click", () => {
      startGameFlow({ withDevTools: false });
    });

    btnDev?.addEventListener("click", () => {
      startGameFlow({ withDevTools: true });
    });
  }

  return { init };
})();

document.addEventListener("DOMContentLoaded", () => {
  window.BootManager.init();
});