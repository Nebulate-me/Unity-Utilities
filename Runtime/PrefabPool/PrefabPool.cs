﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Utilities.Prefabs
{
    public class PrefabPool : MonoBehaviour, IPrefabPool
    {
        private Dictionary<int, IPrefabsGroup> cache = new();
        private Dictionary<(int, DiContainer), IPrefabsGroup> cacheCustomContainer = new();

        [Inject] private DiContainer container;

        [SerializeField] private Transform poolsTransform;
        [SerializeField] private bool useDontDestroyOnLoad = true;

        public PrefabPool()
        {
            Instance = this;
        }

        public static IPrefabPool Instance { get; private set; }

        private void Awake()
        {
            transform.SetParent(null);

            if (useDontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }

        public GameObject Spawn(GameObject prefab, Transform parent = null, Action<GameObject> onSpawn = null)
        {
            var resourceGroup = GetOrCreateGroup(prefab);
            return resourceGroup.Spawn(prefab, parent, onSpawn);
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, Action<GameObject> onSpawn = null)
        {
            var resourceGroup = GetOrCreateGroup(prefab);
            var instance = resourceGroup.Spawn(prefab, position, rotation, parent, onSpawn);

            return instance;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, DiContainer diContainer, Transform parent = null, Action<GameObject> onSpawn = null)
        {
            var resourceGroup = GetOrCreateGroup(prefab, diContainer);
            var instance = resourceGroup.Spawn(prefab, position, rotation, parent, onSpawn);

            return instance;
        }

        public void Despawn(GameObject instance)
        {
            if (!instance.TryGetComponent<PoolableItemComponent>(out var component))
            {
                Debug.LogWarning($"Despawn > can't find PoolableItemComponent of {instance.name}, skipping!");
                return;
            }
            if (cache.TryGetValue(component.PrefabKey, out var resourceGroup))
                resourceGroup.Despawn(instance);
            else
                Destroy(instance);
        }

        public void Despawn(GameObject instance, DiContainer diContainer)
        {
            if (!instance.TryGetComponent<PoolableItemComponent>(out var component))
            {
                Debug.LogWarning($"Despawn > can't find PoolableItemComponent of {instance.name}, skipping!");
                return;
            }
            if (cacheCustomContainer.TryGetValue((component.PrefabKey, diContainer), out var resourceGroup))
                resourceGroup.Despawn(instance);
            else
                Destroy(instance);
        }

        private IPrefabsGroup GetOrCreateGroup(GameObject prefab)
        {
            var prefabKey = PrefabsGroup.GetPrefabKey(prefab);
            var resourceGroup = cache.SafeGet(prefabKey);
            if (resourceGroup != null) return resourceGroup;

            var prefabName = PrefabsGroup.GetPrefabName(prefab);
            resourceGroup = container.InstantiateComponentOnNewGameObject<PrefabsGroup>($"pool_{prefabName}");

            if (prefab.TryGetComponent<PoolableItemConfig>(out var poolableItemConfigComponent))
                resourceGroup.IsPersistantGroup = poolableItemConfigComponent.PersistantGroup;

            resourceGroup.SetParent(poolsTransform);
            cache.Add(prefabKey, resourceGroup);
            return resourceGroup;
        }

        private IPrefabsGroup GetOrCreateGroup(GameObject prefab, DiContainer diContainer)
        {
            var prefabKey = PrefabsGroup.GetPrefabKey(prefab);
            var resourceGroup = cacheCustomContainer.SafeGet((prefabKey, diContainer));
            if (resourceGroup != null) return resourceGroup;

            var prefabName = PrefabsGroup.GetPrefabName(prefab);
            resourceGroup = diContainer.InstantiateComponentOnNewGameObject<PrefabsGroup>($"pool_{prefabName}");

            if (prefab.TryGetComponent<PoolableItemConfig>(out var poolableItemConfigComponent))
                resourceGroup.IsPersistantGroup = poolableItemConfigComponent.PersistantGroup;

            resourceGroup.SetParent(poolsTransform);
            cacheCustomContainer.Add((prefabKey, diContainer), resourceGroup);

            return resourceGroup;
        }

        public void FreeResources()
        {
            var disposedGroupIds = ListPool<int>.Instance.Spawn();

            foreach (var kvp in cache)
            {
                if (kvp.Value.IsPersistantGroup)
                    continue;
                kvp.Value.Dispose();
                disposedGroupIds.Add(kvp.Key);
            }

            var disposedGroupCustomIds = ListPool<(int, DiContainer)>.Instance.Spawn();

            foreach (var kvp in cacheCustomContainer)
            {
                if (kvp.Value.IsPersistantGroup)
                    continue;
                kvp.Value.Dispose();
                disposedGroupCustomIds.Add(kvp.Key);
            }

            foreach (var disposedGroupId in disposedGroupIds) 
                cache.Remove(disposedGroupId);

            foreach (var disposedGroupId in disposedGroupCustomIds) 
                cacheCustomContainer.Remove(disposedGroupId);

            ListPool<int>.Instance.Despawn(disposedGroupIds);
            ListPool<(int, DiContainer)>.Instance.Despawn(disposedGroupCustomIds);
        }
    }
}