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
        public GameObject GetBuilding(BuildingType buildingType) => GetBuildingByType(buildingType);
        public void ReturnBuilding(GameObject buildingObject) => Return(buildingObject);

        /// <summary>
        /// BuildingType 열거형으로 건물을 가져옵니다
        /// </summary>
        public GameObject GetBuildingByType(BuildingType buildingType)
        {
            if (buildingCatalog?.RegisteredPrefabs == null) return null;

            // BuildingCatalog에서 해당 BuildingType을 가진 Building 컴포넌트 찾기
            foreach (GameObject prefab in buildingCatalog.RegisteredPrefabs)
            {
                if (prefab == null) continue;

                Building building = prefab.GetComponent<Building>();
                if (building != null && building.BuildingType == buildingType)
                {
                    return Get(building);
                }
            }

            Debug.LogWarning($"[BuildingPoolingComponent] No building found for BuildingType: {buildingType}");
            return null;
        }
    }
}