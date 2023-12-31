﻿using UnityEngine;

namespace Utilities.Prefabs
{
    public interface IPrefabPool
    {
        GameObject Spawn(GameObject prefab, Transform parent = null);
        GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation);
        GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null);
        void Despawn(GameObject instance);
    }
}