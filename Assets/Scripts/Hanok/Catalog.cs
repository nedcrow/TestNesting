using System.Collections.Generic;
using UnityEngine;

namespace Hanok
{
    public abstract class Catalog : MonoBehaviour
    {
        [SerializeField] protected List<GameObject> registeredPrefabs;
        
        public List<GameObject> RegisteredPrefabs => registeredPrefabs;
        public int Count => registeredPrefabs?.Count ?? 0;
    }
}