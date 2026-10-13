
# 📡 PostLib – Comunicação entre JavaScript e Unity WebGL

**PostLib** é um pacote modular que permite comunicação bidirecional entre Unity WebGL e páginas da Web utilizando a API `postMessage`. Ele automatiza a configuração do ambiente, gerencia mensagens padronizadas e oferece uma ponte entre `JavaScript` e `C#`, facilitando a integração com páginas externas e sistemas embarcados em iframes.

---

## 🧩 Recursos

- 🔄 Comunicação bidirecional (JS ↔ Unity)
- ⚡ Auto-inicialização do listener JS em builds WebGL
- 🧠 Roteamento de mensagens baseado em `_type`
- 📦 Geração automática de `PostLib.jslib`
- 🎮 Inicialização automática do `PostBridge` em runtime
- ✅ Compatível com Unity WebGL (sem interferir no Editor)

---

## 📁 Estrutura do Pacote

```
PostLib/
├── Editor/
│   └── PostLibPluginCreator.cs     ← Gera o .jslib na build
├── Runtime/
│   ├── PostBridge.cs               ← Ponte C# ↔ JS
│   └── PostBridgeBootstrap.cs     ← Garante presença do PostBridge
Plugins/
└── WebGL/
    └── PostLib.jslib          ← Listener postMessage JS
```

---

## 🚀 Instalação

1. Copie a pasta `PostLib` e o conteúdo de `Plugins/WebGL` para o seu projeto Unity.
2. O arquivo `PostLib.jslib` será criado automaticamente após a primeira recompilação do Editor.
3. Ao iniciar sua aplicação WebGL, o `PostBridge` será injetado automaticamente na cena.

---

## 💡 Como Funciona

### JS → Unity

O JavaScript envia mensagens para o Unity via `postMessage`. O listener JS está embutido no arquivo `PostLib.jslib`, que escuta eventos globais:

```javascript
window.postMessage({ _type: "ucip.pause.w2gPauseCommand", pause: true }, "*");
```

### Unity → JS

Você pode enviar mensagens para o navegador via:

```csharp
PostBridge.Send("{ \"_type\": \"my.custom.command\", \"data\": 123 }");
```

---

## 🔄 Roteamento de Mensagens

Todas as mensagens recebidas com campo `_type` são roteadas para seus respectivos manipuladores via `MessageRouter.Route()` com base no valor do enum `PostMappings`.

```csharp
public enum PostMappings
{
    [EnumMember(Value = "ucip.pause.w2gPauseCommand")]
    GamePauseHandler,

    [EnumMember(Value = "ucip.autoplay.w2gInterruptGameplayCommand")]
    AutoPlayInterruptCommand,

    // outros...
}
```

---

## 📦 Exemplo de Mensagem JSON

```json
{
  "_type": "ucip.pause.w2gPauseCommand",
  "pause": true
}
```

---

## 📋 Copiar para a área de transferência

O Safari (iOS) só copia dentro do evento de toque do navegador, e o Unity WebGL processa o toque no frame seguinte. Por isso a cópia precisa ser armada no `PointerDown` do botão:

1. Adicione o componente `ClipboardArmOnPress` ao botão (ou chame `PostBridge.ClipboardArm()` no `PointerDown`).
2. No `onClick`, chame `PostBridge.ClipboardCopy(texto)` — pode ser depois de uma requisição de rede.
3. Se desistir de copiar (sem link, erro), chame `PostBridge.ClipboardCancel()`.

Sem o passo 1 a cópia continua funcionando no Android e no desktop, mas não no iOS.

---

## 🧪 Testes no Editor

Durante o desenvolvimento, as mensagens enviadas por `PostBridge.Send(...)` são apenas logadas no Console para evitar erros em ambientes fora do WebGL.

---

## ❗ Requisitos

- Unity 2020.3+ (recomendado 2021.3 ou superior)
- Target Platform: WebGL

---

## ✅ Licença

Este pacote é open-source e pode ser usado livremente em projetos pessoais e comerciais. Atribuição é apreciada, mas não obrigatória.

---

## 🤝 Contribuições

Sugestões, melhorias e pull requests são bem-vindos! 🚀
