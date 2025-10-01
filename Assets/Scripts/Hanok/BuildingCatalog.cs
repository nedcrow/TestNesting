using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hanok
{
    [CreateAssetMenu(fileName = "BuildingCatalog", menuName = "Hanok/Building Catalog")]
    public class BuildingCatalog : Catalog
    {
        [SerializeField] private List<Building> buildingPrefabs;
        
        public List<Building> BuildingPrefabs => buildingPrefabs;
    }
}