using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T I { get; private set; } // Instance
    protected virtual void Awake()
    {
        if (I == null)
            I = this as T;
        else if (I != this as T)
            Destroy(gameObject);
    }
    protected virtual void OnApplicationQuit()
    {
        I = null;
        Destroy(gameObject);
    }
}

public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
{
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}