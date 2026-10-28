using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PostLib
{
    public class StartSettings : MonoBehaviour
    {
        [SerializeField] private Image logoLoader;
        [SerializeField] private TMP_Text descriptionLoader;
        
        [SerializeField] private Sprite defaultLogo;
        [SerializeField] private Sprite businessLogo;

        public static RegulationType CurrentRegulation { get; private set; }

        public static Action<RegulationType> OnRegulationCloseAnything;
        
#if UNITY_EDITOR
        private void Awake()
        {
            ReceivedRegulation("brazilian");
        } 
#endif

        private void ReceivedRegulation(string regulation)
        {
            // TryParse leaves parsed as default (brazilian) when it fails, so fall back explicitly
            if (!Enum.TryParse(regulation, true, out RegulationType parsed) ||
                !Enum.IsDefined(typeof(RegulationType), parsed))
                parsed = RegulationType.asian;

            CurrentRegulation = parsed;
            Setup(parsed);
        }
        private void Setup(RegulationType regulation)
        {
            
            logoLoader.color = Color.white;
            logoLoader.sprite = regulation.Equals(RegulationType.brazilian) 
                ? defaultLogo 
                : businessLogo;
            descriptionLoader.enabled = regulation != RegulationType.brazilian;
            OnRegulationCloseAnything?.Invoke(regulation);
        }
    }
    
    [System.Serializable]
    public enum RegulationType
    {
        brazilian,
        asian,
        mexico,
    }
}