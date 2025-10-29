using UnityEngine;

namespace Hanok
{
    public class BuildingPoolingComponent : PoolingComponent<Building>
    {
        [SerializeField] private BuildingCatalog buildingCatalog;

        public BuildingCatalog BuildingCatalog => buildingCatalog;

        protected override Catalog GetCatalog() => buildingCatalog;
        protected override string GetComponentTypeName() => "BuildingPoolingComponent";

        public GameObject GetBuilding(Building buildingType) => Get(buildingType);
        public GameObject GetBuilding(BuildingType buildingType, float maxSize=1) => GetBuildingByType(buildingType, maxSize);
        public void ReturnBuilding(GameObject buildingObject) => Return(buildingObject);

        /// <summary>
        /// BuildingType 열거형으로 건물을 가져옵니다
        /// maxSize 이하의 크기 중 가장 큰 건물을 반환합니다
        /// </summary>
        public GameObject GetBuildingByType(BuildingType buildingType, float maxSize = float.MaxValue)
        {
            if (buildingCatalog?.RegisteredPrefabs == null) return null;

            Building bestBuilding = null;
            float bestSize = 0f;

            // BuildingCatalog에서 해당 BuildingType을 가진 Building 컴포넌트 중 가장 큰 것 찾기
            foreach (GameObject prefab in buildingCatalog.RegisteredPrefabs)
            {
                if (prefab == null) continue;

                Building building = prefab.GetComponent<Building>();
                if (building != null && building.BuildingType == buildingType)
                {
                    float buildingSize = Mathf.Max(building.Size2D.x, building.Size2D.y);

                    // maxSize 이하이면서 가장 큰 건물 선택
                    if (buildingSize <= maxSize && buildingSize > bestSize)
                    {
                        bestBuilding = building;
                        bestSize = buildingSize;
                    }
                }
            }

            if (bestBuilding != null)
            {
                return Get(bestBuilding);
            }

            return null;
        }
    }
}