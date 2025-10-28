using System;
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

    /// <summary>
    /// Unity 직렬화를 위한 Vector3 리스트 래퍼 클래스
    /// </summary>
    [Serializable]
    public class Vector3ListWrapper
    {
        public List<Vector3> vertices = new List<Vector3>();

        public Vector3ListWrapper() { }
        public Vector3ListWrapper(List<Vector3> vertices)
        {
            this.vertices = vertices;
        }
    }

    public class House : MonoBehaviour
    {
        [SerializeField] private List<BuildingType> requiredBuildingTypes;
        [SerializeField] private List<Vector3ListWrapper> outlineVerticesWrapper = new List<Vector3ListWrapper>();
        [SerializeField] private float minimumLength = 5f;
        [SerializeField] private float maximumLength = 15f;
        [SerializeField] private List<Building> containedBuildings;

        public List<BuildingType> RequiredBuildingTypes => requiredBuildingTypes;

        // OutlineVertices는 래퍼를 실제 List<List<Vector3>>로 변환하여 반환
        public List<List<Vector3>> OutlineVertices
        {
            get
            {
                var result = new List<List<Vector3>>();
                foreach (var wrapper in outlineVerticesWrapper)
                {
                    result.Add(wrapper.vertices);
                }
                return result;
            }
            set
            {
                outlineVerticesWrapper.Clear();
                foreach (var vertexList in value)
                {
                    outlineVerticesWrapper.Add(new Vector3ListWrapper(vertexList));
                }
            }
        }

        public float MinimumLength => minimumLength;
        public float MaximumLength => maximumLength;
        public List<Building> ContainedBuildings => containedBuildings;
        public HouseMode Mode { get; set; } = HouseMode.Preview;

        public void CompleteHouseOrder()
        {
            foreach (Building building in ContainedBuildings)
            {
                building.CompleteBuildingOrder(this);
            }
            Mode = HouseMode.PlacementConfirmed;
        }
    }
}