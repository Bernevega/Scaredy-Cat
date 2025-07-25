using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool instance { get; private set; }
    [SerializeField] ObjectPoolInitializer[] initPools = new ObjectPoolInitializer[1];
    [SerializeField] Dictionary<string, GameObject[]> pools = new Dictionary<string, GameObject[]>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        for (int i = 0; i < initPools.Length; i++)
        {
            ObjectPoolInitializer initPool = initPools[i];
            if (initPool.objectToPool == null ||
                initPool.initSize == 0) continue;

            GameObject[] goArr = new GameObject[initPool.initSize];
            for (int obj = 0; obj < goArr.Length; obj++)
            {
                goArr[obj] = Instantiate(initPool.objectToPool);
                goArr[obj].name = goArr[obj].name.Replace("(Clone)", "").Trim();
                goArr[obj].transform.SetParent(transform, true);
                goArr[obj].SetActive(false);
            }
            pools.Add(initPool.objectToPool.name, goArr);
        }
    }

    /// <summary>
    /// Note : Activates the game object that is returned.
    /// </summary>
    /// <param name="poolName"></param>
    /// <returns></returns>
    public GameObject objPool_GetObject(string poolName)
    {
        pools.TryGetValue(poolName, out GameObject[] pool);

        if (pool == null) return null;

        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i].activeInHierarchy == false)
            {
                pool[i].SetActive(true);
                return pool[i];
            }
        }

        return null;
    }

    public bool objPool_ReturnObject(GameObject go)
    {
        bool success = false;

        pools.TryGetValue(go.name, out GameObject[] pool);

        if (pool != null)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] == go)
                {
                    pool[i].SetActive(false);
                    pool[i].transform.SetParent(transform, true);
                    success = true;
                    break;
                }
            }
        }

        return success;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }


}

[Serializable]
public struct ObjectPoolInitializer
{
    public GameObject objectToPool;
    public uint initSize;
}
