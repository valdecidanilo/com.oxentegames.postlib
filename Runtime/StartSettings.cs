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
#if UNITY_EDITOR
        private void Awake()
        {
            ReceivedRegulation("brazilian");
        } 
#endif

        private void ReceivedRegulation(string regulation)
        {
            if (Enum.TryParse(regulation, out RegulationType parsed))
                Setup(parsed);
            else
            {
                Debug.LogWarning("Regulation inválido: " + regulation);
                Setup(RegulationType.asian);
            }
        }
        private void Setup(RegulationType regulation)
        {
            logoLoader.color = Color.white;
            logoLoader.sprite = regulation.Equals(RegulationType.brazilian) 
                ? defaultLogo 
                : businessLogo;
            descriptionLoader.enabled = regulation != RegulationType.brazilian;
        }
    }
    
    [System.Serializable]
    public enum RegulationType
    {
        brazilian,
        asian,
    }
}