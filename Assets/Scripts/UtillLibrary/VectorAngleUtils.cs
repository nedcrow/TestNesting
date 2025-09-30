using UnityEngine;

namespace UtilLibrary
{
    /// <summary>
    /// 벡터 간 각도 계산을 위한 유틸리티 클래스
    /// </summary>
    public static class VectorAngleUtils
    {
        /// <summary>
        /// 연속된 세 점으로 이루어진 각도를 라디안으로 반환합니다
        /// </summary>
        /// <param name="prevPoint">이전 점</param>
        /// <param name="currentPoint">현재 점 (각의 꼭짓점)</param>
        /// <param name="nextPoint">다음 점</param>
        /// <returns>각도 (라디안, 0~π 범위)</returns>
        public static float GetAngleAtPointRadians(Vector3 prevPoint, Vector3 currentPoint, Vector3 nextPoint)
        {
            Vector3 dir1 = (currentPoint - prevPoint).normalized;
            Vector3 dir2 = (nextPoint - currentPoint).normalized;

            float dotProduct = Vector3.Dot(dir1, dir2);
            dotProduct = Mathf.Clamp(dotProduct, -1f, 1f); // 부동소수점 오차 방지

            return Mathf.Acos(dotProduct);
        }

        /// <summary>
        /// 연속된 세 점으로 이루어진 각도를 도(degrees)로 반환합니다
        /// </summary>
        /// <param name="prevPoint">이전 점</param>
        /// <param name="currentPoint">현재 점 (각의 꼭짓점)</param>
        /// <param name="nextPoint">다음 점</param>
        /// <returns>각도 (도, 0~180 범위)</returns>
        public static float GetAngleAtPointDegrees(Vector3 prevPoint, Vector3 currentPoint, Vector3 nextPoint)
        {
            return GetAngleAtPointRadians(prevPoint, currentPoint, nextPoint) * Mathf.Rad2Deg;
        }
    }
}