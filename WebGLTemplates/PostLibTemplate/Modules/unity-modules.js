(function () {
  const state = {
    debugPanel: false,
    performanceMonitor: false,
    customUI: false,
  };

  window.UnityModules = {
    state,
    unityInstance: null,
    registry: {},
    _queue: [],

    register(name, module) {
      this.registry[name] = module;
      //console.log(`[UnityModules] Módulo registrado: ${name}`);
    },

    isEnabled(name) {
      return !!this.state[name];
    },

    _ensureMounted(name) {
      const mod = this.registry[name];
      if (!mod) return null;

      if (!mod.__mounted) {
        mod.__mounted = true;
        if (typeof mod.mount === "function") {
          try { mod.mount(); } catch (e) { console.error(e); }
        } else if (typeof mod.initEarly === "function") {
          try { mod.initEarly(); } catch (e) { console.error(e); }
        }
      }
      return mod;
    },

    enable(name) {
      this.state[name] = true;
      const mod = this._ensureMounted(name);
      if (!mod) return;

      if (this.unityInstance && !mod.__initedWithUnity && typeof mod.init === "function") {
        mod.__initedWithUnity = true;
        try { mod.init(this.unityInstance); } catch (e) { console.error(e); }
      }

      if (typeof mod.enable === "function") {
        try { mod.enable(); } catch (e) { console.error(e); }
      } else if (typeof mod.show === "function") {
        try { mod.show(); } catch (e) { console.error(e); }
      }
    },

    disable(name) {
      this.state[name] = false;
      const mod = this.registry[name];
      if (!mod) return;

      if (typeof mod.disable === "function") {
        try { mod.disable(); } catch (e) { console.error(e); }
      } else if (typeof mod.hide === "function") {
        try { mod.hide(); } catch (e) { console.error(e); }
      } else if (mod.panel) {
        mod.panel.style.display = "none";
      } else if (mod.widget) {
        mod.widget.style.display = "none";
      }
    },

    toggle(name) {
      this.isEnabled(name) ? this.disable(name) : this.enable(name);
    },

    init(unityInstance) {
      this.unityInstance = unityInstance;
      //console.log("[UnityModules] Unity conectado.");

      this.sendToUnity("LoaderSettings", "ApplyServerConfig", JSON.stringify({
        version: selectedApiVersion,
        baseUrl: "http://localhost:8000",
        gameName: "unknow"
      }));

      for (const [name, isOn] of Object.entries(this.state)) {
        if (!isOn) continue;

        const mod = this._ensureMounted(name);
        if (!mod) continue;

        if (!mod.__initedWithUnity && typeof mod.init === "function") {
          mod.__initedWithUnity = true;
          try { mod.init(unityInstance); } catch (e) { console.error(e); }
        }
      }

      const queued = this._queue.splice(0);
      for (const m of queued) {
        try { this.unityInstance.SendMessage(m.go, m.method, m.value ?? ""); } catch (e) { }
      }
    },

    sendToUnity(go, method, value) {
      if (this.unityInstance) this.unityInstance.SendMessage(go, method, value ?? "");
      else this._queue.push({ go, method, value });
    },
  };
})();