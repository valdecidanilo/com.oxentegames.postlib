using UnityEngine;

namespace PostLib.Editor
{
    [CreateAssetMenu(fileName = "PostLibSettings", menuName = "PostLib/Settings")]
    public class PostLibSettings : ScriptableObject
    {
        [Tooltip("Ativa ou desativa o PostLib nas builds")]
        public bool enablePostLib = true; // <-- Remova o "static" aqui

        private static PostLibSettings _instance;
        public static PostLibSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<PostLibSettings>("PostLibSettings");
                return _instance;
            }
        }
    }
}