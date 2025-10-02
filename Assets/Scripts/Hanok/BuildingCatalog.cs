using System.Collections.Generic;
using UnityEngine;

namespace Hanok
{
    public class BuildingCatalog : Catalog
    {
        [SerializeField] private List<Building> buildingPrefabs;
        
        public List<Building> BuildingPrefabs => buildingPrefabs;
    }
}