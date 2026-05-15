window.GateManager = (function() {
  function show() {
    const gate = document.getElementById("bootGate");
    if (!gate) return;

    document.body.appendChild(gate);
    gate.style.display = "flex";
    document.body.classList.add("gate-open");
  }

  function hide() {
    const gate = document.getElementById("bootGate");
    if (gate) gate.remove();
    document.body.classList.remove("gate-open");
  }

  function populateGameIDSelect() {
    const gameSelect = document.getElementById("gameIDSelect");
    if (!gameSelect) return;

    gameSelect.innerHTML = "";

    const games = Object.keys(window.GAME_CONFIG || {});
    
    if (games.length === 0) {
      console.warn("[Gate] Nenhum jogo configurado em GAME_CONFIG");
      return;
    }

    games.forEach((gameId, index) => {
      const gameConfig = window.GAME_CONFIG[gameId];
      const option = document.createElement("option");
      option.value = gameId;
      option.textContent = gameConfig.label;
      if (index === 0) option.selected = true;
      gameSelect.appendChild(option);
    });

    gameSelect.addEventListener("change", (e) => {
      window.selectedGame = e.target.value;
      //console.log(`[Gate] Game selecionado: ${window.selectedGame}`);
      
      populateApiVersionSelect();
    });

    if (gameSelect.value) {
      window.selectedGame = gameSelect.value;
    }
  }

  function populateApiVersionSelect() {
    const select = document.getElementById("apiVersionSelect");
    if (!select) return;

    select.innerHTML = "";

    const currentGame = window.selectedGame || Object.keys(window.GAME_CONFIG || {})[0];
    
    if (!currentGame || !window.GAME_CONFIG[currentGame]) {
      //console.warn("[Gate] Jogo não encontrado na configuração:", currentGame);
      return;
    }

    const gameConfig = window.GAME_CONFIG[currentGame];
    const versions = gameConfig.apiVersions;

    versions.forEach((version, index) => {
      const option = document.createElement("option");
      option.value = version.value;
      option.textContent = version.label;
      
      if (version.value === gameConfig.defaultApiVersion) {
        option.selected = true;
      }
      
      select.appendChild(option);
    });

    select.addEventListener("change", (e) => {
      window.selectedApiVersion = e.target.value;
      //console.log(`[Gate] API Version selecionada: ${window.selectedApiVersion} para ${currentGame}`);
    });

    if (select.value) {
      window.selectedApiVersion = select.value;
    } else {
      window.selectedApiVersion = gameConfig.defaultApiVersion;
    }
  }

  return {
    show,
    hide,
    populateGameIDSelect,
    populateApiVersionSelect
  };
})();