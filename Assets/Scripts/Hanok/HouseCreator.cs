using System.Collections.Generic;
using UnityEngine;

namespace Hanok
{
    public class HouseCreator : MonoBehaviour
    {
        [SerializeField] private HouseCatalog houseCatalog;
        [SerializeField] private BuildingCatalog buildingCatalog;
        [SerializeField] private HousePoolingComponent housePoolingComponent;
        [SerializeField] private BuildingPoolingComponent buildingPoolingComponent;
        [SerializeField] private float housePadding = 1f;
        
        public List<House> CreatedHouses { get; private set; }
        public Vector3 FirstPlotLine { get; set; }
        public int MaxHousesPerPlot { get; set; } = 10;
    }
}