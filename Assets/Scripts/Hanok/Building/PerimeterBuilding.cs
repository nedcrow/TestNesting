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

        [Header("Settings")]
        [SerializeField] private float wallSegmentLength = 1f;
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
    }
}