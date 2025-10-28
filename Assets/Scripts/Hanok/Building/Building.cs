using System.Collections.Generic;
using UnityEngine;

namespace Hanok
{
    public enum BuildingMode
    {
        Waiting,
        Preview,
        UnderConstruction,
        Completed,
        Decayed
    }

    [System.Serializable]
    public class ConstructionMaterial
    {
        public string materialName;
        public int requiredAmount;
    }

    public class Building : MonoBehaviour
    {
        [SerializeField] private string buildingName;
        [SerializeField] private BuildingType buildingType;
        [SerializeField] private Vector2 size2D;
        [SerializeField] private int priority;
        [SerializeField] private bool allowDuplicates = true;
        [SerializeField] private List<ConstructionMaterial> requiredMaterials;

        public string BuildingName => buildingName;
        public BuildingType BuildingType => buildingType;
        public Vector2 Size2D => size2D;
        public int Priority => priority;
        public bool AllowDuplicates => allowDuplicates;
        public List<ConstructionMaterial> RequiredMaterials => requiredMaterials;
        public BuildingMode Mode { get; set; } = BuildingMode.Waiting;

        public virtual void CompleteBuildingOrder(House house) { }
    }
}