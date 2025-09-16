using System.Collections.Generic;
using UnityEngine;

namespace Hanok
{
    [CreateAssetMenu(fileName = "HouseCatalog", menuName = "Hanok/House Catalog")]
    public class HouseCatalog : Catalog
    {
        [SerializeField] private List<House> housePrefabs;
        
        public List<House> HousePrefabs => housePrefabs;
    }
}