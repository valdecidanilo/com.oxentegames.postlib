/* ===================================================================== */
/*  PostLib Core – permanece em builds Release                           */
/* ===================================================================== */
(function () {
  const GAME_FEATURES = [
    "gamestatus",
    ["openpage", { delegatePages: ["deposit", "game_history"] }],
    ["sound",    { mute: false, level: 50 }]
  ];

  /* ---------------- API pública -------------------------------------- */
  const PostLib = {
    /** dispara quando qualquer mensagem chega do Unity */
    onMessage: new Set(),

    /** use para enviar mensagens p/ Unity */
    send(msgObj) {
      window.postMessage(msgObj, "*");
    },

    /** atalho p/ iniciar handshake */
    sendInitializationResponse(version = "1.0.0") {
      this.send({
        _type: "ucip.basic.w2gInitializationResponse",
        version,
        features: GAME_FEATURES.map(f => Array.isArray(f) ? f[0] : f)
      });
    }
  };
  window.PostLib = PostLib;        // expõe globalmente

  /* -------- Listener que SEMPRE fica ativo --------------------------- */
  window.addEventListener("message", (event) => {
    const data = event.data;
    if (!data || typeof data !== "object" || !data._type) return;

    // 1) Responde ao InitializationRequest
    if (data._type === "ucip.basic.g2wInitializationRequest") {
      console.log("[PostLib] InitializationRequest recebido");
      PostLib.sendInitializationResponse(data.version);
      return;
    }

    // 2) Notifica quem estiver inscrito
    PostLib.onMessage.forEach(cb => {
      try { cb(data); } catch (e) { console.error(e); }
    });
  });
})();
