using UnityEngine;

namespace Extensions.Patterns
{
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogWarning($"No instance of {typeof(T).Name} found in the scene. Please ensure that an instance is present.");
                }

                return _instance;
            }
        }
        
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
            }
            else
            {
                Debug.LogWarning($"Another instance of {typeof(T).Name} already exists, located on {_instance.gameObject.name}. Destroying this instance.");
                Destroy(gameObject);
            }
        }
        
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
