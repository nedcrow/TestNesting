using System.Collections.Generic;
using UnityEngine;

namespace Hanok
{
    /// <summary>
    /// 테두리를 따라 배치되는 건물 (담장, 울타리 등)
    /// 문, 담, 끝단 GameObject 정보를 담는 데이터 컨테이너
    /// </summary>
    public class PerimeterBuilding : Building
    {
        [Header("Perimeter Components")]
        [SerializeField] private GameObject doorPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject endPrefab;

        [Header("Component Settings")]
        [SerializeField] private BasicPoolingComponent poolingComponent = null;
        [SerializeField] private float wallSegmentLength = 2f;
        [SerializeField] private float doorWidth = 1.5f;
        [SerializeField] private bool allowMultipleDoors = false;
        [SerializeField] private int maxDoors = 1;

        public GameObject DoorPrefab => doorPrefab;
        public GameObject WallPrefab => wallPrefab;
        public GameObject EndPrefab => endPrefab;
        public float WallSegmentLength => wallSegmentLength;
        public float DoorWidth => doorWidth;
        public bool AllowMultipleDoors => allowMultipleDoors;
        public int MaxDoors => maxDoors;

        public void Start()
        {
            if (poolingComponent == null)
            {
                poolingComponent = GetComponent<BasicPoolingComponent>();
            }
        }

        public override void CompleteBuildingOrder(House house)
        {
            Transform doorTransform;
            List<Transform> wallTransforms;
            List<Transform> cornerTransforms;

            // BlueprintRules에서 배치 위치 정보 가져오기
            BlueprintRules.PlacePerimeterBuilding(
                house,
                out doorTransform,
                out wallTransforms,
                out cornerTransforms
                );

            // 실제 프리팹 인스턴스 생성 및 배치
            CreateAndPlaceInstances(doorTransform, wallTransforms, cornerTransforms);

            Debug.Log($"CompleteBuildingOrder");
            if (house.OutlineVertices.Count != 0)
            {
                foreach (var outline in house.OutlineVertices)
                {
                    Debug.Log($"outline count = {outline.Count}");
                }
            }
        }

        /// <summary>
        /// Transform 정보를 기반으로 실제 프리팹 인스턴스를 생성하고 배치합니다
        /// </summary>
        private void CreateAndPlaceInstances(Transform doorTransform, List<Transform> wallTransforms, List<Transform> cornerTransforms)
        {
            if (poolingComponent == null)
            {
                Debug.LogError("[PerimeterBuilding] PoolingComponent is not assigned!");
                return;
            }

            // 등록된 문 GameObject 직접 사용 (새 인스턴스 생성하지 않음)
            if (doorTransform != null && doorPrefab != null)
            {
                doorPrefab.transform.position = doorTransform.position;
                doorPrefab.transform.rotation = doorTransform.rotation;
                doorPrefab.transform.SetParent(transform, true);
                doorPrefab.SetActive(true);
            }

            // 담장: 등록된 wallPrefab + 풀링된 추가 인스턴스들 함께 사용
            if (wallTransforms != null && wallPrefab != null)
            {
                bool useRegisteredWall = true;

                foreach (Transform wallTransform in wallTransforms)
                {
                    if (wallTransform == null) continue;

                    GameObject wallInstance;

                    // 첫 번째는 등록된 wallPrefab 사용
                    if (useRegisteredWall)
                    {
                        wallInstance = wallPrefab;
                        useRegisteredWall = false;
                    }
                    else
                    {
                        // 나머지는 풀에서 가져오기
                        wallInstance = poolingComponent.GetGameObject(wallPrefab);
                    }

                    if (wallInstance != null)
                    {
                        wallInstance.transform.position = wallTransform.position;
                        wallInstance.transform.rotation = wallTransform.rotation;
                        wallInstance.transform.SetParent(transform, true);
                        wallInstance.SetActive(true);
                    }
                }
            }

            // 코너: 등록된 endPrefab + 풀링된 추가 인스턴스들 함께 사용
            if (cornerTransforms != null && endPrefab != null)
            {
                bool useRegisteredEnd = true;

                foreach (Transform cornerTransform in cornerTransforms)
                {
                    if (cornerTransform == null) continue;

                    GameObject cornerInstance;

                    // 첫 번째는 등록된 endPrefab 사용
                    if (useRegisteredEnd)
                    {
                        cornerInstance = endPrefab;
                        useRegisteredEnd = false;
                    }
                    else
                    {
                        // 나머지는 풀에서 가져오기
                        cornerInstance = poolingComponent.GetGameObject(endPrefab);
                    }

                    if (cornerInstance != null)
                    {
                        cornerInstance.transform.position = cornerTransform.position;
                        cornerInstance.transform.rotation = cornerTransform.rotation;
                        cornerInstance.transform.SetParent(transform, true);
                        cornerInstance.SetActive(true);
                    }
                }
            }

            // 원본 Transform들 정리 (BlueprintRules에서 생성된 임시 GameObject들)
            CleanupTemporaryTransforms(doorTransform, wallTransforms, cornerTransforms);
        }

        /// <summary>
        /// BlueprintRules에서 생성된 임시 Transform GameObject들을 정리합니다
        /// </summary>
        private void CleanupTemporaryTransforms(Transform doorTransform, List<Transform> wallTransforms, List<Transform> cornerTransforms)
        {
            // 문 Transform 정리
            if (doorTransform != null && doorTransform.gameObject != null)
            {
                DestroyImmediate(doorTransform.gameObject);
            }

            // 담장 Transform들 정리
            if (wallTransforms != null)
            {
                foreach (Transform wallTransform in wallTransforms)
                {
                    if (wallTransform != null && wallTransform.gameObject != null)
                    {
                        DestroyImmediate(wallTransform.gameObject);
                    }
                }
            }

            // 코너 Transform들 정리
            if (cornerTransforms != null)
            {
                foreach (Transform cornerTransform in cornerTransforms)
                {
                    if (cornerTransform != null && cornerTransform.gameObject != null)
                    {
                        DestroyImmediate(cornerTransform.gameObject);
                    }
                }
            }
        }
    }
}