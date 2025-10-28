using System.Collections.Generic;
using UnityEngine;

namespace Hanok
{
    public class HouseCatalog : Catalog
    {
        [SerializeField] private List<House> housePrefabs;
        
        public List<House> HousePrefabs => housePrefabs;
    }
}