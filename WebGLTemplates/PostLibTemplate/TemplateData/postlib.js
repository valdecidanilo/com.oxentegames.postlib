window.addEventListener("message", (event) => {
    const data = event.data;
    if (!data || typeof data !== "object" || !data._type) return;
    console.log("[PostLib JS] Recebido Unity JS:", response);
    event.source.postMessage(response, "*");
});