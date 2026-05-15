window.GAME_CONFIG = {
  slotKombat: {
    label: "Slot Kombat",
    apiVersions: [
      { value: "v1", label: "API v1" }
    ],
    defaultApiVersion: "v1"
  },
  unknow: {
    label: "Unknown ???",
    apiVersions: [
      { value: "v1", label: "API v1" },
      { value: "v2", label: "API v2" }
    ],
    defaultApiVersion: "v1"
  }
};

window.selectedGame = "unknow";
window.selectedApiVersion = window.GAME_CONFIG[window.selectedGame].defaultApiVersion;

window.getApiVersionsForGame = function(gameId) {
  const game = window.GAME_CONFIG[gameId];
  return game ? game.apiVersions : [];
};

window.getDefaultApiVersionForGame = function(gameId) {
  const game = window.GAME_CONFIG[gameId];
  return game ? game.defaultApiVersion : "v1";
};