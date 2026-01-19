/**
 * Custom UI Module
 * Sistema de UI customizada para overlay no Unity WebGL
 * Salve como: Modules/CustomUI/custom-ui.js
 */

(function() {
  'use strict';
  
  const CustomUI = {
    container: null,
    widgets: {},
    notifications: [],
    isInitialized: false,
    
    /**
     * Inicializa o módulo
     */
    init: function(unityInstance) {
      this.createContainer();
      this.createDefaultWidgets();
      this.setupStyles();
      this.isInitialized = true;
      
      console.log('[CustomUI] ✓ Inicializado');
      
      // Exemplo de uso
      this.showNotification('Custom UI carregada!', 'success');
    },
    
    /**
     * Cria container principal
     */
    createContainer: function() {
      const container = document.createElement('div');
      container.id = 'custom-ui-container';
      container.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        pointer-events: none;
        z-index: 9000;
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif;
      `;
      
      document.body.appendChild(container);
      this.container = container;
    },
    
    /**
     * Cria widgets padrão
     */
    createDefaultWidgets: function() {
      // Área de notificações
      this.createNotificationArea();
      
      // HUD de exemplo
      this.createExampleHUD();
      
      // Menu de contexto
      this.createContextMenu();
      
      // Loading overlay
      this.createLoadingOverlay();
      
      // Toast messages
      this.createToastContainer();
    },
    
    /**
     * Área de notificações
     */
    createNotificationArea: function() {
      const area = document.createElement('div');
      area.id = 'notification-area';
      area.style.cssText = `
        position: fixed;
        top: 60px;
        right: 20px;
        width: 320px;
        max-height: 80vh;
        overflow-y: auto;
        pointer-events: auto;
        z-index: 9100;
      `;
      
      this.container.appendChild(area);
      this.widgets.notificationArea = area;
    },
    
    /**
     * HUD de exemplo
     */
    createExampleHUD: function() {
      const hud = document.createElement('div');
      hud.id = 'example-hud';
      hud.style.cssText = `
        position: fixed;
        bottom: 20px;
        left: 20px;
        background: rgba(0, 0, 0, 0.7);
        backdrop-filter: blur(10px);
        border: 2px solid rgba(255, 255, 255, 0.1);
        border-radius: 12px;
        padding: 15px 20px;
        color: white;
        pointer-events: auto;
        min-width: 200px;
        box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
      `;
      
      hud.innerHTML = `
        <div style="font-size: 12px; color: rgba(255, 255, 255, 0.6); margin-bottom: 8px; font-weight: 500;">PLAYER INFO</div>
        
        <div style="margin-bottom: 10px;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 5px;">
            <span style="font-size: 11px; color: rgba(255, 255, 255, 0.7);">Health</span>
            <span id="hud-health-value" style="font-weight: bold; color: #4ade80;">100</span>
          </div>
          <div style="height: 6px; background: rgba(255, 255, 255, 0.1); border-radius: 3px; overflow: hidden;">
            <div id="hud-health-bar" style="width: 100%; height: 100%; background: linear-gradient(90deg, #4ade80, #22c55e); transition: width 0.3s;"></div>
          </div>
        </div>
        
        <div style="margin-bottom: 10px;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 5px;">
            <span style="font-size: 11px; color: rgba(255, 255, 255, 0.7);">Mana</span>
            <span id="hud-mana-value" style="font-weight: bold; color: #60a5fa;">80</span>
          </div>
          <div style="height: 6px; background: rgba(255, 255, 255, 0.1); border-radius: 3px; overflow: hidden;">
            <div id="hud-mana-bar" style="width: 80%; height: 100%; background: linear-gradient(90deg, #60a5fa, #3b82f6); transition: width 0.3s;"></div>
          </div>
        </div>
        
        <div style="display: flex; gap: 15px; font-size: 11px; color: rgba(255, 255, 255, 0.7);">
          <div>
            <span style="color: rgba(255, 255, 255, 0.5);">XP:</span> 
            <span id="hud-xp" style="color: #fbbf24; font-weight: bold;">1250</span>
          </div>
          <div>
            <span style="color: rgba(255, 255, 255, 0.5);">Level:</span> 
            <span id="hud-level" style="color: #a78bfa; font-weight: bold;">12</span>
          </div>
        </div>
      `;
      
      this.container.appendChild(hud);
      this.widgets.hud = hud;
    },
    
    /**
     * Menu de contexto
     */
    createContextMenu: function() {
      const menu = document.createElement('div');
      menu.id = 'context-menu';
      menu.style.cssText = `
        position: fixed;
        background: rgba(20, 20, 20, 0.95);
        backdrop-filter: blur(10px);
        border: 1px solid rgba(255, 255, 255, 0.1);
        border-radius: 8px;
        padding: 8px;
        min-width: 180px;
        box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5);
        display: none;
        pointer-events: auto;
        z-index: 9200;
      `;
      
      this.container.appendChild(menu);
      this.widgets.contextMenu = menu;
      
      // Fechar ao clicar fora
      document.addEventListener('click', () => {
        menu.style.display = 'none';
      });
    },
    
    /**
     * Loading overlay
     */
    createLoadingOverlay: function() {
      const overlay = document.createElement('div');
      overlay.id = 'loading-overlay';
      overlay.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0, 0, 0, 0.8);
        backdrop-filter: blur(5px);
        display: none;
        justify-content: center;
        align-items: center;
        pointer-events: auto;
        z-index: 9500;
      `;
      
      overlay.innerHTML = `
        <div style="text-align: center; color: white;">
          <div class="spinner" style="
            width: 60px;
            height: 60px;
            border: 4px solid rgba(255, 255, 255, 0.1);
            border-top-color: #3b82f6;
            border-radius: 50%;
            animation: spin 1s linear infinite;
            margin: 0 auto 20px;
          "></div>
          <div id="loading-text" style="font-size: 18px; font-weight: 500;">Loading...</div>
          <div id="loading-subtext" style="font-size: 14px; color: rgba(255, 255, 255, 0.6); margin-top: 8px;">Please wait</div>
        </div>
      `;
      
      this.container.appendChild(overlay);
      this.widgets.loadingOverlay = overlay;
    },
    
    /**
     * Container de toasts
     */
    createToastContainer: function() {
      const container = document.createElement('div');
      container.id = 'toast-container';
      container.style.cssText = `
        position: fixed;
        bottom: 20px;
        right: 20px;
        display: flex;
        flex-direction: column;
        gap: 10px;
        pointer-events: auto;
        z-index: 9300;
      `;
      
      this.container.appendChild(container);
      this.widgets.toastContainer = container;
    },
    
    /**
     * Estilos globais
     */
    setupStyles: function() {
      const style = document.createElement('style');
      style.textContent = `
        @keyframes spin {
          to { transform: rotate(360deg); }
        }
        
        @keyframes slideInRight {
          from {
            transform: translateX(400px);
            opacity: 0;
          }
          to {
            transform: translateX(0);
            opacity: 1;
          }
        }
        
        @keyframes slideOutRight {
          from {
            transform: translateX(0);
            opacity: 1;
          }
          to {
            transform: translateX(400px);
            opacity: 0;
          }
        }
        
        @keyframes fadeIn {
          from { opacity: 0; }
          to { opacity: 1; }
        }
        
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }
        
        .custom-ui-notification {
          animation: slideInRight 0.3s ease-out;
        }
        
        .custom-ui-notification.removing {
          animation: slideOutRight 0.3s ease-in;
        }
        
        .custom-ui-toast {
          animation: slideInRight 0.3s ease-out;
        }
        
        #custom-ui-container *::-webkit-scrollbar {
          width: 8px;
        }
        
        #custom-ui-container *::-webkit-scrollbar-track {
          background: rgba(0, 0, 0, 0.2);
          border-radius: 4px;
        }
        
        #custom-ui-container *::-webkit-scrollbar-thumb {
          background: rgba(255, 255, 255, 0.3);
          border-radius: 4px;
        }
        
        #custom-ui-container *::-webkit-scrollbar-thumb:hover {
          background: rgba(255, 255, 255, 0.5);
        }
      `;
      
      document.head.appendChild(style);
    },
    
    /**
     * ═══════════════════════════════════════════════
     * API PÚBLICA
     * ═══════════════════════════════════════════════
     */
    
    /**
     * Mostra notificação
     */
    showNotification: function(message, type = 'info', duration = 5000) {
      const colors = {
        success: { bg: '#10b981', border: '#059669' },
        error: { bg: '#ef4444', border: '#dc2626' },
        warning: { bg: '#f59e0b', border: '#d97706' },
        info: { bg: '#3b82f6', border: '#2563eb' }
      };
      
      const icons = {
        success: '✓',
        error: '✕',
        warning: '⚠',
        info: 'ℹ'
      };
      
      const color = colors[type] || colors.info;
      const icon = icons[type] || icons.info;
      
      const notification = document.createElement('div');
      notification.className = 'custom-ui-notification';
      notification.style.cssText = `
        background: ${color.bg};
        border-left: 4px solid ${color.border};
        color: white;
        padding: 15px 20px;
        border-radius: 8px;
        margin-bottom: 10px;
        box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
        display: flex;
        align-items: center;
        gap: 12px;
        min-width: 280px;
        max-width: 320px;
        cursor: pointer;
      `;
      
      notification.innerHTML = `
        <div style="font-size: 24px; font-weight: bold;">${icon}</div>
        <div style="flex: 1;">
          <div style="font-weight: 600; margin-bottom: 2px;">${type.toUpperCase()}</div>
          <div style="font-size: 14px; opacity: 0.9;">${message}</div>
        </div>
        <button style="background: none; border: none; color: white; font-size: 20px; cursor: pointer; padding: 0; opacity: 0.7;">×</button>
      `;
      
      const area = this.widgets.notificationArea;
      area.appendChild(notification);
      
      // Remover ao clicar no X
      const closeBtn = notification.querySelector('button');
      closeBtn.onclick = (e) => {
        e.stopPropagation();
        this.removeNotification(notification);
      };
      
      // Remover ao clicar na notificação
      notification.onclick = () => {
        this.removeNotification(notification);
      };
      
      // Auto-remover
      if (duration > 0) {
        setTimeout(() => {
          this.removeNotification(notification);
        }, duration);
      }
      
      return notification;
    },
    
    /**
     * Remove notificação com animação
     */
    removeNotification: function(notification) {
      notification.classList.add('removing');
      setTimeout(() => {
        if (notification.parentNode) {
          notification.parentNode.removeChild(notification);
        }
      }, 300);
    },
    
    /**
     * Toast message rápida
     */
    showToast: function(message, duration = 3000) {
      const toast = document.createElement('div');
      toast.className = 'custom-ui-toast';
      toast.style.cssText = `
        background: rgba(0, 0, 0, 0.9);
        color: white;
        padding: 12px 20px;
        border-radius: 8px;
        font-size: 14px;
        box-shadow: 0 4px 20px rgba(0, 0, 0, 0.5);
        backdrop-filter: blur(10px);
      `;
      toast.textContent = message;
      
      this.widgets.toastContainer.appendChild(toast);
      
      setTimeout(() => {
        toast.style.animation = 'slideOutRight 0.3s ease-in';
        setTimeout(() => {
          if (toast.parentNode) {
            toast.parentNode.removeChild(toast);
          }
        }, 300);
      }, duration);
    },
    
    /**
     * Atualiza HUD
     */
    updateHUD: function(data) {
      if (data.health !== undefined) {
        document.getElementById('hud-health-value').textContent = data.health;
        document.getElementById('hud-health-bar').style.width = data.health + '%';
        
        // Cor baseada na saúde
        const healthBar = document.getElementById('hud-health-bar');
        if (data.health > 50) {
          healthBar.style.background = 'linear-gradient(90deg, #4ade80, #22c55e)';
        } else if (data.health > 25) {
          healthBar.style.background = 'linear-gradient(90deg, #fbbf24, #f59e0b)';
        } else {
          healthBar.style.background = 'linear-gradient(90deg, #ef4444, #dc2626)';
        }
      }
      
      if (data.mana !== undefined) {
        document.getElementById('hud-mana-value').textContent = data.mana;
        document.getElementById('hud-mana-bar').style.width = data.mana + '%';
      }
      
      if (data.xp !== undefined) {
        document.getElementById('hud-xp').textContent = data.xp;
      }
      
      if (data.level !== undefined) {
        document.getElementById('hud-level').textContent = data.level;
      }
    },
    
    /**
     * Mostra menu de contexto
     */
    showContextMenu: function(x, y, items) {
      const menu = this.widgets.contextMenu;
      menu.innerHTML = '';
      
      items.forEach(item => {
        const menuItem = document.createElement('div');
        menuItem.style.cssText = `
          padding: 10px 15px;
          cursor: pointer;
          border-radius: 4px;
          transition: background 0.2s;
          font-size: 14px;
          color: white;
        `;
        
        if (item.icon) {
          menuItem.innerHTML = `<span style="margin-right: 10px;">${item.icon}</span>${item.label}`;
        } else {
          menuItem.textContent = item.label;
        }
        
        menuItem.onmouseenter = () => {
          menuItem.style.background = 'rgba(255, 255, 255, 0.1)';
        };
        
        menuItem.onmouseleave = () => {
          menuItem.style.background = 'transparent';
        };
        
        menuItem.onclick = (e) => {
          e.stopPropagation();
          if (item.onClick) item.onClick();
          menu.style.display = 'none';
        };
        
        menu.appendChild(menuItem);
      });
      
      menu.style.left = x + 'px';
      menu.style.top = y + 'px';
      menu.style.display = 'block';
    },
    
    /**
     * Mostra loading
     */
    showLoading: function(text = 'Loading...', subtext = 'Please wait') {
      const overlay = this.widgets.loadingOverlay;
      document.getElementById('loading-text').textContent = text;
      document.getElementById('loading-subtext').textContent = subtext;
      overlay.style.display = 'flex';
    },
    
    /**
     * Esconde loading
     */
    hideLoading: function() {
      this.widgets.loadingOverlay.style.display = 'none';
    },
    
    /**
     * Cria widget customizado
     */
    createWidget: function(id, html, styles) {
      const widget = document.createElement('div');
      widget.id = id;
      widget.style.cssText = styles;
      widget.innerHTML = html;
      
      this.container.appendChild(widget);
      this.widgets[id] = widget;
      
      return widget;
    },
    
    /**
     * Remove widget
     */
    removeWidget: function(id) {
      const widget = this.widgets[id];
      if (widget && widget.parentNode) {
        widget.parentNode.removeChild(widget);
        delete this.widgets[id];
      }
    },
    
    /**
     * Toggle HUD
     */
    toggleHUD: function() {
      const hud = this.widgets.hud;
      hud.style.display = hud.style.display === 'none' ? 'block' : 'none';
    },
    
    /**
     * Limpa todas notificações
     */
    clearNotifications: function() {
      const area = this.widgets.notificationArea;
      while (area.firstChild) {
        area.removeChild(area.firstChild);
      }
    }
  };
  
  // Auto-registra
  if (window.UnityModules) {
    window.UnityModules.register('customUI', CustomUI);
  }
  
  // Expõe globalmente
  window.CustomUI = CustomUI;
  
  // Adiciona integração com Unity
  if (window.UnityModules) {
    // Funções que o Unity pode chamar
    window.ShowNotification = function(message, type, duration) {
      CustomUI.showNotification(message, type || 'info', duration || 5000);
    };
    
    window.ShowToast = function(message, duration) {
      CustomUI.showToast(message, duration || 3000);
    };
    
    window.UpdateHUD = function(jsonData) {
      try {
        const data = JSON.parse(jsonData);
        CustomUI.updateHUD(data);
      } catch (e) {
        console.error('[CustomUI] Erro ao parsear dados do HUD:', e);
      }
    };
    
    window.ShowLoading = function(text, subtext) {
      CustomUI.showLoading(text, subtext);
    };
    
    window.HideLoading = function() {
      CustomUI.hideLoading();
    };
  }
  
})();