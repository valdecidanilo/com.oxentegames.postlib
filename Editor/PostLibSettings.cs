using UnityEngine;

namespace PostLib.Editor
{
    [CreateAssetMenu(fileName = "PostLibBootstrapSettings", menuName = "PostLib/Bootstrap Settings")]
    public class PostLibSettings: ScriptableObject
    {
        public bool enableBootstrap = true;
    }
}