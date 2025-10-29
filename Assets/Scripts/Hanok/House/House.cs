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

        public void PrepareBuilding(List<Building> buildings)
        {
            containedBuildings = buildings;

            if (buildings == null || buildings.Count == 0) return;

            // 각 건물을 타입에 맞춰 배치
            foreach (Building building in buildings)
            {
                if (building == null) continue;

                switch (building.BuildingType)
                {
                    case BuildingType.An_chae:
                        PlaceAnchae(building);
                        break;

                    default:
                        break;
                }
            }
        }
        public void CompleteHouseOrder(BasicPoolingComponent buildingMemberPooler)
        {
            foreach (var building in containedBuildings)
            {
                if (building is PerimeterBuilding pb)
                {
                    var perimeterBuildingPlacer = GetComponent<PerimeterBuildingPlacer>();
                    if (perimeterBuildingPlacer == null) continue;
                    perimeterBuildingPlacer.PlacePerimeterBuilding(pb, this, buildingMemberPooler);
                }
                building.CompleteBuildingOrder(this);
            }
            Mode = HouseMode.PlacementConfirmed;
        }

        private void PlaceAnchae(Building building)
        {
            if (building == null || OutlineVertices == null || OutlineVertices.Count < 4)
            {
                Debug.LogWarning("[House] Cannot place Anchae: invalid building or outline vertices");
                return;
            }

            // 첫 번째와 세 번째 아웃라인 가져오기
            List<Vector3> firstOutline = OutlineVertices[0];
            List<Vector3> thirdOutline = OutlineVertices[2];

            // 각 아웃라인의 중앙점 계산
            Vector3 firstCenter = Vector3.Lerp(firstOutline[0], firstOutline[firstOutline.Count - 1], 0.5f);
            Vector3 thirdCenter = Vector3.Lerp(thirdOutline[0], thirdOutline[thirdOutline.Count - 1], 0.5f);

            // House 중앙 (첫 번째와 세 번째 아웃라인 중앙점의 후미)
            Vector3 housePosition = Vector3.Lerp(firstCenter, thirdCenter, 0.72f);

            // 건물 위치 설정
            building.transform.position = housePosition;

            // 건물이 첫 번째 아웃라인 중앙을 바라보도록 회전
            Vector3 lookDirection = (firstCenter - housePosition).normalized;
            if (lookDirection != Vector3.zero)
            {
                building.transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            // 건물을 House의 자식으로 설정
            building.transform.SetParent(transform, true);
        }
    }
}