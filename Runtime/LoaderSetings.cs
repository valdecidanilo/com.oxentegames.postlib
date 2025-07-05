using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PostLib
{
    public class LoaderSetings : MonoBehaviour
    {
        [SerializeField] private Image logoLoader;
        [SerializeField] private TMP_Text descriptionLoader;
        
        [SerializeField] private Sprite defaultLogo;
        [SerializeField] private Sprite playtechLogo;
        
        //Invoked by JS in GameObject in Scene with name (LoaderSettings) 
        public void ReceivedRegulation(string regulation)
        {
            var reculationLower = regulation.ToLower();
            Setup(reculationLower);
        }
        private void Setup(string regulation)
        {
            logoLoader.color = Color.white;
            logoLoader.sprite = regulation.Equals("br") 
                ? defaultLogo 
                : playtechLogo;
            descriptionLoader.enabled = regulation != "br";
        }
    }
}
