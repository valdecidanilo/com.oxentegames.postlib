using System;

namespace PostLib.Cypress
{
    // NOTA: estes atributos NÃO ficam sob #if POSTLIB_CYPRESS de propósito.
    // São apenas metadados (pegada desprezível) e ficam sempre disponíveis para
    // que o jogo possa marcar seus membros SEM envolver cada atributo em #if.
    // A lógica que os consome (CypressBridge/Bind) é que fica gated pelo define —
    // então em produção os atributos existem mas ninguém os lê.

    /// <summary>
    /// Marca um método como uma ação chamável pelo Cypress.
    /// O método pode ter zero parâmetros ou um único parâmetro (string, número,
    /// bool, enum ou um tipo desserializável de JSON).
    ///
    /// <code>
    /// [CypressAction("spin")]         // nome explícito
    /// public void RequestSpin() { ... }
    ///
    /// [CypressAction]                 // usa o nome do método ("SetBet")
    /// public void SetBet(float value) { ... }
    /// </code>
    ///
    /// Registre com <c>CypressBridge.Bind(this)</c> (ex.: no Awake do objeto).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class CypressActionAttribute : Attribute
    {
        public string Name { get; }
        public CypressActionAttribute(string name = null) => Name = name;
    }

    /// <summary>
    /// Marca um método (sem parâmetros), propriedade ou campo como parte do snapshot
    /// de estado que o Cypress lê em <c>window.__cypress__.state</c>.
    ///
    /// <code>
    /// [CypressState("balance")] public float Balance => _wallet.Balance;
    /// [CypressState] public bool  IsSpinning;     // vira state.IsSpinning
    /// [CypressState("state")]    public string CurrentStateName() => _fsm.Current.ToString();
    /// </code>
    ///
    /// Registre com <c>CypressBridge.Bind(this)</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field,
        Inherited = true, AllowMultiple = false)]
    public sealed class CypressStateAttribute : Attribute
    {
        public string Name { get; }
        public CypressStateAttribute(string name = null) => Name = name;
    }
}
