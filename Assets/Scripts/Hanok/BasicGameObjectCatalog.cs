using System.Collections.Generic;
using UnityEngine;

namespace Hanok
{
    public class BasicGameObjectCatalog : Catalog
    {
        [SerializeField] private List<GameObject> prefabs;

        public List<GameObject> Prefabs => prefabs;
    }
}