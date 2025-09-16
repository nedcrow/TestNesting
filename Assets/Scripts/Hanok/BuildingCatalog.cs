using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hanok
{
    [CreateAssetMenu(fileName = "BuildingCatalog", menuName = "Hanok/Building Catalog")]
    public class BuildingCatalog : Catalog
    {
        [Header("Inspector References")]
        [SerializeField] private List<BuildingTypeGroup> buildingGroups = new List<BuildingTypeGroup>();
        public List<BuildingTypeGroup> BuildingGroups => buildingGroups;

        // [Header("Runtime Debug - Building Groups by Type")]

        [System.Serializable]
        public class BuildingTypeGroup
        {
            [SerializeField] private BuildingType buildingType;
            [SerializeField] private List<GameObject> prefabs = new List<GameObject>();

            public BuildingType BuildingType => buildingType;
            public List<GameObject> Prefabs => prefabs;

            public BuildingTypeGroup(BuildingType type)
            {
                buildingType = type;
            }

            public void AddPrefab(GameObject prefab)
            {
                if (!prefabs.Contains(prefab))
                {
                    prefabs.Add(prefab);
                }
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                GroupBuildingsByType();
            }
        }

        [ContextMenu("Group Buildings by Type")]
        public void GroupBuildingsByType()
        {
            buildingGroups.Clear();

            if (registeredPrefabs == null || registeredPrefabs.Count == 0)
                return;

            foreach (GameObject prefab in registeredPrefabs)
            {
                if (prefab == null) continue;

                Building buildingComponent = prefab.GetComponent<Building>();
                if (buildingComponent == null) continue;

                BuildingType buildingType = buildingComponent.BuildingType;

                // 해당 타입의 그룹 찾기 또는 생성
                BuildingTypeGroup group = buildingGroups.FirstOrDefault(g => g.BuildingType == buildingType);
                if (group == null)
                {
                    group = new BuildingTypeGroup(buildingType);
                    buildingGroups.Add(group);
                }

                group.AddPrefab(prefab);
            }

            // 타입별로 정렬
            buildingGroups = buildingGroups.OrderBy(g => g.BuildingType.ToString()).ToList();
        }

        public Building GetLongest(BuildingType type)
        {
            BuildingTypeGroup group = buildingGroups.FirstOrDefault(g => g.BuildingType == type);
            if (group == null || group.Prefabs == null || group.Prefabs.Count == 0)
                return null;

            Building longestBuilding = null;
            float maxLength = 0f;

            foreach (GameObject prefab in group.Prefabs)
            {
                if (prefab == null) continue;

                Building building = prefab.GetComponent<Building>();
                if (building == null) continue;

                // size2D의 x, y 중 더 긴 축 찾기
                float currentLength = Mathf.Max(building.Size2D.x, building.Size2D.y);
                
                if (currentLength > maxLength)
                {
                    maxLength = currentLength;
                    longestBuilding = building;
                }
            }

            return longestBuilding;
        }
    }
}