using System.Collections.Generic;
using UnityEngine;

namespace Hanok
{
    public enum HouseMode
    {
        Preview,
        PlacementConfirmed,
        UnderConstruction,
        Completed
    }

    public class House : MonoBehaviour
    {
        [SerializeField] private List<BuildingType> requiredBuildingTypes;
        [SerializeField] private List<List<Vector3>> outlineVertices;
        [SerializeField] private float minimumLength = 5f;
        [SerializeField] private float maximumLength = 15f;
        [SerializeField] private List<Building> containedBuildings;
        
        public List<BuildingType> RequiredBuildingTypes => requiredBuildingTypes;
        public List<List<Vector3>> OutlineVertices => outlineVertices;
        public float MinimumLength => minimumLength;
        public float MaximumLength => maximumLength;
        public List<Building> ContainedBuildings => containedBuildings;
        public HouseMode Mode { get; set; } = HouseMode.Preview;
        public float CurrentLength { get; set; }
    }
}