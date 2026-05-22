using System.Collections.Generic;
using UnityEngine;

public class PooledObject : MonoBehaviour
{
    public GameObject prefabReference;
}

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }
    
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("ObjectPoolManager");
            Instance = go.AddComponent<ObjectPoolManager>();
            DontDestroyOnLoad(go);
        }
    }

    public GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab] = new Queue<GameObject>();
        }

        if (poolDictionary[prefab].Count > 0)
        {
            GameObject obj = poolDictionary[prefab].Dequeue();
            
            // If the object was destroyed externally, grab another one
            if (obj == null)
            {
                return SpawnObject(prefab, position, rotation);
            }
            
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            return obj;
        }
        else
        {
            // Pool empty or first time, create new
            GameObject obj = Instantiate(prefab, position, rotation);
            PooledObject pooledObj = obj.AddComponent<PooledObject>();
            pooledObj.prefabReference = prefab;
            return obj;
        }
    }

    public void ReturnObjectToPool(GameObject obj)
    {
        if (obj == null) return;
        
        PooledObject pooledObj = obj.GetComponent<PooledObject>();
        if (pooledObj != null && pooledObj.prefabReference != null)
        {
            // Clear trail renderers to prevent streaks when repositioned later
            TrailRenderer trail = obj.GetComponent<TrailRenderer>();
            if (trail != null) trail.Clear();
            TrailRenderer[] childTrails = obj.GetComponentsInChildren<TrailRenderer>();
            foreach (var t in childTrails) t.Clear();

            obj.SetActive(false);
            if (!poolDictionary.ContainsKey(pooledObj.prefabReference))
            {
                poolDictionary[pooledObj.prefabReference] = new Queue<GameObject>();
            }
            
            // Avoid adding duplicates (e.g. double kill events)
            if (!poolDictionary[pooledObj.prefabReference].Contains(obj))
            {
                poolDictionary[pooledObj.prefabReference].Enqueue(obj);
            }
        }
        else
        {
            // Fallback for non-pooled objects
            Destroy(obj);
        }
    }
}
