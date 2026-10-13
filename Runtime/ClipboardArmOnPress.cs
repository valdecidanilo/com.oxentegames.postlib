using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PostLib
{
    /// <summary>
    /// Coloque no botão que copia algo: arma a cópia no PointerDown, antes do navegador
    /// soltar o toque, para que <see cref="PostBridge.ClipboardCopy"/> funcione no iOS.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClipboardArmOnPress : MonoBehaviour, IPointerDownHandler
    {
        private Selectable _selectable;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (_selectable != null && !_selectable.IsInteractable())
                return;

            PostBridge.ClipboardArm();
        }
    }
}
