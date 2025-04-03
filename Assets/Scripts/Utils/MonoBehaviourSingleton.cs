using UnityEngine;

namespace Utils
{
    public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviourSingleton<T>
    {
        private static MonoBehaviourSingleton<T> _instance = null;

        public static T Instance
        {
            get 
            {
                if (!_instance)
                    _instance = FindObjectOfType<MonoBehaviourSingleton<T>>();

                return (T)_instance;
            }
        }

        protected virtual void Initialize()
        {

        }

        private void Awake()
        {
            if (_instance) Destroy(this.gameObject);

            _instance = this;

            Initialize();
        }
    }
}
