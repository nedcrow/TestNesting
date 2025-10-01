using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hanok
{
    public abstract class PoolingComponent<T> : MonoBehaviour where T : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] protected int initialPoolSize = 10;
        [SerializeField] protected int maxPoolSize = 50;
        [SerializeField] protected bool allowPoolExpansion = true;

        protected Dictionary<T, Queue<GameObject>> pools;
        protected Dictionary<T, List<GameObject>> activeObjects;
        protected bool isInitialized = false;

        public int InitialPoolSize => initialPoolSize;
        public int MaxPoolSize => maxPoolSize;
        public bool AllowPoolExpansion => allowPoolExpansion;
        public bool IsInitialized => isInitialized;

        protected abstract Catalog GetCatalog();
        protected abstract string GetComponentTypeName();

        protected virtual void Start()
        {
            InitializePools();
        }

        protected virtual void InitializePools()
        {
            var catalog = GetCatalog();
            if (catalog == null)
            {
                Debug.LogError($"[{GetComponentTypeName()}] Catalog is null on {gameObject.name}. Cannot initialize pools.");
                return;
            }

            if (catalog.RegisteredPrefabs == null || catalog.RegisteredPrefabs.Count == 0)
            {
                Debug.LogError($"[{GetComponentTypeName()}] Catalog has no registered prefabs on {gameObject.name}.");
                return;
            }

            // 컬렉션 초기화
            pools = new Dictionary<T, Queue<GameObject>>();
            activeObjects = new Dictionary<T, List<GameObject>>();

            // 각 등록된 프리팹에 대해 풀 생성
            foreach (GameObject registeredPrefab in catalog.RegisteredPrefabs)
            {
                if (registeredPrefab == null) continue;

                T component = registeredPrefab.GetComponent<T>();
                if (component == null)
                {
                    Debug.LogWarning($"[{GetComponentTypeName()}] Registered prefab {registeredPrefab.name} does not have {typeof(T).Name} component. Skipping.");
                    continue;
                }

                CreatePoolForPrefab(component);
            }

            isInitialized = true;
            Debug.Log($"[{GetComponentTypeName()}] All pools initialized with {catalog.RegisteredPrefabs.Count} registered prefabs");
        }

        private void CreatePoolForPrefab(T prefab)
        {
            var pool = new Queue<GameObject>();
            var activeList = new List<GameObject>();

            // 초기 풀 크기만큼 오브젝트 생성
            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject poolObject = CreatePoolObject(prefab);
                if (poolObject != null)
                {
                    pool.Enqueue(poolObject);
                }
            }

            pools[prefab] = pool;
            activeObjects[prefab] = activeList;

            Debug.Log($"[{GetComponentTypeName()}] Pool initialized for {prefab.name} with {initialPoolSize} objects");
        }

        protected virtual GameObject CreatePoolObject(T prefab)
        {
            GameObject poolObject = Instantiate(prefab.gameObject, transform);
            poolObject.SetActive(false);
            return poolObject;
        }

        public virtual GameObject Get(T prefab)
        {
            if (!isInitialized || prefab == null || !pools.ContainsKey(prefab))
            {
                Debug.LogWarning($"[{GetComponentTypeName()}] Cannot get {prefab?.name}. Pool not initialized or prefab not found.");
                return null;
            }

            var pool = pools[prefab];
            var activeList = activeObjects[prefab];

            GameObject obj = GetFromPoolOrCreate(prefab, pool, activeList);
            if (obj != null)
            {
                activeList.Add(obj);
                obj.SetActive(true);
            }

            return obj;
        }

        private GameObject GetFromPoolOrCreate(T prefab, Queue<GameObject> pool, List<GameObject> activeList)
        {
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }

            if (allowPoolExpansion && GetTotalActiveCount() < maxPoolSize)
            {
                Debug.Log($"[{GetComponentTypeName()}] Pool expanded for {prefab.name}");
                return CreatePoolObject(prefab);
            }

            Debug.LogWarning($"[{GetComponentTypeName()}] Pool limit reached for {prefab.name}. Cannot provide more objects.");
            return null;
        }

        public virtual void Return(GameObject obj)
        {
            if (obj == null || !isInitialized) return;

            T component = obj.GetComponent<T>();
            if (component == null)
            {
                Debug.LogWarning($"[{GetComponentTypeName()}] Object {obj.name} does not have {typeof(T).Name} component.");
                return;
            }

            T originalPrefab = FindOriginalPrefab(component);
            if (originalPrefab == null || !pools.ContainsKey(originalPrefab))
            {
                Debug.LogWarning($"[{GetComponentTypeName()}] Cannot find original prefab for {obj.name}");
                return;
            }

            var activeList = activeObjects[originalPrefab];
            var pool = pools[originalPrefab];

            if (activeList.Contains(obj))
            {
                activeList.Remove(obj);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        protected virtual T FindOriginalPrefab(T component)
        {
            var catalog = GetCatalog();
            if (catalog?.RegisteredPrefabs == null) return null;

            string cleanName = component.name.Replace("(Clone)", "").Trim();

            return catalog.RegisteredPrefabs
                .Select(prefab => prefab?.GetComponent<T>())
                .FirstOrDefault(prefabComponent => prefabComponent != null && prefabComponent.name == cleanName);
        }

        public int GetAvailableCount(T prefab)
        {
            if (!isInitialized || prefab == null || !pools.ContainsKey(prefab))
                return 0;
            return pools[prefab].Count;
        }

        public int GetActiveCount(T prefab)
        {
            if (!isInitialized || prefab == null || !activeObjects.ContainsKey(prefab))
                return 0;
            return activeObjects[prefab].Count;
        }

        protected int GetTotalActiveCount()
        {
            return activeObjects?.Values?.Sum(list => list.Count) ?? 0;
        }

        public int GetTotalAvailableCount()
        {
            return pools?.Values?.Sum(pool => pool.Count) ?? 0;
        }

        protected virtual void ClearPool()
        {
            if (pools != null)
            {
                foreach (var pool in pools.Values)
                {
                    while (pool.Count > 0)
                    {
                        var obj = pool.Dequeue();
                        if (obj != null) DestroyImmediate(obj);
                    }
                }
                pools.Clear();
            }

            if (activeObjects != null)
            {
                foreach (var activeList in activeObjects.Values)
                {
                    for (int i = activeList.Count - 1; i >= 0; i--)
                    {
                        var obj = activeList[i];
                        if (obj != null) DestroyImmediate(obj);
                    }
                }
                activeObjects.Clear();
            }

            isInitialized = false;
        }
    }
}