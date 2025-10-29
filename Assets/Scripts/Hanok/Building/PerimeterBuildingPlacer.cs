using System.Collections.Generic;
using UnityEngine;

namespace Hanok
{
    /// <summary>
    /// PerimeterBuilding의 배치 로직을 담당하는 컴포넌트
    /// 문, 담장, 코너를 배치합니다
    /// </summary>
    public class PerimeterBuildingPlacer : MonoBehaviour
    {
        [SerializeField] private BasicPoolingComponent poolingComponent = null;
        private BasicPoolingComponent PoolingComponent => poolingComponent;

        public void PlacePerimeterBuilding(PerimeterBuilding building, House house, BasicPoolingComponent targetPoolingComponent)
        {
            if (building == null || house == null)
            {
                Debug.LogWarning("[PerimeterBuildingPlacer] Building or House is null");
                return;
            }

            if (house.OutlineVertices == null || house.OutlineVertices.Count != 4)
            {
                Debug.LogWarning($"[PerimeterBuildingPlacer] House must have exactly 4 edges, but has {house.OutlineVertices?.Count ?? 0} ({house.name})");
                return;
            }

            poolingComponent = targetPoolingComponent;

            if (PoolingComponent == null)
            {
                Debug.LogWarning($"[PerimeterBuildingPlacer] Null BasicPoolingComponent ({house.name})");
                return;
            }

            building.transform.SetParent(house.transform, true);
            building.transform.localPosition = Vector3.zero;

            // 1. 배치 위치 계산
            Transform doorTransform = CreateDoor(house);
            List<Transform> cornerTransforms = CreateCorners(house);
            List<Transform> wallTransforms = CreateWalls(house, doorTransform, building.WallSegmentLength);

            // 2. 실제 프리팹 인스턴스 생성 및 배치
            InstantiatePrefabs(building, doorTransform, wallTransforms, cornerTransforms);

            // 3. 임시 Transform 정리
            CleanupTemporaryTransforms(doorTransform, wallTransforms, cornerTransforms);

            poolingComponent = null;
        }

        #region Blueprint Rules

        /// <summary>
        /// 문을 생성합니다 (첫 번째 라인의 중앙에 배치)
        /// </summary>
        private Transform CreateDoor(House house)
        {
            List<Vector3> firstEdge = house.OutlineVertices[0];
            Vector3 doorPosition;

            // 첫 번째 라인이 곡선인지 확인
            if (IsCurvedEdge(firstEdge))
            {
                // 곡선인 경우: 중간 정점
                int midIndex = firstEdge.Count / 2;
                doorPosition = firstEdge[midIndex];
            }
            else
            {
                // 직선인 경우: 시작-끝 사이의 중앙
                doorPosition = Vector3.Lerp(firstEdge[0], firstEdge[firstEdge.Count - 1], 0.5f);
            }

            // 문 GameObject 생성
            GameObject doorObj = new GameObject("PerimeterDoor");
            doorObj.transform.position = doorPosition;
            doorObj.transform.SetParent(house.transform);

            // 문 방향 설정 (첫 번째 라인의 방향)
            Vector3 doorDirection = (firstEdge[firstEdge.Count - 1] - firstEdge[0]).normalized;
            doorObj.transform.rotation = Quaternion.LookRotation(doorDirection);

            return doorObj.transform;
        }

        /// <summary>
        /// 코너들을 생성합니다 (4개 라인의 연결 지점)
        /// </summary>
        private List<Transform> CreateCorners(House house)
        {
            var corners = new List<Transform>();

            // 4개의 라인을 순회하며 각 라인의 시작점에 코너 생성
            for (int i = 0; i < house.OutlineVertices.Count; i++)
            {
                List<Vector3> currentEdge = house.OutlineVertices[i];
                List<Vector3> prevEdge = house.OutlineVertices[(i - 1 + house.OutlineVertices.Count) % house.OutlineVertices.Count];

                // 현재 라인의 시작점 = 코너 위치
                Vector3 cornerPosition = currentEdge[0];

                GameObject cornerObj = new GameObject($"PerimeterCorner_{i}");
                cornerObj.transform.position = cornerPosition;
                cornerObj.transform.SetParent(house.transform);

                // 코너 회전: 이전 라인의 끝 방향과 현재 라인의 시작 방향의 bisector
                Vector3 incomingDir = (prevEdge[prevEdge.Count - 1] - prevEdge[0]).normalized;
                Vector3 outgoingDir = (currentEdge[currentEdge.Count - 1] - currentEdge[0]).normalized;

                // 첫 번째와 두 번째 코너는 정방향, 세 번째와 네 번째는 반대 방향
                if (i == 0 || i == 1)
                {
                    Vector3 bisector = (incomingDir + outgoingDir).normalized;
                    cornerObj.transform.rotation = Quaternion.LookRotation(bisector);
                }
                else
                {
                    Vector3 bisector = -(incomingDir + outgoingDir).normalized;
                    cornerObj.transform.rotation = Quaternion.LookRotation(bisector);
                }

                corners.Add(cornerObj.transform);
            }

            return corners;
        }

        /// <summary>
        /// 담장들을 생성합니다 (4개 라인을 따라 배치)
        /// </summary>
        private List<Transform> CreateWalls(House house, Transform door, float wallSegmentLength)
        {
            var walls = new List<Transform>();

            // 4개의 라인을 순회
            for (int i = 0; i < house.OutlineVertices.Count; i++)
            {
                List<Vector3> edge = house.OutlineVertices[i];

                // 첫 번째 라인(i=0)에 문이 있는지 확인
                bool hasDoorOnThisEdge = (i == 0 && door != null);

                // 해당 라인이 곡선인지 확인
                if (IsCurvedEdge(edge))
                {
                    // 곡선 담장 생성
                    CreateCurvedWalls(house, edge, walls, hasDoorOnThisEdge, door, i, wallSegmentLength);
                }
                else
                {
                    // 직선 담장 생성
                    CreateStraightWalls(house, edge[0], edge[edge.Count - 1], walls, hasDoorOnThisEdge, door, i, wallSegmentLength);
                }
            }

            return walls;
        }

        /// <summary>
        /// 특정 변이 곡선인지 확인합니다 (3개 이상의 정점이 있으면 곡선)
        /// </summary>
        private bool IsCurvedEdge(List<Vector3> edgeVertices)
        {
            return edgeVertices != null && edgeVertices.Count > 2;
        }

        #endregion

        #region Wall Creation

        /// <summary>
        /// 직선 담장을 생성합니다
        /// </summary>
        private void CreateStraightWalls(House house, Vector3 start, Vector3 end, List<Transform> walls, bool hasDoor, Transform door, int edgeIndex, float wallSegmentLength)
        {
            Vector3 direction = (end - start).normalized;
            float totalLength = Vector3.Distance(start, end);
            int segmentCount = Mathf.CeilToInt(totalLength / wallSegmentLength);
            float actualSegmentLength = totalLength / segmentCount;

            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 segmentCenter = start + direction * (i * actualSegmentLength + actualSegmentLength * 0.5f);

                // 문 위치 건너뛰기
                if (hasDoor && door != null && Vector3.Distance(segmentCenter, door.position) < 0.49f)
                    continue;

                GameObject wallObj = new GameObject($"PerimeterWall_Edge{edgeIndex}_Segment{i}");
                wallObj.transform.position = segmentCenter;
                wallObj.transform.SetParent(house.transform);
                wallObj.transform.rotation = Quaternion.LookRotation(direction);

                walls.Add(wallObj.transform);
            }
        }

        /// <summary>
        /// 곡선 담장을 생성합니다 (지정된 거리 간격으로 재샘플링)
        /// </summary>
        private void CreateCurvedWalls(House house, List<Vector3> curvePoints, List<Transform> walls, bool hasDoor, Transform door, int edgeIndex, float wallSegmentLength)
        {
            if (curvePoints == null || curvePoints.Count < 2) return;

            // 곡선을 지정된 거리 간격으로 재샘플링
            List<Vector3> resampledPoints = ResampleCurveByDistance(curvePoints, wallSegmentLength);

            for (int i = 0; i < resampledPoints.Count; i++)
            {
                Vector3 wallPosition = resampledPoints[i];

                // 문 위치 건너뛰기
                if (hasDoor && door != null && Vector3.Distance(wallPosition, door.position) < 0.49f)
                    continue;

                GameObject wallObj = new GameObject($"PerimeterWall_Edge{edgeIndex}_Point{i}");
                wallObj.transform.position = wallPosition;
                wallObj.transform.SetParent(house.transform);

                // 방향 계산: 다음 점이 있으면 그 방향, 없으면 이전 점 방향
                Vector3 direction;
                if (i < resampledPoints.Count - 1)
                {
                    direction = (resampledPoints[i + 1] - wallPosition).normalized;
                }
                else if (i > 0)
                {
                    direction = (wallPosition - resampledPoints[i - 1]).normalized;
                }
                else
                {
                    // 단일 점인 경우 원본 곡선 방향 사용
                    direction = curvePoints.Count > 1 ? (curvePoints[1] - curvePoints[0]).normalized : Vector3.forward;
                }

                wallObj.transform.rotation = Quaternion.LookRotation(direction);
                walls.Add(wallObj.transform);
            }
        }

        /// <summary>
        /// 곡선을 지정된 거리 간격으로 재샘플링합니다
        /// </summary>
        private List<Vector3> ResampleCurveByDistance(List<Vector3> curvePoints, float interval)
        {
            List<Vector3> resampledPoints = new List<Vector3>();

            if (curvePoints == null || curvePoints.Count < 2)
                return resampledPoints;

            // 전체 곡선 길이 계산
            float totalLength = 0f;
            List<float> cumulativeLengths = new List<float> { 0f };

            for (int i = 0; i < curvePoints.Count - 1; i++)
            {
                float segmentLength = Vector3.Distance(curvePoints[i], curvePoints[i + 1]);
                totalLength += segmentLength;
                cumulativeLengths.Add(totalLength);
            }

            if (totalLength < 0.001f)
            {
                resampledPoints.Add(curvePoints[0]);
                return resampledPoints;
            }

            // 간격마다 샘플링
            float currentDistance = 0f;
            while (currentDistance <= totalLength)
            {
                Vector3 point = GetPointAtDistance(curvePoints, cumulativeLengths, currentDistance);
                resampledPoints.Add(point);
                currentDistance += interval;
            }

            return resampledPoints;
        }

        /// <summary>
        /// 곡선 상의 특정 거리에 위치한 점을 반환합니다
        /// </summary>
        private Vector3 GetPointAtDistance(List<Vector3> curvePoints, List<float> cumulativeLengths, float targetDistance)
        {
            // 시작점보다 앞이면 시작점 반환
            if (targetDistance <= 0f)
                return curvePoints[0];

            // 끝점보다 뒤면 끝점 반환
            float totalLength = cumulativeLengths[cumulativeLengths.Count - 1];
            if (targetDistance >= totalLength)
                return curvePoints[curvePoints.Count - 1];

            // 해당 거리가 속한 세그먼트 찾기
            for (int i = 0; i < cumulativeLengths.Count - 1; i++)
            {
                if (targetDistance >= cumulativeLengths[i] && targetDistance <= cumulativeLengths[i + 1])
                {
                    // 세그먼트 내 로컬 위치 계산
                    float segmentStart = cumulativeLengths[i];
                    float segmentEnd = cumulativeLengths[i + 1];
                    float segmentLength = segmentEnd - segmentStart;

                    if (segmentLength < 0.001f)
                        return curvePoints[i];

                    float t = (targetDistance - segmentStart) / segmentLength;
                    return Vector3.Lerp(curvePoints[i], curvePoints[i + 1], t);
                }
            }

            return curvePoints[curvePoints.Count - 1];
        }

        #endregion

        #region Prefab Instantiation

        /// <summary>
        /// Transform 정보를 기반으로 실제 프리팹 인스턴스를 생성하고 배치합니다
        /// </summary>
        private void InstantiatePrefabs(PerimeterBuilding building, Transform doorTransform, List<Transform> wallTransforms, List<Transform> cornerTransforms)
        {
            // 문 배치
            if (doorTransform != null && building.DoorPrefab != null)
            {
                GameObject doorInstance = PoolingComponent.GetGameObject(building.DoorPrefab);
                if (doorInstance != null)
                {
                    doorInstance.transform.position = doorTransform.position;
                    doorInstance.transform.rotation = doorTransform.rotation;
                    doorInstance.transform.SetParent(transform, true);
                    doorInstance.SetActive(true);
                }
            }

            // 담장 배치
            if (wallTransforms != null && building.WallPrefab != null)
            {
                foreach (Transform wallTransform in wallTransforms)
                {
                    if (wallTransform == null) continue;

                    GameObject wallInstance = PoolingComponent.GetGameObject(building.WallPrefab);
                    if (wallInstance != null)
                    {
                        wallInstance.transform.position = wallTransform.position;
                        wallInstance.transform.rotation = wallTransform.rotation;
                        wallInstance.transform.SetParent(transform, true);
                        wallInstance.SetActive(true);
                    }
                }
            }

            // 코너 배치
            if (cornerTransforms != null && building.EndPrefab != null)
            {
                foreach (Transform cornerTransform in cornerTransforms)
                {
                    if (cornerTransform == null) continue;

                    GameObject cornerInstance = PoolingComponent.GetGameObject(building.EndPrefab);
                    if (cornerInstance != null)
                    {
                        cornerInstance.transform.position = cornerTransform.position;
                        cornerInstance.transform.rotation = cornerTransform.rotation;
                        cornerInstance.transform.SetParent(transform, true);
                        cornerInstance.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// 임시 Transform GameObject들을 정리합니다
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

        #endregion
    }
}
