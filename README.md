
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
- 🧪 **Ponte de testes E2E para Cypress** (ver seção abaixo e [CYPRESS.md](CYPRESS.md))

---

## 📁 Estrutura do Pacote

```
PostLib/
├── Editor/
│   ├── PostLibPluginCreator.cs     ← Gera o .jslib na build
│   └── CypressPluginCreator.cs     ← Gera o Cypress.jslib (só sob POSTLIB_CYPRESS)
├── Runtime/
│   ├── PostBridge.cs               ← Ponte C# ↔ JS
│   ├── PostBridgeBootstrap.cs      ← Garante presença do PostBridge
│   └── Cypress/                    ← Ponte de testes E2E (só sob POSTLIB_CYPRESS)
│       ├── CypressBridge.cs             ← window.__cypress__ ↔ C#
│       ├── CypressAttributes.cs         ← [CypressAction] / [CypressState]
│       └── CypressBridgeBootstrap.cs    ← Cria o CypressBridge em runtime
Plugins/
└── WebGL/
    ├── PostLib.jslib          ← Listener postMessage JS
    └── Cypress.jslib          ← API de teste (gerado só quando habilitado)
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

## 🧪 Testes no Editor

Durante o desenvolvimento, as mensagens enviadas por `PostBridge.Send(...)` são apenas logadas no Console para evitar erros em ambientes fora do WebGL.

---

## 🧪 Ponte de Testes E2E (Cypress)

Unity WebGL desenha **tudo dentro de um único `<canvas>`** — não há DOM, botões ou
textos que o Cypress possa consultar com `cy.get(...)`. A ponte resolve isso expondo
uma **API de teste** em `window.__cypress__`, sem depender de pixels:

- **Cypress → Unity (ações):** `win.unityInstance.SendMessage('CypressBridge', 'Exec', json)`
- **Unity → Cypress (estado):** o C# escreve o snapshot em `window.__cypress__.state`

> 📖 Guia completo com exemplos prontos (config, commands, specs): **[CYPRESS.md](CYPRESS.md)**

### 🔒 Segurança — pegada zero em produção

A ponte é uma **porta dos fundos** (permite forçar spin, mudar aposta etc.) e por isso
é totalmente controlada pelo define de compilação **`POSTLIB_CYPRESS`**:

- **Sem o define** → nada é compilado, o `Cypress.jslib` é apagado. Produção intocada.
- **Com o define** → a ponte é criada automaticamente antes da primeira cena.

Ligue/desligue pelo menu **`PostLib ▸ Cypress ▸ Ativar/Desativar ponte E2E (WebGL)`**
(ou adicione `POSTLIB_CYPRESS` aos *Scripting Define Symbols* do WebGL manualmente).
Faça **builds de QA** com o define ligado e **builds de produção** com ele desligado.

### 🎮 Expondo ações e estado do jogo

Você escolhe o que o Cypress pode fazer e ler. Há dois estilos — combine à vontade.

**1. API explícita (recomendada — sem reflection, imune a code stripping):**

```csharp
#if POSTLIB_CYPRESS
using PostLib.Cypress;

void Awake()
{
    CypressBridge.RegisterState("balance", () => Wallet.Balance);
    CypressBridge.RegisterState("state",   () => Fsm.Current.ToString());
    CypressBridge.RegisterAction("spin",   _   => RequestSpin());
    CypressBridge.RegisterAction("setBet", arg => SetBet(float.Parse(arg)));
}
#endif
```

**2. Atributos de marcação `[CypressAction]` / `[CypressState]`:**

> Os atributos ficam **sempre disponíveis** (são só metadados), então você não precisa
> envolvê-los em `#if`. Apenas a chamada `Bind`/`Register` precisa do define, pois toca a
> `CypressBridge` (que é gated).

```csharp
using PostLib.Cypress;

public class SlotController : MonoBehaviour
{
    [CypressState("balance")] public float Balance => _wallet.Balance;
    [CypressState("state")]   public string StateName() => _fsm.Current.ToString();
    [CypressState]            public bool IsSpinning;          // → state.IsSpinning

    [CypressAction("spin")]   public void RequestSpin() { ... }
    [CypressAction]           public void SetBet(float value) { ... }  // ação "SetBet"

    void Awake()
    {
#if POSTLIB_CYPRESS
        CypressBridge.Bind(this);   // registra tudo que estiver marcado
#endif
    }
}
```

> ⚠️ **Code stripping (IL2CPP):** métodos chamados só por reflection (atributos) podem
> ser removidos em builds com *Managed Stripping Level* alto. Como o build de QA usa o
> define `POSTLIB_CYPRESS`, use *Stripping Level = Minimal/Low* nesse build, ou prefira a
> **API explícita** (estilo 1), que não sofre esse problema.

### 📡 Eventos pontuais

Para o Cypress esperar por acontecimentos discretos (fim do spin, big win…):

```csharp
CypressBridge.Emit("spin_end", new { win = 12.5f });
// vira window.__cypress__.events = [{ evt: "spin_end", data: { win: 12.5 }, seq: 0 }, ...]
```

### ▶️ Lado Cypress (resumo)

```js
// cypress/support/commands.js
Cypress.Commands.add('unity', (action, arg) =>
  cy.window().its('unityInstance').then(u =>
    u.SendMessage('CypressBridge', 'Exec', JSON.stringify({ action, arg }))));

Cypress.Commands.add('unityState', () =>
  cy.window().its('__cypress__.state'));

// cypress/e2e/spin.cy.js
cy.visit('/?e2e=1');
cy.window().its('__cypress__.ready', { timeout: 60000 }).should('be.true');
cy.unityState().its('balance').then(before => {
  cy.unity('setBet', 1);
  cy.unity('spin');
  cy.unityState().its('state', { timeout: 15000 }).should('eq', 'Idle');
  cy.unityState().its('balance').should('not.eq', before);
});
```

O guia **[CYPRESS.md](CYPRESS.md)** traz a versão completa (helpers de eventos, `Ack`,
polling de estado, exemplos de slot com free spins e compra de bônus).

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
