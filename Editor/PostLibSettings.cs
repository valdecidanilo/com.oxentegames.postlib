using UnityEngine;

namespace PostLib.Editor
{
    [CreateAssetMenu(fileName = "PostLibSettings", menuName = "PostLib/Settings")]
    public class PostLibSettings : ScriptableObject
    {
        public bool enablePostLib = true;

        private static PostLibSettings _instance;
        public static PostLibSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<PostLibSettings>("PostLibSettings");
                    if (_instance == null)
                    {
                        Debug.LogError("[PostLib] PostLibSettings.asset não encontrado em Resources.");
                    }
                }
                return _instance;
            }
        }
    }
}