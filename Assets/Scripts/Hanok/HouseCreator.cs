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

        private void Awake()
        {
            CreatedHouses = new List<House>();
            ValidateComponents();
        }

        #region Components Validation
        /// <summary>
        /// 필요한 컴포넌트들이 할당되어 있는지 확인하고, 없으면 자동으로 찾아서 할당합니다
        /// </summary>
        private void ValidateComponents()
        {
            ValidateHouseCatalog();
            ValidateBuildingCatalog();
            ValidateHousePoolingComponent();
            ValidateBuildingPoolingComponent();
        }

        private void ValidateHouseCatalog()
        {
            if (houseCatalog == null)
            {
                // 자신에서 찾기
                houseCatalog = GetComponent<HouseCatalog>();

                // 자식들에서 찾기
                if (houseCatalog == null)
                {
                    houseCatalog = GetComponentInChildren<HouseCatalog>();
                }

                if (houseCatalog != null)
                {
                    Debug.Log($"[HouseCreator] HouseCatalog automatically found and assigned from {houseCatalog.gameObject.name}");
                }
                else
                {
                    Debug.LogError($"[HouseCreator] HouseCatalog is missing! Please assign it in the inspector or add it to this GameObject or its children. GameObject: {gameObject.name}");
                }
            }
        }

        private void ValidateBuildingCatalog()
        {
            if (buildingCatalog == null)
            {
                // 자신에서 찾기
                buildingCatalog = GetComponent<BuildingCatalog>();

                // 자식들에서 찾기
                if (buildingCatalog == null)
                {
                    buildingCatalog = GetComponentInChildren<BuildingCatalog>();
                }

                if (buildingCatalog != null)
                {
                    Debug.Log($"[HouseCreator] BuildingCatalog automatically found and assigned from {buildingCatalog.gameObject.name}");
                }
                else
                {
                    Debug.LogError($"[HouseCreator] BuildingCatalog is missing! Please assign it in the inspector or add it to this GameObject or its children. GameObject: {gameObject.name}");
                }
            }
        }

        private void ValidateHousePoolingComponent()
        {
            if (housePoolingComponent == null)
            {
                // 자신에서 찾기
                housePoolingComponent = GetComponent<HousePoolingComponent>();

                // 자식들에서 찾기
                if (housePoolingComponent == null)
                {
                    housePoolingComponent = GetComponentInChildren<HousePoolingComponent>();
                }

                if (housePoolingComponent != null)
                {
                    Debug.Log($"[HouseCreator] HousePoolingComponent automatically found and assigned from {housePoolingComponent.gameObject.name}");
                }
                else
                {
                    Debug.LogError($"[HouseCreator] HousePoolingComponent is missing! Please assign it in the inspector or add it to this GameObject or its children. GameObject: {gameObject.name}");
                }
            }
        }

        private void ValidateBuildingPoolingComponent()
        {
            if (buildingPoolingComponent == null)
            {
                // 자신에서 찾기
                buildingPoolingComponent = GetComponent<BuildingPoolingComponent>();

                // 자식들에서 찾기
                if (buildingPoolingComponent == null)
                {
                    buildingPoolingComponent = GetComponentInChildren<BuildingPoolingComponent>();
                }

                if (buildingPoolingComponent != null)
                {
                    Debug.Log($"[HouseCreator] BuildingPoolingComponent automatically found and assigned from {buildingPoolingComponent.gameObject.name}");
                }
                else
                {
                    Debug.LogError($"[HouseCreator] BuildingPoolingComponent is missing! Please assign it in the inspector or add it to this GameObject or its children. GameObject: {gameObject.name}");
                }
            }
        }

        /// <summary>
        /// 실행 시간에 필수 컴포넌트들이 할당되어 있는지 검증합니다
        /// </summary>
        /// <returns>모든 필수 컴포넌트가 유효하면 true</returns>
        private bool ValidateRequiredComponents()
        {
            bool isValid = true;

            if (housePoolingComponent == null)
            {
                Debug.LogError("[HouseCreator] HousePoolingComponent is missing! Cannot prepare houses without it.");
                isValid = false;
            }

            if (houseCatalog == null)
            {
                Debug.LogError("[HouseCreator] HouseCatalog is missing! Cannot select house types without it.");
                isValid = false;
            }

            if (buildingPoolingComponent == null)
            {
                Debug.LogWarning("[HouseCreator] BuildingPoolingComponent is missing! House building preparation will be skipped.");
                // buildingPoolingComponent는 경고만 출력하고 진행
            }

            if (buildingCatalog == null)
            {
                Debug.LogWarning("[HouseCreator] BuildingCatalog is missing! House building preparation will be skipped.");
                // buildingCatalog는 경고만 출력하고 진행
            }

            return isValid;
        }
        #endregion

        /// <summary>
        /// 여러 Plot을 기반으로 하우스들을 준비합니다
        /// </summary>
        /// <param name="plots">하우스를 배치할 Plot 리스트</param>
        /// <param name="houseType">배치할 하우스 타입 (null이면 자동 선택)</param>
        /// <returns>성공적으로 준비된 하우스 개수</returns>
        public int PrepareHouses(List<Plot> plots, House houseType = null)
        {
            if (plots == null || plots.Count == 0)
            {
                Debug.LogWarning("[HouseCreator] No plots provided for house preparation.");
                return 0;
            }

            // 실행 시간 컴포넌트 검증
            if (!ValidateRequiredComponents())
            {
                return 0;
            }

            ClearExistingHouses();

            int successCount = 0;

            foreach (Plot plot in plots)
            {
                if (plot == null) continue;

                House selectedHouseType = houseType ?? SelectOptimalHouseType(plot);
                if (selectedHouseType == null)
                {
                    Debug.LogWarning($"[HouseCreator] Could not determine house type for plot {plot.name}");
                    continue;
                }

                House preparedHouse = PrepareHouseForPlot(plot, selectedHouseType);
                if (preparedHouse != null)
                {
                    CreatedHouses.Add(preparedHouse);
                    successCount++;
                }
            }

            Debug.Log($"[HouseCreator] Successfully prepared {successCount} houses from {plots.Count} plots");
            return successCount;
        }

        /// <summary>
        /// 단일 Plot에 대해 하우스를 준비합니다
        /// </summary>
        /// <param name="plot">하우스를 배치할 Plot</param>
        /// <param name="houseType">배치할 하우스 타입</param>
        /// <returns>준비된 House 컴포넌트</returns>
        private House PrepareHouseForPlot(Plot plot, House houseType)
        {
            if (plot == null || houseType == null) return null;

            // 풀에서 하우스 가져오기
            GameObject houseObject = housePoolingComponent.GetHouse(houseType);
            if (houseObject == null)
            {
                Debug.LogWarning($"[HouseCreator] Failed to get house of type {houseType.name} from pool");
                return null;
            }

            House house = houseObject.GetComponent<House>();
            if (house == null)
            {
                Debug.LogError($"[HouseCreator] House component not found on pooled object {houseObject.name}");
                return null;
            }

            // Plot 경계에 맞춰 하우스 위치 및 크기 설정
            ConfigureHouseForPlot(house, plot);

            // 하우스 내부 건물들 준비
            PrepareHouseBuildings(house, plot);

            Debug.Log($"[HouseCreator] House {house.name} prepared for plot {plot.name}");
            return house;
        }

        /// <summary>
        /// Plot 크기와 형태에 맞는 최적의 하우스 타입을 선택합니다
        /// </summary>
        /// <param name="plot">분석할 Plot</param>
        /// <returns>최적의 House 타입</returns>
        private House SelectOptimalHouseType(Plot plot)
        {
            if (houseCatalog == null || houseCatalog.RegisteredPrefabs == null || houseCatalog.RegisteredPrefabs.Count == 0)
            {
                Debug.LogWarning("[HouseCreator] No house types available in catalog");
                return null;
            }

            // Plot의 크기 계산
            var plotBounds = CalculatePlotBounds(plot);
            float plotArea = plotBounds.size.x * plotBounds.size.z;

            House bestHouse = null;
            float bestScore = float.MinValue;

            foreach (GameObject prefab in houseCatalog.RegisteredPrefabs)
            {
                if (prefab == null) continue;

                House house = prefab.GetComponent<House>();
                if (house == null) continue;

                // 하우스가 Plot에 맞는지 점수 계산
                float score = CalculateHouseFitScore(house, plotBounds, plotArea);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestHouse = house;
                }
            }

            return bestHouse;
        }

        /// <summary>
        /// 하우스가 Plot에 얼마나 잘 맞는지 점수를 계산합니다
        /// </summary>
        private float CalculateHouseFitScore(House house, Bounds plotBounds, float plotArea)
        {
            // 기본적인 크기 적합도 계산
            float houseLength = house.MaximumLength;
            float plotLength = Mathf.Max(plotBounds.size.x, plotBounds.size.z);

            // 길이가 Plot 범위 내에 있는지 확인
            if (houseLength < house.MinimumLength || houseLength > plotLength - housePadding * 2)
                return float.MinValue; // 맞지 않음

            // 점수 계산: 크기 효율성과 여백 고려
            float sizeEfficiency = houseLength / plotLength;
            float paddingPenalty = (housePadding * 2) / plotLength;

            return sizeEfficiency - paddingPenalty * 0.5f;
        }

        /// <summary>
        /// Plot의 경계 상자를 계산합니다
        /// </summary>
        private Bounds CalculatePlotBounds(Plot plot)
        {
            var vertices = plot.GetFlattenedOutlineVertices();
            if (vertices.Count == 0)
            {
                vertices = plot.GetVertexPositions();
            }

            if (vertices.Count == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            Vector3 min = vertices[0];
            Vector3 max = vertices[0];

            foreach (Vector3 vertex in vertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }

            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;

            return new Bounds(center, size);
        }

        /// <summary>
        /// Plot에 맞춰 하우스의 위치와 크기를 설정합니다
        /// </summary>
        private void ConfigureHouseForPlot(House house, Plot plot)
        {
            var plotBounds = CalculatePlotBounds(plot);

            // 하우스를 Plot 중앙에 배치
            house.transform.position = plotBounds.center;

            // 필요시 하우스 크기 조정 (TODO: House 컴포넌트에 크기 조정 기능 추가 시 구현)
            // house.SetSize(plotBounds.size);
        }

        /// <summary>
        /// 하우스 내부 건물들을 준비합니다
        /// </summary>
        private void PrepareHouseBuildings(House house, Plot plot)
        {
            if (buildingPoolingComponent == null)
            {
                Debug.LogWarning("[HouseCreator] BuildingPoolingComponent not assigned. Skipping building preparation.");
                return;
            }

            if (buildingCatalog == null)
            {
                Debug.LogWarning("[HouseCreator] BuildingCatalog not assigned. Skipping building preparation.");
                return;
            }

            // TODO: 1. House의 필요 건물 리스트 가져오기
            List<BuildingType> requiredBuildings = house.RequiredBuildingTypes;
            if (requiredBuildings == null || requiredBuildings.Count == 0) return;

            // TODO: 2. Plot 내부 영역 계산 및 배치 가능 구역 분석
            // var plotBounds = CalculatePlotBounds(plot);
            // var availableArea = CalculateAvailableBuildingArea(plotBounds, house);            

            // TODO: 3. 각 필요 건물 타입에 대해 반복 배치
            foreach (BuildingType buildingType in requiredBuildings)
            {
                //     TODO: 3.1. 풀에서 건물 가져오기
                GameObject buildingObject = buildingPoolingComponent.GetBuilding(buildingType);
                if (buildingObject == null) continue;
                buildingObject.SetActive(true);

                // Building 컴포넌트 가져오기
                Building building = buildingObject.GetComponent<Building>();
                if (building == null)
                {
                    Debug.LogWarning($"[HouseCreator] Building component not found on {buildingObject.name}");
                    continue;
                }

                //
                //     TODO: 3.2. 건물 배치 위치 계산
                //     Vector3 placementPosition = CalculateBuildingPlacement(buildingType, availableArea, plotBounds);
                //
                //     TODO: 3.3. 건물 위치 및 회전 설정
                    buildingObject.transform.position = house.transform.position;
                //     buildingObject.transform.rotation = CalculateBuildingRotation(buildingType, house);
                //
                //     TODO: 3.4. 건물을 House의 자식으로 설정
                //     buildingObject.transform.SetParent(house.transform, true);
                //
                //     TODO: 3.5. 배치된 건물 영역을 사용 가능 영역에서 제외
                //     UpdateAvailableArea(availableArea, buildingObject, buildingType);

                // 하우스의 건물 리스트에 추가
                if (house.ContainedBuildings == null)
                {
                    // ContainedBuildings가 null인 경우 초기화 (readonly property이므로 직접 할당 불가)
                    Debug.LogWarning($"[HouseCreator] House {house.name} ContainedBuildings list is null. Cannot track building.");
                }
                else
                {
                    house.ContainedBuildings.Add(building);
                }
            }

            // TODO: 4. 배치 완료 후 House 상태 업데이트
            // house.OnBuildingsPlaced();

            Debug.Log($"[HouseCreator] Building preparation for house {house.name} - TODO: Implement building placement logic above");
        }

        /// <summary>
        /// 기존에 생성된 하우스들을 정리합니다
        /// </summary>
        public void ClearExistingHouses()
        {
            if (CreatedHouses == null) return;

            foreach (House house in CreatedHouses)
            {
                if (house != null)
                {
                    // 하우스 내부 건물들 먼저 정리
                    ClearExistingBuildings(house);

                    // 하우스 자체를 풀로 반환
                    if (housePoolingComponent != null)
                    {
                        housePoolingComponent.ReturnHouse(house.gameObject);
                    }
                }
            }

            CreatedHouses.Clear();
        }

        /// <summary>
        /// 특정 하우스 내부의 모든 건물들을 정리합니다
        /// </summary>
        private void ClearExistingBuildings(House house)
        {
            if (house == null || buildingPoolingComponent == null) return;

            // 하우스에 포함된 건물 리스트 사용
            if (house.ContainedBuildings != null)
            {
                // 리스트의 복사본 생성 (순회 중 원본 리스트 수정 방지)
                var buildingsToReturn = new List<Building>(house.ContainedBuildings);

                foreach (Building building in buildingsToReturn)
                {
                    if (building != null)
                    {
                        // 건물을 풀로 반환
                        buildingPoolingComponent.ReturnBuilding(building.gameObject);
                    }
                }

                // 하우스의 건물 리스트 정리
                house.ContainedBuildings.Clear();
            }
        }

        /// <summary>
        /// 모든 준비된 하우스들을 활성화합니다
        /// </summary>
        public void ActivateAllHouses()
        {
            if (CreatedHouses == null) return;

            foreach (House house in CreatedHouses)
            {
                if (house != null)
                {
                    house.gameObject.SetActive(true);
                }
            }

            Debug.Log($"[HouseCreator] Activated {CreatedHouses.Count} houses");
        }

        /// <summary>
        /// 모든 준비된 하우스들을 비활성화합니다
        /// </summary>
        public void DeactivateAllHouses()
        {
            if (CreatedHouses == null) return;

            foreach (House house in CreatedHouses)
            {
                if (house != null)
                {
                    house.gameObject.SetActive(false);
                }
            }

            Debug.Log($"[HouseCreator] Deactivated {CreatedHouses.Count} houses");
        }
    }
}