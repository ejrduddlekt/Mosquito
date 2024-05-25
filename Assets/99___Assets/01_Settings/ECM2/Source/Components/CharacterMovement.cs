using System;
using System.Collections.Generic;
using UnityEngine;

namespace ECM2
{
    #region ENUMS

    /// <summary>
    /// 이동을 제한하는 축.
    /// </summary>

    public enum PlaneConstraint
    {
        None,
        ConstrainXAxis,
        ConstrainYAxis,
        ConstrainZAxis,
        Custom
    }

    /// <summary>
    /// 캐릭터 캡슐 기준의 히트 위치, 예: 측면, 위, 아래.
    /// </summary>

    public enum HitLocation
    {
        None = 0,
        Sides = 1,
        Above = 2,
        Below = 4,
    }

    /// <summary>
    /// 캐릭터 충돌 동작.
    /// </summary>

    [Flags]
    public enum CollisionBehaviour
    {
        Default = 0,

        /// <summary>
        /// 캐릭터가 다른 콜라이더 위를 걸을 수 있는지 여부를 결정합니다.
        /// </summary>

        Walkable = 1 << 0,
        NotWalkable = 1 << 1,

        /// <summary>
        /// 캐릭터가 다른 콜라이더 위에 머물 수 있는지 여부를 결정합니다.
        /// </summary>

        CanPerchOn = 1 << 2,
        CanNotPerchOn = 1 << 3,

        /// <summary>
        /// 캐릭터가 다른 콜라이더 위로 올라설 수 있는지 여부를 정의합니다.
        /// </summary>

        CanStepOn = 1 << 4,
        CanNotStepOn = 1 << 5,

        /// <summary>
        /// 캐릭터가 서 있는 객체와 함께 이동할 수 있는지 여부를 정의합니다.
        /// </summary>

        CanRideOn = 1 << 6,
        CanNotRideOn = 1 << 7
    }

    #endregion

    #region STRUCTS

    /// <summary>
    /// 발견된 지면에 대한 정보를 보유합니다 (있다면).
    /// </summary>

    public struct FindGroundResult
    {
        /// <summary>
        /// 지면을 맞췄는가? 예: 캡슐의 하단 구체에 충돌했는가.
        /// </summary>

        public bool hitGround;

        /// <summary>
        /// 발견된 지면이 걸을 수 있는 지면인가?
        /// </summary>

        public bool isWalkable;

        /// <summary>
        /// 걸을 수 있는 지면인가? (예: hitGround == true && isWalkable == true).
        /// </summary>

        public bool isWalkableGround => hitGround && isWalkable;

        /// <summary>
        /// 캐릭터의 위치, 레이캐스트 결과인 경우 point와 같습니다.
        /// </summary>

        public Vector3 position;

        /// <summary>
        /// 월드 공간에서의 충돌 지점.
        /// </summary>

        public Vector3 point => hitResult.point;

        /// <summary>
        /// 충돌 표면의 법선 벡터.
        /// </summary>

        public Vector3 normal => hitResult.normal;

        /// <summary>
        /// 월드 공간에서의 충돌 법선, 스윕에 의해 맞은 객체의 경우. 예를 들어 캡슐이 평평한 평면을 맞추면, 이는 평면에서 밖으로 향하는 정규화된 벡터입니다. 표면의 모서리나 가장자리와 충돌하는 경우, 일반적으로 "가장 반대되는" 법선(쿼리 방향과 반대되는)이 선택됩니다.
        /// </summary>

        public Vector3 surfaceNormal;

        /// <summary>
        /// 맞은 객체의 콜라이더.
        /// </summary>

        public Collider collider;

        /// <summary>
        /// 맞은 콜라이더에 연결된 리지드바디. 콜라이더가 리지드바디에 연결되지 않은 경우 null입니다.
        /// </summary>

        public Rigidbody rigidbody => collider ? collider.attachedRigidbody : null;

        /// <summary>
        /// 맞은 리지드바디 또는 콜라이더의 트랜스폼.
        /// </summary>

        public Transform transform
        {
            get
            {
                if (collider == null)
                    return null;

                Rigidbody attachedRigidbody = collider.attachedRigidbody;
                return attachedRigidbody ? attachedRigidbody.transform : collider.transform;
            }
        }

        /// <summary>
        /// 스윕된 캡슐에서 계산된 지면까지의 거리.
        /// </summary>

        public float groundDistance;

        /// <summary>
        /// 레이캐스트를 사용하여 유효한 걸을 수 있는 지면을 발견한 경우 true입니다 (스윕 테스트가 걸을 수 있는 표면을 제공하지 못한 경우 발생합니다).
        /// </summary>

        public bool isRaycastResult;

        /// <summary>
        /// 레이캐스트에서 계산된 지면까지의 거리. isRaycast가 true인 경우에만 유효합니다.
        /// </summary>

        public float raycastDistance;

        /// <summary>
        /// 지면을 찾기 위한 테스트의 히트 결과.
        /// </summary>

        public RaycastHit hitResult;

        /// <summary>
        /// 지면까지의 거리 가져오기, raycastDistance 또는 distance 중 하나.
        /// </summary>

        public float GetDistanceToGround()
        {
            return isRaycastResult ? raycastDistance : groundDistance;
        }

        /// <summary>
        /// 이 결과를 스윕 테스트 결과로 초기화합니다.
        /// </summary>

        public void SetFromSweepResult(bool hitGround, bool isWalkable, Vector3 position, float sweepDistance,
            ref RaycastHit inHit, Vector3 surfaceNormal)
        {
            this.hitGround = hitGround;
            this.isWalkable = isWalkable;

            this.position = position;

            collider = inHit.collider;

            groundDistance = sweepDistance;

            isRaycastResult = false;
            raycastDistance = 0.0f;

            hitResult = inHit;

            this.surfaceNormal = surfaceNormal;
        }

        public void SetFromSweepResult(bool hitGround, bool isWalkable, Vector3 position, Vector3 point, Vector3 normal,
            Vector3 surfaceNormal, Collider collider, float sweepDistance)
        {
            this.hitGround = hitGround;
            this.isWalkable = isWalkable;

            this.position = position;

            this.collider = collider;

            groundDistance = sweepDistance;

            isRaycastResult = false;
            raycastDistance = 0.0f;

            hitResult = new RaycastHit
            {
                point = point,
                normal = normal,

                distance = sweepDistance
            };

            this.surfaceNormal = surfaceNormal;
        }

        /// <summary>
        /// 이 결과를 레이캐스트 결과로 초기화합니다.
        /// </summary>

        public void SetFromRaycastResult(bool hitGround, bool isWalkable, Vector3 position, float sweepDistance,
            float castDistance, ref RaycastHit inHit)
        {
            this.hitGround = hitGround;
            this.isWalkable = isWalkable;

            this.position = position;

            collider = inHit.collider;

            groundDistance = sweepDistance;

            isRaycastResult = true;
            raycastDistance = castDistance;

            float oldDistance = hitResult.distance;

            hitResult = inHit;
            hitResult.distance = oldDistance;

            surfaceNormal = hitResult.normal;
        }
    }

    /// <summary>
    /// 이 캐릭터의 충돌을 설명합니다.
    /// </summary>

    public struct CollisionResult
    {
        /// <summary>
        /// 캐릭터가 중첩되었는지 여부.
        /// </summary>

        public bool startPenetrating;

        /// <summary>
        /// 캐릭터 캡슐 기준의 히트 위치, 예: 아래, 측면, 위.
        /// </summary>

        public HitLocation hitLocation;

        /// <summary>
        /// 맞은 지면이 걸을 수 있는 지면인가?
        /// </summary>

        public bool isWalkable;

        /// <summary>
        /// 이 충돌 시 캐릭터의 위치.
        /// </summary>

        public Vector3 position;

        /// <summary>
        /// 이 충돌 시 캐릭터의 속도.
        /// </summary>

        public Vector3 velocity;

        /// <summary>
        /// 충돌한 객체의 속도.
        /// </summary>

        public Vector3 otherVelocity;

        /// <summary>
        /// 월드 공간에서의 충돌 지점.
        /// </summary>

        public Vector3 point;

        /// <summary>
        /// 월드 공간에서의 충돌 법선.
        /// </summary>

        public Vector3 normal;

        /// <summary>
        /// 월드 공간에서의 충돌 법선, 스윕에 의해 맞은 객체의 경우. 예를 들어 캡슐이 평평한 평면을 맞추면, 이는 평면에서 밖으로 향하는 정규화된 벡터입니다. 표면의 모서리나 가장자리와 충돌하는 경우, 일반적으로 "가장 반대되는" 법선(쿼리 방향과 반대되는)이 선택됩니다.
        /// </summary>

        public Vector3 surfaceNormal;

        /// <summary>
        /// 이 충돌 시 캐릭터의 변위.
        /// </summary>

        public Vector3 displacementToHit;

        /// <summary>
        /// 충돌 후 남은 변위.
        /// </summary>

        public Vector3 remainingDisplacement;

        /// <summary>
        /// 맞은 객체의 콜라이더.
        /// </summary>

        public Collider collider;

        /// <summary>
        /// 맞은 콜라이더에 연결된 리지드바디. 콜라이더가 리지드바디에 연결되지 않은 경우 null입니다.
        /// </summary>

        public Rigidbody rigidbody => collider ? collider.attachedRigidbody : null;

        /// <summary>
        /// 맞은 리지드바디 또는 콜라이더의 트랜스폼.
        /// </summary>

        public Transform transform
        {
            get
            {
                if (collider == null)
                    return null;

                Rigidbody rb = collider.attachedRigidbody;
                return rb ? rb.transform : collider.transform;
            }
        }

        /// <summary>
        /// 이 충돌에 대한 정보를 포함하는 구조체.
        /// </summary>

        public RaycastHit hitResult;
    }

    #endregion

    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class CharacterMovement : MonoBehaviour
    {
        #region ENUMS

        /// <summary>
        /// 디패너트레이션(depenetration) 동작.
        /// </summary>

        [Flags]
        private enum DepenetrationBehaviour
        {
            IgnoreNone = 0,

            IgnoreStatic = 1 << 0,
            IgnoreDynamic = 1 << 1,
            IgnoreKinematic = 1 << 2
        }

        #endregion

        #region STRUCTS

        /// <summary>
        /// 고급 설정을 포함하는 구조체.
        /// </summary>

        [Serializable]
        public struct Advanced
        {
            [Tooltip("캐릭터 컨트롤러의 최소 이동 거리입니다. 캐릭터가 이 거리보다 적게 이동하려고 하면 전혀 이동하지 않습니다. 이는 떨림을 줄이는 데 사용할 수 있습니다. 대부분의 상황에서는 이 값을 0으로 유지하는 것이 좋습니다.")]
            public float minMoveDistance;
            public float minMoveDistanceSqr => minMoveDistance * minMoveDistance;

            [Tooltip("이동 중에 사용되는 최대 반복 횟수.")]
            public int maxMovementIterations;

            [Tooltip("침투를 해결하는 데 사용되는 최대 반복 횟수.")]
            public int maxDepenetrationIterations;

            [Space(15f)]
            [Tooltip("활성화되면, 캐릭터가 동적 리지드바디와 상호작용할 때 적용됩니다.")]
            public bool enablePhysicsInteraction;

            [Tooltip("활성화되면, 캐릭터가 다른 캐릭터와 상호작용할 때 적용됩니다.")]
            public bool allowPushCharacters;

            [Tooltip("활성화되면, 캐릭터가 서 있는 이동 플랫폼과 함께 이동합니다.")]
            public bool impartPlatformMovement;

            [Tooltip("활성화되면, 캐릭터가 서 있는 이동 플랫폼과 함께 회전합니다 (yaw-only).")]
            public bool impartPlatformRotation;

            [Tooltip("활성화되면, 플랫폼에서 점프하거나 떨어질 때 플랫폼의 속도를 부여받습니다.")]
            public bool impartPlatformVelocity;

            public void Reset()
            {
                minMoveDistance = 0.0f;

                maxMovementIterations = 5;
                maxDepenetrationIterations = 1;

                enablePhysicsInteraction = false;
                allowPushCharacters = false;
                impartPlatformMovement = false;
                impartPlatformRotation = false;
                impartPlatformVelocity = false;
            }

            public void OnValidate()
            {
                minMoveDistance = Mathf.Max(minMoveDistance, 0.0f);

                maxMovementIterations = Mathf.Max(maxMovementIterations, 1);
                maxDepenetrationIterations = Mathf.Max(maxDepenetrationIterations, 1);
            }
        }

        /// <summary>
        /// 플랫폼에 대한 정보를 포함하는 구조체.
        /// </summary>

        public struct MovingPlatform
        {
            /// <summary>
            /// 이전 프레임의 활성 플랫폼.
            /// </summary>

            public Rigidbody lastPlatform;

            /// <summary>
            /// 현재 활성 플랫폼.
            /// </summary>

            public Rigidbody platform;

            /// <summary>
            /// 활성 플랫폼에서의 캐릭터의 마지막 위치.
            /// </summary>

            public Vector3 position;

            /// <summary>
            /// 플랫폼의 로컬 공간에서의 활성 플랫폼에서의 캐릭터의 마지막 위치.
            /// </summary>

            public Vector3 localPosition;

            /// <summary>
            /// 마지막으로 평가된 프레임 동안의 캐릭터의 델타 위치.
            /// </summary>

            public Vector3 deltaPosition;

            /// <summary>
            /// 활성 플랫폼에서의 캐릭터의 마지막 회전.
            /// </summary>

            public Quaternion rotation;

            /// <summary>
            /// 플랫폼의 로컬 공간에서의 활성 플랫폼에서의 캐릭터의 마지막 회전.
            /// </summary>

            public Quaternion localRotation;

            /// <summary>
            /// 마지막으로 평가된 프레임 동안의 캐릭터의 델타 회전. impartPlatformRotation이 true인 경우에만 유효합니다.
            /// </summary>

            public Quaternion deltaRotation;

            /// <summary>
            /// 현재 활성 플랫폼 속도.
            /// </summary>

            public Vector3 platformVelocity;
        }

        #endregion

        #region CONSTANTS

        private const float kKindaSmallNumber = 0.0001f;  // 아주 작은 수
        private const float kHemisphereLimit = 0.01f;  // 반구 제한

        private const int kMaxCollisionCount = 16;  // 최대 충돌 수
        private const int kMaxOverlapCount = 16;  // 최대 겹침 수

        private const float kSweepEdgeRejectDistance = 0.0015f;  // 스윕 엣지 거부 거리

        private const float kMinGroundDistance = 0.019f;  // 최소 지면 거리
        private const float kMaxGroundDistance = 0.024f;  // 최대 지면 거리
        private const float kAvgGroundDistance = (kMinGroundDistance + kMaxGroundDistance) * 0.5f;  // 평균 지면 거리

        private const float kMinWalkableSlopeLimit = 1.000000f;  // 최소 걸을 수 있는 경사 한계
        private const float kMaxWalkableSlopeLimit = 0.017452f;  // 최대 걸을 수 있는 경사 한계

        private const float kPenetrationOffset = 0.00125f;  // 침투 오프셋

        private const float kContactOffset = 0.01f;  // 접촉 오프셋
        private const float kSmallContactOffset = 0.001f;  // 작은 접촉 오프셋

        #endregion

        #region EDITOR EXPOSED FIELDS

        [Space(15f)]
        [Tooltip("캐릭터의 움직임을 제한하여 잠긴 축을 따라 이동할 수 없도록 합니다.")]
        [SerializeField]
        private PlaneConstraint _planeConstraint;

        [Space(15f)]
        [SerializeField, Tooltip("아바타의 루트 트랜스폼입니다.")]
        private Transform _rootTransform;

        [SerializeField, Tooltip("루트 트랜스폼이 발 위치에서 이 오프셋으로 위치하게 됩니다.")]
        private Vector3 _rootTransformOffset = new Vector3(0, 0, 0);

        [Space(15f)]
        [Tooltip("캐릭터의 캡슐 콜라이더 반지름.")]
        [SerializeField]
        private float _radius;

        [Tooltip("캐릭터의 캡슐 콜라이더 높이")]
        [SerializeField]
        private float _height;

        [Space(15f)]
        [Tooltip("걸을 수 있는 표면의 최대 각도(도 단위).")]
        [SerializeField]
        private float _slopeLimit;

        [Tooltip("유효한 계단의 최대 높이(미터 단위).")]
        [SerializeField]
        private float _stepOffset;

        [Tooltip("캐릭터의 위치에서 가장자리에 더 가까운 경우 캐릭터가 표면 가장자리에 앉을 수 있도록 합니다.\n" +
                 "캐릭터가 아래의 걸을 수 있는 표면의 stepOffset 내에 있으면 떨어지지 않습니다.")]
        [SerializeField]
        private float _perchOffset;

        [Tooltip("경사면에 앉을 때, 걸을 수 있는 지면 위에 얼마나 높이 앉을 수 있는지 결정할 때 stepOffset에 이 추가 거리를 더합니다.\n" +
                 "계단 오르기를 시작하기 위해 여전히 stepOffset을 적용하지만, 이는 캐릭터가 가장자리에 걸치거나 지면에서 약간 더 높이 오를 수 있도록 합니다.")]
        [SerializeField]
        private float _perchAdditionalHeight;

        [Space(15f)]
        [Tooltip("활성화된 경우, SlopeLimitBehaviour 컴포넌트가 있는 콜라이더가 이 경사 제한을 무시할 수 있습니다.")]
        [SerializeField]
        private bool _slopeLimitOverride;

        [Tooltip("활성화된 경우, 캐릭터가 평평한 꼭대기를 사용하는 것처럼 머리 충돌을 처리합니다.")]
        [SerializeField]
        private bool _useFlatTop;

        [Tooltip("캐릭터가 평평한 바닥을 사용하는 것처럼 지면 검사를 수행합니다." +
                 "이는 캐릭터가 가장자리에서 천천히 내려가는 상황을 방지합니다(캡슐이 가장자리에 '균형을 잡는' 상황).")]
        [SerializeField]
        private bool _useFlatBaseForGroundChecks;

        [Space(15f)]
        [Tooltip("캐릭터 충돌 레이어 마스크.")]
        [SerializeField]
        private LayerMask _collisionLayers = 1;

        [Tooltip("기본적으로 쿼리(raycast, spherecast, overlap 테스트 등)가 트리거를 맞추는지 여부를 지정하여 전역 Physics.queriesHitTriggers를 재정의합니다." +
                 "쿼리가 트리거 콜라이더를 무시하도록 하려면 Ignore를 사용하십시오.")]
        [SerializeField]
        private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

        [Space(15f)]
        [SerializeField]
        private Advanced _advanced;

        #endregion

        #region FIELDS

        private Transform _transform;  // 트랜스폼

        private Rigidbody _rigidbody;  // 리지드바디

        private CapsuleCollider _capsuleCollider;  // 캡슐 콜라이더

        private Vector3 _capsuleCenter;  // 캡슐 중심
        private Vector3 _capsuleTopCenter;  // 캡슐 상단 중심
        private Vector3 _capsuleBottomCenter;  // 캡슐 하단 중심

        private readonly HashSet<Rigidbody> _ignoredRigidbodies = new HashSet<Rigidbody>();  // 무시된 리지드바디
        private readonly HashSet<Collider> _ignoredColliders = new HashSet<Collider>();  // 무시된 콜라이더

        private readonly RaycastHit[] _hits = new RaycastHit[kMaxCollisionCount];  // 히트 배열
        private readonly Collider[] _overlaps = new Collider[kMaxOverlapCount];  // 겹침 배열

        private int _collisionCount;  // 충돌 수
        private readonly CollisionResult[] _collisionResults = new CollisionResult[kMaxCollisionCount];  // 충돌 결과 배열

        [SerializeField, HideInInspector]
        private float _minSlopeLimit;  // 최소 경사 한계

        private bool _detectCollisions = true;  // 충돌 감지 여부

        private bool _isConstrainedToGround = true;  // 지면에 제한되는지 여부
        private float _unconstrainedTimer;  // 제한 해제 타이머

        private Vector3 _constraintPlaneNormal;  // 제한 평면 법선

        private Vector3 _characterUp;  // 캐릭터의 위쪽 방향

        private Vector3 _transformedCapsuleCenter;  // 변환된 캡슐 중심
        private Vector3 _transformedCapsuleTopCenter;  // 변환된 캡슐 상단 중심
        private Vector3 _transformedCapsuleBottomCenter;  // 변환된 캡슐 하단 중심

        private Vector3 _velocity;  // 속도

        private Vector3 _pendingForces;  // 대기 중인 힘
        private Vector3 _pendingImpulses;  // 대기 중인 임펄스
        private Vector3 _pendingLaunchVelocity;  // 대기 중인 발사 속도

        private float _pushForceScale = 1.0f;  // 밀기 힘 스케일

        private bool _hasLanded;  // 착륙 여부

        private FindGroundResult _foundGround;  // 발견된 지면 결과
        private FindGroundResult _currentGround;  // 현재 지면 결과

        private Rigidbody _parentPlatform;  // 부모 플랫폼
        private MovingPlatform _movingPlatform;  // 이동 플랫폼

        #endregion

        #region PROPERTIES

        /// <summary>
        /// 캐시된 캐릭터의 트랜스폼.
        /// </summary>
        public new Transform transform
        {
            get
            {
#if UNITY_EDITOR
                if (_transform == null)
                    _transform = GetComponent<Transform>();
#endif
                return _transform;
            }
        }

        /// <summary>
        /// 캐릭터의 리지드바디.
        /// </summary>
        public new Rigidbody rigidbody
        {
            get
            {
#if UNITY_EDITOR
                if (_rigidbody == null)
                    _rigidbody = GetComponent<Rigidbody>();
#endif
                return _rigidbody;
            }
        }

        /// <summary>
        /// 리지드바디 인터폴레이션 설정.
        /// </summary>
        public RigidbodyInterpolation interpolation
        {
            get => rigidbody.interpolation;
            set => rigidbody.interpolation = value;
        }

        /// <summary>
        /// 캐릭터의 콜라이더.
        /// </summary>
        public new Collider collider
        {
            get
            {
#if UNITY_EDITOR
                if (_capsuleCollider == null)
                    _capsuleCollider = GetComponent<CapsuleCollider>();
#endif
                return _capsuleCollider;
            }
        }

        /// <summary>
        /// 아바타의 루트 본.
        /// </summary>
        public Transform rootTransform
        {
            get => _rootTransform;
            set => _rootTransform = value;
        }

        /// <summary>
        /// 루트 트랜스폼이 이 오프셋으로 위치하게 됩니다.
        /// </summary>
        public Vector3 rootTransformOffset
        {
            get => _rootTransformOffset;
            set => _rootTransformOffset = value;
        }

        /// <summary>
        /// 캐릭터의 현재 위치.
        /// </summary>
        public Vector3 position
        {
            get => GetPosition();
            set => SetPosition(value);
        }

        /// <summary>
        /// 캐릭터의 현재 회전.
        /// </summary>
        public Quaternion rotation
        {
            get => GetRotation();
            set => SetRotation(value);
        }

        /// <summary>
        /// 월드 공간에서의 캐릭터의 중심.
        /// </summary>
        public Vector3 worldCenter => position + rotation * _capsuleCenter;

        /// <summary>
        /// 캐릭터의 업데이트된 위치.
        /// </summary>
        public Vector3 updatedPosition { get; private set; }

        /// <summary>
        /// 캐릭터의 업데이트된 회전.
        /// </summary>
        public Quaternion updatedRotation { get; private set; }

        /// <summary>
        /// 캐릭터의 현재 상대 속도.
        /// 상대적 속도는 이 외부에서 발생하는 트랜스폼의 움직임을 추적하지 않습니다,
        /// 예를 들어, 다른 움직이는 트랜스폼(예: 움직이는 차량) 아래에 부모로 설정된 캐릭터의 경우.
        /// </summary>
        public ref Vector3 velocity => ref _velocity;

        /// <summary>
        /// 캐릭터의 속도.
        /// </summary>
        public float speed => _velocity.magnitude;

        /// <summary>
        /// 캐릭터의 앞쪽 벡터를 따른 속도(예: 로컬 공간에서).
        /// </summary>
        public float forwardSpeed => _velocity.dot(transform.forward);

        /// <summary>
        /// 캐릭터의 오른쪽 벡터를 따른 속도(예: 로컬 공간에서).
        /// </summary>
        public float sidewaysSpeed => _velocity.dot(transform.right);

        /// <summary>
        /// 캐릭터의 캡슐 콜라이더 반지름.
        /// </summary>
        public float radius
        {
            get => _radius;
            set => SetDimensions(value, _height);
        }

        /// <summary>
        /// 캐릭터의 캡슐 콜라이더 높이.
        /// </summary>
        public float height
        {
            get => _height;
            set => SetDimensions(_radius, value);
        }

        /// <summary>
        /// 걸을 수 있는 경사면의 최대 각도(도 단위).
        /// </summary>
        public float slopeLimit
        {
            get => _slopeLimit;
            set
            {
                _slopeLimit = Mathf.Clamp(value, 0.0f, 89.0f);
                // 수치적 정밀도 오류를 피하기 위해 0.01f를 추가합니다.
                _minSlopeLimit = Mathf.Cos((_slopeLimit + 0.01f) * Mathf.Deg2Rad);
            }
        }

        /// <summary>
        /// 유효한 계단의 최대 높이(미터 단위).
        /// </summary>
        public float stepOffset
        {
            get => _stepOffset;
            set => _stepOffset = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 캐릭터의 위치에서 가장자리에 더 가까운 경우 캐릭터가 표면 가장자리에 앉을 수 있도록 합니다.
        /// 캐릭터가 아래의 걸을 수 있는 표면의 stepOffset 내에 있으면 떨어지지 않습니다.
        /// </summary>
        public float perchOffset
        {
            get => _perchOffset;
            set => _perchOffset = Mathf.Clamp(value, 0.0f, _radius);
        }

        /// <summary>
        /// 경사면에 앉을 때, 걸을 수 있는 지면 위에 얼마나 높이 앉을 수 있는지 결정할 때 stepOffset에 이 추가 거리를 더합니다.
        /// </summary>
        public float perchAdditionalHeight
        {
            get => _perchAdditionalHeight;
            set => _perchAdditionalHeight = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 외부 경사 한계 재정의를 허용해야 합니까?
        /// </summary>
        public bool slopeLimitOverride
        {
            get => _slopeLimitOverride;
            set => _slopeLimitOverride = value;
        }

        /// <summary>
        /// 활성화된 경우, 캐릭터가 평평한 꼭대기를 사용하는 것처럼 머리 충돌을 처리합니다.
        /// </summary>
        public bool useFlatTop
        {
            get => _useFlatTop;
            set => _useFlatTop = value;
        }

        /// <summary>
        /// 캐릭터가 평평한 바닥을 사용하는 것처럼 지면 검사를 수행합니다.
        /// 이는 캐릭터가 가장자리에서 천천히 내려가는 상황을 방지합니다(캡슐이 가장자리에 '균형을 잡는' 상황).
        /// </summary>
        public bool useFlatBaseForGroundChecks
        {
            get => _useFlatBaseForGroundChecks;
            set => _useFlatBaseForGroundChecks = value;
        }

        /// <summary>
        /// 충돌 감지 중에 고려해야 할 레이어.
        /// </summary>
        public LayerMask collisionLayers
        {
            get => _collisionLayers;
            set => _collisionLayers = value;
        }

        /// <summary>
        /// 캐릭터가 트리거와 상호작용하는 방법을 결정합니다.
        /// </summary>
        public QueryTriggerInteraction triggerInteraction
        {
            get => _triggerInteraction;
            set => _triggerInteraction = value;
        }

        /// <summary>
        /// 충돌 감지를 수행해야 합니까?
        /// </summary>
        public bool detectCollisions
        {
            get => _detectCollisions;
            set
            {
                _detectCollisions = value;
                if (_capsuleCollider)
                    _capsuleCollider.enabled = _detectCollisions;
            }
        }

        /// <summary>
        /// 마지막 Move 호출 동안 환경과 충돌한 캡슐의 부분.
        /// </summary>
        public CollisionFlags collisionFlags { get; private set; }

        /// <summary>
        /// 캐릭터의 움직임이 평면에 제한되어 있습니까?
        /// </summary>
        public bool isConstrainedToPlane => _planeConstraint != PlaneConstraint.None;

        /// <summary>
        /// 걸을 수 있는 지면에 있을 때 움직임을 지면에 제한해야 합니까?
        /// 지면 제한을 토글합니다.
        /// </summary>
        public bool constrainToGround
        {
            get => _isConstrainedToGround;
            set => _isConstrainedToGround = value;
        }

        /// <summary>
        /// 캐릭터가 걸을 수 있는 지면에 제한되어 있습니까?
        /// </summary>
        public bool isConstrainedToGround => _isConstrainedToGround && _unconstrainedTimer == 0.0f;

        /// <summary>
        /// 지면 제한이 일시적으로 비활성화되어 있습니까?
        /// </summary>
        public bool isGroundConstraintPaused => _isConstrainedToGround && _unconstrainedTimer > 0.0f;

        /// <summary>
        /// isGroundConstraintPaused가 true인 경우, 이것은 남은 일시 정지 시간을 나타냅니다.
        /// </summary>
        public float unconstrainedTimer => _unconstrainedTimer;

        /// <summary>
        /// 마지막 Move 호출 동안 캐릭터가 지면에 있었습니까?
        /// </summary>
        public bool wasOnGround { get; private set; }

        /// <summary>
        /// 캐릭터가 지면에 있습니까?
        /// </summary>
        public bool isOnGround => _currentGround.hitGround;

        /// <summary>
        /// 마지막 Move 호출 동안 캐릭터가 걸을 수 있는 지면에 있었습니까?
        /// </summary>
        public bool wasOnWalkableGround { get; private set; }

        /// <summary>
        /// 캐릭터가 걸을 수 있는 지면에 있습니까?
        /// </summary>
        public bool isOnWalkableGround => _currentGround.isWalkableGround;

        /// <summary>
        /// 마지막 Move 호출 동안 캐릭터가 걸을 수 있는 지면에 있었고 지면에 제한되어 있었습니까?
        /// </summary>
        public bool wasGrounded { get; private set; }

        /// <summary>
        /// 캐릭터가 걸을 수 있는 지면에 있고 지면에 제한되어 있습니까?
        /// </summary>
        public bool isGrounded => isOnWalkableGround && isConstrainedToGround;

        /// <summary>
        /// 지면까지의 서명된 거리.
        /// </summary>
        public float groundDistance => _currentGround.groundDistance;

        /// <summary>
        /// 현재 지면 충돌 지점.
        /// </summary>
        public Vector3 groundPoint => _currentGround.point;

        /// <summary>
        /// 현재 지면 법선.
        /// </summary>
        public Vector3 groundNormal => _currentGround.normal;

        /// <summary>
        /// 현재 지면 표면 법선.
        /// </summary>
        public Vector3 groundSurfaceNormal => _currentGround.surfaceNormal;

        /// <summary>
        /// 현재 지면 콜라이더.
        /// </summary>
        public Collider groundCollider => _currentGround.collider;

        /// <summary>
        /// 현재 지면 트랜스폼.
        /// </summary>
        public Transform groundTransform => _currentGround.transform;

        /// <summary>
        /// 맞은 콜라이더에 연결된 리지드바디. 콜라이더가 리지드바디에 연결되지 않은 경우 null입니다.
        /// </summary>
        public Rigidbody groundRigidbody => _currentGround.rigidbody;

        /// <summary>
        /// 현재 지면에 대한 정보를 포함하는 구조체.
        /// </summary>
        public FindGroundResult currentGround => _currentGround;

        /// <summary>
        /// 현재 이동 플랫폼에 대한 정보를 포함하는 구조체(있다면).
        /// </summary>
        public MovingPlatform movingPlatform => _movingPlatform;

        /// <summary>
        /// 착지 시의 최종 속도(예: isGrounded).
        /// </summary>
        public Vector3 landedVelocity { get; private set; }

        /// <summary>
        /// 이동 중인 플랫폼에서 명확하게 비이동성 세계 장애물이 없는 것으로 알려진 경우 true로 설정합니다.
        /// 기반 이동 중 스윕을 피하기 위한 최적화, 주의해서 사용하십시오.
        /// </summary>
        public bool fastPlatformMove { get; set; }

        /// <summary>
        /// 캐릭터가 서 있는 이동 플랫폼과 함께 이동하는지 여부.
        /// true인 경우, 캐릭터는 이동 플랫폼과 함께 이동합니다.
        /// </summary>
        public bool impartPlatformMovement
        {
            get => _advanced.impartPlatformMovement;
            set => _advanced.impartPlatformMovement = value;
        }

        /// <summary>
        /// 캐릭터가 서 있는 플랫폼의 회전 변화에 영향을 받는지 여부.
        /// true인 경우, 캐릭터는 이동 플랫폼과 함께 회전합니다.
        /// </summary>
        public bool impartPlatformRotation
        {
            get => _advanced.impartPlatformRotation;
            set => _advanced.impartPlatformRotation = value;
        }

        /// <summary>
        /// true인 경우, 플랫폼에서 점프하거나 떨어질 때 플랫폼의 속도를 부여합니다.
        /// </summary>
        public bool impartPlatformVelocity
        {
            get => _advanced.impartPlatformVelocity;
            set => _advanced.impartPlatformVelocity = value;
        }

        /// <summary>
        /// 활성화된 경우, 플레이어가 동적 리지드바디와 상호작용할 때 적용됩니다.
        /// </summary>
        public bool enablePhysicsInteraction
        {
            get => _advanced.enablePhysicsInteraction;
            set => _advanced.enablePhysicsInteraction = value;
        }

        /// <summary>
        /// 활성화된 경우, 플레이어가 다른 캐릭터와 상호작용할 때 적용됩니다.
        /// </summary>
        public bool physicsInteractionAffectsCharacters
        {
            get => _advanced.allowPushCharacters;
            set => _advanced.allowPushCharacters = value;
        }

        /// <summary>
        /// 걷는 동안 리지드바디에 적용되는 힘(질량 및 상대 속도에 의한)이 이 값으로 스케일됩니다.
        /// </summary>
        public float pushForceScale
        {
            get => _pushForceScale;
            set => _pushForceScale = Mathf.Max(0.0f, value);
        }

        #endregion

        #region CALLBACKS

        /// <summary>
        /// 캐릭터가 주어진 콜라이더와 충돌해야 하는지 정의할 수 있습니다.
        /// </summary>
        /// <param name="collider">콜라이더.</param>
        /// <returns>주어진 콜라이더를 필터링(무시)하려면 true, 충돌하려면 false를 반환합니다.</returns>
        public delegate bool ColliderFilterCallback(Collider collider);

        /// <summary>
        /// 캐릭터가 콜라이더와 충돌할 때의 동작을 정의할 수 있습니다.
        /// </summary>
        /// <param name="collider">충돌한 콜라이더</param>
        /// <returns>원하는 충돌 동작 플래그를 반환합니다.</returns>
        public delegate CollisionBehaviour CollisionBehaviourCallback(Collider collider);

        /// <summary>
        /// 동적 객체와의 충돌 응답을 수정할 수 있습니다,
        /// 예: 결과적인 임펄스 및 / 또는 적용 지점(CollisionResult.point)을 계산합니다.
        /// </summary>
        public delegate void CollisionResponseCallback(ref CollisionResult inCollisionResult, ref Vector3 characterImpulse, ref Vector3 otherImpulse);

        /// <summary>
        /// 캐릭터가 주어진 콜라이더와 충돌해야 하는지 정의할 수 있습니다.
        /// 콜라이더를 필터링(무시)하려면 true를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// </summary>
        public ColliderFilterCallback colliderFilterCallback { get; set; }

        /// <summary>
        /// 캐릭터가 콜라이더와 충돌할 때의 동작을 정의할 수 있습니다.
        /// </summary>
        public CollisionBehaviourCallback collisionBehaviourCallback { get; set; }

        /// <summary>
        /// 동적 객체와의 충돌 응답을 수정할 수 있습니다,
        /// 예: 결과적인 임펄스 및 / 또는 적용 지점(CollisionResult.point)을 계산합니다.
        /// </summary>
        public CollisionResponseCallback collisionResponseCallback { get; set; }

        #endregion

        #region EVENTS

        public delegate void CollidedEventHandler(ref CollisionResult collisionResult);
        public delegate void FoundGroundEventHandler(ref FindGroundResult foundGround);

        /// <summary>
        /// 캐릭터가 Move 중 다른 객체와 충돌할 때 발생하는 이벤트입니다.
        /// 여러 번 호출될 수 있습니다.
        /// </summary>
        public event CollidedEventHandler Collided;

        /// <summary>
        /// 캐릭터가 다운캐스트 스윕의 결과로 지면(걸을 수 있는 지면 또는 걸을 수 없는 지면)을 찾을 때 발생하는 이벤트입니다(예: FindGround 메서드).
        /// </summary>
        public event FoundGroundEventHandler FoundGround;

        /// <summary>
        /// Collided 이벤트를 트리거합니다.
        /// </summary>
        private void OnCollided()
        {
            if (Collided == null)
                return;

            for (int i = 0; i < _collisionCount; i++)
                Collided.Invoke(ref _collisionResults[i]);
        }

        /// <summary>
        /// FoundGround 이벤트를 트리거합니다.
        /// </summary>
        private void OnFoundGround()
        {
            FoundGround?.Invoke(ref _currentGround);
        }

        #endregion

        #region GEOM_NOMRAL_METHODS

        private Vector3 FindOpposingNormal(Vector3 sweepDirDenorm, ref RaycastHit inHit)
        {
            const float kThickness = (kContactOffset - kSweepEdgeRejectDistance) * 0.5f;

            Vector3 result = inHit.normal;

            Vector3 rayOrigin = inHit.point - sweepDirDenorm;

            float rayLength = sweepDirDenorm.magnitude * 2f;
            Vector3 rayDirection = sweepDirDenorm / sweepDirDenorm.magnitude;

            if (Raycast(rayOrigin, rayDirection, rayLength, _collisionLayers, out RaycastHit hitResult, kThickness))
                result = hitResult.normal;

            return result;
        }

        private static Vector3 FindBoxOpposingNormal(Vector3 sweepDirDenorm, ref RaycastHit inHit)
        {
            Transform localToWorld = inHit.transform;

            Vector3 localContactNormal = localToWorld.InverseTransformDirection(inHit.normal);
            Vector3 localTraceDirDenorm = localToWorld.InverseTransformDirection(sweepDirDenorm);

            Vector3 bestLocalNormal = localContactNormal;
            float bestOpposingDot = float.MaxValue;

            for (int i = 0; i < 3; i++)
            {
                if (localContactNormal[i] > kKindaSmallNumber)
                {
                    float traceDotFaceNormal = localTraceDirDenorm[i];
                    if (traceDotFaceNormal < bestOpposingDot)
                    {
                        bestOpposingDot = traceDotFaceNormal;
                        bestLocalNormal = Vector3.zero;
                        bestLocalNormal[i] = 1.0f;
                    }
                }
                else if (localContactNormal[i] < -kKindaSmallNumber)
                {
                    float traceDotFaceNormal = -localTraceDirDenorm[i];
                    if (traceDotFaceNormal < bestOpposingDot)
                    {
                        bestOpposingDot = traceDotFaceNormal;
                        bestLocalNormal = Vector3.zero;
                        bestLocalNormal[i] = -1.0f;
                    }
                }
            }

            return localToWorld.TransformDirection(bestLocalNormal);
        }

        private static Vector3 FindBoxOpposingNormal(Vector3 displacement, Vector3 hitNormal, Transform hitTransform)
        {
            Transform localToWorld = hitTransform;

            Vector3 localContactNormal = localToWorld.InverseTransformDirection(hitNormal);
            Vector3 localTraceDirDenorm = localToWorld.InverseTransformDirection(displacement);

            Vector3 bestLocalNormal = localContactNormal;
            float bestOpposingDot = float.MaxValue;

            for (int i = 0; i < 3; i++)
            {
                if (localContactNormal[i] > kKindaSmallNumber)
                {
                    float traceDotFaceNormal = localTraceDirDenorm[i];
                    if (traceDotFaceNormal < bestOpposingDot)
                    {
                        bestOpposingDot = traceDotFaceNormal;
                        bestLocalNormal = Vector3.zero;
                        bestLocalNormal[i] = 1.0f;
                    }
                }
                else if (localContactNormal[i] < -kKindaSmallNumber)
                {
                    float traceDotFaceNormal = -localTraceDirDenorm[i];
                    if (traceDotFaceNormal < bestOpposingDot)
                    {
                        bestOpposingDot = traceDotFaceNormal;
                        bestLocalNormal = Vector3.zero;
                        bestLocalNormal[i] = -1.0f;
                    }
                }
            }

            return localToWorld.TransformDirection(bestLocalNormal);
        }

        private static Vector3 FindTerrainOpposingNormal(ref RaycastHit inHit)
        {
            TerrainCollider terrainCollider = inHit.collider as TerrainCollider;

            if (terrainCollider != null)
            {
                Vector3 localPoint = terrainCollider.transform.InverseTransformPoint(inHit.point);

                TerrainData terrainData = terrainCollider.terrainData;

                Vector3 interpolatedNormal = terrainData.GetInterpolatedNormal(localPoint.x / terrainData.size.x,
                    localPoint.z / terrainData.size.z);

                return interpolatedNormal;
            }

            return inHit.normal;
        }

        /// <summary>
        /// 실제 표면 법선을 검색하는 도우미 메서드, 보통 스윕 방향에 가장 '반대되는' 법선입니다.
        /// </summary>

        private Vector3 FindGeomOpposingNormal(Vector3 sweepDirDenorm, ref RaycastHit inHit)
        {
            // SphereCollider 또는 CapsuleCollider

            if (inHit.collider is SphereCollider _ || inHit.collider is CapsuleCollider _)
            {
                // 특별한 계산을 하지 않습니다. inHit.normal이 올바른 값입니다.

                return inHit.normal;
            }

            // BoxCollider

            if (inHit.collider is BoxCollider _)
            {
                return FindBoxOpposingNormal(sweepDirDenorm, ref inHit);
            }

            // 비-볼록 MeshCollider (읽기/쓰기가 가능해야 함!)

            if (inHit.collider is MeshCollider nonConvexMeshCollider && !nonConvexMeshCollider.convex)
            {
                Mesh sharedMesh = nonConvexMeshCollider.sharedMesh;
                if (sharedMesh && sharedMesh.isReadable)
                    return MeshUtility.FindMeshOpposingNormal(sharedMesh, ref inHit);

                // 읽기/쓰기가 불가능하면, 레이캐스트로 대체...

                return FindOpposingNormal(sweepDirDenorm, ref inHit);
            }

            // 볼록 MeshCollider

            if (inHit.collider is MeshCollider convexMeshCollider && convexMeshCollider.convex)
            {
                // Unity에서 노멀을 계산할 수 있는 데이터가 노출되지 않습니다. 레이캐스트로 대체...

                return FindOpposingNormal(sweepDirDenorm, ref inHit);
            }

            // TerrainCollider

            if (inHit.collider is TerrainCollider)
            {
                return FindTerrainOpposingNormal(ref inHit);
            }

            return inHit.normal;
        }

        #endregion

        #region METHODS

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        /// <summary>
        /// 주어진 속도에 마찰과 제동 감속을 적용합니다.
        /// </summary>
        /// <param name="currentVelocity">캐릭터의 현재 속도입니다.</param>
        /// <param name="friction">제동 시 적용되는 마찰(항력) 계수입니다.</param>
        /// <param name="deceleration">캐릭터가 속도를 줄이는 비율입니다. 이는 속도를 일정 값만큼 직접적으로 낮추는 일정한 반대 힘입니다.</param>
        /// <param name="deltaTime">시뮬레이션 델타 시간입니다.</param>
        /// <returns>업데이트된 속도를 반환합니다.</returns>

        private static Vector3 ApplyVelocityBraking(Vector3 currentVelocity, float friction, float deceleration, float deltaTime)
        {
            // 마찰이나 제동이 없으면 현재 속도를 반환합니다.

            bool isZeroFriction = friction == 0.0f;
            bool isZeroBraking = deceleration == 0.0f;

            if (isZeroFriction && isZeroBraking)
                return currentVelocity;

            // 속도를 줄여서 멈춥니다.

            Vector3 oldVel = currentVelocity;
            Vector3 revAcceleration = isZeroBraking ? Vector3.zero : -deceleration * currentVelocity.normalized;

            // 마찰과 제동을 적용합니다.

            currentVelocity += (-friction * currentVelocity + revAcceleration) * deltaTime;

            // 방향을 반대로 하지 않습니다.

            if (Vector3.Dot(currentVelocity, oldVel) <= 0.0f)
                return Vector3.zero;

            // 거의 0이거나 최소 임계값보다 낮을 경우 0으로 클램프합니다.

            float sqrSpeed = currentVelocity.sqrMagnitude;
            if (sqrSpeed <= 0.00001f || !isZeroBraking && sqrSpeed <= 0.01f)
                return Vector3.zero;

            return currentVelocity;
        }

        /// <summary>
        /// 원하는 속도가 최대 속도에서 얼마나 떨어져 있는지를 결정합니다.
        /// </summary>
        /// <param name="desiredVelocity">목표 속도입니다.</param>
        /// <param name="maxSpeed">허용되는 최대 속도입니다.</param>
        /// <returns>0에서 1 사이의 아날로그 입력 보정을 반환합니다.</returns>

        private static float ComputeAnalogInputModifier(Vector3 desiredVelocity, float maxSpeed)
        {
            if (maxSpeed > 0.0f && desiredVelocity.sqrMagnitude > 0.0f)
                return Mathf.Clamp01(desiredVelocity.magnitude / maxSpeed);

            return 0.0f;
        }

        /// <summary>
        /// 주어진 상태에 대해 마찰 또는 제동 마찰과 가속 또는 감속의 영향을 적용하여 새로운 속도를 계산합니다.
        /// </summary>
        /// <param name="currentVelocity">캐릭터의 현재 속도입니다.</param>
        /// <param name="desiredVelocity">목표 속도입니다.</param>
        /// <param name="maxSpeed">지면에서의 최대 속도입니다. 또한, 떨어질 때(즉, 지면에 있지 않을 때)의 최대 수평 속도를 결정합니다.</param>
        /// <param name="acceleration">가속 시(즉, desiredVelocity != Vector3.zero일 때)의 속도 변화 비율입니다.</param>
        /// <param name="deceleration">제동 시(즉, 가속하지 않거나 캐릭터가 최대 속도를 초과한 경우) 캐릭터가 속도를 줄이는 비율입니다.
        /// 이는 속도를 일정 값만큼 직접적으로 낮추는 일정한 반대 힘입니다.</param>
        /// <param name="friction">이동 제어에 영향을 미치는 설정입니다. 값이 높을수록 방향 변경이 더 빨라집니다.</param>
        /// <param name="brakingFriction">제동 시(즉, desiredVelocity == Vector3.zero이거나 캐릭터가 최대 속도를 초과한 경우) 적용되는 마찰(항력) 계수입니다.</param>
        /// <param name="deltaTime">시뮬레이션 델타 시간입니다. 기본값은 Time.deltaTime입니다.</param>
        /// <returns>업데이트된 속도를 반환합니다.</returns>

        private static Vector3 CalcVelocity(Vector3 currentVelocity, Vector3 desiredVelocity, float maxSpeed,
            float acceleration, float deceleration, float friction, float brakingFriction, float deltaTime)
        {
            // 요청된 이동 방향을 계산합니다.

            float desiredSpeed = desiredVelocity.magnitude;
            Vector3 desiredMoveDirection = desiredSpeed > 0.0f ? desiredVelocity / desiredSpeed : Vector3.zero;

            // 요청된 가속도(아날로그 입력을 고려한)

            float analogInputModifier = ComputeAnalogInputModifier(desiredVelocity, maxSpeed);
            Vector3 requestedAcceleration = acceleration * analogInputModifier * desiredMoveDirection;

            // 실제 최대 속도(아날로그 입력을 고려한)

            float actualMaxSpeed = Mathf.Max(0.0f, maxSpeed * analogInputModifier);

            // 마찰
            // 입력 가속도가 없거나 최대 속도를 초과하여 속도를 줄여야 할 경우에만 제동을 적용합니다.

            bool isZeroAcceleration = requestedAcceleration.isZero();
            bool isVelocityOverMax = currentVelocity.isExceeding(actualMaxSpeed);

            if (isZeroAcceleration || isVelocityOverMax)
            {
                // 제동 전의 현재 속도

                Vector3 oldVelocity = currentVelocity;

                // 마찰과 제동을 적용합니다.

                currentVelocity = ApplyVelocityBraking(currentVelocity, brakingFriction, deceleration, deltaTime);

                // 시작할 때 최대 속도를 초과한 경우 제동으로 인해 최대 속도 이하로 내려가지 않도록 합니다.

                if (isVelocityOverMax && currentVelocity.sqrMagnitude < actualMaxSpeed.square() &&
                    Vector3.Dot(requestedAcceleration, oldVelocity) > 0.0f)
                    currentVelocity = oldVelocity.normalized * actualMaxSpeed;
            }
            else
            {
                // 마찰, 이는 방향 변경 능력에 영향을 미칩니다.

                currentVelocity -= (currentVelocity - desiredMoveDirection * currentVelocity.magnitude) * Mathf.Min(friction * deltaTime, 1.0f);
            }

            // 가속도를 적용합니다.

            if (!isZeroAcceleration)
            {
                float newMaxSpeed = currentVelocity.isExceeding(actualMaxSpeed) ? currentVelocity.magnitude : actualMaxSpeed;

                currentVelocity += requestedAcceleration * deltaTime;
                currentVelocity = currentVelocity.clampedTo(newMaxSpeed);
            }

            // 새로운 속도를 반환합니다.

            return currentVelocity;
        }

        /// <summary>
        /// 리지드바디가 주어진 worldPoint에서 가지는 속도를 얻기 위한 도우미 메서드로, 리지드바디의 각속도를 고려하여 속도를 계산합니다.
        /// 리지드바디가 캐릭터일 경우, 캐릭터의 속도를 반환합니다.
        /// </summary>

        private static Vector3 GetRigidbodyVelocity(Rigidbody rigidbody, Vector3 worldPoint)
        {
            if (rigidbody == null)
                return Vector3.zero;

            return rigidbody.TryGetComponent(out CharacterMovement controller)
                ? controller.velocity
                : rigidbody.GetPointVelocity(worldPoint);
        }

        /// <summary>
        /// 주어진 동작 플래그가 CollisionBehaviour.Walkable 값을 포함하는지 테스트하는 도우미 메서드입니다.
        /// </summary>

        private static bool IsWalkable(CollisionBehaviour behaviourFlags)
        {
            return (behaviourFlags & CollisionBehaviour.Walkable) != 0;
        }

        /// <summary>
        /// 주어진 동작 플래그가 CollisionBehaviour.NotWalkable 값을 포함하는지 테스트하는 도우미 메서드입니다.
        /// </summary>

        private static bool IsNotWalkable(CollisionBehaviour behaviourFlags)
        {
            return (behaviourFlags & CollisionBehaviour.NotWalkable) != 0;
        }

        /// <summary>
        /// 주어진 동작 플래그가 CollisionBehaviour.CanPerchOn 값을 포함하는지 테스트하는 도우미 메서드입니다.
        /// </summary>

        private static bool CanPerchOn(CollisionBehaviour behaviourFlags)
        {
            return (behaviourFlags & CollisionBehaviour.CanPerchOn) != 0;
        }

        /// <summary>
        /// 주어진 동작 플래그가 CollisionBehaviour.CanNotPerchOn 값을 포함하는지 테스트하는 도우미 메서드입니다.
        /// </summary>

        private static bool CanNotPerchOn(CollisionBehaviour behaviourFlags)
        {
            return (behaviourFlags & CollisionBehaviour.CanNotPerchOn) != 0;
        }

        /// <summary>
        /// 주어진 동작 플래그가 CollisionBehaviour.CanStepOn 값을 포함하는지 테스트하는 도우미 메서드입니다.
        /// </summary>

        private static bool CanStepOn(CollisionBehaviour behaviourFlags)
        {
            return (behaviourFlags & CollisionBehaviour.CanStepOn) != 0;
        }

        /// <summary>
        /// 주어진 동작 플래그가 CollisionBehaviour.CanNotStepOn 값을 포함하는지 테스트하는 도우미 메서드입니다.
        /// </summary>

        private static bool CanNotStepOn(CollisionBehaviour behaviourFlags)
        {
            return (behaviourFlags & CollisionBehaviour.CanNotStepOn) != 0;
        }

        /// <summary>
        /// 주어진 동작 플래그가 CollisionBehaviour.CanRideOn 값을 포함하는지 테스트하는 도우미 메서드입니다.
        /// </summary>

        private static bool CanRideOn(CollisionBehaviour behaviourFlags)
        {
            return (behaviourFlags & CollisionBehaviour.CanRideOn) != 0;
        }

        /// <summary>
        /// 주어진 동작 플래그가 CollisionBehaviour.CanNotRideOn 값을 포함하는지 테스트하는 도우미 메서드입니다.
        /// </summary>

        private static bool CanNotRideOn(CollisionBehaviour behaviourFlags)
        {
            return (behaviourFlags & CollisionBehaviour.CanNotRideOn) != 0;
        }

        /// <summary>
        /// 주어진 치수로 캡슐을 생성하는 도우미 함수입니다.
        /// </summary>
        /// <param name="radius">캡슐의 반지름입니다.</param>
        /// <param name="height">캡슐의 높이입니다.</param>
        /// <param name="center">로컬 공간에서의 출력 캡슐 중심입니다.</param>
        /// <param name="bottomCenter">로컬 공간에서의 출력 캡슐 하단 구체 중심입니다.</param>
        /// <param name="topCenter">로컬 공간에서의 출력 캡슐 상단 구체 중심입니다.</param>


        private static void MakeCapsule(float radius, float height, out Vector3 center, out Vector3 bottomCenter, out Vector3 topCenter)
        {
            radius = Mathf.Max(radius, 0.0f);
            height = Mathf.Max(height, radius * 2.0f);

            center = height * 0.5f * Vector3.up;

            float sideHeight = height - radius * 2.0f;

            bottomCenter = center - sideHeight * 0.5f * Vector3.up;
            topCenter = center + sideHeight * 0.5f * Vector3.up;
        }

        /// <summary>
        /// 캐릭터의 경계 볼륨(예: 캡슐) 치수를 지정합니다.
        /// </summary>
        /// <param name="characterRadius">캐릭터의 볼륨 반지름입니다.</param>
        /// <param name="characterHeight">캐릭터의 볼륨 높이입니다.</param>

        public void SetDimensions(float characterRadius, float characterHeight)
        {
            _radius = Mathf.Max(characterRadius, 0.0f);
            _height = Mathf.Max(characterHeight, characterRadius * 2.0f);

            MakeCapsule(_radius, _height, out _capsuleCenter, out _capsuleBottomCenter, out _capsuleTopCenter);

#if UNITY_EDITOR
            if (_capsuleCollider == null)
                _capsuleCollider = GetComponent<CapsuleCollider>();
#endif

            if (_capsuleCollider)
            {
                _capsuleCollider.radius = _radius;
                _capsuleCollider.height = _height;
                _capsuleCollider.center = _capsuleCenter;
            }
        }

        /// <summary>
        /// 캐릭터의 경계 볼륨(예: 캡슐) 높이를 지정합니다.
        /// </summary>
        /// <param name="characterHeight">캐릭터의 볼륨 높이입니다.</param>

        public void SetHeight(float characterHeight)
        {
            _height = Mathf.Max(characterHeight, _radius * 2.0f);

            MakeCapsule(_radius, _height, out _capsuleCenter, out _capsuleBottomCenter, out _capsuleTopCenter);

#if UNITY_EDITOR
            if (_capsuleCollider == null)
                _capsuleCollider = GetComponent<CapsuleCollider>();
#endif

            if (_capsuleCollider)
            {
                _capsuleCollider.height = _height;
                _capsuleCollider.center = _capsuleCenter;
            }
        }

        /// <summary>
        /// 필요한 컴포넌트를 캐시하고 초기화합니다.
        /// </summary>

        private void CacheComponents()
        {
            _transform = GetComponent<Transform>();

            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody)
            {
                _rigidbody.drag = 0.0f;
                _rigidbody.angularDrag = 0.0f;

                _rigidbody.useGravity = false;
                _rigidbody.isKinematic = true;
            }

            _capsuleCollider = GetComponent<CapsuleCollider>();
        }

        /// <summary>
        /// 현재 평면 제약 조건의 법선을 반환합니다.
        /// </summary>

        public Vector3 GetPlaneConstraintNormal()
        {
            return _constraintPlaneNormal;
        }

        /// <summary>
        /// 이동을 제한하는 축을 정의합니다. 지정된 축을 따라 이동하는 것은 불가능합니다.
        /// </summary>

        public void SetPlaneConstraint(PlaneConstraint constrainAxis, Vector3 planeNormal)
        {
            _planeConstraint = constrainAxis;

            switch (_planeConstraint)
            {
                case PlaneConstraint.None:
                    {
                        _constraintPlaneNormal = Vector3.zero;

                        if (_rigidbody)
                            _rigidbody.constraints = RigidbodyConstraints.None;

                        break;
                    }

                case PlaneConstraint.ConstrainXAxis:
                    {
                        _constraintPlaneNormal = Vector3.right;

                        if (_rigidbody)
                            _rigidbody.constraints = RigidbodyConstraints.FreezePositionX;

                        break;
                    }

                case PlaneConstraint.ConstrainYAxis:
                    {
                        _constraintPlaneNormal = Vector3.up;

                        if (_rigidbody)
                            _rigidbody.constraints = RigidbodyConstraints.FreezePositionY;

                        break;
                    }

                case PlaneConstraint.ConstrainZAxis:
                    {
                        _constraintPlaneNormal = Vector3.forward;

                        if (_rigidbody)
                            _rigidbody.constraints = RigidbodyConstraints.FreezePositionZ;

                        break;
                    }

                case PlaneConstraint.Custom:
                    {
                        _constraintPlaneNormal = planeNormal;

                        if (_rigidbody)
                            _rigidbody.constraints = RigidbodyConstraints.None;

                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 주어진 방향(정규화된) 벡터를 현재 제약 평면에 맞추어 제약합니다(_constrainToPlane != None인 경우)
        /// 또는 주어진 벡터를 반환합니다(_constrainToPlane == None인 경우).
        /// </summary>

        public Vector3 ConstrainDirectionToPlane(Vector3 direction)
        {
            return ConstrainVectorToPlane(direction).normalized;
        }

        /// <summary>
        /// 주어진 벡터를 현재 PlaneConstraint(제약 조건)에 맞춥니다.
        /// </summary>

        public Vector3 ConstrainVectorToPlane(Vector3 vector)
        {
            return isConstrainedToPlane ? vector.projectedOnPlane(_constraintPlaneNormal) : vector;
        }

        /// <summary>
        /// 마지막 이동 충돌 플래그를 초기화합니다.
        /// </summary>

        private void ResetCollisionFlags()
        {
            collisionFlags = CollisionFlags.None;
        }

        /// <summary>
        /// 현재 충돌 플래그에 HitLocation을 추가합니다.
        /// </summary>

        private void UpdateCollisionFlags(HitLocation hitLocation)
        {
            collisionFlags |= (CollisionFlags)hitLocation;
        }

        /// <summary>
        /// 주어진 법선에 대해 캡슐의 히트 위치를 결정합니다.
        /// </summary>

        private HitLocation ComputeHitLocation(Vector3 inNormal)
        {
            float verticalComponent = inNormal.dot(_characterUp);

            if (verticalComponent > kHemisphereLimit)
                return HitLocation.Below;

            return verticalComponent < -kHemisphereLimit ? HitLocation.Above : HitLocation.Sides;
        }

        /// <summary>
        /// 주어진 콜라이더와 충격 법선이 걷기 가능한 지면으로 간주되어야 하는지 여부를 결정합니다.
        /// </summary>

        private bool IsWalkable(Collider inCollider, Vector3 inNormal)
        {
            // 히트가 캡슐 하단 구체에 있지 않으면 신경 쓰지 않습니다.

            if (ComputeHitLocation(inNormal) != HitLocation.Below)
                return false;

            // 충돌 동작 콜백이 할당된 경우, 걷기 가능/불가능 플래그를 확인합니다.

            if (collisionBehaviourCallback != null)
            {
                CollisionBehaviour collisionBehaviour = collisionBehaviourCallback.Invoke(inCollider);

                if (IsWalkable(collisionBehaviour))
                    return Vector3.Dot(inNormal, _characterUp) > kMaxWalkableSlopeLimit;

                if (IsNotWalkable(collisionBehaviour))
                    return Vector3.Dot(inNormal, _characterUp) > kMinWalkableSlopeLimit;
            }

            // slopeLimitOverride가 활성화된 경우, SlopeLimitBehaviour 컴포넌트를 확인합니다.

            float actualSlopeLimit = _minSlopeLimit;

            if (_slopeLimitOverride && inCollider.TryGetComponent(out SlopeLimitBehaviour slopeLimitOverrideComponent))
            {
                switch (slopeLimitOverrideComponent.walkableSlopeBehaviour)
                {
                    case SlopeBehaviour.Walkable:
                        actualSlopeLimit = kMaxWalkableSlopeLimit;
                        break;

                    case SlopeBehaviour.NotWalkable:
                        actualSlopeLimit = kMinWalkableSlopeLimit;
                        break;

                    case SlopeBehaviour.Override:
                        actualSlopeLimit = slopeLimitOverrideComponent.slopeLimitCos;
                        break;

                    case SlopeBehaviour.Default:
                        break;
                }
            }

            // 주어진 법선이 걷기 가능한지 여부를 결정합니다.

            return Vector3.Dot(inNormal, _characterUp) > actualSlopeLimit;
        }

        /// <summary>
        /// 걷기 가능한 지면에서 이동 중 비걷기 가능한 지면과 충돌 시, 히트 법선을 수정합니다(예: 차단 히트 법선).
        /// 걷기 불가능한 표면으로 밀리지 않도록 하거나,
        /// 충격이 캡슐의 상단 부분에 있을 때 지면으로 밀리지 않도록 합니다.
        /// </summary>

        private Vector3 ComputeBlockingNormal(Vector3 inNormal, bool isWalkable)
        {
            if ((isGrounded || _hasLanded) && !isWalkable)
            {
                Vector3 actualGroundNormal = _hasLanded ? _foundGround.normal : _currentGround.normal;

                Vector3 forward = actualGroundNormal.perpendicularTo(inNormal);
                Vector3 blockingNormal = forward.perpendicularTo(_characterUp);

                if (Vector3.Dot(blockingNormal, inNormal) < 0.0f)
                    blockingNormal = -blockingNormal;

                if (!blockingNormal.isZero())
                    inNormal = blockingNormal;

                return inNormal;
            }

            return inNormal;

        }

        /// <summary>
        /// 주어진 콜라이더가 필터링(무시)되어야 하는지 여부를 결정합니다.
        /// 필터링해야 하면 true를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// </summary>

        private bool ShouldFilter(Collider otherCollider)
        {
            if (otherCollider == _capsuleCollider || otherCollider.attachedRigidbody == rigidbody)
                return true;

            if (_ignoredColliders.Contains(otherCollider))
                return true;

            Rigidbody attachedRigidbody = otherCollider.attachedRigidbody;
            if (attachedRigidbody && _ignoredRigidbodies.Contains(attachedRigidbody))
                return true;

            return colliderFilterCallback != null && colliderFilterCallback.Invoke(otherCollider);
        }

        /// <summary>
        /// 캐릭터의 콜라이더(예: CapsuleCollider)가 otherCollider와의 모든 충돌을 무시하도록 합니다.
        /// 참고: otherCollider가 CollisionLayers 마스크에 있는 경우 Move 호출 중에 캐릭터가 여전히 다른 것과 충돌할 수 있습니다.
        /// </summary>

        public void CapsuleIgnoreCollision(Collider otherCollider, bool ignore = true)
        {
            if (otherCollider == null)
                return;

            Physics.IgnoreCollision(_capsuleCollider, otherCollider, ignore);
        }

        /// <summary>
        /// 캐릭터가 otherCollider와의 모든 충돌을 무시하도록 합니다.
        /// </summary>

        public void IgnoreCollision(Collider otherCollider, bool ignore = true)
        {
            if (otherCollider == null)
                return;

            if (ignore)
                _ignoredColliders.Add(otherCollider);
            else
                _ignoredColliders.Remove(otherCollider);
        }

        /// <summary>
        /// 캐릭터가 otherRigidbody에 부착된 모든 콜라이더와의 충돌을 무시하도록 합니다.
        /// </summary>

        public void IgnoreCollision(Rigidbody otherRigidbody, bool ignore = true)
        {
            if (otherRigidbody == null)
                return;

            if (ignore)
                _ignoredRigidbodies.Add(otherRigidbody);
            else
                _ignoredRigidbodies.Remove(otherRigidbody);
        }

        /// <summary>
        /// 마지막 Move 호출 중의 충돌 결과를 지웁니다.
        /// </summary>

        private void ClearCollisionResults()
        {
            _collisionCount = 0;
        }

        /// <summary>
        /// Move 중에 발견된 충돌 결과 목록에 CollisionResult를 추가합니다.
        /// CollisionResult가 otherRigidbody와 관련된 경우 첫 번째 것만 추가합니다.
        /// </summary>

        private void AddCollisionResult(ref CollisionResult collisionResult)
        {
            UpdateCollisionFlags(collisionResult.hitLocation);

            if (collisionResult.rigidbody)
            {
                // 현재 타고 있는 플랫폼에 대한 모든 동적 충돌을 처리하지 않습니다.

                if (collisionResult.rigidbody == _movingPlatform.platform)
                    return;

                // 리지드바디와의 첫 번째 충돌만 처리합니다.

                for (int i = 0; i < _collisionCount; i++)
                {
                    if (collisionResult.rigidbody == _collisionResults[i].rigidbody)
                        return;
                }
            }

            if (_collisionCount < kMaxCollisionCount)
                _collisionResults[_collisionCount++] = collisionResult;
        }

        /// <summary>
        /// 마지막 Move 호출 중에 발견된 충돌 수를 반환합니다.
        /// </summary>

        public int GetCollisionCount()
        {
            return _collisionCount;
        }

        /// <summary>
        /// 마지막 Move 호출 목록에서 CollisionResult를 검색합니다.
        /// </summary>

        public CollisionResult GetCollisionResult(int index)
        {
            return _collisionResults[index];
        }

        /// <summary>
        /// 주어진 콜라이더들을 분리하기 위해 필요한 최소 번역 거리(MTD)를 계산합니다.
        /// 더 나은 결과를 위해 팽창된 캡슐을 사용합니다.
        /// </summary>

        private bool ComputeInflatedMTD(Vector3 characterPosition, Quaternion characterRotation, float mtdInflation,
            Collider hitCollider, Transform hitTransform, out Vector3 mtdDirection, out float mtdDistance)
        {
            mtdDirection = Vector3.zero;
            mtdDistance = 0.0f;

            _capsuleCollider.radius = _radius + mtdInflation * 1.0f;
            _capsuleCollider.height = _height + mtdInflation * 2.0f;

            bool mtdResult = Physics.ComputePenetration(_capsuleCollider, characterPosition, characterRotation,
                hitCollider, hitTransform.position, hitTransform.rotation, out Vector3 recoverDirection, out float recoverDistance);

            if (mtdResult)
            {
                if (IsFinite(recoverDirection))
                {
                    mtdDirection = recoverDirection;
                    mtdDistance = Mathf.Max(Mathf.Abs(recoverDistance) - mtdInflation, 0.0f) + kKindaSmallNumber;
                }
                else
                {
                    Debug.LogWarning($"Warning: ComputeInflatedMTD_Internal: MTD returned NaN " + recoverDirection.ToString("F4"));
                }
            }

            _capsuleCollider.radius = _radius;
            _capsuleCollider.height = _height;

            return mtdResult;
        }

        /// <summary>
        /// 주어진 콜라이더들을 분리하기 위해 필요한 최소 번역 거리(MTD)를 계산합니다.
        /// 더 나은 결과를 위해 팽창된 캡슐을 사용하며, 작은 팽창으로 더 높은 정확도를 시도한 후 정밀도 문제로 실패한 경우 더 큰 팽창을 사용합니다.
        /// </summary>

        private bool ComputeMTD(Vector3 characterPosition, Quaternion characterRotation, Collider hitCollider, Transform hitTransform, out Vector3 mtdDirection, out float mtdDistance)
        {
            const float kSmallMTDInflation = 0.0025f;
            const float kLargeMTDInflation = 0.0175f;

            if (ComputeInflatedMTD(characterPosition, characterRotation, kSmallMTDInflation, hitCollider, hitTransform, out mtdDirection, out mtdDistance) ||
                ComputeInflatedMTD(characterPosition, characterRotation, kLargeMTDInflation, hitCollider, hitTransform, out mtdDirection, out mtdDistance))
            {
                // 성공

                return true;
            }

            // 실패

            return false;
        }

        /// <summary>
        /// 지정된 콜라이더에 대해 캐릭터의 볼륨 중첩을 해결합니다.
        /// </summary>

        private void ResolveOverlaps(DepenetrationBehaviour depenetrationBehaviour = DepenetrationBehaviour.IgnoreNone)
        {
            if (!detectCollisions)
                return;

            bool ignoreStatic = (depenetrationBehaviour & DepenetrationBehaviour.IgnoreStatic) != 0;
            bool ignoreDynamic = (depenetrationBehaviour & DepenetrationBehaviour.IgnoreDynamic) != 0;
            bool ignoreKinematic = (depenetrationBehaviour & DepenetrationBehaviour.IgnoreKinematic) != 0;

            for (int i = 0; i < _advanced.maxDepenetrationIterations; i++)
            {
                Vector3 top = updatedPosition + _transformedCapsuleTopCenter;
                Vector3 bottom = updatedPosition + _transformedCapsuleBottomCenter;

                int overlapCount = Physics.OverlapCapsuleNonAlloc(bottom, top, _radius, _overlaps, _collisionLayers, triggerInteraction);
                if (overlapCount == 0)
                    break;

                for (int j = 0; j < overlapCount; j++)
                {
                    Collider overlappedCollider = _overlaps[j];

                    if (ShouldFilter(overlappedCollider))
                        continue;

                    Rigidbody attachedRigidbody = overlappedCollider.attachedRigidbody;

                    if (ignoreStatic && attachedRigidbody == null)
                        continue;

                    if (attachedRigidbody)
                    {
                        bool isKinematic = attachedRigidbody.isKinematic;

                        if (ignoreKinematic && isKinematic)
                            continue;

                        if (ignoreDynamic && !isKinematic)
                            continue;
                    }

                    if (ComputeMTD(updatedPosition, updatedRotation, overlappedCollider, overlappedCollider.transform, out Vector3 recoverDirection, out float recoverDistance))
                    {
                        recoverDirection = ConstrainDirectionToPlane(recoverDirection);

                        HitLocation hitLocation = ComputeHitLocation(recoverDirection);

                        bool isWalkable = IsWalkable(overlappedCollider, recoverDirection);

                        Vector3 impactNormal = ComputeBlockingNormal(recoverDirection, isWalkable);

                        updatedPosition += impactNormal * (recoverDistance + kPenetrationOffset);

                        if (_collisionCount < kMaxCollisionCount)
                        {
                            Vector3 point;

                            if (hitLocation == HitLocation.Above)
                                point = updatedPosition + _transformedCapsuleTopCenter - recoverDirection * _radius;
                            else if (hitLocation == HitLocation.Below)
                                point = updatedPosition + _transformedCapsuleBottomCenter - recoverDirection * _radius;
                            else
                                point = updatedPosition + _transformedCapsuleCenter - recoverDirection * _radius;

                            CollisionResult collisionResult = new CollisionResult
                            {
                                startPenetrating = true,

                                hitLocation = hitLocation,
                                isWalkable = isWalkable,

                                position = updatedPosition,

                                velocity = _velocity,
                                otherVelocity = GetRigidbodyVelocity(attachedRigidbody, point),

                                point = point,
                                normal = impactNormal,

                                surfaceNormal = impactNormal,

                                collider = overlappedCollider
                            };

                            AddCollisionResult(ref collisionResult);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 주어진 캡슐을 물리 세계와 비교하고 모든 중첩된 콜라이더를 반환합니다.
        /// 중첩된 콜라이더 수를 반환합니다.
        /// </summary>

        public int OverlapTest(Vector3 characterPosition, Quaternion characterRotation, float testRadius,
            float testHeight, int layerMask, Collider[] results, QueryTriggerInteraction queryTriggerInteraction)
        {
            MakeCapsule(testRadius, testHeight, out Vector3 _, out Vector3 bottomCenter, out Vector3 topCenter);

            Vector3 top = characterPosition + characterRotation * topCenter;
            Vector3 bottom = characterPosition + characterRotation * bottomCenter;

            int rawOverlapCount =
                Physics.OverlapCapsuleNonAlloc(bottom, top, testRadius, results, layerMask, queryTriggerInteraction);

            if (rawOverlapCount == 0)
                return 0;

            int filteredOverlapCount = rawOverlapCount;

            for (int i = 0; i < rawOverlapCount; i++)
            {
                Collider overlappedCollider = results[i];

                if (ShouldFilter(overlappedCollider))
                {
                    if (i < --filteredOverlapCount)
                        results[i] = results[filteredOverlapCount];
                }
            }

            return filteredOverlapCount;
        }

        /// <summary>
        /// 주어진 캡슐을 물리 세계와 비교하고 모든 중첩된 콜라이더를 반환합니다.
        /// 중첩된 콜라이더 배열을 반환합니다.
        /// </summary>

        public Collider[] OverlapTest(Vector3 characterPosition, Quaternion characterRotation, float testRadius,
            float testHeight, int layerMask, QueryTriggerInteraction queryTriggerInteraction, out int overlapCount)
        {
            overlapCount = OverlapTest(characterPosition, characterRotation, testRadius, testHeight, layerMask,
                _overlaps, queryTriggerInteraction);

            return _overlaps;
        }

        /// <summary>
        /// 캐릭터의 캡슐을 물리 세계와 비교하고 모든 중첩된 콜라이더를 반환합니다.
        /// 중첩된 콜라이더 배열을 반환합니다.
        /// </summary>

        public Collider[] OverlapTest(int layerMask, QueryTriggerInteraction queryTriggerInteraction,
            out int overlapCount)
        {
            overlapCount =
                OverlapTest(position, rotation, radius, height, layerMask, _overlaps, queryTriggerInteraction);

            return _overlaps;
        }

        /// <summary>
        /// 테스트 높이를 사용하여 월드 공간에서 캐릭터의 캡슐 모양의 볼륨과 겹치는 콜라이더가 있는지 확인합니다.
        /// 차단 중첩이 있는 경우 true를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// </summary>

        public bool CheckCapsule()
        {
            IgnoreCollision(_movingPlatform.platform);

            int overlapCount =
                OverlapTest(position, rotation, radius, height, collisionLayers, _overlaps, triggerInteraction);

            IgnoreCollision(_movingPlatform.platform, false);

            return overlapCount > 0;
        }

        /// <summary>
        /// 테스트 높이를 사용하여 월드 공간에서 캐릭터의 캡슐 모양의 볼륨과 겹치는 콜라이더가 있는지 확인합니다.
        /// 차단 중첩이 있는 경우 true를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// </summary>

        public bool CheckHeight(float testHeight)
        {
            IgnoreCollision(_movingPlatform.platform);

            int overlapCount =
                OverlapTest(position, rotation, radius, testHeight, collisionLayers, _overlaps, triggerInteraction);

            IgnoreCollision(_movingPlatform.platform, false);

            return overlapCount > 0;
        }

        /// <summary>
        /// 충돌 지점까지의 2D 거리가 가장자리 공차(CapsuleRadius에서 작은 거부 임계값 뺀 값) 내에 있는지 여부를 반환합니다.
        /// 바닥 또는 착륙 지점을 찾을 때 인접한 충돌을 거부하는 데 유용합니다.
        /// </summary>

        public bool IsWithinEdgeTolerance(Vector3 characterPosition, Vector3 inPoint, float testRadius)
        {
            float distFromCenterSq = (inPoint - characterPosition).projectedOnPlane(_characterUp).sqrMagnitude;

            float reducedRadius = Mathf.Max(kSweepEdgeRejectDistance + kKindaSmallNumber,
                testRadius - kSweepEdgeRejectDistance);

            return distFromCenterSq < reducedRadius * reducedRadius;
        }

        /// <summary>
        /// 충돌 결과를 기준으로 유효하지 않은 착륙 지점과 충돌한 후 유효한 착륙 지점을 찾아야 하는지 여부를 결정합니다.
        /// 예를 들어, 기하학적 가장자리에서 캡슐의 하단 부분에 착륙하는 것은 걷기 가능한 표면일 수 있지만 걷기 불가능한 표면 법선으로 보고되었을 수 있습니다.
        /// </summary>

        private bool ShouldCheckForValidLandingSpot(ref CollisionResult inCollision)
        {
            // 캡슐의 하단 부분에서 표면 가장자리를 맞았는지 확인합니다.
            // 이 경우 법선이 표면 법선과 같지 않으며, 아래쪽 스윕이 가장자리 위의 걷기 가능한 표면을 찾을 수 있습니다.

            if (inCollision.hitLocation == HitLocation.Below && inCollision.normal != inCollision.surfaceNormal)
            {
                if (IsWithinEdgeTolerance(updatedPosition, inCollision.point, _radius))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 공급된 CollisionResult가 착륙 시 유효한 착륙 지점인지 여부를 확인합니다.
        /// </summary>

        private bool IsValidLandingSpot(Vector3 characterPosition, ref CollisionResult inCollision)
        {
            // 걷기 불가능한 지면 법선을 거부합니다.

            if (!inCollision.isWalkable)
                return false;

            // 캡슐의 하단 반구 위에 있는 히트를 거부합니다(수직 표면을 따라 미끄러질 때 발생할 수 있음).

            if (inCollision.hitLocation != HitLocation.Below)
                return false;

            // 캡슐의 반지름 가장자리에 거의 걸쳐 있는 히트를 거부합니다.

            if (!IsWithinEdgeTolerance(characterPosition, inCollision.point, _radius))
            {
                inCollision.isWalkable = false;

                return false;
            }

            FindGround(characterPosition, out FindGroundResult groundResult);
            {
                inCollision.isWalkable = groundResult.isWalkableGround;

                if (inCollision.isWalkable)
                {
                    _foundGround = groundResult;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 지정된 레이어의 콜라이더에 대해 길이 distance로 방향 direction에서 시작하여 origin에서 광선을 발사합니다.
        /// </summary>

        public bool Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask, out RaycastHit hitResult,
            float thickness = 0.0f)
        {
            hitResult = default;

            int rawHitCount = thickness == 0.0f
                ? Physics.RaycastNonAlloc(origin, direction, _hits, distance, layerMask, triggerInteraction)
                : Physics.SphereCastNonAlloc(origin - direction * thickness, thickness, direction, _hits, distance, layerMask, triggerInteraction);

            if (rawHitCount == 0)
                return false;

            float closestDistance = Mathf.Infinity;

            int hitIndex = -1;
            for (int i = 0; i < rawHitCount; i++)
            {
                ref RaycastHit hit = ref _hits[i];
                if (hit.distance <= 0.0f || ShouldFilter(hit.collider))
                    continue;

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    hitIndex = i;
                }
            }

            if (hitIndex != -1)
            {
                hitResult = _hits[hitIndex];
                return true;
            }

            return false;
        }

        /// <summary>
        /// 씬의 모든 콜라이더에 대해 캡슐을 발사하고 적중한 세부 정보를 반환합니다.
        /// 캡슐 스윕이 콜라이더와 교차하면 True를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// </summary>

        private bool CapsuleCast(Vector3 characterPosition, float castRadius, Vector3 castDirection, float castDistance,
            int layerMask, out RaycastHit hitResult, out bool startPenetrating)
        {
            hitResult = default;
            startPenetrating = false;

            Vector3 top = characterPosition + _transformedCapsuleTopCenter;
            Vector3 bottom = characterPosition + _transformedCapsuleBottomCenter;

            int rawHitCount = Physics.CapsuleCastNonAlloc(bottom, top, castRadius, castDirection, _hits, castDistance,
                layerMask, triggerInteraction);

            if (rawHitCount == 0)
                return false;

            float closestDistance = Mathf.Infinity;

            int hitIndex = -1;
            for (int i = 0; i < rawHitCount; i++)
            {
                ref RaycastHit hit = ref _hits[i];
                if (ShouldFilter(hit.collider))
                    continue;

                if (hit.distance <= 0.0f)
                    startPenetrating = true;
                else if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    hitIndex = i;
                }
            }

            if (hitIndex != -1)
            {
                hitResult = _hits[hitIndex];
                return true;
            }

            return false;
        }

        /// <summary>
        /// 주어진 배열을 거리별로 정렬(오름차순)합니다(삽입 정렬).
        /// </summary>        

        private static void SortArray(RaycastHit[] array, int length)
        {
            for (int i = 1; i < length; i++)
            {
                RaycastHit key = array[i];
                int flag = 0;

                for (int j = i - 1; j >= 0 && flag != 1;)
                {
                    if (key.distance < array[j].distance)
                    {
                        array[j + 1] = array[j];
                        j--;
                        array[j + 1] = key;
                    }
                    else flag = 1;
                }
            }
        }

        /// <summary>
        /// 씬의 모든 콜라이더에 대해 캡슐을 발사하고 적중한 세부 정보를 반환합니다.
        /// 캡슐 스윕이 콜라이더와 교차하면 True를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// 이전 버전과 달리 MTD를 사용하여 침투를 해결하는 유효한 히트를 반환합니다.
        /// </summary>

        private bool CapsuleCastEx(Vector3 characterPosition, float castRadius, Vector3 castDirection, float castDistance, int layerMask,
            out RaycastHit hitResult, out bool startPenetrating, out Vector3 recoverDirection, out float recoverDistance, bool ignoreNonBlockingOverlaps = false)
        {
            hitResult = default;

            startPenetrating = default;
            recoverDirection = default;
            recoverDistance = default;

            Vector3 top = characterPosition + _transformedCapsuleTopCenter;
            Vector3 bottom = characterPosition + _transformedCapsuleBottomCenter;

            int rawHitCount =
                Physics.CapsuleCastNonAlloc(bottom, top, castRadius, castDirection, _hits, castDistance, layerMask, triggerInteraction);

            if (rawHitCount == 0)
                return false;

            for (int i = 0; i < rawHitCount; i++)
            {
                ref RaycastHit hit = ref _hits[i];
                if (ShouldFilter(hit.collider))
                    continue;

                bool isOverlapping = hit.distance <= 0.0f;
                if (isOverlapping)
                {
                    if (ComputeMTD(characterPosition, updatedRotation, hit.collider, hit.collider.transform, out Vector3 mtdDirection, out float mtdDistance))
                    {
                        mtdDirection = ConstrainDirectionToPlane(mtdDirection);

                        HitLocation hitLocation = ComputeHitLocation(mtdDirection);

                        Vector3 point;
                        if (hitLocation == HitLocation.Above)
                            point = characterPosition + _transformedCapsuleTopCenter - mtdDirection * _radius;
                        else if (hitLocation == HitLocation.Below)
                            point = characterPosition + _transformedCapsuleBottomCenter - mtdDirection * _radius;
                        else
                            point = characterPosition + _transformedCapsuleCenter - mtdDirection * _radius;

                        Vector3 impactNormal = ComputeBlockingNormal(mtdDirection, IsWalkable(hit.collider, mtdDirection));

                        hit.point = point;
                        hit.normal = impactNormal;
                        hit.distance = -mtdDistance;
                    }
                }
            }

            //@Deprecated, this caused memory allocations due the use of IComparer
            //Array.Sort(_hits, 0, rawHitCount, _hitComparer);

            if (rawHitCount > 2)
            {
                SortArray(_hits, rawHitCount);
            }

            float mostOpposingDot = Mathf.Infinity;

            int hitIndex = -1;
            for (int i = 0; i < rawHitCount; i++)
            {
                ref RaycastHit hit = ref _hits[i];
                if (ShouldFilter(hit.collider))
                    continue;

                bool isOverlapping = hit.distance <= 0.0f && !hit.point.isZero();
                if (isOverlapping)
                {
                    // 중첩

                    float movementDotNormal = Vector3.Dot(castDirection, hit.normal);

                    if (ignoreNonBlockingOverlaps)
                    {
                        // 침투가 시작된 경우, 침투에서 벗어나고 있는 경우 무시할 수 있습니다.
                        // 이는 벽에 갇히는 것을 방지하는 데 도움이 됩니다.

                        bool isMovingOut = movementDotNormal > 0.0f;
                        if (isMovingOut)
                            continue;
                    }

                    if (movementDotNormal < mostOpposingDot)
                    {
                        mostOpposingDot = movementDotNormal;
                        hitIndex = i;
                    }
                }
                else if (hitIndex == -1)
                {
                    // 히트
                    // 유효한 중첩 히트가 없는 경우(즉, hitIndex == -1) 첫 번째 비중첩 차단 히트를 사용해야 합니다.

                    hitIndex = i;
                    break;
                }
            }

            if (hitIndex >= 0)
            {
                hitResult = _hits[hitIndex];

                if (hitResult.distance <= 0.0f)
                {
                    startPenetrating = true;
                    recoverDirection = hitResult.normal;
                    recoverDistance = Mathf.Abs(hitResult.distance);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 캐릭터가 씬을 통해 이동할 경우 충돌할지 여부를 테스트합니다.
        /// 리지드바디 스윕이 콜라이더와 교차하면 True를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// </summary>

        /// <summary>
        /// 캐릭터가 씬을 통해 이동할 경우 충돌할지 여부를 테스트합니다.
        /// 리지드바디 스윕이 콜라이더와 교차하면 True를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// </summary>

        private bool SweepTest(Vector3 sweepOrigin, float sweepRadius, Vector3 sweepDirection, float sweepDistance,
            int sweepLayerMask, out RaycastHit hitResult, out bool startPenetrating)
        {
            // Cast further than the distance we need, to try to take into account small edge cases (e.g. Casts fail 
            // when moving almost parallel to an obstacle for small distances).

            hitResult = default;

            bool innerCapsuleHit =
                CapsuleCast(sweepOrigin, sweepRadius, sweepDirection, sweepDistance + sweepRadius, sweepLayerMask,
                    out RaycastHit innerCapsuleHitResult, out startPenetrating) && innerCapsuleHitResult.distance <= sweepDistance;

            float outerCapsuleRadius = sweepRadius + kContactOffset;

            bool outerCapsuleHit =
                CapsuleCast(sweepOrigin, outerCapsuleRadius, sweepDirection, sweepDistance + outerCapsuleRadius,
                    sweepLayerMask, out RaycastHit outerCapsuleHitResult, out _) && outerCapsuleHitResult.distance <= sweepDistance;

            bool foundBlockingHit = innerCapsuleHit || outerCapsuleHit;
            if (!foundBlockingHit)
                return false;

            if (!outerCapsuleHit)
            {
                hitResult = innerCapsuleHitResult;
                hitResult.distance = Mathf.Max(0.0f, hitResult.distance - kContactOffset);
            }
            else if (innerCapsuleHit && innerCapsuleHitResult.distance < outerCapsuleHitResult.distance)
            {
                hitResult = innerCapsuleHitResult;
                hitResult.distance = Mathf.Max(0.0f, hitResult.distance - kContactOffset);
            }
            else
            {
                hitResult = outerCapsuleHitResult;
                hitResult.distance = Mathf.Max(0.0f, hitResult.distance - kSmallContactOffset);
            }

            return true;
        }

        /// <summary>
        /// 지정된 콜라이더에 대해 캐릭터의 볼륨을 스윕 테스트하여, 충돌이 감지되면 근처 히트 지점에서 멈추거나, 충돌이 없으면 전체 변위를 적용합니다.
        /// 리지드바디 스윕이 콜라이더와 교차하면 true를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// 이전 버전과 달리, 원하는 경우 차단 중첩에 대한 유효한 히트를 올바르게 반환하고 MTD를 사용하여 침투를 해결합니다.
        /// </summary>

        private bool SweepTestEx(Vector3 sweepOrigin, float sweepRadius, Vector3 sweepDirection, float sweepDistance, int sweepLayerMask,
            out RaycastHit hitResult, out bool startPenetrating, out Vector3 recoverDirection, out float recoverDistance, bool ignoreBlockingOverlaps = false)
        {
            // 필요한 거리보다 더 멀리 캐스트하여 작은 엣지 케이스를 고려합니다(예: 작은 거리에서 장애물과 거의 평행하게 이동할 때 캐스트가 실패하는 경우).

            hitResult = default;

            bool innerCapsuleHit =
                CapsuleCastEx(sweepOrigin, sweepRadius, sweepDirection, sweepDistance + sweepRadius, sweepLayerMask,
                out RaycastHit innerCapsuleHitResult, out startPenetrating, out recoverDirection, out recoverDistance, ignoreBlockingOverlaps) && innerCapsuleHitResult.distance <= sweepDistance;

            if (innerCapsuleHit && startPenetrating)
            {
                hitResult = innerCapsuleHitResult;
                hitResult.distance = Mathf.Max(0.0f, hitResult.distance - kSmallContactOffset);

                return true;
            }

            float outerCapsuleRadius = sweepRadius + kContactOffset;

            bool outerCapsuleHit =
                CapsuleCast(sweepOrigin, outerCapsuleRadius, sweepDirection, sweepDistance + outerCapsuleRadius, sweepLayerMask,
                out RaycastHit outerCapsuleHitResult, out _) && outerCapsuleHitResult.distance <= sweepDistance;

            bool foundBlockingHit = innerCapsuleHit || outerCapsuleHit;
            if (!foundBlockingHit)
                return false;

            if (!outerCapsuleHit)
            {
                hitResult = innerCapsuleHitResult;
                hitResult.distance = Mathf.Max(0.0f, hitResult.distance - kContactOffset);
            }
            else if (innerCapsuleHit && innerCapsuleHitResult.distance < outerCapsuleHitResult.distance)
            {
                hitResult = innerCapsuleHitResult;
                hitResult.distance = Mathf.Max(0.0f, hitResult.distance - kContactOffset);
            }
            else
            {
                hitResult = outerCapsuleHitResult;
                hitResult.distance = Mathf.Max(0.0f, hitResult.distance - kSmallContactOffset);
            }

            return true;
        }

        /// <summary>
        /// 침투를 해결합니다.
        /// </summary>

        private bool ResolvePenetration(Vector3 displacement, Vector3 proposedAdjustment)
        {
            Vector3 adjustment = ConstrainVectorToPlane(proposedAdjustment);
            if (adjustment.isZero())
                return false;

            // 오버랩 테스트와 스윕 테스트의 차이로 인해 또 다른 오버랩에 들어가지 않도록 오버랩 테스트를 조금 더 엄격하게 만듭니다.

            const float kOverlapInflation = 0.001f;

            if (!(OverlapTest(updatedPosition + adjustment, updatedRotation, _radius + kOverlapInflation, _height, _collisionLayers, _overlaps, triggerInteraction) > 0))
            {
                // 스윕 없이 이동 가능

                updatedPosition += adjustment;

                return true;
            }
            else
            {
                Vector3 lastPosition = updatedPosition;

                // 가능한 멀리 스윕을 시도하며, 비차단 중첩을 무시합니다. 그렇지 않으면 침투를 해결하기 위해 물체에서 벗어날 수 없습니다.

                bool hit = CapsuleCastEx(updatedPosition, _radius, adjustment.normalized, adjustment.magnitude, _collisionLayers,
                    out RaycastHit sweepHitResult, out bool startPenetrating, out Vector3 recoverDirection, out float recoverDistance, true);

                if (!hit)
                    updatedPosition += adjustment;
                else
                    updatedPosition += adjustment.normalized * Mathf.Max(sweepHitResult.distance - kSmallContactOffset, 0.0f);

                // 여전히 막혀 있나요?

                bool moved = updatedPosition != lastPosition;
                if (!moved && startPenetrating)
                {
                    // 두 MTD 결과를 결합하여 여러 표면에서 벗어나는 새로운 방향을 얻습니다.

                    Vector3 secondMTD = recoverDirection * (recoverDistance + kContactOffset + kPenetrationOffset);
                    Vector3 combinedMTD = adjustment + secondMTD;

                    if (secondMTD != adjustment && !combinedMTD.isZero())
                    {
                        lastPosition = updatedPosition;

                        hit = CapsuleCastEx(updatedPosition, _radius, combinedMTD.normalized, combinedMTD.magnitude,
                            _collisionLayers, out sweepHitResult, out _, out _, out _, true);

                        if (!hit)
                            updatedPosition += combinedMTD;
                        else
                            updatedPosition += combinedMTD.normalized * Mathf.Max(sweepHitResult.distance - kSmallContactOffset, 0.0f);

                        moved = updatedPosition != lastPosition;
                    }
                }

                // 여전히 막혀 있나요?

                if (!moved)
                {
                    // 제안된 조정과 시도된 이동 방향을 결합해 보십시오.
                    // 이는 여러 객체와의 침투를 해결할 수 있습니다.

                    Vector3 moveDelta = ConstrainVectorToPlane(displacement);
                    if (!moveDelta.isZero())
                    {
                        lastPosition = updatedPosition;

                        Vector3 newAdjustment = adjustment + moveDelta;
                        hit = CapsuleCastEx(updatedPosition, _radius, newAdjustment.normalized, newAdjustment.magnitude,
                            _collisionLayers, out sweepHitResult, out _, out _, out _, true);

                        if (!hit)
                            updatedPosition += newAdjustment;
                        else
                            updatedPosition += newAdjustment.normalized * Mathf.Max(sweepHitResult.distance - kSmallContactOffset, 0.0f);

                        moved = updatedPosition != lastPosition;

                        // 마지막으로, MTD 조정 없이 원래 이동을 시도하지만 MTD 법선을 따라 침투를 허용합니다.
                        // 더 나은 침투 해제 법선을 시도하려고 원래 이동이 차단된 경우, 시도 중에 다른 지오메트리에 부딪힐 수 있습니다.
                        // 이는 반드시 침투에서 완전히 벗어나지 않을 수도 있지만, 일부 경우에는 침투를 해결하는 데 진전을 보입니다.

                        if (!moved && Vector3.Dot(moveDelta, adjustment) > 0.0f)
                        {
                            lastPosition = updatedPosition;

                            hit = CapsuleCastEx(updatedPosition, _radius, moveDelta.normalized, moveDelta.magnitude,
                                _collisionLayers, out sweepHitResult, out _, out _, out _, true);

                            if (!hit)
                                updatedPosition += moveDelta;
                            else
                                updatedPosition += moveDelta.normalized * Mathf.Max(sweepHitResult.distance - kSmallContactOffset, 0.0f);

                            moved = updatedPosition != lastPosition;
                        }
                    }
                }

                return moved;
            }
        }

        /// <summary>
        /// 충돌이 감지되면 근처 히트 지점에서 멈추거나, 충돌이 없으면 전체 변위를 적용하여 캐릭터의 볼륨을 이동합니다.
        /// 리지드바디 스윕이 콜라이더와 교차하면 true를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// </summary>

        private bool MovementSweepTest(Vector3 characterPosition, Vector3 inVelocity, Vector3 displacement,
            out CollisionResult collisionResult)
        {
            collisionResult = default;

            Vector3 sweepOrigin = characterPosition;
            Vector3 sweepDirection = displacement.normalized;

            float sweepRadius = _radius;
            float sweepDistance = displacement.magnitude;

            int sweepLayerMask = _collisionLayers;

            bool hit = SweepTestEx(sweepOrigin, sweepRadius, sweepDirection, sweepDistance, sweepLayerMask,
                out RaycastHit hitResult, out bool startPenetrating, out Vector3 recoverDirection, out float recoverDistance);

            if (startPenetrating)
            {
                // 초기 침투 처리

                Vector3 requestedAdjustment = recoverDirection * (recoverDistance + kContactOffset + kPenetrationOffset);

                if (ResolvePenetration(displacement, requestedAdjustment))
                {
                    // 원래 이동 재시도

                    sweepOrigin = updatedPosition;
                    hit = SweepTestEx(sweepOrigin, sweepRadius, sweepDirection, sweepDistance, sweepLayerMask,
                        out hitResult, out startPenetrating, out _, out _);
                }
            }

            if (!hit)
                return false;

            HitLocation hitLocation = ComputeHitLocation(hitResult.normal);

            Vector3 displacementToHit = sweepDirection * hitResult.distance;
            Vector3 remainingDisplacement = displacement - displacementToHit;

            Vector3 hitPosition = sweepOrigin + displacementToHit;

            Vector3 surfaceNormal = hitResult.normal;

            bool isWalkable = false;
            bool hitGround = hitLocation == HitLocation.Below;

            if (hitGround)
            {
                surfaceNormal = FindGeomOpposingNormal(displacement, ref hitResult);

                isWalkable = IsWalkable(hitResult.collider, surfaceNormal);
            }

            collisionResult = new CollisionResult
            {
                startPenetrating = startPenetrating,

                hitLocation = hitLocation,
                isWalkable = isWalkable,

                position = hitPosition,

                velocity = inVelocity,
                otherVelocity = GetRigidbodyVelocity(hitResult.rigidbody, hitResult.point),

                point = hitResult.point,
                normal = hitResult.normal,

                surfaceNormal = surfaceNormal,

                displacementToHit = displacementToHit,
                remainingDisplacement = remainingDisplacement,

                collider = hitResult.collider,

                hitResult = hitResult
            };

            return true;
        }

        /// <summary>
        /// 캐릭터의 볼륨을 지정된 변위 벡터를 따라 스윕하여, 충돌이 감지되면 근처 히트 지점에서 멈춥니다.
        /// 리지드바디 스윕이 콜라이더와 교차하면 true를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// </summary>

        public bool MovementSweepTest(Vector3 characterPosition, Vector3 sweepDirection, float sweepDistance,
            out CollisionResult collisionResult)
        {
            return MovementSweepTest(characterPosition, velocity, sweepDirection * sweepDistance, out collisionResult);
        }

        /// <summary>
        /// 결과 슬라이드가 캐릭터를 더 빠르게 위로 밀어 올릴 수 있는 경우 낙하할 때 슬라이드 벡터를 제한합니다.
        /// </summary>

        private Vector3 HandleSlopeBoosting(Vector3 slideResult, Vector3 displacement, Vector3 inNormal)
        {
            Vector3 result = slideResult;

            float yResult = Vector3.Dot(result, _characterUp);
            if (yResult > 0.0f)
            {
                // 원래 의도한 것보다 더 높이 올라가지 않도록 합니다.

                float yLimit = Vector3.Dot(displacement, _characterUp);
                if (yResult - yLimit > kKindaSmallNumber)
                {
                    if (yLimit > 0.0f)
                    {
                        // 방향을 변경하고 다시 충돌 지점으로 향하는 것을 방지하기 위해 전체 벡터를 재조정합니다(Z 구성 요소만이 아니라).

                        float upPercent = yLimit / yResult;
                        result *= upPercent;
                    }
                    else
                    {
                        // 아래로 향하고 있었지만 위로 반사될 예정이었습니다. 단순히 반사를 수평으로 만듭니다.

                        result = Vector3.zero;
                    }

                    // 원래 결과의 남은 부분을 수평으로 만들고 충돌 법선에 평행하게 만듭니다.

                    Vector3 lateralRemainder = (slideResult - result).projectedOnPlane(_characterUp);
                    Vector3 lateralNormal = inNormal.projectedOnPlane(_characterUp).normalized;
                    Vector3 adjust = lateralRemainder.projectedOnPlane(lateralNormal);

                    result += adjust;
                }
            }

            return result;
        }

        /// <summary>
        /// 표면을 따라 슬라이드 벡터를 계산합니다.
        /// </summary>

        private Vector3 ComputeSlideVector(Vector3 displacement, Vector3 inNormal, bool isWalkable)
        {
            if (isGrounded)
            {
                if (isWalkable)
                    displacement = displacement.tangentTo(inNormal, _characterUp);
                else
                {
                    Vector3 right = inNormal.perpendicularTo(groundNormal);
                    Vector3 up = right.perpendicularTo(inNormal);

                    displacement = displacement.projectedOnPlane(inNormal);
                    displacement = displacement.tangentTo(up, _characterUp);
                }
            }
            else
            {
                if (isWalkable)
                {
                    if (_isConstrainedToGround)
                        displacement = displacement.projectedOnPlane(_characterUp);

                    displacement = displacement.projectedOnPlane(inNormal);
                }
                else
                {
                    Vector3 slideResult = displacement.projectedOnPlane(inNormal);

                    if (_isConstrainedToGround)
                        slideResult = HandleSlopeBoosting(slideResult, displacement, inNormal);

                    displacement = slideResult;
                }
            }

            return ConstrainVectorToPlane(displacement);
        }

        /// <summary>
        /// Move 호출 중 캐릭터의 경계 볼륨의 충돌을 해결합니다.
        /// </summary>

        private int SlideAlongSurface(int iteration, Vector3 inputDisplacement, ref Vector3 inVelocity,
            ref Vector3 displacement, ref CollisionResult inHit, ref Vector3 prevNormal)
        {
            if (useFlatTop && inHit.hitLocation == HitLocation.Above)
            {
                Vector3 surfaceNormal = FindBoxOpposingNormal(displacement, inHit.normal, inHit.transform);

                if (inHit.normal != surfaceNormal)
                {
                    inHit.normal = surfaceNormal;
                    inHit.surfaceNormal = surfaceNormal;
                }
            }

            inHit.normal = ComputeBlockingNormal(inHit.normal, inHit.isWalkable);

            if (inHit.isWalkable && isConstrainedToGround)
            {
                inVelocity = ComputeSlideVector(inVelocity, inHit.normal, true);
                displacement = ComputeSlideVector(displacement, inHit.normal, true);
            }
            else
            {
                if (iteration == 0)
                {
                    inVelocity = ComputeSlideVector(inVelocity, inHit.normal, inHit.isWalkable);
                    displacement = ComputeSlideVector(displacement, inHit.normal, inHit.isWalkable);

                    iteration++;
                }
                else if (iteration == 1)
                {
                    Vector3 crease = prevNormal.perpendicularTo(inHit.normal);

                    Vector3 oVel = inputDisplacement.projectedOnPlane(crease);

                    Vector3 nVel = ComputeSlideVector(displacement, inHit.normal, inHit.isWalkable);
                    nVel = nVel.projectedOnPlane(crease);

                    if (oVel.dot(nVel) <= 0.0f || prevNormal.dot(inHit.normal) < 0.0f)
                    {
                        inVelocity = ConstrainVectorToPlane(inVelocity.projectedOn(crease));
                        displacement = ConstrainVectorToPlane(displacement.projectedOn(crease));

                        ++iteration;
                    }
                    else
                    {
                        inVelocity = ComputeSlideVector(inVelocity, inHit.normal, inHit.isWalkable);
                        displacement = ComputeSlideVector(displacement, inHit.normal, inHit.isWalkable);
                    }
                }
                else
                {
                    inVelocity = Vector3.zero;
                    displacement = Vector3.zero;
                }

                prevNormal = inHit.normal;
            }

            return iteration;
        }

        /// <summary>
        /// 충돌 제약된 이동을 수행합니다.
        /// 이는 이동 중 만난 장애물에 부드럽게 슬라이딩하는 프로세스를 의미합니다.
        /// _probingPosition을 업데이트합니다.
        /// </summary>

        private void PerformMovement(float deltaTime)
        {
            // 초기 중첩 해결

            DepenetrationBehaviour depenetrationFlags = !enablePhysicsInteraction
                ? DepenetrationBehaviour.IgnoreDynamic
                : DepenetrationBehaviour.IgnoreNone;

            ResolveOverlaps(depenetrationFlags);

            //
            // 지면에 닿아 있는 경우, 수직 성분을 버립니다.

            if (isGrounded)
                _velocity = _velocity.projectedOnPlane(_characterUp);

            // 변위 계산

            Vector3 displacement = _velocity * deltaTime;

            //
            // 지면에 닿아 있는 경우, 현재 지면 법선을 따라 변위를 재정렬합니다.

            if (isGrounded)
            {
                displacement = displacement.tangentTo(groundNormal, _characterUp);
                displacement = ConstrainVectorToPlane(displacement);
            }

            //
            // 이동 전 변위를 캐시합니다.

            Vector3 inputDisplacement = displacement;

            //
            // 현재 차단 중첩으로 이동하는 것을 방지하고, 이를 충돌로 처리하여 슬라이드합니다.

            int iteration = 0;
            Vector3 prevNormal = default;

            for (int i = 0; i < _collisionCount; i++)
            {
                ref CollisionResult collisionResult = ref _collisionResults[i];

                bool opposesMovement = displacement.dot(collisionResult.normal) < 0.0f;
                if (!opposesMovement)
                    continue;

                // 낙하 중인 경우, 히트가 유효한 착륙 지점인지 확인합니다.

                if (isConstrainedToGround && !isOnWalkableGround)
                {
                    if (IsValidLandingSpot(updatedPosition, ref collisionResult))
                    {
                        _hasLanded = true;
                        landedVelocity = collisionResult.velocity;
                    }
                    else
                    {
                        // 히트 결과를 기반으로 일반적으로 유효하지 않은 착륙 지점을 유효한 착륙 지점으로 변환할 수 있는지 확인합니다.

                        if (collisionResult.hitLocation == HitLocation.Below)
                        {
                            FindGround(updatedPosition, out FindGroundResult groundResult);

                            collisionResult.isWalkable = groundResult.isWalkableGround;
                            if (collisionResult.isWalkable)
                            {
                                _foundGround = groundResult;

                                _hasLanded = true;
                                landedVelocity = collisionResult.velocity;
                            }
                        }
                    }

                    // 유효한 착륙 지점을 찾지 못했지만 지면을 맞았을 경우, 스윕 히트 결과로 _foundGround를 업데이트합니다.

                    if (!_hasLanded && collisionResult.hitLocation == HitLocation.Below)
                    {
                        _foundGround.SetFromSweepResult(true, false, updatedPosition, collisionResult.point,
                            collisionResult.normal, collisionResult.surfaceNormal, collisionResult.collider,
                            collisionResult.hitResult.distance);
                    }
                }

                //
                // 차단 중첩을 따라 슬라이드합니다.

                iteration = SlideAlongSurface(iteration, inputDisplacement, ref _velocity, ref displacement,
                    ref collisionResult, ref prevNormal);
            }

            //
            // 충돌 제약된 이동 수행(aka: 충돌 및 슬라이드)

            int maxSlideCount = _advanced.maxMovementIterations;
            while (detectCollisions && maxSlideCount-- > 0 && displacement.sqrMagnitude > _advanced.minMoveDistanceSqr)
            {
                bool collided = MovementSweepTest(updatedPosition, _velocity, displacement,
                    out CollisionResult collisionResult);

                if (!collided)
                    break;

                // 히트 지점까지의 변위를 적용하고 남은 변위로 업데이트합니다.

                updatedPosition += collisionResult.displacementToHit;

                displacement = collisionResult.remainingDisplacement;

                // '장벽'에 부딪혔을 경우, 올라가 보려고 합니다.

                if (isGrounded && !collisionResult.isWalkable)
                {
                    if (CanStepUp(collisionResult.collider) &&
                        StepUp(ref collisionResult, out CollisionResult stepResult))
                    {
                        updatedPosition = stepResult.position;

                        displacement = Vector3.zero;
                        break;
                    }
                }

                // 낙하 중인 경우, 히트가 유효한 착륙 지점인지 확인합니다.

                if (isConstrainedToGround && !isOnWalkableGround)
                {
                    if (IsValidLandingSpot(updatedPosition, ref collisionResult))
                    {
                        _hasLanded = true;
                        landedVelocity = collisionResult.velocity;
                    }
                    else
                    {
                        // 히트 결과를 기반으로 일반적으로 유효하지 않은 착륙 지점을 유효한 착륙 지점으로 변환할 수 있는지 확인합니다.

                        if (ShouldCheckForValidLandingSpot(ref collisionResult))
                        {
                            FindGround(updatedPosition, out FindGroundResult groundResult);

                            collisionResult.isWalkable = groundResult.isWalkableGround;
                            if (collisionResult.isWalkable)
                            {
                                _foundGround = groundResult;

                                _hasLanded = true;
                                landedVelocity = collisionResult.velocity;
                            }
                        }
                    }

                    // 유효한 착륙 지점을 찾지 못했지만 지면을 맞았을 경우, 스윕 히트 결과로 _foundGround를 업데이트합니다.

                    if (!_hasLanded && collisionResult.hitLocation == HitLocation.Below)
                    {
                        float sweepDistance = collisionResult.hitResult.distance;
                        Vector3 surfaceNormal = collisionResult.surfaceNormal;

                        _foundGround.SetFromSweepResult(true, false, updatedPosition, sweepDistance,
                            ref collisionResult.hitResult, surfaceNormal);
                    }
                }

                //
                // 충돌 해결(히트 표면을 따라 슬라이드)

                iteration = SlideAlongSurface(iteration, inputDisplacement, ref _velocity, ref displacement,
                    ref collisionResult, ref prevNormal);

                //
                // 충돌 결과를 캐시합니다.

                AddCollisionResult(ref collisionResult);
            }

            //
            // 남은 변위를 적용합니다.

            if (displacement.sqrMagnitude > _advanced.minMoveDistanceSqr)
                updatedPosition += displacement;

            //
            // 지면에 닿아 있거나 착륙한 경우, 수직 이동을 버리지만 그 크기를 유지합니다.

            if (isGrounded || _hasLanded)
            {
                _velocity = _velocity.projectedOnPlane(_characterUp).normalized * _velocity.magnitude;
                _velocity = ConstrainVectorToPlane(_velocity);
            }
        }

        /// <summary>
        /// CollisionBehavior 플래그에 따라 다른 콜라이더 위에 앉을 수 있는지 여부를 결정합니다.
        /// </summary>

        private bool CanPerchOn(Collider otherCollider)
        {
            // 입력 콜라이더 유효성 검사

            if (otherCollider == null)
                return false;

            // 충돌 동작 콜백이 할당된 경우, 이를 사용합니다.

            if (collisionBehaviourCallback != null)
            {
                CollisionBehaviour collisionBehaviour = collisionBehaviourCallback.Invoke(otherCollider);

                if (CanPerchOn(collisionBehaviour))
                    return true;

                if (CanNotPerchOn(collisionBehaviour))
                    return false;
            }

            // 기본 경우, perchOffset으로 관리됩니다.

            return true;
        }

        /// <summary>
        /// 표면 가장자리에서 캐릭터가 앉을 수 없도록 하는 캡슐 가장자리에서의 거리를 반환합니다.
        /// </summary>

        private float GetPerchRadiusThreshold()
        {
            // 음수 값을 허용하지 않습니다.

            return Mathf.Max(0.0f, _radius - perchOffset);
        }

        /// <summary>
        /// 걷기 가능한 표면이라면 표면 가장자리에서 떨어지지 않고 서 있을 수 있는 반경을 반환합니다.
        /// </summary>

        private float GetValidPerchRadius(Collider otherCollider)
        {
            if (!CanPerchOn(otherCollider))
                return 0.0011f;

            return Mathf.Clamp(_perchOffset, 0.0011f, _radius);
        }

        /// <summary>
        /// 스윕 테스트 결과가 유효한 앉기 위치일 수 있는지 여부를 확인하고, 그런 경우 ComputePerchResult를 사용하여 위치를 확인해야 합니다.
        /// </summary>

        private bool ShouldComputePerchResult(Vector3 characterPosition, ref RaycastHit inHit)
        {
            // 가장자리 반경이 매우 작은 경우, 앉으려고 하지 마십시오.

            if (GetPerchRadiusThreshold() <= kSweepEdgeRejectDistance)
            {
                return false;
            }

            float distFromCenterSq = (inHit.point - characterPosition).projectedOnPlane(_characterUp).sqrMagnitude;
            float standOnEdgeRadius = GetValidPerchRadius(inHit.collider);

            if (distFromCenterSq <= standOnEdgeRadius.square())
            {
                // 이미 앉기 반경 내에 있습니다.

                return false;
            }

            return true;
        }

        /// <summary>
        /// 지정된 레이어 마스크에 대해 씬의 모든 콜라이더에 캡슐을 발사하고, 적중한 세부 정보를 반환합니다.
        /// </summary>

        private bool CapsuleCast(Vector3 point1, Vector3 point2, float castRadius, Vector3 castDirection,
            float castDistance, int castLayerMask, out RaycastHit hitResult, out bool startPenetrating)
        {
            hitResult = default;
            startPenetrating = false;

            int rawHitCount = Physics.CapsuleCastNonAlloc(point1, point2, castRadius, castDirection, _hits,
                castDistance, castLayerMask, triggerInteraction);

            if (rawHitCount == 0)
                return false;

            float closestDistance = Mathf.Infinity;

            int hitIndex = -1;
            for (int i = 0; i < rawHitCount; i++)
            {
                ref RaycastHit hit = ref _hits[i];
                if (ShouldFilter(hit.collider))
                    continue;

                if (hit.distance <= 0.0f)
                    startPenetrating = true;
                else if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    hitIndex = i;
                }
            }

            if (hitIndex != -1)
            {
                hitResult = _hits[hitIndex];
                return true;
            }

            return false;
        }

        /// <summary>
        /// 광선을 따라 상자를 발사하고, 적중한 세부 정보를 반환합니다.
        /// </summary>

        private bool BoxCast(Vector3 center, Vector3 halfExtents, Quaternion orientation, Vector3 castDirection,
            float castDistance, int castLayerMask, out RaycastHit hitResult, out bool startPenetrating)
        {
            hitResult = default;
            startPenetrating = default;

            int rawHitCount = Physics.BoxCastNonAlloc(center, halfExtents, castDirection, _hits, orientation,
                castDistance, castLayerMask, triggerInteraction);

            if (rawHitCount == 0)
                return false;

            float closestDistance = Mathf.Infinity;

            int hitIndex = -1;
            for (int i = 0; i < rawHitCount; i++)
            {
                ref RaycastHit hit = ref _hits[i];
                if (ShouldFilter(hit.collider))
                    continue;

                if (hit.distance <= 0.0f)
                    startPenetrating = true;
                else if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    hitIndex = i;
                }
            }

            if (hitIndex != -1)
            {
                hitResult = _hits[hitIndex];
                return true;
            }

            return false;
        }

        /// <summary>
        /// 캐릭터의 위쪽 축을 따라 세계를 향해 아래로 스윕하여 첫 번째 차단 히트를 반환합니다.
        /// </summary>

        private bool GroundSweepTest(Vector3 characterPosition, float capsuleRadius, float capsuleHalfHeight,
            float sweepDistance, out RaycastHit hitResult, out bool startPenetrating)
        {
            bool foundBlockingHit;

            if (!useFlatBaseForGroundChecks)
            {
                Vector3 characterCenter = characterPosition + _transformedCapsuleCenter;

                Vector3 point1 = characterCenter - _characterUp * (capsuleHalfHeight - capsuleRadius);
                Vector3 point2 = characterCenter + _characterUp * (capsuleHalfHeight - capsuleRadius);

                Vector3 sweepDirection = -1.0f * _characterUp;

                foundBlockingHit = CapsuleCast(point1, point2, capsuleRadius, sweepDirection, sweepDistance,
                    _collisionLayers, out hitResult, out startPenetrating);
            }
            else
            {
                // 박스를 주요 축을 따라 회전시켜 가장자리가 주 축을 따라 배열되도록 하여 먼저 테스트합니다(즉, 45도 회전).

                Vector3 center = characterPosition + _transformedCapsuleCenter;
                Vector3 halfExtents = new Vector3(capsuleRadius * 0.707f, capsuleHalfHeight, capsuleRadius * 0.707f);

                Quaternion sweepOrientation = rotation * Quaternion.Euler(0f, -rotation.eulerAngles.y, 0f);
                Vector3 sweepDirection = -1.0f * _characterUp;

                LayerMask sweepLayerMask = _collisionLayers;

                foundBlockingHit = BoxCast(center, halfExtents, sweepOrientation * Quaternion.Euler(0.0f, 45.0f, 0.0f),
                    sweepDirection, sweepDistance, sweepLayerMask, out hitResult, out startPenetrating);

                if (!foundBlockingHit && !startPenetrating)
                {
                    // 동일한 박스로 다시 테스트하지만, 회전하지 않습니다.

                    foundBlockingHit = BoxCast(center, halfExtents, sweepOrientation, sweepDirection, sweepDistance,
                        sweepLayerMask, out hitResult, out startPenetrating);
                }
            }

            return foundBlockingHit;
        }

        /// <summary>
        /// 캡슐의 아래쪽 반구에 의해 처음으로 영향을 받은 지점까지의 스윕 거리를 계산하여 collisionResult에 결과를 저장합니다.
        /// 이 거리는 스윕의 거리 또는 캡슐의 바닥에서부터의 거리입니다(레이캐스트의 경우).
        /// </summary>

        public void ComputeGroundDistance(Vector3 characterPosition, float sweepRadius, float sweepDistance,
            float castDistance, out FindGroundResult outGroundResult)
        {
            outGroundResult = default;

            // 스윕 거리는 레이캐스트 거리보다 크거나 같아야 합니다.
            // 그렇지 않으면 HitResult를 스윕 결과로 해석할 수 없습니다.

            if (sweepDistance < castDistance)
                return;

            float characterRadius = _radius;
            float characterHeight = _height;
            float characterHalfHeight = characterHeight * 0.5f;

            bool foundGround = default;
            bool startPenetrating = default;

            // 스윕 테스트

            if (sweepDistance > 0.0f && sweepRadius > 0.0f)
            {
                // 표면에 시작하는 경우 이상한 결과를 방지하기 위해 더 짧은 높이를 사용합니다.
                // 이것은 또한 침투에서 조정할 수 있게 합니다.

                const float kShrinkScale = 0.9f;
                float shrinkHeight = (characterHalfHeight - characterRadius) * (1.0f - kShrinkScale);

                float capsuleRadius = sweepRadius;
                float capsuleHalfHeight = characterHalfHeight - shrinkHeight;

                float actualSweepDistance = sweepDistance + shrinkHeight;

                foundGround = GroundSweepTest(characterPosition, capsuleRadius, capsuleHalfHeight, actualSweepDistance,
                    out RaycastHit hitResult, out startPenetrating);

                if (foundGround || startPenetrating)
                {
                    // 인접한 히트를 거부합니다. 우리는 캡슐의 하단 부분에 대한 히트만 신경 씁니다.
                    // 충돌 지점에 대한 2D 거리를 확인하고, 반경 내에서 허용 오차가 있는 경우 거부합니다.

                    if (startPenetrating || !IsWithinEdgeTolerance(characterPosition, hitResult.point, capsuleRadius))
                    {
                        // 약간 작은 반경과 더 짧은 높이의 캡슐을 사용하여 인접한 물체를 피합니다.
                        // 캡슐이 거의 0이 아니어야 추적이 시작 지점에서 선 추적으로 되돌아가지 않고 올바른 길이를 가집니다.

                        const float kShrinkScaleOverlap = 0.1f;
                        shrinkHeight = (characterHalfHeight - characterRadius) * (1.0f - kShrinkScaleOverlap);

                        capsuleRadius = Mathf.Max(0.0011f, capsuleRadius - kSweepEdgeRejectDistance - kKindaSmallNumber);
                        capsuleHalfHeight = Mathf.Max(capsuleRadius, characterHalfHeight - shrinkHeight);

                        actualSweepDistance = sweepDistance + shrinkHeight;

                        foundGround = GroundSweepTest(characterPosition, capsuleRadius, capsuleHalfHeight,
                            actualSweepDistance, out hitResult, out startPenetrating);
                    }

                    if (foundGround && !startPenetrating)
                    {
                        // 캡슐을 추적하기 위해 높이를 줄였기 때문에 히트 거리를 줄입니다.
                        // 우리는 여기서 음수 거리를 허용합니다. 이것은 우리가 침투에서 벗어나는 것을 가능하게 합니다.

                        float maxPenetrationAdjust = Mathf.Max(kMaxGroundDistance, characterRadius);
                        float sweepResult = Mathf.Max(-maxPenetrationAdjust, hitResult.distance - shrinkHeight);

                        Vector3 sweepDirection = -1.0f * _characterUp;
                        Vector3 hitPosition = characterPosition + sweepDirection * sweepResult;

                        Vector3 surfaceNormal = hitResult.normal;

                        bool isWalkable = false;
                        bool hitGround = sweepResult <= sweepDistance &&
                                         ComputeHitLocation(hitResult.normal) == HitLocation.Below;

                        if (hitGround)
                        {
                            if (useFlatBaseForGroundChecks)
                                isWalkable = IsWalkable(hitResult.collider, surfaceNormal);
                            else
                            {
                                surfaceNormal = FindGeomOpposingNormal(sweepDirection * sweepDistance, ref hitResult);

                                isWalkable = IsWalkable(hitResult.collider, surfaceNormal);
                            }
                        }

                        outGroundResult.SetFromSweepResult(hitGround, isWalkable, hitPosition, sweepResult,
                            ref hitResult, surfaceNormal);

                        if (outGroundResult.isWalkableGround)
                            return;
                    }
                }
            }

            // 스윕이 모든 것을 놓친 경우 레이캐스트를 실행하고 싶지 않습니다.
            // 그러나 스윕이 침투로 인해 막혀 있었던 경우에는 레이캐스트를 시도하고 싶습니다.

            if (!foundGround && !startPenetrating)
                return;

            // 레이캐스트

            if (castDistance > 0.0f)
            {
                Vector3 rayOrigin = characterPosition + _transformedCapsuleCenter;
                Vector3 rayDirection = -1.0f * _characterUp;

                float shrinkHeight = characterHalfHeight;
                float rayLength = castDistance + shrinkHeight;

                foundGround = Raycast(rayOrigin, rayDirection, rayLength, _collisionLayers, out RaycastHit hitResult);

                if (foundGround && hitResult.distance > 0.0f)
                {
                    // 레이캐스트를 시작한 높이 때문에 히트 거리를 줄입니다.
                    // 우리는 여기서 음수 거리를 허용합니다. 이것은 우리가 침투에서 벗어나는 것을 가능하게 합니다.

                    float MaxPenetrationAdjust = Mathf.Max(kMaxGroundDistance, characterRadius);
                    float castResult = Mathf.Max(-MaxPenetrationAdjust, hitResult.distance - shrinkHeight);

                    if (castResult <= castDistance && IsWalkable(hitResult.collider, hitResult.normal))
                    {
                        outGroundResult.SetFromRaycastResult(true, true, outGroundResult.position,
                            outGroundResult.groundDistance, castResult, ref hitResult);

                        return;
                    }
                }
            }

            // 수용 가능한 히트가 없었습니다.

            outGroundResult.isWalkable = false;
        }

        /// <summary>
        /// 주어진 위치에서 캡슐에 대해 더 작은 반경으로 스윕 결과를 계산하고,
        /// 스윕이 충돌 지점에서 유효한 걷기 가능한 법선과 접촉하는 경우 true를 반환합니다.
        /// 이것은 캡슐이 작은 ledge 또는 걷기 불가능한 표면의 가장자리에 앉을 수 있는지 여부를 결정하는 데 사용될 수 있습니다.
        /// </summary>

        private bool ComputePerchResult(Vector3 characterPosition, float testRadius, float inMaxGroundDistance,
            ref RaycastHit inHit, out FindGroundResult perchGroundResult)
        {
            perchGroundResult = default;

            if (inMaxGroundDistance <= 0.0f)
                return false;

            // 캡슐의 반경이 줄어들기 때문에 우리가 놓칠 수 있는 히트를 잡기 위해 실제 요청된 거리보다 더 멀리 스윕합니다.

            float inHitAboveBase = Mathf.Max(0.0f, Vector3.Dot(inHit.point - characterPosition, _characterUp));
            float perchCastDist = Mathf.Max(0.0f, inMaxGroundDistance - inHitAboveBase);
            float perchSweepDist = Mathf.Max(0.0f, inMaxGroundDistance);

            float actualSweepDist = perchSweepDist + _radius;
            ComputeGroundDistance(characterPosition, testRadius, actualSweepDist, perchCastDist, out perchGroundResult);

            if (!perchGroundResult.isWalkable)
                return false;
            else if (inHitAboveBase + perchGroundResult.groundDistance > inMaxGroundDistance)
            {
                // 최대 거리를 초과하여 무언가를 맞았습니다.

                perchGroundResult.isWalkable = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 주어진 위치에서 캡슐에 대해 지면을 찾기 위해 수직 캐스트를 스윕합니다.
        /// ShouldComputePerchResult()가 아래로 스윕 결과에 대해 true를 반환하면, 앉기를 시도합니다.
        /// 충돌이 비활성화된 경우 지면을 찾을 수 없습니다(예: detectCollisions == false).
        /// </summary>

        public void FindGround(Vector3 characterPosition, out FindGroundResult outGroundResult)
        {
            // 충돌이 없으면 지면도 없습니다...

            if (!_detectCollisions)
            {
                outGroundResult = default;
                return;
            }

            // 걷는 경우 높이 확인을 약간 늘립니다.
            // 지면 높이 조정이 나중에 지면 결과를 무효화하지 않도록 합니다.

            float heightCheckAdjust = isGrounded ? kMaxGroundDistance + kKindaSmallNumber : -kMaxGroundDistance;
            float sweepDistance = Mathf.Max(kMaxGroundDistance, stepOffset + heightCheckAdjust);

            // 지면 스윕

            ComputeGroundDistance(characterPosition, _radius, sweepDistance, sweepDistance, out outGroundResult);

            // outGroundResult.hitResult는 이제 수직 지면 확인의 결과입니다.
            // 이 위치에 "앉으려고" 해야 하는지 확인합니다.

            if (outGroundResult.hitGround && !outGroundResult.isRaycastResult)
            {
                Vector3 positionOnGround = outGroundResult.position;

                if (ShouldComputePerchResult(positionOnGround, ref outGroundResult.hitResult))
                {
                    float maxPerchGroundDistance = sweepDistance;
                    if (isGrounded)
                        maxPerchGroundDistance += perchAdditionalHeight;

                    float validPerchRadius = GetValidPerchRadius(outGroundResult.collider);

                    if (ComputePerchResult(positionOnGround, validPerchRadius, maxPerchGroundDistance,
                        ref outGroundResult.hitResult, out FindGroundResult perchGroundResult))
                    {
                        // 지면 거리 조정이 너무 높이 올라가게 하지 않습니다.
                        // 그렇지 않으면 다음에 앉기 거리를 초과하여 이동하여 떨어질 수 있습니다.

                        float moveUpDist = kAvgGroundDistance - outGroundResult.groundDistance;
                        if (moveUpDist + perchGroundResult.groundDistance >= maxPerchGroundDistance)
                        {
                            outGroundResult.groundDistance = kAvgGroundDistance;
                        }

                        // 일반 캡슐이 걷기 불가능한 표면에 있지만 앉으면 서 있을 수 있는 경우,
                        // 걷기 가능한 법선으로 오버라이드합니다.

                        if (!outGroundResult.isWalkableGround)
                        {
                            // 지면 거리는 정상적으로 캡슐의 충돌 지점까지의 거리로 사용됩니다.
                            // AdjustGroundHeight()가 올바르게 작동하도록 합니다.

                            float groundDistance = outGroundResult.groundDistance;
                            float raycastDistance = Mathf.Max(kMinGroundDistance, groundDistance);

                            outGroundResult.SetFromRaycastResult(true, true, outGroundResult.position, groundDistance,
                                raycastDistance, ref perchGroundResult.hitResult);
                        }
                    }
                    else
                    {
                        // 지면이 없었거나(또는 걷기 불가능하여 유효하지 않은 경우) 여기 앉을 수 없었습니다.
                        // 따라서 지면을 무효화하여(이는 우리가 떨어지기 시작하도록 만듭니다).

                        outGroundResult.isWalkable = false;
                    }
                }
            }
        }

        /// <summary>
        /// 걷는 동안 지면에서의 거리를 조정하여, 걷는 동안 지면에서 약간의 오프셋을 유지하려고 합니다(현재 GroundResult를 기준으로).
        /// 캐릭터가 isConstrainedToGround == true인 경우에만.
        /// </summary>

        private void AdjustGroundHeight()
        {
            // 지면 확인이 아무것도 맞지 않은 경우, 높이를 조정하지 않습니다.

            if (!_currentGround.isWalkableGround || !isConstrainedToGround)
                return;

            float lastGroundDistance = _currentGround.groundDistance;

            if (_currentGround.isRaycastResult)
            {
                if (lastGroundDistance < kMinGroundDistance && _currentGround.raycastDistance >= kMinGroundDistance)
                {
                    // 이것은 우리가 걷기 불가능한 벽을 스케일링하게 만듭니다.

                    return;
                }
                else
                {
                    // 레이캐스트로 돌아가는 것은 스윕이 걷기 불가능했거나 침투 상태였음을 의미합니다.
                    // 수직 조정을 위해 레이캐스트 거리를 사용합니다.

                    lastGroundDistance = _currentGround.raycastDistance;
                }
            }

            // 지면 높이를 유지하기 위해 위아래로 이동합니다.

            if (lastGroundDistance < kMinGroundDistance || lastGroundDistance > kMaxGroundDistance)
            {
                float initialY = Vector3.Dot(updatedPosition, _characterUp);
                float moveDistance = kAvgGroundDistance - lastGroundDistance;

                Vector3 displacement = _characterUp * moveDistance;

                Vector3 sweepOrigin = updatedPosition;
                Vector3 sweepDirection = displacement.normalized;

                float sweepRadius = _radius;
                float sweepDistance = displacement.magnitude;

                int sweepLayerMask = _collisionLayers;

                bool hit = SweepTestEx(sweepOrigin, sweepRadius, sweepDirection, sweepDistance, sweepLayerMask,
                    out RaycastHit hitResult, out bool startPenetrating, out _, out _, true);

                if (!hit && !startPenetrating)
                {
                    // 충돌이 없으면 전체 변위를 적용합니다.

                    updatedPosition += displacement;
                    _currentGround.groundDistance += moveDistance;
                }
                else if (moveDistance > 0.0f)
                {
                    // 위로 이동

                    updatedPosition += sweepDirection * hitResult.distance;

                    float currentY = Vector3.Dot(updatedPosition, _characterUp);
                    _currentGround.groundDistance += currentY - initialY;
                }
                else
                {
                    // 아래로 이동

                    updatedPosition += sweepDirection * hitResult.distance;

                    float currentY = Vector3.Dot(updatedPosition, _characterUp);
                    _currentGround.groundDistance = currentY - initialY;
                }
            }

            // 루트 변환 위치를 조정합니다(오프셋 및 skinWidth를 고려합니다).

            if (_rootTransform)
            {
                _rootTransform.localPosition =
                    _rootTransformOffset - new Vector3(0.0f, kAvgGroundDistance, 0.0f);
            }
        }

        /// <summary>
        /// 캐릭터가 주어진 콜라이더 위로 올라갈 수 있는지 여부를 결정합니다.
        /// </summary>

        private bool CanStepUp(Collider otherCollider)
        {
            // 입력 콜라이더를 검증합니다.

            if (otherCollider == null)
                return false;

            // 충돌 행동 콜백이 할당된 경우, 그것을 사용합니다.

            if (collisionBehaviourCallback != null)
            {
                CollisionBehaviour collisionBehaviour = collisionBehaviourCallback.Invoke(otherCollider);

                if (CanStepOn(collisionBehaviour))
                    return true;

                if (CanNotStepOn(collisionBehaviour))
                    return false;
            }

            // 기본 경우, stepOffset에 의해 관리됩니다.

            return true;
        }

        /// <summary>
        /// 계단이나 경사를 올라갑니다.
        /// CanStepUp(collider)가 false를 반환하면 아무 작업도 하지 않고 false를 반환합니다. 계단 오르기에 성공하면 true를 반환합니다.
        /// </summary>

        private bool StepUp(ref CollisionResult inCollision, out CollisionResult stepResult)
        {
            stepResult = default;

            // 캡슐의 상단이 무언가를 맞고 있는 경우에는 올라가지 않습니다.

            if (inCollision.hitLocation == HitLocation.Above)
                return false;

            // 실제 지면 충돌 지점의 최대 계단 높이를 적용해야 합니다.

            float characterInitialGroundPositionY = Vector3.Dot(inCollision.position, _characterUp);
            float groundPointY = characterInitialGroundPositionY;

            float actualGroundDistance = Mathf.Max(0.0f, _currentGround.GetDistanceToGround());
            characterInitialGroundPositionY -= actualGroundDistance;

            float stepTravelUpHeight = Mathf.Max(0.0f, stepOffset - actualGroundDistance);
            float stepTravelDownHeight = stepOffset + kMaxGroundDistance * 2.0f;

            bool hitVerticalFace =
                !IsWithinEdgeTolerance(inCollision.position, inCollision.point, _radius + kContactOffset);

            if (!_currentGround.isRaycastResult && !hitVerticalFace)
                groundPointY = Vector3.Dot(groundPoint, _characterUp);
            else
                groundPointY -= _currentGround.groundDistance;

            // 지면에서 일정 거리 이상 아래에 있는 충돌은 무시합니다.

            float initialImpactY = Vector3.Dot(inCollision.point, _characterUp);
            if (initialImpactY <= characterInitialGroundPositionY)
                return false;

            // 계단 오르기, 수직 벽처럼 취급합니다.

            Vector3 sweepOrigin = inCollision.position;
            Vector3 sweepDirection = _characterUp;

            float sweepRadius = _radius;
            float sweepDistance = stepTravelUpHeight;

            int sweepLayerMask = _collisionLayers;

            bool foundBlockingHit = SweepTest(sweepOrigin, sweepRadius, sweepDirection, sweepDistance, sweepLayerMask,
                out RaycastHit hitResult, out bool startPenetrating);

            if (startPenetrating)
                return false;

            if (!foundBlockingHit)
                sweepOrigin += sweepDirection * sweepDistance;
            else
                sweepOrigin += sweepDirection * hitResult.distance;

            // 앞으로 이동(수평 변위만 적용)

            Vector3 displacement = inCollision.remainingDisplacement;
            Vector3 displacement2D = ConstrainVectorToPlane(Vector3.ProjectOnPlane(displacement, _characterUp));

            sweepDistance = displacement.magnitude;
            sweepDirection = displacement2D.normalized;

            foundBlockingHit = SweepTest(sweepOrigin, sweepRadius, sweepDirection, sweepDistance, sweepLayerMask,
                out hitResult, out startPenetrating);

            if (startPenetrating)
                return false;

            if (!foundBlockingHit)
                sweepOrigin += sweepDirection * sweepDistance;
            else
            {
                // '장애물'을 넘지 못했습니다, 반환합니다.

                return false;
            }

            // 아래로 이동

            sweepDirection = -_characterUp;
            sweepDistance = stepTravelDownHeight;

            foundBlockingHit = SweepTest(sweepOrigin, sweepRadius, sweepDirection, sweepDistance, sweepLayerMask,
                out hitResult, out startPenetrating);

            if (!foundBlockingHit || startPenetrating)
                return false;

            // 이 단계 시퀀스가 최대 계단 높이를 초과하여 이동할 수 있게 했는지 확인합니다.

            float deltaY = Vector3.Dot(hitResult.point, _characterUp) - groundPointY;
            if (deltaY > stepOffset)
                return false;

            // 계단 위의 위치가 명확한지 확인합니다.

            Vector3 positionOnStep = sweepOrigin + sweepDirection * hitResult.distance;

            if (OverlapTest(positionOnStep, updatedRotation, _radius, _height, _collisionLayers, _overlaps, triggerInteraction) > 0)
                return false;

            // 여기에서 걷기 불가능한 표면 법선을 거부합니다.

            Vector3 surfaceNormal = FindGeomOpposingNormal(sweepDirection * sweepDistance, ref hitResult);

            bool isWalkable = IsWalkable(hitResult.collider, surfaceNormal);
            if (!isWalkable)
            {
                // 이동 방향에 반대하는 법선을 거부합니다.

                bool normalTowardsMe = Vector3.Dot(displacement, surfaceNormal) < 0.0f;
                if (normalTowardsMe)
                    return false;

                // 또한 아래로 내려가는 도중 시작 위치보다 높아질 경우에도 거부합니다.

                if (Vector3.Dot(positionOnStep, _characterUp) > Vector3.Dot(inCollision.position, _characterUp))
                    return false;
            }

            // 아래로 스윕하는 동안 캡슐 가장자리에 매우 가까운 무언가를 맞았을 경우, 이동을 거부합니다.
            // 이는 FindGround와의 일관성을 유지합니다.

            if (!IsWithinEdgeTolerance(positionOnStep, hitResult.point, _radius + kContactOffset))
                return false;

            // 더 높은 곳으로 이동할 경우 유효하지 않은 표면에 올라가는 것을 거부합니다.

            if (deltaY > 0.0f && !CanStepUp(hitResult.collider))
                return false;

            // 계단 위의 새로운 위치를 출력합니다.

            stepResult = new CollisionResult
            {
                position = positionOnStep
            };

            return true;
        }

        /// <summary>
        /// 캐릭터가 지면에서 자유롭게 벗어날 수 있도록 지면 제약을 일시적으로 해제합니다.
        /// 예: 캐릭터 발사, 점프 등.
        /// </summary>

        public void PauseGroundConstraint(float unconstrainedTime = 0.1f)
        {
            _unconstrainedTimer = Mathf.Max(0.0f, unconstrainedTime);
        }

        /// <summary>
        /// 현재 지면 결과를 업데이트합니다.
        /// </summary>

        private void UpdateCurrentGround(ref FindGroundResult inGroundResult)
        {
            wasOnGround = isOnGround;

            wasOnWalkableGround = isOnWalkableGround;

            wasGrounded = isGrounded;

            _currentGround = inGroundResult;
        }

        /// <summary>
        /// Move 호출 중 캐릭터의 경계 볼륨에 대한 충돌을 처리합니다.
        /// 이전과 달리, 이는 캐릭터의 속도를 수정하거나 업데이트하지 않습니다.
        /// </summary>

        private int SlideAlongSurface(int iteration, Vector3 inputDisplacement, ref Vector3 displacement,
            ref CollisionResult inHit, ref Vector3 prevNormal)
        {
            inHit.normal = ComputeBlockingNormal(inHit.normal, inHit.isWalkable);

            if (inHit.isWalkable && isConstrainedToGround)
                displacement = ComputeSlideVector(displacement, inHit.normal, true);
            else
            {
                if (iteration == 0)
                {
                    displacement = ComputeSlideVector(displacement, inHit.normal, inHit.isWalkable);
                    iteration++;
                }
                else if (iteration == 1)
                {
                    Vector3 crease = prevNormal.perpendicularTo(inHit.normal);

                    Vector3 oVel = inputDisplacement.projectedOnPlane(crease);

                    Vector3 nVel = ComputeSlideVector(displacement, inHit.normal, inHit.isWalkable);
                    nVel = nVel.projectedOnPlane(crease);

                    if (oVel.dot(nVel) <= 0.0f || prevNormal.dot(inHit.normal) < 0.0f)
                    {
                        displacement = ConstrainVectorToPlane(displacement.projectedOn(crease));
                        ++iteration;
                    }
                    else
                    {
                        displacement = ComputeSlideVector(displacement, inHit.normal, inHit.isWalkable);
                    }
                }
                else
                {
                    displacement = Vector3.zero;
                }

                prevNormal = inHit.normal;
            }

            return iteration;
        }

        /// <summary>
        /// 충돌 제약 이동을 수행합니다.
        /// 이는 움직이는 플랫폼에 서 있을 때 캐릭터를 이동시키기 위해 독점적으로 사용됩니다. 캐릭터의 상태를 업데이트하지 않습니다.
        /// </summary>

        private void MoveAndSlide(Vector3 displacement)
        {
            //
            // 충돌 제약 이동 수행 (aka: 충돌 및 슬라이드)

            Vector3 inputDisplacement = displacement;

            int iteration = default;
            Vector3 prevNormal = default;

            int maxSlideCount = _advanced.maxMovementIterations;
            while (maxSlideCount-- > 0 && displacement.sqrMagnitude > _advanced.minMoveDistanceSqr)
            {
                bool collided = MovementSweepTest(updatedPosition, default, displacement, out CollisionResult collisionResult);
                if (!collided)
                    break;

                // 히트 위치까지 변위를 적용하고 남은 변위로 업데이트합니다.

                updatedPosition += collisionResult.displacementToHit;

                displacement = collisionResult.remainingDisplacement;

                //
                // 충돌 해결 (충돌 표면을 따라 슬라이드)

                iteration = SlideAlongSurface(iteration, inputDisplacement, ref displacement, ref collisionResult, ref prevNormal);

                //
                // 충돌 결과를 캐시합니다.

                AddCollisionResult(ref collisionResult);
            }

            //
            // 남은 변위를 적용합니다.

            if (displacement.sqrMagnitude > _advanced.minMoveDistanceSqr)
                updatedPosition += displacement;
        }

        /// <summary>
        /// 캐릭터가 주어진 콜라이더에 탈 수 있는지 여부를 결정합니다(이동하는 플랫폼으로 사용).
        /// </summary>

        private bool CanRideOn(Collider otherCollider)
        {
            // 입력 콜라이더를 검증합니다.

            if (otherCollider == null)
                return false;

            // 충돌 행동 콜백이 할당된 경우, 그것을 사용합니다.

            if (collisionBehaviourCallback != null)
            {
                CollisionBehaviour collisionBehaviour = collisionBehaviourCallback.Invoke(otherCollider);

                if (CanRideOn(collisionBehaviour) && otherCollider.attachedRigidbody)
                    return true;

                if (CanNotRideOn(collisionBehaviour) && otherCollider.attachedRigidbody)
                    return false;
            }

            // 기본적으로 걷기 가능한 리지드바디(키네틱 및 동적)에 탑승을 허용합니다.

            return otherCollider.attachedRigidbody;
        }

        /// <summary>
        /// 현재 플랫폼 콜라이더와의 충돌 감지를 무시합니다.
        /// </summary>

        private void IgnoreCurrentPlatform(bool ignore)
        {
            IgnoreCollision(_movingPlatform.platform, ignore);
        }

        /// <summary>
        /// 이를 이동하는 '플랫폼'에 명시적으로 연결할 수 있게 하여 지면 상태에 의존하지 않도록 합니다.
        /// </summary>

        public void AttachTo(Rigidbody parent)
        {
            _parentPlatform = parent;
        }

        /// <summary>
        /// 현재 활성 이동 플랫폼을 업데이트합니다(있는 경우).
        /// </summary>

        private void UpdateCurrentPlatform()
        {
            _movingPlatform.lastPlatform = _movingPlatform.platform;

            if (_parentPlatform)
                _movingPlatform.platform = _parentPlatform;
            else if (isGrounded && CanRideOn(groundCollider))
                _movingPlatform.platform = groundCollider.attachedRigidbody;
            else
                _movingPlatform.platform = null;

            if (_movingPlatform.platform != null)
            {
                Transform platformTransform = _movingPlatform.platform.transform;

                _movingPlatform.position = updatedPosition;
                _movingPlatform.localPosition = platformTransform.InverseTransformPoint(updatedPosition);

                _movingPlatform.rotation = updatedRotation;
                _movingPlatform.localRotation = Quaternion.Inverse(platformTransform.rotation) * updatedRotation;
            }
        }

        /// <summary>
        /// 이동 플랫폼 데이터를 업데이트하고 캐릭터를 함께 이동/회전시킵니다(허용되는 경우).
        /// </summary>

        private void UpdatePlatformMovement(float deltaTime)
        {
            Vector3 lastPlatformVelocity = _movingPlatform.platformVelocity;

            if (!_movingPlatform.platform)
                _movingPlatform.platformVelocity = Vector3.zero;
            else
            {
                Transform platformTransform = _movingPlatform.platform.transform;

                Vector3 newPositionOnPlatform = platformTransform.TransformPoint(_movingPlatform.localPosition);
                Vector3 deltaPosition = newPositionOnPlatform - _movingPlatform.position;

                _movingPlatform.deltaPosition = deltaPosition;
                _movingPlatform.platformVelocity = deltaTime > 0.0f ? deltaPosition / deltaTime : Vector3.zero;

                if (impartPlatformRotation)
                {
                    Quaternion newRotationOnPlatform = platformTransform.rotation * _movingPlatform.localRotation;
                    Quaternion deltaRotation = newRotationOnPlatform * Quaternion.Inverse(_movingPlatform.rotation);

                    _movingPlatform.deltaRotation = deltaRotation;

                    Vector3 newForward = Vector3
                        .ProjectOnPlane(deltaRotation * updatedRotation * Vector3.forward, _characterUp).normalized;

                    updatedRotation = Quaternion.LookRotation(newForward, _characterUp);
                }
            }

            if (impartPlatformMovement && _movingPlatform.platformVelocity.sqrMagnitude > 0.0f)
            {
                if (fastPlatformMove)
                    updatedPosition += _movingPlatform.platformVelocity * deltaTime;
                else
                {
                    IgnoreCurrentPlatform(true);

                    MoveAndSlide(_movingPlatform.platformVelocity * deltaTime);

                    IgnoreCurrentPlatform(false);
                }
            }

            if (impartPlatformVelocity)
            {
                Vector3 impartedPlatformVelocity = Vector3.zero;

                if (_movingPlatform.lastPlatform && _movingPlatform.platform != _movingPlatform.lastPlatform)
                {
                    impartedPlatformVelocity -= _movingPlatform.platformVelocity;

                    impartedPlatformVelocity += lastPlatformVelocity;
                }

                if (_movingPlatform.lastPlatform == null && _movingPlatform.platform)
                {
                    impartedPlatformVelocity -= _movingPlatform.platformVelocity;
                }

                _velocity += impartedPlatformVelocity;
            }
        }

        /// <summary>
        /// 캐릭터 대 리지드바디 또는 캐릭터 대 캐릭터의 충돌 응답 임펄스를 계산합니다.
        /// </summary>

        private void ComputeDynamicCollisionResponse(ref CollisionResult inCollisionResult,
            out Vector3 characterImpulse, out Vector3 otherImpulse)
        {
            characterImpulse = default;
            otherImpulse = default;

            float massRatio = 0.0f;

            Rigidbody otherRigidbody = inCollisionResult.rigidbody;
            if (!otherRigidbody.isKinematic || otherRigidbody.TryGetComponent(out CharacterMovement _))
            {
                float mass = rigidbody.mass;
                massRatio = mass / (mass + inCollisionResult.rigidbody.mass);
            }

            Vector3 normal = inCollisionResult.normal;

            float velocityDotNormal = Vector3.Dot(inCollisionResult.velocity, normal);
            float otherVelocityDotNormal = Vector3.Dot(inCollisionResult.otherVelocity, normal);

            if (velocityDotNormal < 0.0f)
                characterImpulse += velocityDotNormal * normal;

            if (otherVelocityDotNormal > velocityDotNormal)
            {
                Vector3 relVel = (otherVelocityDotNormal - velocityDotNormal) * normal;

                characterImpulse += relVel * (1.0f - massRatio);
                otherImpulse -= relVel * massRatio;
            }
        }

        /// <summary>
        /// 동적 충돌(예: 캐릭터 대 리지드바디 또는 캐릭터 대 다른 캐릭터)에 대한 충돌 응답 임펄스를 계산하고 적용합니다.
        /// </summary>

        private void ResolveDynamicCollisions()
        {
            if (!enablePhysicsInteraction)
                return;

            for (int i = 0; i < _collisionCount; i++)
            {
                ref CollisionResult collisionResult = ref _collisionResults[i];
                if (collisionResult.isWalkable)
                    continue;

                Rigidbody otherRigidbody = collisionResult.rigidbody;
                if (otherRigidbody == null)
                    continue;

                ComputeDynamicCollisionResponse(ref collisionResult, out Vector3 characterImpulse, out Vector3 otherImpulse);

                collisionResponseCallback?.Invoke(ref collisionResult, ref characterImpulse, ref otherImpulse);

                if (otherRigidbody.TryGetComponent(out CharacterMovement otherCharacter))
                {
                    if (physicsInteractionAffectsCharacters)
                    {
                        velocity += characterImpulse;
                        otherCharacter.velocity += otherImpulse * pushForceScale;
                    }
                }
                else
                {
                    _velocity += characterImpulse;

                    if (!otherRigidbody.isKinematic)
                    {
                        otherRigidbody.AddForceAtPosition(otherImpulse * pushForceScale, collisionResult.point,
                            ForceMode.VelocityChange);
                    }
                }
            }

            if (isGrounded)
                _velocity = _velocity.projectedOnPlane(_characterUp).normalized * _velocity.magnitude;

            _velocity = ConstrainVectorToPlane(_velocity);
        }

        /// <summary>
        /// 캐릭터의 현재 위치를 업데이트합니다.
        /// updateGround가 true인 경우, 지면을 찾아 캐릭터의 현재 지면 결과를 업데이트합니다.
        /// </summary>

        public void SetPosition(Vector3 newPosition, bool updateGround = false)
        {
            updatedPosition = newPosition;

            if (updateGround)
            {
                FindGround(updatedPosition, out FindGroundResult groundResult);
                {
                    UpdateCurrentGround(ref groundResult);

                    AdjustGroundHeight();

                    UpdateCurrentPlatform();
                }
            }

            rigidbody.position = updatedPosition;
            transform.position = updatedPosition;
        }

        /// <summary>
        /// 캐릭터의 현재 위치를 반환합니다.
        /// </summary>

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        /// <summary>
        /// 접촉 오프셋을 고려한 캐릭터의 발 위치를 반환합니다.
        /// </summary>

        public Vector3 GetFootPosition()
        {
            return transform.position - transform.up * kAvgGroundDistance;
        }

        /// <summary>
        /// 캐릭터의 현재 회전을 업데이트합니다.
        /// </summary>

        public void SetRotation(Quaternion newRotation)
        {
            updatedRotation = newRotation;

            rigidbody.rotation = updatedRotation;
            transform.rotation = updatedRotation;
        }

        /// <summary>
        /// 캐릭터의 현재 회전을 반환합니다.
        /// </summary>

        public Quaternion GetRotation()
        {
            return transform.rotation;
        }

        /// <summary>
        /// 캐릭터의 세계 공간 위치와 회전을 설정합니다.
        /// updateGround가 true인 경우, 지면을 찾아 캐릭터의 현재 지면 결과를 업데이트합니다.
        /// </summary>

        public void SetPositionAndRotation(Vector3 newPosition, Quaternion newRotation, bool updateGround = false)
        {
            updatedPosition = newPosition;
            updatedRotation = newRotation;

            if (updateGround)
            {
                FindGround(updatedPosition, out FindGroundResult groundResult);
                {
                    UpdateCurrentGround(ref groundResult);

                    AdjustGroundHeight();

                    UpdateCurrentPlatform();
                }
            }

            rigidbody.position = updatedPosition;
            rigidbody.rotation = updatedRotation;

            transform.SetPositionAndRotation(updatedPosition, updatedRotation);
        }

        /// <summary>
        /// 주어진 방향(세계 공간)으로 캐릭터를 향하게 하여 maxDegreesDelta를 사용하여 회전 속도를 제어합니다.
        /// </summary>
        /// <param name="worldDirection">세계 공간에서의 목표 방향입니다.</param>
        /// <param name="maxDegreesDelta">초당 회전 변화(Deg/s).</param>
        /// <param name="updateYawOnly">True인 경우, 회전은 캐릭터의 평면(업-축으로 정의됨)에서 수행됩니다.</param>

        public void RotateTowards(Vector3 worldDirection, float maxDegreesDelta, bool updateYawOnly = true)
        {
            Vector3 characterUp = transform.up;

            if (updateYawOnly)
                worldDirection = worldDirection.projectedOnPlane(characterUp);

            if (worldDirection == Vector3.zero)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(worldDirection, characterUp);

            rotation = Quaternion.RotateTowards(rotation, targetRotation, maxDegreesDelta);
        }

        /// <summary>
        /// Move 호출 중 사용되는 캐시된 필드를 업데이트합니다.
        /// </summary>

        private void UpdateCachedFields()
        {
            _hasLanded = false;
            _foundGround = default;

            updatedPosition = transform.position;
            updatedRotation = transform.rotation;

            _characterUp = updatedRotation * Vector3.up;

            _transformedCapsuleCenter = updatedRotation * _capsuleCenter;
            _transformedCapsuleTopCenter = updatedRotation * _capsuleTopCenter;
            _transformedCapsuleBottomCenter = updatedRotation * _capsuleBottomCenter;

            ResetCollisionFlags();
        }

        /// <summary>
        /// 축적된 모든 힘, 포함된 발사 속도를 지웁니다.
        /// </summary>

        public void ClearAccumulatedForces()
        {
            _pendingForces = Vector3.zero;
            _pendingImpulses = Vector3.zero;
            _pendingLaunchVelocity = Vector3.zero;
        }

        /// <summary>
        /// 캐릭터에 힘을 추가합니다.
        /// 이 힘은 축적되어 Move 메소드 호출 중에 적용됩니다.
        /// </summary>

        public void AddForce(Vector3 force, ForceMode forceMode = ForceMode.Force)
        {
            switch (forceMode)
            {
                case ForceMode.Force:
                    {
                        _pendingForces += force / rigidbody.mass;
                        break;
                    }

                case ForceMode.Acceleration:
                    {
                        _pendingForces += force;
                        break;
                    }

                case ForceMode.Impulse:
                    {
                        _pendingImpulses += force / rigidbody.mass;
                        break;
                    }

                case ForceMode.VelocityChange:
                    {
                        _pendingImpulses += force;
                        break;
                    }
            }
        }

        /// <summary>
        /// 이 캐릭터에 폭발 효과를 시뮬레이션하는 힘을 적용합니다.
        /// 폭발은 특정 중심 위치와 반경을 가진 구로 모델링됩니다. 
        /// 일반적으로 구 바깥의 것은 폭발의 영향을 받지 않으며, 중심에서 멀어짐에 따라 힘은 비례하여 감소합니다.
        /// 그러나 반경에 0을 전달하면, 중심에서 얼마나 떨어져 있든 상관없이 전체 힘이 적용됩니다.
        /// 힘의 방향은 주어진 원점에서 캐릭터 중심으로 향합니다.
        /// </summary>

        public void AddExplosionForce(float strength, Vector3 origin, float radius, ForceMode forceMode = ForceMode.Force)
        {
            Vector3 delta = worldCenter - origin;
            float deltaMagnitude = delta.magnitude;

            float forceMagnitude = strength;
            if (radius > 0.0f)
                forceMagnitude *= 1.0f - Mathf.Clamp01(deltaMagnitude / radius);

            AddForce(delta.normalized * forceMagnitude, forceMode);
        }

        /// <summary>
        /// 캐릭터에 보류 중인 발사 속도를 설정합니다. 이 속도는 다음 Move 호출 시 처리됩니다.
        /// </summary>
        /// <param name="launchVelocity">원하는 발사 속도입니다.</param>
        /// <param name="overrideVerticalVelocity">True인 경우, 캐릭터의 속도의 수직 성분을 추가하는 대신 교체합니다.</param>
        /// <param name="overrideLateralVelocity">True인 경우, 캐릭터의 XY 부분의 속도를 추가하는 대신 교체합니다.</param>

        public void LaunchCharacter(Vector3 launchVelocity, bool overrideVerticalVelocity = false, bool overrideLateralVelocity = false)
        {
            // 최종 속도 계산

            Vector3 finalVelocity = launchVelocity;

            // 덮어쓰지 않을 경우, 주어진 발사 속도에 측면 속도를 추가합니다.

            Vector3 characterUp = transform.up;

            if (!overrideLateralVelocity)
                finalVelocity += _velocity.projectedOnPlane(characterUp);

            // 덮어쓰지 않을 경우, 주어진 발사 속도에 수직 속도를 추가합니다.

            if (!overrideVerticalVelocity)
                finalVelocity += _velocity.projectedOn(characterUp);

            _pendingLaunchVelocity = finalVelocity;
        }

        /// <summary>
        /// 캐릭터의 속도를 업데이트하고 모든 보류 중인 힘과 임펄스를 적용하고 지웁니다.
        /// </summary>

        private void UpdateVelocity(Vector3 newVelocity, float deltaTime)
        {
            // 새로운 속도 할당

            _velocity = newVelocity;

            // 보류 중인 축적된 힘 추가

            _velocity += _pendingForces * deltaTime;
            _velocity += _pendingImpulses;

            // 보류 중인 발사 속도 적용

            if (_pendingLaunchVelocity.sqrMagnitude > 0.0f)
                _velocity = _pendingLaunchVelocity;

            // 축적된 힘을 지웁니다.

            ClearAccumulatedForces();

            // 평면 제약 적용 (있는 경우)

            _velocity = ConstrainVectorToPlane(_velocity);
        }

        /// <summary>
        /// 주어진 속도 벡터를 따라 캐릭터를 이동시킵니다.
        /// 이는 충돌 제약 이동을 수행하여 이 이동 중 발견된 모든 충돌/오버랩을 해결합니다.
        /// </summary>
        /// <param name="newVelocity">현재 프레임에 대한 업데이트된 속도입니다. 이는 일반적으로 중력으로 인한 수직 이동과 캐릭터가 이동 중일 때의 측면 이동의 조합입니다.</param>
        /// <param name="deltaTime">시뮬레이션 델타타임입니다. 할당되지 않은 경우, 기본값은 Time.deltaTime입니다.</param>
        /// <returns>CollisionFlags를 반환합니다. 이는 충돌 방향을 나타냅니다: 없음, 측면, 위, 아래.</returns>

        public CollisionFlags Move(Vector3 newVelocity, float deltaTime)
        {
            UpdateCachedFields();

            ClearCollisionResults();

            UpdateVelocity(newVelocity, deltaTime);

            UpdatePlatformMovement(deltaTime);

            PerformMovement(deltaTime);

            if (isGrounded || _hasLanded)
                FindGround(updatedPosition, out _foundGround);

            UpdateCurrentGround(ref _foundGround);
            {
                if (_unconstrainedTimer > 0.0f)
                {
                    _unconstrainedTimer -= deltaTime;
                    if (_unconstrainedTimer <= 0.0f)
                        _unconstrainedTimer = 0.0f;
                }
            }

            AdjustGroundHeight();

            UpdateCurrentPlatform();

            ResolveDynamicCollisions();

            SetPositionAndRotation(updatedPosition, updatedRotation);

            OnCollided();

            if (!wasOnWalkableGround && isOnGround)
                OnFoundGround();

            return collisionFlags;
        }

        /// <summary>
        /// 캐릭터를 현재 속도에 따라 이동시킵니다.
        /// 이는 충돌 제약 이동을 수행하여 이 이동 중 발견된 모든 충돌/오버랩을 해결합니다.
        /// </summary>
        /// <param name="deltaTime">시뮬레이션 델타타임입니다.</param>

        public CollisionFlags Move(float deltaTime)
        {
            return Move(_velocity, deltaTime);
        }

        /// <summary>
        /// 마찰 기반 물리 모델을 사용하여 캐릭터의 속도를 업데이트하고, 업데이트된 속도에 따라 캐릭터를 이동시킵니다.
        /// 이는 충돌 제약 이동을 수행하여 이 이동 중 발견된 모든 충돌/오버랩을 해결합니다.
        /// </summary>
        /// <param name="desiredVelocity">목표 속도</param>
        /// <param name="maxSpeed">지면에 있는 동안의 최대 속도입니다. 또한, 낙하 중(즉, 지면에 있지 않은 경우)의 최대 수평 속도를 결정합니다.</param>
        /// <param name="acceleration">가속할 때(즉, desiredVelocity != Vector3.zero) 속도의 변화율입니다.</param>
        /// <param name="deceleration">브레이크를 걸 때(즉, 가속하지 않거나 캐릭터가 최대 속도를 초과한 경우) 캐릭터가 속도를 줄이는 비율입니다.
        /// 이는 속도를 일정 값만큼 직접 낮추는 일정한 반대 힘입니다.</param>
        /// <param name="friction">이동 제어에 영향을 주는 설정입니다. 값이 클수록 방향 변경이 더 빨라집니다.</param>
        /// <param name="brakingFriction">브레이크를 걸 때(즉, desiredVelocity == Vector3.zero, 또는 캐릭터가 최대 속도를 초과한 경우) 적용되는 마찰(드래그) 계수입니다.</param>
        /// <param name="gravity">현재 중력 힘입니다.</param>
        /// <param name="onlyHorizontal">낙하 중(즉, 지면에 있지 않은 경우) 수직 속도 성분을 무시하여 중력 효과를 유지할지 여부를 결정합니다.</param>
        /// <param name="deltaTime">시뮬레이션 델타타임입니다.</param>
        /// <returns>CollisionFlags를 반환합니다. 이는 충돌 방향을 나타냅니다: 없음, 측면, 위, 아래.</returns>

        public CollisionFlags SimpleMove(Vector3 desiredVelocity, float maxSpeed, float acceleration,
            float deceleration, float friction, float brakingFriction, Vector3 gravity, bool onlyHorizontal, float deltaTime)
        {
            if (isGrounded)
            {
                // 새로운 속도 계산

                velocity = CalcVelocity(velocity, desiredVelocity, maxSpeed, acceleration, deceleration, friction,
                    brakingFriction, deltaTime);
            }
            else
            {
                // 지면에 있지 않은 속도 계산

                Vector3 worldUp = -1.0f * gravity.normalized;
                Vector3 v = onlyHorizontal ? velocity.projectedOnPlane(worldUp) : velocity;

                if (onlyHorizontal)
                    desiredVelocity = desiredVelocity.projectedOnPlane(worldUp);

                // 걷기 불가능한 지면에 있는가?

                if (isOnGround)
                {
                    // '벽'으로 이동 중인 경우, 기여를 제한합니다.
                    // 벽과 평행하게 이동은 허용하되, 벽으로 들어가는 것은 허용하지 않습니다. 이는 우리가 위로 밀리는 것을 방지합니다.

                    Vector3 actualGroundNormal = groundNormal;
                    if (desiredVelocity.dot(actualGroundNormal) < 0.0f)
                    {
                        actualGroundNormal = actualGroundNormal.projectedOnPlane(worldUp).normalized;
                        desiredVelocity = desiredVelocity.projectedOnPlane(actualGroundNormal);
                    }
                }

                // 새로운 속도 계산

                v = CalcVelocity(v, desiredVelocity, maxSpeed, acceleration, deceleration, friction, brakingFriction, deltaTime);

                // 캐릭터의 속도 업데이트

                if (onlyHorizontal)
                    velocity += Vector3.ProjectOnPlane(v - velocity, worldUp);
                else
                    velocity += v - velocity;

                // 중력 가속도 적용

                velocity += gravity * deltaTime;
            }

            // 이동 수행

            return Move(deltaTime);
        }

        /// <summary>
        /// 게임 오브젝트의 충돌 행렬에서 CollisionLayers를 초기화합니다.
        /// </summary>

        [ContextMenu("Init Collision Layers from Collision Matrix")]
        private void InitCollisionMask()
        {
            int layer = gameObject.layer;

            _collisionLayers = 0;
            for (int i = 0; i < 32; i++)
            {
                if (!Physics.GetIgnoreLayerCollision(layer, i))
                    _collisionLayers |= 1 << i;
            }
        }

        /// <summary>
        /// 적절한 시뮬레이션 연속성을 보장하여 이전 시뮬레이션 상태를 복원합니다.
        /// </summary>

        public void SetState(Vector3 inPosition, Quaternion inRotation, Vector3 inVelocity,
            bool inConstrainedToGround, float inUnconstrainedTimer, bool inHitGround, bool inIsWalkable)
        {
            _velocity = inVelocity;

            _isConstrainedToGround = inConstrainedToGround;
            _unconstrainedTimer = Mathf.Max(0.0f, inUnconstrainedTimer);

            _currentGround.hitGround = inHitGround;
            _currentGround.isWalkable = inIsWalkable;

            SetPositionAndRotation(inPosition, inRotation, isGrounded);
        }

        #endregion

        #region MONOBEHAVIOUR

        private void Reset()
        {
            SetDimensions(0.5f, 2.0f);
            SetPlaneConstraint(PlaneConstraint.None, Vector3.zero);

            _slopeLimit = 45.0f;
            _stepOffset = 0.45f;
            _perchOffset = 0.5f;
            _perchAdditionalHeight = 0.4f;

            _triggerInteraction = QueryTriggerInteraction.Ignore;

            _advanced.Reset();

            _isConstrainedToGround = true;

            _pushForceScale = 1.0f;
        }

        private void OnValidate()
        {
            SetDimensions(_radius, _height);
            SetPlaneConstraint(_planeConstraint, _constraintPlaneNormal);

            slopeLimit = _slopeLimit;
            stepOffset = _stepOffset;
            perchOffset = _perchOffset;
            perchAdditionalHeight = _perchAdditionalHeight;

            _advanced.OnValidate();
        }

        private void Awake()
        {
            CacheComponents();

            SetDimensions(_radius, _height);
            SetPlaneConstraint(_planeConstraint, _constraintPlaneNormal);
        }

        private void OnEnable()
        {
            updatedPosition = transform.position;
            updatedRotation = transform.rotation;

            UpdateCachedFields();
        }

#if UNITY_EDITOR

        private static void DrawDisc(Vector3 _pos, Quaternion _rot, float _radius, Color _color = default,
            bool solid = true)
        {
            if (_color != default)
                UnityEditor.Handles.color = _color;

            Matrix4x4 mtx = Matrix4x4.TRS(_pos, _rot, UnityEditor.Handles.matrix.lossyScale);

            using (new UnityEditor.Handles.DrawingScope(mtx))
            {
                if (solid)
                    UnityEditor.Handles.DrawSolidDisc(Vector3.zero, Vector3.up, _radius);
                else
                    UnityEditor.Handles.DrawWireDisc(Vector3.zero, Vector3.up, _radius);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 발 위치 그리기

            float skinRadius = _radius;
            Vector3 footPosition = GetFootPosition();

            Gizmos.color = new Color(0.569f, 0.957f, 0.545f, 0.5f);
            Gizmos.DrawLine(footPosition + Vector3.left * skinRadius, footPosition + Vector3.right * skinRadius);
            Gizmos.DrawLine(footPosition + Vector3.back * skinRadius, footPosition + Vector3.forward * skinRadius);

            // perch 오프셋 반경 그리기

            if (perchOffset > 0.0f && perchOffset < radius)
            {
                DrawDisc(footPosition, rotation, _perchOffset, new Color(0.569f, 0.957f, 0.545f, 0.15f));
                DrawDisc(footPosition, rotation, _perchOffset, new Color(0.569f, 0.957f, 0.545f, 0.75f), false);
            }

            // step Offset 그리기

            if (stepOffset > 0.0f)
            {
                DrawDisc(footPosition + transform.up * stepOffset, rotation, radius * 1.15f,
                    new Color(0.569f, 0.957f, 0.545f, 0.75f), false);
            }
        }

#endif

        #endregion
    }
}