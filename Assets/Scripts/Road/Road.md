# Road System Design

## Overview

Unity 3D에서 클릭→클릭 방식으로 도로를 건설하며, 스냅핑, Bezier 곡선, 도로 연결, 성능 최적화를 위한 세그먼트화를 지원합니다.

## Core Classes

### RoadBuilder

* **역할**: 도로 건설을 총괄하는 메인 컨트롤러
* **주요 기능**:

  * 클릭→클릭 방식 도로 생성(첫 클릭으로 시작점, 두 번째 클릭으로 생성 완료)
  * 기존 도로에 자동 스냅(`snapDistance` 범위)
  * Bezier 곡선과 직선 모드 지원
  * Alt + 스크롤로 조절 가능한 Sin Wave 곡선 모드
  * 실시간 미리보기
  * Road 단위 삭제 가능

### RoadComponent

* **역할**: 개별 도로의 데이터/상태 관리
* **주요 데이터**:

  * `centerline`: 도로 센터라인 좌표
  * `leftEdgeLine`, `rightEdgeLine`: 좌/우 엣지 라인(miter join 적용)
  * `frontCap`, `endCap`: 센터라인의 시작과 끝에 cap이 있는지의 상태값.
  * `width`: 도로 폭
  * `State` : 프리뷰 상태인지, 풀링을 위해 쉬는 상태인지 활성화된 상태인지 확인. 
* **주요 기능**:
  * 코너 겹침/틈 문제를 줄이기 위한 miter join 기반 엣지 라인 생성
  * 주변 RoadComponent 탐색해서 도로 양 끝의 Cap 관리

## Input Handling

### Mouse Input

* **첫 번째 좌클릭**: 시작점 설정(자동 스냅 포함)
* **두 번째 좌클릭**: 도로 생성
* **우클릭(미리보기 중)**: 건설 취소
* **우클릭(대기 상태)**: 청크 삭제
* **ESC**: 미리보기 취소

### Keyboard Modifiers

* **Shift**: 직선 모드 강제
* **Alt**: 와이드 아크 모드(Sin Wave)
* **Alt + Mouse Wheel**: 곡선 강도 조절

## Road Connection System

### Snap Mechanism

* 기존 도로로부터 `snapDistance` 내에서 자동 스냅
* 동일 레이어만 탐색.
* 같은 RoadComponent 내의 chunk는 대상 제외.
* 다른 RoadComponent 센터라인에서 가장 가까운 점을 찾아서 사용.
* 끝점 연결: 기존 도로의 진행 방향에 정렬
* 중간 연결: 직각(Perpendicular)으로 접근
* Physics.OverlapSphere를 활용한 효율적 감지

### Cap

* 연결된 시작, 끝점은 Cap을 생성하지 않음
* `frontCap`, `endCap` 필요 여부 확인은 미리보기 중, RoadComponent 생성시, 제거시 확인.
* `RoadComponent`는 지워질 때 닿아있던 `RoadComponent`들에게 cap 다시 확인하고 업데이트하도록 명령.

## Mesh Generation

### Segmentation

* 긴 도로를 `segmentLength` 기준으로 작은 청크로 분할
* 각 청크는 독립적인 GameObject + MeshCollider
* 성능 최적화 및 부분 삭제 지원

### Edge Line Generation

* 코너 처리를 위한 miter join 알고리즘
* 예각에서 과도한 길이를 제한(`maxMiterLength`)
* 연결되지 않은 끝점에 End Cap 자동 추가

## Curve Algorithms

### Straight Mode (Default)

* 기본적으로 직선만 생성 (`BuildStraight`)
* Shift 키로 강제 직선 모드 활성화
* 시작점과 끝점을 직접 연결

### Elastic Curve Mode (Alt + 드래그)

* Alt 키를 누른 순간의 커서 위치가 참조점(Reference Point)이 됨
* 참조점을 기반으로 한 3차 Bezier 곡선 생성 (`BuildElasticCurve`)
* **기하학적 관계 기반 동적 탄젠트**:
  - 시작점 → 참조점 방향은 항상 고정 (첫 번째 컨트롤 포인트)
  - 끝점 탄젠트는 길이 비율에 따라 동적 계산:
    * `lengthRatio ≈ 1.0`: 시작점 방향과 수평 (커서B 케이스)
    * `refCursorRatio ≈ 1.0`: 참조점과 수직 (커서A 케이스)
    * 기타: 길이 비율에 따른 점진적 방향 변화
* **자연스러운 밴드 효과**: 커서 이동에 따라 전체 곡선이 탄성적으로 변형
* 샘플 밀도: `samplesPerMeter`로 곡선 품질 조절

## Visual Feedback

### Preview

* 반투명 도로 메시를 활용한 실시간 미리보기
* `previewColor` 적용

### Debug Visualization (Gizmos)

* 청록: 센터라인
* 노랑: 좌측 엣지
* 마젠타: 우측 엣지
* 초록: 시작 방향
* 빨강: 끝 방향

## Performance Optimization

### Chunk-Based Management

* 도로를 `segmentLength` 단위로 분할 관리
* 청크별 개별 삭제/수정 가능
* GPU Instancing 지원 머티리얼 사용

### Efficient Snap Search

* Physics.OverlapSphere 활용
* 중복 검사 방지(HashSet)
* 레이어 마스크 기반 필터링

## Configuration Parameters

### Road Properties

* `roadWidth`: 도로 폭 (기본 2.0f)
* `roadMaterial`: 도로 머티리얼 (MeshRenderer용)
* `uvTilingPerMeter`: UV 타일링 비율 (기본 1.0f)

### Input & Raycasting

* `groundMask`: 지면 레이어 마스크 (도로 건설 가능 영역)
* `rayMaxDistance`: 카메라 레이캐스트 최대 거리 (기본 2000f)

### Curve Control

* `samplesPerMeter`: 곡선 샘플 밀도 (기본 1.5f)
* *제거됨*: `handleLenRatio`, `maxHandleLen`, `altCurveStrength` (구 Bezier/Sin Wave 시스템)

### Construction & Optimization

* `snapDistance`: 스냅 감지 거리 (기본 0.5f)
* `segmentLength`: 청크 분할 길이 (기본 6f)

### Visual Feedback

* `previewColor`: 미리보기 도로 색상 (기본: 노란색 반투명)

## Extensibility

현재 구조는 향후 확장을 고려해 설계되었습니다:

* 다양한 도로 타입(폭, 머티리얼)
* 교차로/분기 시스템
* 차선/노면 표시 시스템
* 지형 적응형 도로(경사, 고도 변화)
