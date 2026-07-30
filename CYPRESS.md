# 🧪 Testando Unity WebGL com Cypress via PostLib

Guia prático da **ponte E2E** do PostLib. Cobre o lado Unity (o que expor) e o lado
Cypress (como dirigir e verificar o jogo), com exemplos prontos para um slot.

> **Por que uma ponte?** Unity WebGL renderiza tudo em um único `<canvas>`. Não existe
> DOM: nada de `<button>`, `data-testid` ou `cy.contains()`. O Cypress enxerga só pixels.
> Em vez de testar por imagem (frágil), a ponte expõe as **ações** e o **estado** do jogo
> em `window.__cypress__`, e os testes chamam ações e verificam estado.

---

## Índice

1. [Como funciona](#como-funciona)
2. [Ativar a ponte (define `POSTLIB_CYPRESS`)](#1-ativar-a-ponte)
3. [Expor ações e estado no Unity](#2-expor-ações-e-estado-no-unity)
4. [Configurar o Cypress](#3-configurar-o-cypress)
5. [Comandos customizados](#4-comandos-customizados-supportcommandsjs)
6. [Exemplos de specs](#5-exemplos-de-specs)
7. [Contrato de `window.__cypress__`](#6-contrato-de-windowcypress)
8. [Boas práticas e armadilhas](#7-boas-práticas-e-armadilhas)

---

## Como funciona

```
┌─────────────┐   SendMessage('CypressBridge','Exec', json)   ┌──────────────┐
│   Cypress   │ ───────────────────────────────────────────► │  CypressBridge│
│  (browser)  │                                               │     (C#)      │
│             │ ◄─────────────────────────────────────────── │               │
└─────────────┘     window.__cypress__.state / .events        └──────────────┘
        ▲                    (via Cypress.jslib)                      │
        └──────────── lê estado com cy.window() ─────────────────────┘
```

- **Ações (Cypress → Unity):** um único ponto de entrada `Exec(json)`. O JSON tem
  `{ id?, action, arg? }`. A ponte roteia para o handler registrado com aquele nome.
- **Estado (Unity → Cypress):** a cada boot e após cada ação, a ponte serializa todos os
  provedores registrados e grava em `window.__cypress__.state`.
- **Eventos (Unity → Cypress):** `CypressBridge.Emit(...)` empurra itens em
  `window.__cypress__.events` para esperar por acontecimentos discretos.

---

## 1. Ativar a ponte

A ponte **só existe** quando o define de compilação `POSTLIB_CYPRESS` está ligado no
target WebGL. Sem ele: nenhum código é compilado e o `Cypress.jslib` é apagado
(**pegada zero em produção**).

**Menu (recomendado):**

```
PostLib ▸ Cypress ▸ Ativar ponte E2E (WebGL)
PostLib ▸ Cypress ▸ Desativar ponte E2E (WebGL)
```

**Manual:** `Project Settings ▸ Player ▸ WebGL ▸ Scripting Define Symbols` → adicione
`POSTLIB_CYPRESS`.

> Faça **builds de QA** com o define ligado e **builds de produção** com ele desligado.
> Dica: mantenha dois presets de build para não esquecer.

Ao rodar, o console mostra:
`[Cypress] Ponte E2E ativa (POSTLIB_CYPRESS). window.__cypress__ disponível.`

---

## 2. Expor ações e estado no Unity

Você decide **o que** o Cypress pode fazer e ler. Dois estilos — combine à vontade.
Envolva o código de teste em `#if POSTLIB_CYPRESS` para não vazar para produção.

### Estilo A — API explícita (recomendada)

Sem reflection, imune a *code stripping*. Ideal para um único ponto de fiação:

```csharp
#if POSTLIB_CYPRESS
using PostLib.Cypress;

public static class CypressWiring
{
    public static void Wire(GameController game)
    {
        // Estado (lido em window.__cypress__.state)
        CypressBridge.RegisterState("state",     () => game.Fsm.Current.ToString());
        CypressBridge.RegisterState("balance",   () => game.Wallet.Balance);
        CypressBridge.RegisterState("bet",       () => game.Bet);
        CypressBridge.RegisterState("lastWin",   () => game.LastWin);
        CypressBridge.RegisterState("spinning",  () => game.IsSpinning);
        CypressBridge.RegisterState("freeSpins", () => game.FreeSpinsRemaining);

        // Ações (chamadas via cy.unity('spin'), cy.unity('setBet', 2)…)
        CypressBridge.RegisterAction("spin",     _   => game.RequestSpin());
        CypressBridge.RegisterAction("setBet",   arg => game.SetBet(float.Parse(arg)));
        CypressBridge.RegisterAction("buyBonus", _   => game.BuyBonus());
        CypressBridge.RegisterAction("forceResult", json =>
        {
            var r = Newtonsoft.Json.JsonConvert.DeserializeObject<DebugResult>(json);
            game.QueueDebugResult(r);   // útil para forçar cenários determinísticos
        });
    }
}
#endif
```

Chame uma vez, quando o jogo estiver pronto:

```csharp
void Start()
{
#if POSTLIB_CYPRESS
    CypressWiring.Wire(this);
#endif
}
```

### Estilo B — Atributos de marcação

Declarativo: marque os membros e chame `Bind(this)` uma vez. Os atributos ficam
**sempre disponíveis** (só metadados), então não precisam de `#if`; apenas o `Bind` precisa.

```csharp
using PostLib.Cypress;
using UnityEngine;

public class SlotController : MonoBehaviour
{
    [CypressState("balance")] public float Balance => _wallet.Balance;
    [CypressState("state")]   public string StateName() => _fsm.Current.ToString();
    [CypressState]            public bool IsSpinning;        // → state.IsSpinning
    [CypressState]            public int  FreeSpins;         // → state.FreeSpins

    [CypressAction("spin")]   public void RequestSpin() { /* ... */ }
    [CypressAction]           public void SetBet(float value) { /* ... */ }  // ação "SetBet"
    [CypressAction("buyBonus")] public void BuyBonus() { /* ... */ }

    void Awake()
    {
#if POSTLIB_CYPRESS
        CypressBridge.Bind(this);   // registra tudo que estiver marcado neste objeto
#endif
    }
}
```

- `[CypressState]` aceita **propriedade, campo ou método sem parâmetros**.
- `[CypressAction]` aceita método com **0 ou 1 parâmetro** (`string`, `int`, `long`,
  `float`, `double`, `decimal`, `bool`, `enum` ou um tipo desserializável de JSON).
- Sem nome no atributo → usa o nome do membro.

> ⚠️ **Code stripping (IL2CPP).** Métodos chamados só por reflection podem ser removidos
> quando o *Managed Stripping Level* é alto. No build de QA (com `POSTLIB_CYPRESS`) use
> *Stripping Level = Minimal/Low*, ou adicione um `link.xml` preservando o assembly do
> jogo, ou use o **Estilo A**. O Estilo A nunca sofre disso.

### Eventos pontuais

Para esperar por acontecimentos discretos em vez de fazer polling de estado:

```csharp
CypressBridge.Emit("spin_end", new { win = game.LastWin });
CypressBridge.Emit("big_win");
CypressBridge.Emit("free_spins_enter", new { count = 10 });
```

Cada chamada adiciona `{ evt, data, seq }` em `window.__cypress__.events`.

---

## 3. Configurar o Cypress

Instale no repositório de testes (fora do Unity):

```bash
npm install --save-dev cypress
```

`cypress.config.js` — aponte para onde o build WebGL de QA é servido:

```js
const { defineConfig } = require('cypress');

module.exports = defineConfig({
  e2e: {
    baseUrl: 'http://localhost:8080',   // servidor do build WebGL de QA
    defaultCommandTimeout: 15000,       // WebGL é mais lento que web comum
    video: true,
    supportFile: 'cypress/support/e2e.js',
  },
});
```

Sirva o build (exemplo simples):

```bash
npx http-server ./Build/WebGL-QA -p 8080
```

> Se o build usa compressão Brotli/Gzip do Unity, garanta os headers corretos
> (`Content-Encoding`) ou desligue a compressão no build de QA para simplificar.

---

## 4. Comandos customizados (`support/commands.js`)

```js
// cypress/support/commands.js

// Espera o jogo bootar (a ponte chama Cypress_Ready no Start).
Cypress.Commands.add('unityReady', (timeout = 90000) =>
  cy.window({ timeout }).its('__cypress__.ready', { timeout }).should('be.true'));

// Dispara uma ação no jogo. Ex.: cy.unity('setBet', 2)
Cypress.Commands.add('unity', (action, arg) =>
  cy.window().its('unityInstance').then((u) => {
    const payload = { action };
    if (arg !== undefined) payload.arg = arg;
    u.SendMessage('CypressBridge', 'Exec', JSON.stringify(payload));
  }));

// Lê o snapshot de estado inteiro.
Cypress.Commands.add('unityState', () =>
  cy.window().its('__cypress__.state'));

// Lê um campo específico do estado (com retry embutido do Cypress).
Cypress.Commands.add('unityStateField', (field, timeout = 15000) =>
  cy.window({ timeout }).its(`__cypress__.state.${field}`, { timeout }));

// Espera um evento emitido pelo jogo aparecer em window.__cypress__.events.
Cypress.Commands.add('unityEvent', (name, timeout = 20000) =>
  cy.window({ timeout })
    .its('__cypress__.events', { timeout })
    .should((events) => {
      expect(events.some((e) => e.evt === name), `evento "${name}"`).to.be.true;
    }));

// Limpa a fila de eventos entre passos, se necessário.
Cypress.Commands.add('unityClearEvents', () =>
  cy.window().then((w) => { if (w.__cypress__) w.__cypress__.events = []; }));
```

`support/e2e.js`:

```js
import './commands';
```

---

## 5. Exemplos de specs

### Spin básico credita o ganho

```js
// cypress/e2e/spin.cy.js
describe('Slot — spin', () => {
  beforeEach(() => {
    cy.visit('/?e2e=1');   // o ?e2e=1 é uma convenção sua; a ponte já sobe com o define
    cy.unityReady();
  });

  it('faz um spin e resolve para Idle', () => {
    cy.unityStateField('state').should('eq', 'Idle');

    cy.unityState().its('balance').then((saldoAntes) => {
      cy.unity('setBet', 1);
      cy.unity('spin');

      cy.unityStateField('spinning').should('be.true');
      cy.unityStateField('state', 20000).should('eq', 'Idle');   // spin terminou

      cy.unityState().its('balance').should('not.eq', saldoAntes);
    });
  });
});
```

### Esperando por um evento em vez de polling

```js
it('emite spin_end ao final do giro', () => {
  cy.unityClearEvents();
  cy.unity('spin');
  cy.unityEvent('spin_end');
  cy.window().its('__cypress__.events').then((events) => {
    const last = events.find((e) => e.evt === 'spin_end');
    expect(last.data).to.have.property('win');
  });
});
```

### Compra de bônus entra em free spins

```js
it('buy bonus entra em free spins', () => {
  cy.unity('buyBonus');
  cy.unityEvent('free_spins_enter');
  cy.unityStateField('freeSpins').should('be.greaterThan', 0);
});
```

### Cenário determinístico (forçando resultado)

Requer uma ação de debug no jogo (ver `forceResult` no Estilo A):

```js
it('paga corretamente uma linha vencedora conhecida', () => {
  cy.unity('setBet', 1);
  cy.unity('forceResult', { symbols: ['A', 'A', 'A'], payout: 10 });
  cy.unity('spin');
  cy.unityStateField('state', 20000).should('eq', 'Idle');
  cy.unityStateField('lastWin').should('eq', 10);
});
```

### Confirmação síncrona com `Ack`

Passe um `id` para receber a confirmação do comando em `window.__cypress__.acks[id]`:

```js
Cypress.Commands.add('unityExec', (action, arg) => {
  const id = `cmd_${Cypress._.uniqueId()}`;
  cy.window().its('unityInstance').then((u) =>
    u.SendMessage('CypressBridge', 'Exec', JSON.stringify({ id, action, arg })));
  return cy.window().its(`__cypress__.acks.${id}`, { timeout: 15000 });
});

it('rejeita ação desconhecida', () => {
  cy.unityExec('naoExiste').should('deep.include', { ok: false });
});
```

---

## 6. Contrato de `window.__cypress__`

Objeto criado pela ponte no navegador:

| Campo          | Tipo     | Descrição                                                        |
| -------------- | -------- | ---------------------------------------------------------------- |
| `ready`        | boolean  | `true` quando o jogo bootou (`Cypress_Ready`).                   |
| `state`        | object   | Snapshot dos provedores registrados. Atualizado a cada ação.     |
| `stateVersion` | number   | Incrementa a cada publicação de estado (útil para detectar mudança). |
| `events`       | array    | Lista de `{ evt, data, seq }` emitidos por `CypressBridge.Emit`. |
| `acks`         | object   | Mapa `id → payload` das confirmações de `Exec` com `id`.         |

Formato do comando `Exec`:

```jsonc
{
  "id": "cmd_42",     // opcional — se presente, gera um ack
  "action": "setBet", // obrigatório — nome registrado (case-insensitive)
  "arg": 2            // opcional — string, número, bool ou objeto JSON
}
```

Hook opcional no template WebGL para reagir ao ready:

```js
window.__onCypressReady = function () { console.log('Unity pronto para testes'); };
```

---

## 7. Boas práticas e armadilhas

- **Nunca envie o define para produção.** É uma porta dos fundos. Use o menu do PostLib e
  mantenha builds de QA e de produção separados.
- **Prefira estado + eventos a screenshots.** Testar pixels em WebGL é frágil; a ponte
  torna as asserções determinísticas.
- **`unityInstance` precisa estar no `window`.** Os templates do PostLib já expõem
  `window.unityInstance`. Se usar template próprio, garanta isso no `.jslib` de load.
- **Timeouts generosos.** O boot do WebGL pode levar dezenas de segundos; use
  `cy.unityReady()` antes de qualquer ação.
- **Determinismo.** Para asserir pagamentos exatos, exponha uma ação de debug que force o
  resultado do servidor/RNG, senão asserte propriedades (saldo mudou, estado voltou a Idle).
- **Code stripping.** Se usar atributos (`Bind`) e as ações "sumirem" no build, baixe o
  *Managed Stripping Level* do build de QA ou migre para a API explícita.
- **Limpe eventos entre passos** com `cy.unityClearEvents()` quando reusar a mesma sessão.

---

## Resumo do fluxo

1. `PostLib ▸ Cypress ▸ Ativar ponte E2E (WebGL)` (liga o define no build de QA).
2. No jogo, registre ações/estado (`RegisterAction/RegisterState` ou atributos + `Bind`).
3. Gere o build WebGL de QA e sirva em um `baseUrl`.
4. Escreva specs usando `cy.unityReady()`, `cy.unity(...)`, `cy.unityState()`.
5. Para produção, `PostLib ▸ Cypress ▸ Desativar` — pegada zero.
