/*
const GAME_FEATURES = [
    "gamestatus",
    ["openpage", { delegatePages: ["deposit", "game_history"] }],
    ["sound", { mute: false, level: 50 }]
];
window.addEventListener("message", (event) => {
    const data = event.data;
    if (!data || typeof data !== "object" || !data._type) return;

    if (data._type === "ucip.basic.g2wInitializationRequest") {
        const response = {
            _type: "ucip.basic.w2gInitializationResponse",
            version: data.version || "1.0.0",
            features: GAME_FEATURES.map(f => Array.isArray(f) ? f[0] : f)
        };
        setTimeout(() => {
            event.source.postMessage(response, "*");
            console.log("[PostLib JS] Initialization enviado:", response);
        }, 100);
    }
});
*/