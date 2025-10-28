using System.Collections.Generic;
using UnityEngine;

namespace Hanok
{
    public static class BlueprintRules
    {
        public static void PlacePerimeterBuilding(House house, out Transform door, out List<Transform> walls, out List<Transform> corners, bool hasCurve = false)
        {
            door = null;
            walls = new List<Transform>();
            corners = new List<Transform>();

            if (house == null || house.OutlineVertices == null)
            {
                Debug.LogWarning("[BlueprintRules] House or OutlineVertices is null");
                return;
            }

            // 하우스 영역의 테두리 정점들 가져오기
            List<Vector3> perimeterVertices = GetPerimeterVertices(house.OutlineVertices);
            if (perimeterVertices.Count < 4)
            {
                Debug.LogWarning("[BlueprintRules] Not enough perimeter vertices for perimeter building");
                return;
            }

            // 문 위치 계산 및 생성
            door = CreateDoor(house, perimeterVertices, hasCurve);

            // 코너 위치 계산 및 생성
            corners = CreateCorners(house, perimeterVertices);

            // 담장 위치 계산 및 생성
            walls = CreateWalls(house, perimeterVertices, door, hasCurve);
        }

        /// <summary>
        /// 하우스의 테두리 정점들을 가져옵니다
        /// </summary>
        private static List<Vector3> GetPerimeterVertices(List<List<Vector3>> outlineVertices)
        {
            var vertices = new List<Vector3>();

            vertices.Add(outlineVertices[0][0]);
            vertices.Add(outlineVertices[0][outlineVertices[0].Count-1]);
            vertices.Add(outlineVertices[2][0]);
            vertices.Add(outlineVertices[2][outlineVertices[2].Count-1]);

            return vertices;
        }

        /// <summary>
        /// 문을 생성합니다
        /// </summary>
        private static Transform CreateDoor(House house, List<Vector3> perimeterVertices, bool hasCurve)
        {
            Vector3 doorPosition;

            if (hasCurve && house.OutlineVertices.Count > 0 && house.OutlineVertices[0].Count > 0)
            {
                // 곡선인 경우: OutlineVertices[0]의 중앙
                var curveVertices = house.OutlineVertices[0];
                int midIndex = curveVertices.Count / 2;
                doorPosition = curveVertices[midIndex];
            }
            else
            {
                // 직선인 경우: 0,1번째 코너 사이의 중앙
                doorPosition = Vector3.Lerp(perimeterVertices[0], perimeterVertices[1], 0.5f);
            }

            // 문 GameObject 생성
            GameObject doorObj = new GameObject("PerimeterDoor");
            doorObj.transform.position = doorPosition;
            doorObj.transform.SetParent(house.transform);

            // 문 방향 설정 (0,1 코너 방향)
            Vector3 doorDirection = (perimeterVertices[1] - perimeterVertices[0]).normalized;
            doorObj.transform.rotation = Quaternion.LookRotation(doorDirection);

            return doorObj.transform;
        }

        /// <summary>
        /// 코너들을 생성합니다
        /// </summary>
        private static List<Transform> CreateCorners(House house, List<Vector3> perimeterVertices)
        {
            var corners = new List<Transform>();

            for (int i = 0; i < perimeterVertices.Count; i++)
            {
                GameObject cornerObj = new GameObject($"PerimeterCorner_{i}");
                cornerObj.transform.position = perimeterVertices[i];
                cornerObj.transform.SetParent(house.transform);

                // 코너 회전 계산
                Vector3 incomingDir = Vector3.zero;
                Vector3 outgoingDir = Vector3.zero;

                if (perimeterVertices.Count > 2)
                {
                    int prevIndex = (i - 1 + perimeterVertices.Count) % perimeterVertices.Count;
                    int nextIndex = (i + 1) % perimeterVertices.Count;

                    incomingDir = (perimeterVertices[i] - perimeterVertices[prevIndex]).normalized;
                    outgoingDir = (perimeterVertices[nextIndex] - perimeterVertices[i]).normalized;
                }

                // 0,1번째 코너는 문과 같은 각도, 2,3번째 코너는 문과 반대 각도
                if (i == 0 || i == 1)
                {
                    // 문과 같은 각도 (정방향)
                    Vector3 bisector = (incomingDir + outgoingDir).normalized;
                    cornerObj.transform.rotation = Quaternion.LookRotation(bisector);
                }
                else
                {
                    // 문과 반대 각도
                    Vector3 bisector = -(incomingDir + outgoingDir).normalized;
                    cornerObj.transform.rotation = Quaternion.LookRotation(bisector);
                }

                corners.Add(cornerObj.transform);
            }

            return corners;
        }

        /// <summary>
        /// 담장들을 생성합니다
        /// </summary>
        private static List<Transform> CreateWalls(House house, List<Vector3> perimeterVertices, Transform door, bool hasCurve)
        {
            var walls = new List<Transform>();

            for (int i = 0; i < perimeterVertices.Count; i++)
            {
                Vector3 startPos = perimeterVertices[i];
                Vector3 endPos = perimeterVertices[(i + 1) % perimeterVertices.Count];

                // 문이 있는 세그먼트인지 확인
                bool hasDoorOnSegment = door != null && IsPointOnSegment(startPos, endPos, door.position, 1f);

                if (hasCurve && house.OutlineVertices.Count > i && house.OutlineVertices[i].Count > 0)
                {
                    // 곡선 담장 생성
                    CreateCurvedWalls(house, house.OutlineVertices[i], walls, hasDoorOnSegment, door);
                }
                else
                {
                    // 직선 담장 생성
                    CreateStraightWalls(house, startPos, endPos, walls, hasDoorOnSegment, door);
                }
            }

            return walls;
        }

        /// <summary>
        /// 곡선 담장을 생성합니다
        /// </summary>
        private static void CreateCurvedWalls(House house, List<Vector3> curvePoints, List<Transform> walls, bool hasDoor, Transform door)
        {
            for (int i = 0; i < curvePoints.Count - 1; i++)
            {
                Vector3 segmentStart = curvePoints[i];
                Vector3 segmentEnd = curvePoints[i + 1];

                // 문 위치 건너뛰기
                if (hasDoor && door != null && IsPointOnSegment(segmentStart, segmentEnd, door.position, 1f))
                    continue;

                GameObject wallObj = new GameObject($"PerimeterWall_Curve_{i}");
                wallObj.transform.position = Vector3.Lerp(segmentStart, segmentEnd, 0.5f);
                wallObj.transform.SetParent(house.transform);

                Vector3 direction = (segmentEnd - segmentStart).normalized;
                wallObj.transform.rotation = Quaternion.LookRotation(direction);

                walls.Add(wallObj.transform);
            }
        }

        /// <summary>
        /// 직선 담장을 생성합니다
        /// </summary>
        private static void CreateStraightWalls(House house, Vector3 start, Vector3 end, List<Transform> walls, bool hasDoor, Transform door)
        {
            Vector3 direction = (end - start).normalized;
            float totalLength = Vector3.Distance(start, end);
            int segmentCount = Mathf.FloorToInt(totalLength);
            float actualSegmentLength = totalLength / segmentCount;

            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 segmentCenter = start + direction * (i * actualSegmentLength + actualSegmentLength * 0.5f);

                // 문 위치 건너뛰기
                if (hasDoor && door != null && Vector3.Distance(segmentCenter, door.position) < 1f)
                    continue;

                GameObject wallObj = new GameObject($"PerimeterWall_Straight_{walls.Count}");
                wallObj.transform.position = segmentCenter;
                wallObj.transform.SetParent(house.transform);
                wallObj.transform.rotation = Quaternion.LookRotation(direction);

                walls.Add(wallObj.transform);
            }
        }

        /// <summary>
        /// 점이 세그먼트 위에 있는지 확인합니다
        /// </summary>
        private static bool IsPointOnSegment(Vector3 segmentStart, Vector3 segmentEnd, Vector3 point, float tolerance)
        {
            Vector3 segmentVector = segmentEnd - segmentStart;
            Vector3 pointVector = point - segmentStart;

            float segmentLength = segmentVector.magnitude;
            if (segmentLength < 0.001f) return false;

            float projection = Vector3.Dot(pointVector, segmentVector) / segmentLength;
            Vector3 closestPointOnSegment = segmentStart + segmentVector.normalized * projection;

            return Vector3.Distance(point, closestPointOnSegment) <= tolerance &&
                   projection >= 0 && projection <= segmentLength;
        }
    }  
}

