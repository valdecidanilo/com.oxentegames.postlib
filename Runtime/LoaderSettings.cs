using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PostLib
{
    public class LoaderSettings : MonoBehaviour
    {
        [SerializeField] private Image logoLoader;
        [SerializeField] private TMP_Text descriptionLoader;
        
        [SerializeField] private Sprite defaultLogo;
        [SerializeField] private Sprite businessLogo;

        public static RegulationType CurrentRegulation { get; private set; }

        public static Action<RegulationType> OnRegulationCloseAnything;
        
#if UNITY_EDITOR
        public virtual void Awake()
        {
            ReceivedRegulation("brazilian");
        } 
#endif

        public virtual void ReceivedRegulation(string regulation)
        {
            if (Enum.TryParse(regulation, out RegulationType parsed))
                Setup(parsed);
            else
            {
                Debug.LogWarning("Regulation inválido: " + regulation);
                Setup(RegulationType.asian);
            }
            CurrentRegulation = parsed;
        }
        public virtual void Setup(RegulationType regulation)
        {
            
            logoLoader.color = Color.white;
            logoLoader.sprite = regulation.Equals(RegulationType.brazilian) 
                ? defaultLogo 
                : businessLogo;
            descriptionLoader.enabled = regulation != RegulationType.brazilian;
            OnRegulationCloseAnything?.Invoke(regulation);
        }
        public virtual void OnStartClicked(string value) { }
    }
    
    [System.Serializable]
    public enum RegulationType
    {
        brazilian,
        asian,
    }
}