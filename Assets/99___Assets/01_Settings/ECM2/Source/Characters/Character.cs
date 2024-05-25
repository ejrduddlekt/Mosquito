using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ECM2
{
    [RequireComponent(typeof(CharacterMovement))]
    public class Character : MonoBehaviour
    {
        #region ENUMS

        public enum MovementMode
        {
            /// <summary>
            /// 이동을 비활성화하고 캐릭터의 속도 및 모든 대기 중인 힘/충격을 초기화합니다.
            /// </summary>

            None,

            /// <summary>
            /// 마찰의 영향을 받으며 표면 위를 걷고 "장애물을 넘을 수 있는" 상태입니다. 수직 속도는 0입니다.
            /// </summary>

            Walking,

            /// <summary>
            /// 점프 후 또는 표면 가장자리에서 떨어질 때 중력의 영향을 받으며 떨어지는 상태입니다.
            /// </summary>

            Falling,

            /// <summary>
            /// 중력의 영향을 무시하고 비행하는 상태입니다.
            /// </summary>

            Flying,

            /// <summary>
            /// 중력과 부력의 영향을 받으며 유체 볼륨을 통해 수영하는 상태입니다.
            /// </summary>

            Swimming,

            /// <summary>
            /// 사용자 정의 이동 모드로, 여러 하위 모드를 포함할 수 있습니다.
            /// </summary>

            Custom
        }

        public enum RotationMode
        {
            /// <summary>
            /// 캐릭터의 회전을 비활성화합니다.
            /// </summary>

            None,

            /// <summary>
            /// rotationRate를 회전 변경 속도로 사용하여 가속 방향으로 캐릭터를 부드럽게 회전시킵니다.
            /// </summary>

            OrientRotationToMovement,

            /// <summary>
            /// rotationRate를 회전 변경 속도로 사용하여 카메라의 시야 방향으로 캐릭터를 부드럽게 회전시킵니다.
            /// </summary>

            OrientRotationToViewDirection,

            /// <summary>
            /// 루트 모션이 캐릭터 회전을 처리하도록 합니다.
            /// </summary>

            OrientWithRootMotion,

            /// <summary>
            /// 사용자 정의 회전 모드입니다.
            /// </summary>

            Custom
        }

        #endregion

        #region EDITOR EXPOSED FIELDS

        [Space(15f)]
        [Tooltip("캐릭터의 현재 회전 모드입니다.")]
        [SerializeField]
        private RotationMode _rotationMode;

        [Tooltip("초당 회전 변화량 (Deg / s).\n" +
                 "회전 모드가 OrientRotationToMovement 또는 OrientRotationToViewDirection일 때 사용됩니다.")]
        [SerializeField]
        private float _rotationRate;

        [Space(15f)]
        [Tooltip("캐릭터의 기본 이동 모드입니다. 플레이어 시작 시 사용됩니다.")]
        [SerializeField]
        private MovementMode _startingMovementMode;

        [Space(15f)]
        [Tooltip("걷는 동안 최대 지면 속도입니다.\n" +
                 "또한 떨어질 때 최대 측면 속도를 결정합니다.")]
        [SerializeField]
        private float _maxWalkSpeed;

        [Tooltip("아날로그 스틱 최소 기울기에서 걷기 위해 가속해야 하는 지면 속도입니다.")]
        [SerializeField]
        private float _minAnalogWalkSpeed;

        [Tooltip("최대 가속도 (속도의 변화율)입니다.")]
        [SerializeField]
        private float _maxAcceleration;

        [Tooltip("걷는 동안 가속하지 않을 때의 감속도입니다.\n" +
                 "이는 속도를 일정 값만큼 직접 줄이는 일정한 반대 힘입니다.")]
        [SerializeField]
        private float _brakingDecelerationWalking;

        [Tooltip("이동 제어에 영향을 주는 설정입니다.\n" +
                 "값이 높을수록 방향 변경이 빨라집니다.\n" +
                 "useSeparateBrakingFriction이 false인 경우 가속이 0일 때 더 빨리 멈출 수 있는 능력에도 영향을 줍니다.")]
        [SerializeField]
        private float _groundFriction;

        [Space(15.0f)]
        [Tooltip("캐릭터가 앉을 수 있습니까?")]
        [SerializeField]
        private bool _canEverCrouch;

        [Tooltip("canEverCrouch == true일 때, 앉았을 때 캐릭터 높이를 결정합니다.")]
        [SerializeField]
        private float _crouchedHeight;

        [Tooltip("canEverCrouch == true일 때, 서 있을 때 캐릭터 높이를 결정합니다.")]
        [SerializeField]
        private float _unCrouchedHeight;

        [Tooltip("앉아 있을 때 최대 지면 속도입니다.")]
        [SerializeField]
        private float _maxWalkSpeedCrouched;

        [Space(15f)]
        [Tooltip("캐릭터가 떨어질 때 최대 수직 속도입니다. 예: 터미널 속도.")]
        [SerializeField]
        private float _maxFallSpeed;

        [Tooltip("떨어질 때 가속하지 않을 때의 측면 감속도입니다.")]
        [SerializeField]
        private float _brakingDecelerationFalling;

        [Tooltip("떨어질 때 측면 이동에 적용할 마찰력입니다.\n" +
                 "useSeparateBrakingFriction이 false인 경우 가속이 0일 때 더 빨리 멈출 수 있는 능력에도 영향을 줍니다.")]
        [SerializeField]
        private float _fallingLateralFriction;

        [Range(0.0f, 1.0f)]
        [Tooltip("떨어질 때 캐릭터의 측면 이동 제어 양입니다.\n" +
                 "0 = 제어 없음, 1 = 최대 가속에서 전체 제어.")]
        [SerializeField]
        private float _airControl;

        [Space(15.0f)]
        [Tooltip("캐릭터가 점프할 수 있습니까?")]
        [SerializeField]
        private bool _canEverJump;

        [Tooltip("앉은 상태에서 점프할 수 있습니까?")]
        [SerializeField]
        private bool _canJumpWhileCrouching;

        [Tooltip("캐릭터가 수행할 수 있는 최대 점프 횟수입니다.")]
        [SerializeField]
        private int _jumpMaxCount;

        [Tooltip("점프 시 초기 속도 (즉각적인 수직 속도)입니다.")]
        [SerializeField]
        private float _jumpImpulse;

        [Tooltip("점프를 유지할 최대 시간 (초)입니다. 예: 가변 높이 점프.")]
        [SerializeField]
        private float _jumpMaxHoldTime;

        [Tooltip("지면에 닿기 전에 점프를 트리거할 수 있는 최대 시간 (초)입니다.")]
        [SerializeField]
        private float _jumpMaxPreGroundedTime;

        [Tooltip("지면에서 떨어진 후 점프를 트리거할 수 있는 최대 시간 (초)입니다.")]
        [SerializeField]
        private float _jumpMaxPostGroundedTime;

        [Space(15f)]
        [Tooltip("최대 비행 속도입니다.")]
        [SerializeField]
        private float _maxFlySpeed;

        [Tooltip("비행 중 가속하지 않을 때의 감속도입니다.")]
        [SerializeField]
        private float _brakingDecelerationFlying;

        [Tooltip("비행 중 이동에 적용할 마찰력입니다.")]
        [SerializeField]
        private float _flyingFriction;

        [Space(15f)]
        [Tooltip("최대 수영 속도입니다.")]
        [SerializeField]
        private float _maxSwimSpeed;

        [Tooltip("수영 중 가속하지 않을 때의 감속도입니다.")]
        [SerializeField]
        private float _brakingDecelerationSwimming;

        [Tooltip("수영 중 이동에 적용할 마찰력입니다.")]
        [SerializeField]
        private float _swimmingFriction;

        [Tooltip("물의 부력 비율입니다. 1 = 중립 부력, 0 = 부력 없음.")]
        [SerializeField]
        private float _buoyancy;

        [Tooltip("이 캐릭터의 중력입니다.")]
        [Space(15f)]
        [SerializeField]
        private Vector3 _gravity;

        [Tooltip("이 객체가 중력에 영향을 받는 정도입니다.\n" +
                 "중력 방향을 변경할 수 있도록 음수로 설정할 수 있습니다.")]
        [SerializeField]
        private float _gravityScale;

        [Space(15f)]
        [Tooltip("애니메이션이 캐릭터의 이동을 결정해야 합니까?")]
        [SerializeField]
        private bool _useRootMotion;

        [Space(15f)]
        [Tooltip("캐릭터가 서 있는 플랫폼이 움직일 때 캐릭터도 움직입니까?")]
        [SerializeField]
        private bool _impartPlatformMovement;

        [Tooltip("캐릭터가 서 있는 플랫폼의 회전 변화를 받습니까?")]
        [SerializeField]
        private bool _impartPlatformRotation;

        [Tooltip("true인 경우 플랫폼에서 점프하거나 떨어질 때 플랫폼의 속도를 적용합니다.")]
        [SerializeField]
        private bool _impartPlatformVelocity;

        [Space(15f)]
        [Tooltip("활성화된 경우, 플레이어가 동적 리지드바디와 상호작용할 때 영향을 받습니다.")]
        [SerializeField]
        private bool _enablePhysicsInteraction;

        [Tooltip("걷는 동안 캐릭터와 충돌할 때 밀기 힘을 적용해야 합니까?")]
        [SerializeField]
        private bool _applyPushForceToCharacters;

        [Tooltip("서 있는 동안 리지드바디에 아래로 향하는 힘을 적용해야 합니까?")]
        [SerializeField]
        private bool _applyStandingDownwardForce;

        [Space(15.0f)]
        [Tooltip("이 캐릭터의 질량 (kg 단위)입니다.\n" +
                 "enablePhysicsInteraction == true인 경우 다른 캐릭터나 동적 리지드바디와 상호작용하는 방식을 결정합니다.")]
        [SerializeField]
        private float _mass;

        [Tooltip("걷는 동안 리지드바디에 적용되는 힘은 (질량과 상대 속도에 따라) 이 값으로 스케일됩니다.")]
        [SerializeField]
        private float _pushForceScale;

        [Tooltip("서 있는 동안 리지드바디에 적용되는 힘 (질량과 중력에 따라) 이 값으로 스케일됩니다.")]
        [SerializeField]
        private float _standingDownwardForceScale;

        [Space(15f)]
        [Tooltip("플레이어의 카메라에 대한 참조입니다.\n" +
                 "지정된 경우 캐릭터의 이동은 이 카메라를 기준으로, 그렇지 않은 경우 이동은 세계 축을 기준으로 합니다.")]
        [SerializeField]
        private Camera _camera;

        #endregion

        #region FIELDS

        protected readonly List<PhysicsVolume> _physicsVolumes = new List<PhysicsVolume>();

        private Coroutine _lateFixedUpdateCoroutine;
        private bool _enableAutoSimulation = true;

        private Transform _transform;
        private CharacterMovement _characterMovement;
        private Animator _animator;
        private RootMotionController _rootMotionController;
        private Transform _cameraTransform;

        /// <summary>
        /// 캐릭터의 현재 이동 모드입니다.
        /// </summary>

        private MovementMode _movementMode = MovementMode.None;

        /// <summary>
        /// 캐릭터의 사용자 정의 이동 모드 (하위 모드).
        /// _movementMode == Custom인 경우에만 적용됩니다.
        /// </summary>

        private int _customMovementMode;

        private bool _useSeparateBrakingFriction;
        private float _brakingFriction;

        private bool _useSeparateBrakingDeceleration;
        private float _brakingDeceleration;

        private Vector3 _movementDirection = Vector3.zero;
        private Vector3 _rotationInput = Vector3.zero;

        private Vector3 _desiredVelocity = Vector3.zero;

        protected bool _isCrouched;

        protected bool _isJumping;
        private float _jumpInputHoldTime;
        private float _jumpForceTimeRemaining;
        private int _jumpCurrentCount;

        protected float _fallingTime;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// 이 캐릭터의 카메라 변환입니다.
        /// 지정된 경우 캐릭터의 이동은 이 카메라를 기준으로, 그렇지 않은 경우 이동은 세계 축을 기준으로 합니다.
        /// </summary>

        public new Camera camera
        {
            get => _camera;
            set => _camera = value;
        }

        /// <summary>
        /// 캐시된 카메라 변환 (있는 경우).
        /// </summary>

        public Transform cameraTransform
        {
            get
            {
                if (_camera != null)
                    _cameraTransform = _camera.transform;

                return _cameraTransform;
            }
        }

        /// <summary>
        /// 캐시된 캐릭터 변환입니다.
        /// </summary>

        public new Transform transform => _transform;

        /// <summary>
        /// 캐시된 CharacterMovement 구성 요소입니다.
        /// </summary>

        public CharacterMovement characterMovement => _characterMovement;

        /// <summary>
        /// 캐시된 Animator 구성 요소입니다. null일 수 있습니다.
        /// </summary>

        public Animator animator => _animator;

        /// <summary>
        /// 캐시된 캐릭터의 RootMotionController 구성 요소입니다. null일 수 있습니다.
        /// </summary>

        public RootMotionController rootMotionController => _rootMotionController;

        /// <summary>
        /// 회전 모드가 OrientRotationToMovement 또는 OrientRotationToViewDirection일 때 사용하는 초당 회전 변화량입니다.
        /// </summary>

        public float rotationRate
        {
            get => _rotationRate;
            set => _rotationRate = value;
        }

        /// <summary>
        /// 캐릭터의 현재 회전 모드입니다.
        /// </summary>

        public RotationMode rotationMode
        {
            get => _rotationMode;
            set => _rotationMode = value;
        }

        /// <summary>
        /// 걷는 동안 최대 지면 속도입니다. 또한 떨어질 때 최대 측면 속도를 결정합니다.
        /// </summary>

        public float maxWalkSpeed
        {
            get => _maxWalkSpeed;
            set => _maxWalkSpeed = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 아날로그 스틱 최소 기울기에서 걷기 위해 가속해야 하는 지면 속도입니다.
        /// </summary>

        public float minAnalogWalkSpeed
        {
            get => _minAnalogWalkSpeed;
            set => _minAnalogWalkSpeed = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 최대 가속도 (속도의 변화율)입니다.
        /// </summary>

        public float maxAcceleration
        {
            get => _maxAcceleration;
            set => _maxAcceleration = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 걷는 동안 가속하지 않을 때의 감속도입니다.
        /// 이는 속도를 일정 값만큼 직접 줄이는 일정한 반대 힘입니다.
        /// </summary>

        public float brakingDecelerationWalking
        {
            get => _brakingDecelerationWalking;
            set => _brakingDecelerationWalking = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 이동 제어에 영향을 주는 설정입니다.
        /// 값이 높을수록 방향 변경이 빨라집니다.
        /// useSeparateBrakingFriction이 false인 경우 가속이 0일 때 더 빨리 멈출 수 있는 능력에도 영향을 줍니다.
        /// </summary>

        public float groundFriction
        {
            get => _groundFriction;
            set => _groundFriction = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 캐릭터가 앉을 수 있습니까?
        /// </summary>

        public bool canEverCrouch
        {
            get => _canEverCrouch;
            set => _canEverCrouch = value;
        }

        /// <summary>
        /// canEverCrouch == true일 때, 앉았을 때 캐릭터 높이를 결정합니다.
        /// </summary>

        public float crouchedHeight
        {
            get => _crouchedHeight;
            set => _crouchedHeight = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// canEverCrouch == true일 때, 서 있을 때 캐릭터 높이를 결정합니다.
        /// </summary>

        public float unCrouchedHeight
        {
            get => _unCrouchedHeight;
            set => _unCrouchedHeight = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 앉아 있을 때 최대 지면 속도입니다.
        /// </summary>

        public float maxWalkSpeedCrouched
        {
            get => _maxWalkSpeedCrouched;
            set => _maxWalkSpeedCrouched = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 앉기 입력이 눌렸습니까?
        /// </summary>

        public bool crouchInputPressed { get; protected set; }

        /// <summary>
        /// 떨어질 때 최대 수직 속도 (m/s)입니다.
        /// 예: 터미널 속도.
        /// </summary>

        public float maxFallSpeed
        {
            get => _maxFallSpeed;
            set => _maxFallSpeed = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 떨어질 때 가속하지 않을 때의 측면 감속도입니다.
        /// </summary>

        public float brakingDecelerationFalling
        {
            get => _brakingDecelerationFalling;
            set => _brakingDecelerationFalling = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 떨어질 때 측면 공기 이동에 적용할 마찰력입니다.
        /// </summary>

        public float fallingLateralFriction
        {
            get => _fallingLateralFriction;
            set => _fallingLateralFriction = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 캐릭터의 낙하 시간입니다.
        /// </summary>

        public float fallingTime => _fallingTime;

        /// <summary>
        /// 떨어질 때 캐릭터의 측면 이동 제어 양입니다.
        /// 0 = 제어 없음, 1 = 최대 가속에서 전체 제어.
        /// </summary>

        public float airControl
        {
            get => _airControl;
            set => _airControl = Mathf.Clamp01(value);
        }

        /// <summary>
        /// 캐릭터가 점프할 수 있습니까?
        /// </summary>

        public bool canEverJump
        {
            get => _canEverJump;
            set => _canEverJump = value;
        }

        /// <summary>
        /// 앉은 상태에서 점프할 수 있습니까?
        /// </summary>

        public bool canJumpWhileCrouching
        {
            get => _canJumpWhileCrouching;
            set => _canJumpWhileCrouching = value;
        }

        /// <summary>
        /// 캐릭터가 수행할 수 있는 최대 점프 횟수입니다.
        /// </summary>

        public int jumpMaxCount
        {
            get => _jumpMaxCount;
            set => _jumpMaxCount = Mathf.Max(1, value);
        }

        /// <summary>
        /// 점프 시 초기 속도 (즉각적인 수직 속도)입니다.
        /// </summary>

        public float jumpImpulse
        {
            get => _jumpImpulse;
            set => _jumpImpulse = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 점프를 유지할 최대 시간 (초)입니다. 예: 가변 높이 점프.
        /// </summary>

        public float jumpMaxHoldTime
        {
            get => _jumpMaxHoldTime;
            set => _jumpMaxHoldTime = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 지면에 닿기 전에 점프를 트리거할 수 있는 최대 시간 (초)입니다.
        /// </summary>

        public float jumpMaxPreGroundedTime
        {
            get => _jumpMaxPreGroundedTime;
            set => _jumpMaxPreGroundedTime = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 지면에서 떨어진 후 점프를 트리거할 수 있는 최대 시간 (초)입니다.
        /// </summary>

        public float jumpMaxPostGroundedTime
        {
            get => _jumpMaxPostGroundedTime;
            set => _jumpMaxPostGroundedTime = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 플레이어가 점프 입력을 누른 시간 (초)입니다.
        /// </summary>

        public float jumpInputHoldTime
        {
            get => _jumpInputHoldTime;
            protected set => _jumpInputHoldTime = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// jumpMaxHoldTime > 0인 경우 점프 힘이 남아 있는 시간입니다.
        /// </summary>

        public float jumpForceTimeRemaining
        {
            get => _jumpForceTimeRemaining;
            protected set => _jumpForceTimeRemaining = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 수행된 점프 횟수를 추적합니다.
        /// </summary>

        public int jumpCurrentCount
        {
            get => _jumpCurrentCount;
            protected set => _jumpCurrentCount = Mathf.Max(0, value);
        }

        /// <summary>
        /// 점프 최고점을 알릴지 여부입니다.
        /// OnReachedJumpApex 이벤트를 수신하려면 true로 설정합니다.
        /// </summary>

        public bool notifyJumpApex { get; set; }

        /// <summary>
        /// 점프 입력이 눌렸습니까?
        /// </summary>

        public bool jumpInputPressed { get; protected set; }

        /// <summary>
        /// 최대 비행 속도입니다.
        /// </summary>

        public float maxFlySpeed
        {
            get => _maxFlySpeed;
            set => _maxFlySpeed = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 비행 중 가속하지 않을 때의 감속도입니다.
        /// </summary>

        public float brakingDecelerationFlying
        {
            get => _brakingDecelerationFlying;
            set => _brakingDecelerationFlying = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 비행 중 이동에 적용할 마찰력입니다.
        /// </summary>

        public float flyingFriction
        {
            get => _flyingFriction;
            set => _flyingFriction = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 최대 수영 속도입니다.
        /// </summary>

        public float maxSwimSpeed
        {
            get => _maxSwimSpeed;
            set => _maxSwimSpeed = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 수영 중 가속하지 않을 때의 감속도입니다.
        /// </summary>

        public float brakingDecelerationSwimming
        {
            get => _brakingDecelerationSwimming;
            set => _brakingDecelerationSwimming = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 수영 중 이동에 적용할 마찰력입니다.
        /// </summary>

        public float swimmingFriction
        {
            get => _swimmingFriction;
            set => _swimmingFriction = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 물의 부력 비율입니다. 1 = 중립 부력, 0 = 부력 없음.
        /// </summary>

        public float buoyancy
        {
            get => _buoyancy;
            set => _buoyancy = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 별도의 브레이크 마찰을 사용할지 여부입니다.
        /// </summary>

        public bool useSeparateBrakingFriction
        {
            get => _useSeparateBrakingFriction;
            set => _useSeparateBrakingFriction = value;
        }

        /// <summary>
        /// 브레이크 시 적용되는 마찰 (드래그) 계수입니다 (가속 = 0이거나 캐릭터가 최대 속도를 초과하는 경우).
        /// useSeparateBrakingFriction이 true인 경우 모든 이동 모드에서 사용되는 값입니다.
        /// </summary>

        public float brakingFriction
        {
            get => _brakingFriction;
            set => _brakingFriction = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 별도의 브레이크 감속을 사용할지 여부입니다.
        /// </summary>

        public bool useSeparateBrakingDeceleration
        {
            get => _useSeparateBrakingDeceleration;
            set => _useSeparateBrakingDeceleration = value;
        }

        /// <summary>
        /// 가속하지 않을 때의 감속도입니다.
        /// 이는 속도를 일정 값만큼 직접 줄이는 일정한 반대 힘입니다.
        /// useSeparateBrakingDeceleration이 true인 경우 모든 이동 모드에서 사용되는 값입니다.
        /// </summary>

        public float brakingDeceleration
        {
            get => _brakingDeceleration;
            set => _brakingDeceleration = value;
        }

        /// <summary>
        /// 캐릭터의 중력 (gravityScale로 수정됨). 기본값은 Physics.gravity입니다.
        /// </summary>

        public Vector3 gravity
        {
            get => _gravity * _gravityScale;
            set => _gravity = value;
        }

        /// <summary>
        /// 이 객체가 중력에 영향을 받는 정도입니다.
        /// 중력 방향을 변경할 수 있도록 음수로 설정할 수 있습니다.
        /// </summary>

        public float gravityScale
        {
            get => _gravityScale;
            set => _gravityScale = value;
        }

        /// <summary>
        /// 애니메이션이 캐릭터의 이동을 결정해야 합니까?
        /// </summary>

        public bool useRootMotion
        {
            get => _useRootMotion;
            set => _useRootMotion = value;
        }

        /// <summary>
        /// 활성화된 경우, 플레이어가 동적 리지드바디와 상호작용할 때 영향을 받습니다.
        /// </summary>

        public bool enablePhysicsInteraction
        {
            get => _enablePhysicsInteraction;
            set
            {
                _enablePhysicsInteraction = value;

                if (_characterMovement)
                    _characterMovement.enablePhysicsInteraction = _enablePhysicsInteraction;
            }
        }

        /// <summary>
        /// 걷는 동안 다른 캐릭터와 충돌할 때 밀기 힘을 적용해야 합니까?
        /// </summary>

        public bool applyPushForceToCharacters
        {
            get => _applyPushForceToCharacters;
            set
            {
                _applyPushForceToCharacters = value;

                if (_characterMovement)
                    _characterMovement.physicsInteractionAffectsCharacters = _applyPushForceToCharacters;
            }
        }

        /// <summary>
        /// 서 있는 동안 리지드바디에 아래로 향하는 힘을 적용해야 합니까?
        /// </summary>

        public bool applyStandingDownwardForce
        {
            get => _applyStandingDownwardForce;
            set => _applyStandingDownwardForce = value;
        }

        /// <summary>
        /// 이 캐릭터의 질량 (kg 단위)입니다.
        /// </summary>

        public float mass
        {
            get => _mass;
            set
            {
                _mass = Mathf.Max(1e-07f, value);

                if (_characterMovement && _characterMovement.rigidbody)
                    _characterMovement.rigidbody.mass = _mass;
            }
        }

        /// <summary>
        /// 걷는 동안 리지드바디에 적용되는 힘은 (질량과 상대 속도에 따라) 이 값으로 스케일됩니다.
        /// </summary>

        public float pushForceScale
        {
            get => _pushForceScale;
            set
            {
                _pushForceScale = Mathf.Max(0.0f, value);

                if (_characterMovement)
                    _characterMovement.pushForceScale = _pushForceScale;
            }
        }

        /// <summary>
        /// 서 있는 동안 리지드바디에 적용되는 힘 (질량과 중력에 따라) 이 값으로 스케일됩니다.
        /// </summary>

        public float standingDownwardForceScale
        {
            get => _standingDownwardForceScale;
            set => _standingDownwardForceScale = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// true인 경우 플랫폼에서 점프하거나 떨어질 때 플랫폼의 속도를 적용합니다.
        /// </summary>

        public bool impartPlatformVelocity
        {
            get => _impartPlatformVelocity;
            set
            {
                _impartPlatformVelocity = value;

                if (_characterMovement)
                    _characterMovement.impartPlatformVelocity = _impartPlatformVelocity;
            }
        }

        /// <summary>
        /// 캐릭터가 서 있는 플랫폼이 움직일 때 캐릭터도 움직입니까?
        /// true인 경우 캐릭터도 플랫폼과 함께 움직입니다.
        /// </summary>

        public bool impartPlatformMovement
        {
            get => _impartPlatformMovement;
            set
            {
                _impartPlatformMovement = value;

                if (_characterMovement)
                    _characterMovement.impartPlatformMovement = _impartPlatformMovement;
            }
        }

        /// <summary>
        /// 캐릭터가 서 있는 플랫폼의 회전 변화를 받습니까?
        /// true인 경우 캐릭터도 플랫폼과 함께 회전합니다.
        /// </summary>

        public bool impartPlatformRotation
        {
            get => _impartPlatformRotation;
            set
            {
                _impartPlatformRotation = value;

                if (_characterMovement)
                    _characterMovement.impartPlatformRotation = _impartPlatformRotation;
            }
        }

        /// <summary>
        /// 캐릭터의 현재 위치 (읽기 전용)
        /// SetPosition 메서드를 사용하여 수정합니다. 
        /// </summary>

        public Vector3 position => characterMovement.position;

        /// <summary>
        /// 캐릭터의 현재 위치 (읽기 전용).
        /// SetRotation 메서드를 사용하여 수정합니다. 
        /// </summary>

        public Quaternion rotation => characterMovement.rotation;

        /// <summary>
        /// 캐릭터의 현재 속도 (읽기 전용).
        /// SetVelocity 메서드를 사용하여 수정합니다. 
        /// </summary>

        public Vector3 velocity => characterMovement.velocity;

        /// <summary>
        /// 캐릭터의 현재 속도입니다.
        /// </summary>

        public float speed => characterMovement.velocity.magnitude;

        /// <summary>
        /// 캐릭터의 현재 반지름 (읽기 전용).
        /// CharacterMovement SetDimensions 메서드를 사용하여 수정합니다. 
        /// </summary>

        public float radius => characterMovement.radius;

        /// <summary>
        /// 캐릭터의 현재 높이 (읽기 전용).
        /// CharacterMovement SetDimensions 메서드를 사용하여 수정합니다. 
        /// </summary>

        public float height => characterMovement.height;

        /// <summary>
        /// 캐릭터의 현재 이동 모드 (읽기 전용).
        /// SetMovementMode 메서드를 사용하여 수정합니다.
        /// </summary>

        public MovementMode movementMode => _movementMode;

        /// <summary>
        /// 캐릭터의 사용자 정의 이동 모드 (하위 모드).
        /// _movementMode == Custom인 경우에만 적용됩니다 (읽기 전용).
        /// SetMovementMode 메서드를 사용하여 수정합니다.
        /// </summary>

        public int customMovementMode => _customMovementMode;

        /// <summary>
        /// 이 구성 요소와 겹치는 PhysicsVolume. 없음인 경우 NULL입니다.
        /// </summary>

        public PhysicsVolume physicsVolume { get; protected set; }

        /// <summary>
        /// true인 경우 LateFixedUpdate 코루틴을 활성화하여 이 캐릭터를 시뮬레이션합니다.
        /// false인 경우 이 캐릭터를 시뮬레이션하려면 Simulate 메서드를 호출해야 합니다.
        /// 기본적으로 활성화되어 있습니다.
        /// </summary>

        public bool enableAutoSimulation
        {
            get => _enableAutoSimulation;
            set
            {
                _enableAutoSimulation = value;
                EnableAutoSimulationCoroutine(_enableAutoSimulation);
            }
        }

        // 캐릭터가 일시 정지 상태입니까?

        public bool isPaused { get; private set; }

        #endregion

        #region EVENTS

        public delegate void PhysicsVolumeChangedEventHandler(PhysicsVolume newPhysicsVolume);

        public delegate void MovementModeChangedEventHandler(MovementMode prevMovementMode, int prevCustomMode);
        public delegate void CustomMovementModeUpdateEventHandler(float deltaTime);

        public delegate void CustomRotationModeUpdateEventHandler(float deltaTime);

        public delegate void BeforeSimulationUpdateEventHandler(float deltaTime);
        public delegate void AfterSimulationUpdateEventHandler(float deltaTime);
        public delegate void CharacterMovementUpdateEventHandler(float deltaTime);

        public delegate void CollidedEventHandler(ref CollisionResult collisionResult);
        public delegate void FoundGroundEventHandler(ref FindGroundResult foundGround);
        public delegate void LandedEventHandled(Vector3 landingVelocity);

        public delegate void CrouchedEventHandler();
        public delegate void UnCrouchedEventHandler();

        public delegate void JumpedEventHandler();
        public delegate void ReachedJumpApexEventHandler();

        /// <summary>
        /// 캐릭터가 PhysicsVolume에 들어가거나 떠날 때 트리거되는 이벤트입니다.
        /// </summary>

        public event PhysicsVolumeChangedEventHandler PhysicsVolumeChanged;

        /// <summary>
        /// MovementMode 변경 시 트리거되는 이벤트입니다.
        /// </summary>

        public event MovementModeChangedEventHandler MovementModeChanged;

        /// <summary>
        /// 사용자 정의 캐릭터 이동 모드를 구현하기 위한 이벤트입니다.
        /// MovementMode가 Custom으로 설정된 경우 호출됩니다.
        /// </summary>

        public event CustomMovementModeUpdateEventHandler CustomMovementModeUpdated;

        /// <summary>
        /// 사용자 정의 캐릭터 회전 모드를 구현하기 위한 이벤트입니다.
        /// RotationMode가 Custom으로 설정된 경우 호출됩니다.
        /// </summary>

        public event CustomRotationModeUpdateEventHandler CustomRotationModeUpdated;

        /// <summary>
        /// 캐릭터 시뮬레이션 업데이트 전에 호출되는 이벤트입니다.
        /// 이 '후크'를 사용하여 외부에서 캐릭터 '상태'를 업데이트할 수 있습니다.
        /// </summary>

        public event BeforeSimulationUpdateEventHandler BeforeSimulationUpdated;

        /// <summary>
        /// 캐릭터 시뮬레이션 업데이트 후 호출되는 이벤트입니다.
        /// 이 '후크'를 사용하여 외부에서 캐릭터 '상태'를 업데이트할 수 있습니다.
        /// </summary>

        public event AfterSimulationUpdateEventHandler AfterSimulationUpdated;

        /// <summary>
        /// CharacterMovement 구성 요소가 업데이트될 때 (예: Move 호출) 호출되는 이벤트입니다.
        /// 이 시점에서 캐릭터 이동이 완료되고 상태가 최신입니다. 
        /// 이 '후크'를 사용하여 외부에서 캐릭터 '상태'를 업데이트할 수 있습니다.
        /// </summary>

        public event CharacterMovementUpdateEventHandler CharacterMovementUpdated;

        /// <summary>
        /// 캐릭터가 Move 중 다른 캐릭터와 충돌할 때 트리거되는 이벤트입니다.
        /// 여러 번 호출될 수 있습니다.
        /// </summary>

        public event CollidedEventHandler Collided;

        /// <summary>
        /// 캐릭터가 다운캐스트 스윕 결과로 지면 (걸을 수 있는 지면 또는 걸을 수 없는 지면)을 찾을 때 트리거되는 이벤트입니다 (예: FindGround 메서드).
        /// </summary>

        public event FoundGroundEventHandler FoundGround;

        /// <summary>
        /// 캐릭터가 떨어지면서 걸을 수 있는 지면을 찾을 때 트리거되는 이벤트입니다 (예: FindGround 메서드).
        /// </summary>

        public event LandedEventHandled Landed;

        /// <summary>
        /// 캐릭터가 웅크리기 상태에 들어갈 때 트리거되는 이벤트입니다.
        /// </summary>

        public event CrouchedEventHandler Crouched;

        /// <summary>
        /// 캐릭터가 웅크리기 상태에서 벗어날 때 트리거되는 이벤트입니다.
        /// </summary>

        public event UnCrouchedEventHandler UnCrouched;

        /// <summary>
        /// 캐릭터가 점프를 성공적으로 트리거했을 때 호출됩니다.
        /// </summary>

        public event JumpedEventHandler Jumped;

        /// <summary>
        /// 캐릭터가 점프 최고점에 도달했을 때 (예: 수직 속도가 양수에서 음수로 변할 때) 호출됩니다.
        /// notifyJumpApex == true인 경우에만 트리거됩니다.
        /// </summary>

        public event ReachedJumpApexEventHandler ReachedJumpApex;

        /// <summary>
        /// 사용자 정의 캐릭터 이동 모드를 구현하기 위한 이벤트입니다.
        /// MovementMode가 Custom으로 설정된 경우 호출됩니다.
        /// 파생된 캐릭터 클래스는 CustomMovementMode 메서드를 재정의해야 합니다. 
        /// </summary>

        protected virtual void OnCustomMovementMode(float deltaTime)
        {
            // 이벤트 트리거

            CustomMovementModeUpdated?.Invoke(deltaTime);
        }

        /// <summary>
        /// 사용자 정의 캐릭터 회전 모드를 구현하기 위한 이벤트입니다.
        /// RotationMode가 Custom으로 설정된 경우 호출됩니다.
        /// 파생된 캐릭터 클래스는 CustomRotationMode 메서드를 재정의해야 합니다. 
        /// </summary>

        protected virtual void OnCustomRotationMode(float deltaTime)
        {
            CustomRotationModeUpdated?.Invoke(deltaTime);
        }

        /// <summary>
        /// 캐릭터 시뮬레이션 업데이트 시작 시 호출됩니다. 현재 이동 모드 업데이트 전입니다.
        /// 이 '후크'를 사용하여 외부에서 캐릭터 '상태'를 업데이트할 수 있습니다.
        /// </summary>

        protected virtual void OnBeforeSimulationUpdate(float deltaTime)
        {
            BeforeSimulationUpdated?.Invoke(deltaTime);
        }

        /// <summary>
        /// 현재 이동 모드 업데이트 후 호출됩니다.
        /// 이 '후크'를 사용하여 외부에서 캐릭터 '상태'를 업데이트할 수 있습니다. 
        /// </summary>

        protected virtual void OnAfterSimulationUpdate(float deltaTime)
        {
            AfterSimulationUpdated?.Invoke(deltaTime);
        }

        /// <summary>
        /// CharacterMovement 구성 요소가 업데이트될 때 (예: Move 호출) 호출되는 이벤트입니다.
        /// 이 시점에서 캐릭터 이동이 완료되고 상태가 최신입니다. 
        /// 이 '후크'를 사용하여 외부에서 캐릭터 '상태'를 업데이트할 수 있습니다.
        /// </summary>

        protected virtual void OnCharacterMovementUpdated(float deltaTime)
        {
            CharacterMovementUpdated?.Invoke(deltaTime);
        }

        /// <summary>
        /// 캐릭터가 Move 중 다른 캐릭터와 충돌할 때 트리거되는 이벤트입니다.
        /// 여러 번 호출될 수 있습니다.
        /// </summary>

        protected virtual void OnCollided(ref CollisionResult collisionResult)
        {
            Collided?.Invoke(ref collisionResult);
        }

        /// <summary>
        /// 캐릭터가 다운캐스트 스윕 결과로 지면 (걸을 수 있는 지면 또는 걸을 수 없는 지면)을 찾을 때 트리거되는 이벤트입니다 (예: FindGround 메서드).
        /// </summary>

        protected virtual void OnFoundGround(ref FindGroundResult foundGround)
        {
            FoundGround?.Invoke(ref foundGround);
        }

        /// <summary>
        /// 캐릭터가 걷는 이동 모드에 들어갈 때 (예: isOnWalkableGround 및 isConstrainedToGround) 트리거되는 이벤트입니다.
        /// </summary>

        protected virtual void OnLanded(Vector3 landingVelocity)
        {
            Landed?.Invoke(landingVelocity);
        }

        /// <summary>
        /// 캐릭터가 웅크리기 상태에 들어갈 때 호출됩니다.
        /// </summary>

        protected virtual void OnCrouched()
        {
            Crouched?.Invoke();
        }

        /// <summary>
        /// 캐릭터가 웅크리기 상태에서 벗어날 때 호출됩니다.
        /// </summary>

        protected virtual void OnUnCrouched()
        {
            UnCrouched?.Invoke();
        }

        /// <summary>
        /// 점프가 성공적으로 트리거되었을 때 호출됩니다.
        /// </summary>

        protected virtual void OnJumped()
        {
            Jumped?.Invoke();
        }

        /// <summary>
        /// 캐릭터가 점프 최고점에 도달했을 때 (예: 수직 속도가 양수에서 음수로 변할 때) 호출됩니다.
        /// notifyJumpApex == true인 경우에만 트리거됩니다.
        /// </summary>

        protected virtual void OnReachedJumpApex()
        {
            ReachedJumpApex?.Invoke();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// 캐릭터의 중력 벡터를 gravityScale로 수정한 값을 반환합니다.
        /// </summary>

        public Vector3 GetGravityVector()
        {
            return gravity;
        }

        /// <summary>
        /// 중력 방향 (정규화된 벡터)을 반환합니다.
        /// </summary>

        public Vector3 GetGravityDirection()
        {
            return gravity.normalized;
        }

        /// <summary>
        /// 현재 중력 규모를 고려한 중력 크기를 반환합니다.
        /// </summary>

        public float GetGravityMagnitude()
        {
            return gravity.magnitude;
        }

        /// <summary>
        /// 캐릭터의 중력 벡터를 설정합니다.
        /// </summary>

        public void SetGravityVector(Vector3 newGravityVector)
        {
            _gravity = newGravityVector;
        }

        /// <summary>
        /// Auto-simulation 코루틴 (예: LateFixedUpdate)을 시작/중지합니다.
        /// </summary>

        private void EnableAutoSimulationCoroutine(bool enable)
        {
            if (enable)
            {
                if (_lateFixedUpdateCoroutine != null)
                    StopCoroutine(_lateFixedUpdateCoroutine);

                _lateFixedUpdateCoroutine = StartCoroutine(LateFixedUpdate());
            }
            else
            {
                if (_lateFixedUpdateCoroutine != null)
                    StopCoroutine(_lateFixedUpdateCoroutine);
            }
        }

        /// <summary>
        /// 사용된 구성 요소를 캐시합니다.
        /// </summary>

        protected virtual void CacheComponents()
        {
            _transform = GetComponent<Transform>();
            _characterMovement = GetComponent<CharacterMovement>();
            _animator = GetComponentInChildren<Animator>();
            _rootMotionController = GetComponentInChildren<RootMotionController>();

            {
                characterMovement.impartPlatformMovement = _impartPlatformMovement;
                characterMovement.impartPlatformRotation = _impartPlatformRotation;
                characterMovement.impartPlatformVelocity = _impartPlatformVelocity;

                characterMovement.enablePhysicsInteraction = _enablePhysicsInteraction;
                characterMovement.physicsInteractionAffectsCharacters = _applyPushForceToCharacters;
                characterMovement.pushForceScale = _pushForceScale;

                mass = _mass;
            }
        }

        /// <summary>
        /// 주어진 새로운 볼륨을 현재 Physics Volume으로 설정합니다.
        /// PhysicsVolumeChanged 이벤트를 트리거합니다.
        /// </summary>

        protected virtual void SetPhysicsVolume(PhysicsVolume newPhysicsVolume)
        {
            // 변경 사항이 없으면 아무 것도 하지 않음

            if (newPhysicsVolume == physicsVolume)
                return;

            // PhysicsVolumeChanged 이벤트 트리거

            OnPhysicsVolumeChanged(newPhysicsVolume);

            // 현재 물리 볼륨 업데이트

            physicsVolume = newPhysicsVolume;
        }

        /// <summary>
        /// 이 캐릭터의 PhysicsVolume이 변경되었을 때 호출됩니다.
        /// </summary>

        protected virtual void OnPhysicsVolumeChanged(PhysicsVolume newPhysicsVolume)
        {
            if (newPhysicsVolume && newPhysicsVolume.waterVolume)
            {
                // 물 볼륨에 진입

                SetMovementMode(MovementMode.Swimming);
            }
            else if (IsInWaterPhysicsVolume() && newPhysicsVolume == null)
            {
                // 물 볼륨을 떠남

                // Swimming 상태인 경우 Falling 모드로 변경

                if (IsSwimming())
                {
                    SetMovementMode(MovementMode.Falling);
                }
            }

            // PhysicsVolumeChanged 이벤트 트리거

            PhysicsVolumeChanged?.Invoke(newPhysicsVolume);
        }

        /// <summary>
        /// 캐릭터의 현재 물리 볼륨을 업데이트합니다.
        /// </summary>

        protected virtual void UpdatePhysicsVolume(PhysicsVolume newPhysicsVolume)
        {
            // 캐릭터가 PhysicsVolume 안에 있는지 또는 외부에 있는지 확인합니다.
            // 캐릭터의 중심을 기준으로 사용됩니다.

            Vector3 characterCenter = characterMovement.worldCenter;

            if (newPhysicsVolume && newPhysicsVolume.boxCollider.ClosestPoint(characterCenter) == characterCenter)
            {
                // 물리 볼륨에 진입

                SetPhysicsVolume(newPhysicsVolume);
            }
            else
            {
                // 물리 볼륨을 떠남

                SetPhysicsVolume(null);
            }
        }

        /// <summary>
        /// 볼륨 목록에 새 물리 볼륨을 추가하려고 시도합니다.
        /// </summary>

        protected virtual void AddPhysicsVolume(Collider other)
        {
            if (other.TryGetComponent(out PhysicsVolume volume) && !_physicsVolumes.Contains(volume))
                _physicsVolumes.Insert(0, volume);
        }

        /// <summary>
        /// 볼륨 목록에서 물리 볼륨을 제거하려고 시도합니다.
        /// </summary>

        protected virtual void RemovePhysicsVolume(Collider other)
        {
            if (other.TryGetComponent(out PhysicsVolume volume) && _physicsVolumes.Contains(volume))
                _physicsVolumes.Remove(volume);
        }

        /// <summary>
        /// 우선순위가 높은 볼륨을 현재 물리 볼륨으로 설정합니다.
        /// </summary>

        protected virtual void UpdatePhysicsVolumes()
        {
            // 우선순위가 높은 볼륨 찾기

            PhysicsVolume volume = null;
            int maxPriority = int.MinValue;

            for (int i = 0, c = _physicsVolumes.Count; i < c; i++)
            {
                PhysicsVolume vol = _physicsVolumes[i];
                if (vol.priority <= maxPriority)
                    continue;

                maxPriority = vol.priority;
                volume = vol;
            }

            // 캐릭터의 현재 볼륨 업데이트

            UpdatePhysicsVolume(volume);
        }

        /// <summary>
        /// 캐릭터가 물 물리 볼륨에 있는지 확인합니다.
        /// </summary>

        public virtual bool IsInWaterPhysicsVolume()
        {
            return physicsVolume && physicsVolume.waterVolume;
        }

        /// <summary>
        /// 캐릭터에 힘을 추가합니다.
        /// 이 힘은 축적되어 Move 메서드 호출 시 적용됩니다.
        /// </summary>

        public void AddForce(Vector3 force, ForceMode forceMode = ForceMode.Force)
        {
            characterMovement.AddForce(force, forceMode);
        }

        /// <summary>
        /// 폭발 효과를 시뮬레이트하기 위해 리지드바디에 힘을 적용합니다.
        /// 폭발은 특정 중심 위치와 반경을 가진 구체로 모델링됩니다. 일반적으로 구체 외부의 것은 폭발에 영향을 받지 않으며 중심에서의 거리에 비례하여 힘이 감소합니다.
        /// 그러나 반경에 대해 0의 값을 전달하면 중심이 리지드바디에서 얼마나 떨어져 있는지에 관계없이 전체 힘이 적용됩니다.
        /// </summary>

        public void AddExplosionForce(float forceMagnitude, Vector3 origin, float explosionRadius,
            ForceMode forceMode = ForceMode.Force)
        {
            characterMovement.AddExplosionForce(forceMagnitude, origin, explosionRadius, forceMode);
        }

        /// <summary>
        /// 캐릭터에 대해 보류 중인 발사 속도를 설정합니다. 이 속도는 다음 Move 호출 시 처리됩니다.
        /// overrideVerticalVelocity가 true인 경우 캐릭터의 수직 구성 요소를 대체합니다.
        /// overrideLateralVelocity가 true인 경우 캐릭터의 XY 부분을 대체합니다.
        /// </summary>

        public void LaunchCharacter(Vector3 launchVelocity, bool overrideVerticalVelocity = false,
            bool overrideLateralVelocity = false)
        {
            characterMovement.LaunchCharacter(launchVelocity, overrideVerticalVelocity, overrideLateralVelocity);
        }

        /// <summary>
        /// 충돌 감지가 활성화되어야 합니까?
        /// </summary>

        public void DetectCollisions(bool detectCollisions)
        {
            characterMovement.detectCollisions = detectCollisions;
        }

        /// <summary>
        /// 캐릭터가 otherCollider와의 모든 충돌을 무시하도록 합니다.
        /// </summary>

        public void IgnoreCollision(Collider otherCollider, bool ignore = true)
        {
            characterMovement.IgnoreCollision(otherCollider, ignore);
        }

        /// <summary>
        /// 캐릭터가 otherRigidbody에 부착된 모든 콜라이더와의 충돌을 무시하도록 합니다.
        /// </summary>

        public void IgnoreCollision(Rigidbody otherRigidbody, bool ignore = true)
        {
            characterMovement.IgnoreCollision(otherRigidbody, ignore);
        }

        /// <summary>
        /// 캐릭터의 콜라이더 (예: 캡슐 콜라이더)가 otherCollider와의 모든 충돌을 무시하도록 합니다.
        /// 주의: 충돌 레이어 마스크에 있는 경우 다른 Move 호출 중에 충돌할 수 있습니다.
        /// </summary>

        public void CapsuleIgnoreCollision(Collider otherCollider, bool ignore = true)
        {
            characterMovement.CapsuleIgnoreCollision(otherCollider, ignore);
        }

        /// <summary>
        /// 지면 제약을 일시적으로 비활성화하여 캐릭터가 자유롭게 지면을 떠날 수 있도록 합니다.
        /// 예: LaunchCharacter, Jump 등.
        /// </summary>

        public void PauseGroundConstraint(float seconds = 0.1f)
        {
            characterMovement.PauseGroundConstraint(seconds);
        }

        /// <summary>
        /// 걷을 수 있는 지면에 있을 때 이동을 지면에 제약해야 합니까?
        /// 활성화된 경우 캐릭터는 수직 속도를 무시하고 지면에 제약됩니다.  
        /// </summary>

        public void EnableGroundConstraint(bool enable)
        {
            characterMovement.constrainToGround = enable;
        }

        /// <summary>
        /// 마지막 Move 호출에서 캐릭터가 지면에 있었습니까?
        /// </summary>

        public bool WasOnGround()
        {
            return characterMovement.wasOnGround;
        }

        /// <summary>
        /// 캐릭터가 지면에 있습니까?
        /// </summary>

        public bool IsOnGround()
        {
            return characterMovement.isOnGround;
        }

        /// <summary>
        /// 마지막 Move 호출에서 캐릭터가 걷을 수 있는 지면에 있었습니까?
        /// </summary>

        public bool WasOnWalkableGround()
        {
            return characterMovement.wasOnWalkableGround;
        }

        /// <summary>
        /// 캐릭터가 걷을 수 있는 지면에 있습니까?
        /// </summary>

        public bool IsOnWalkableGround()
        {
            return characterMovement.isOnWalkableGround;
        }

        /// <summary>
        /// 마지막 Move 호출에서 캐릭터가 걷을 수 있는 지면에 있고 지면에 제약되었습니까?
        /// </summary>

        public bool WasGrounded()
        {
            return characterMovement.wasGrounded;
        }

        /// <summary>
        /// 캐릭터가 걷을 수 있는 지면에 있고 지면에 제약되어 있습니까?
        /// </summary>

        public bool IsGrounded()
        {
            return characterMovement.isGrounded;
        }

        /// <summary>
        /// CharacterMovement 구성 요소를 반환합니다. null이 아님이 보장됩니다.
        /// </summary>

        public CharacterMovement GetCharacterMovement()
        {
            return characterMovement;
        }

        /// <summary>
        /// Animator 구성 요소를 반환하거나 찾을 수 없는 경우 null을 반환합니다.
        /// </summary>

        public Animator GetAnimator()
        {
            return animator;
        }

        /// <summary>
        /// RootMotionController를 반환하거나 찾을 수 없는 경우 null을 반환합니다.
        /// </summary>

        public RootMotionController GetRootMotionController()
        {
            return rootMotionController;
        }

        /// <summary>
        /// 캐릭터의 현재 PhysicsVolume을 반환합니다. 없을 경우 null입니다.
        /// </summary>

        public PhysicsVolume GetPhysicsVolume()
        {
            return physicsVolume;
        }

        /// <summary>
        /// 캐릭터의 현재 위치입니다.
        /// </summary>

        public Vector3 GetPosition()
        {
            return characterMovement.position;
        }

        /// <summary>
        /// 캐릭터의 위치를 설정합니다.
        /// 이것은 보간을 준수하여 두 위치 사이의 매 프레임에 부드러운 전환을 제공합니다.
        /// </summary>

        public void SetPosition(Vector3 position, bool updateGround = false)
        {
            characterMovement.SetPosition(position, updateGround);
        }

        /// <summary>
        /// 캐릭터의 위치를 즉시 수정합니다.
        /// SetPosition과 달리 이 메서드는 리지드바디 보간을 비활성화한 후 캐릭터의 위치를 업데이트합니다 (interpolating == true). 이는 즉각적인 움직임을 초래합니다.
        /// interpolating == true인 경우, 텔레포트 후 리지드바디 보간을 다시 활성화합니다.
        /// </summary>

        public void TeleportPosition(Vector3 newPosition, bool interpolating = true, bool updateGround = false)
        {
            if (interpolating)
            {
                characterMovement.interpolation = RigidbodyInterpolation.None;
            }

            characterMovement.SetPosition(newPosition, updateGround);

            if (interpolating)
            {
                characterMovement.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        /// <summary>
        /// 캐릭터의 현재 회전입니다.
        /// </summary>

        public Quaternion GetRotation()
        {
            return characterMovement.rotation;
        }

        /// <summary>
        /// 캐릭터의 현재 회전을 설정합니다.
        /// </summary>

        public void SetRotation(Quaternion newRotation)
        {
            characterMovement.rotation = newRotation;
        }

        /// <summary>
        /// 캐릭터의 회전을 즉시 수정합니다.
        /// SetRotation과 달리 이 메서드는 리지드바디 보간을 비활성화한 후 캐릭터의 회전을 업데이트합니다 (interpolating == true). 이는 즉각적인 회전을 초래합니다.
        /// interpolating == true인 경우, 텔레포트 후 리지드바디 보간을 다시 활성화합니다.
        /// </summary>

        public void TeleportRotation(Quaternion newRotation, bool interpolating = true)
        {
            if (interpolating)
            {
                characterMovement.interpolation = RigidbodyInterpolation.None;
            }

            characterMovement.SetRotation(newRotation);

            if (interpolating)
            {
                characterMovement.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        /// <summary>
        /// 캐릭터의 현재 업 벡터입니다.
        /// </summary>

        public virtual Vector3 GetUpVector()
        {
            return transform.up;
        }

        /// <summary>
        /// 캐릭터의 현재 오른쪽 벡터입니다.
        /// </summary>

        public virtual Vector3 GetRightVector()
        {
            return transform.right;
        }

        /// <summary>
        /// 캐릭터의 현재 앞쪽 벡터입니다.
        /// </summary>

        public virtual Vector3 GetForwardVector()
        {
            return transform.forward;
        }

        /// <summary>
        /// 회전율을 사용하여 캐릭터를 지정된 방향으로 회전시킵니다.
        /// updateYawOnly가 true인 경우 회전은 캐릭터의 요 축에만 영향을 미칩니다.
        /// </summary>

        public virtual void RotateTowards(Vector3 worldDirection, float deltaTime, bool updateYawOnly = true)
        {
            Vector3 characterUp = GetUpVector();

            if (updateYawOnly)
                worldDirection = Vector3.ProjectOnPlane(worldDirection, characterUp);

            if (worldDirection == Vector3.zero)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(worldDirection, characterUp);
            characterMovement.rotation = Quaternion.RotateTowards(rotation, targetRotation, rotationRate * deltaTime);
        }

        /// <summary>
        /// 루트 모션 회전을 캐릭터 회전에 추가합니다.
        /// </summary>

        protected virtual void RotateWithRootMotion()
        {
            if (useRootMotion && rootMotionController)
                characterMovement.rotation = rootMotionController.ConsumeRootMotionRotation() * characterMovement.rotation;
        }

        /// <summary>
        /// 캐릭터의 현재 상대 속도입니다.
        /// 이 속도는 상대적이므로 이 외부에서 발생하는 변환에 대한 움직임을 추적하지 않습니다,
        /// 예: 다른 움직이는 변환 하에 부모가 된 캐릭터, 움직이는 차량 등.
        /// </summary>

        public Vector3 GetVelocity()
        {
            return characterMovement.velocity;
        }

        /// <summary>
        /// 캐릭터의 속도를 설정합니다.
        /// </summary>

        public void SetVelocity(Vector3 newVelocity)
        {
            characterMovement.velocity = newVelocity;
        }

        /// <summary>
        /// 캐릭터의 현재 속도입니다.
        /// </summary>

        public float GetSpeed()
        {
            return characterMovement.velocity.magnitude;
        }

        /// <summary>
        /// 캐릭터의 반지름입니다.
        /// </summary>

        public float GetRadius()
        {
            return characterMovement.radius;
        }

        /// <summary>
        /// 캐릭터의 현재 높이입니다.
        /// </summary>

        public float GetHeight()
        {
            return characterMovement.height;
        }

        /// <summary>
        /// 현재 이동 모드에 대한 움직임 방향 (월드 공간에서)입니다.
        /// </summary>

        public Vector3 GetMovementDirection()
        {
            return _movementDirection;
        }

        /// <summary>
        /// 캐릭터의 이동 방향 (월드 공간에서)을 할당합니다. 예: 우리가 원하는 이동 방향 벡터.
        /// </summary>

        public void SetMovementDirection(Vector3 movementDirection)
        {
            _movementDirection = movementDirection;
        }

        /// <summary>
        /// 요 값을 설정합니다.
        /// 현재 피치 및 롤 값을 재설정합니다.
        /// </summary>

        public virtual void SetYaw(float value)
        {
            characterMovement.rotation = Quaternion.Euler(0.0f, value, 0.0f);
        }

        /// <summary>
        /// 요 입력값을 추가합니다 (위쪽 축).
        /// </summary>

        public virtual void AddYawInput(float value)
        {
            _rotationInput.y += value;
        }

        /// <summary>
        /// 피치 입력값을 추가합니다 (오른쪽 축).
        /// </summary>

        public virtual void AddPitchInput(float value)
        {
            _rotationInput.x += value;
        }

        /// <summary>
        /// 롤 입력값을 추가합니다 (앞쪽 축).
        /// </summary>

        public virtual void AddRollInput(float value)
        {
            _rotationInput.z += value;
        }

        /// <summary>
        /// 입력 회전 (예: AddPitchInput, AddYawInput, AddRollInput)을 캐릭터 회전에 추가합니다.
        /// </summary>

        protected virtual void ConsumeRotationInput()
        {
            // 회전 입력을 적용합니다 (있을 경우)

            if (_rotationInput != Vector3.zero)
            {
                // 회전 입력을 소비합니다 (예: 적용하고 지웁니다)

                characterMovement.rotation *= Quaternion.Euler(_rotationInput);

                _rotationInput = Vector3.zero;
            }
        }

        /// <summary>
        /// 캐릭터의 현재 이동 모드입니다.
        /// </summary>

        public MovementMode GetMovementMode()
        {
            return _movementMode;
        }

        /// <summary>
        /// 캐릭터의 사용자 정의 이동 모드 (하위 모드).
        /// _movementMode == Custom인 경우에만 적용됩니다.
        /// </summary>

        public int GetCustomMovementMode()
        {
            return _customMovementMode;
        }

        /// <summary>
        /// 이동 모드를 변경합니다.
        /// 새로운 사용자 정의 하위 모드 (newCustomMode)는 newMovementMode == Custom인 경우에만 적용됩니다.
        /// OnMovementModeChanged 이벤트를 트리거합니다.
        /// </summary>

        public void SetMovementMode(MovementMode newMovementMode, int newCustomMode = 0)
        {
            // 변경 사항이 없으면 아무 것도 하지 않음

            if (newMovementMode == _movementMode)
            {
                // 사용자 정의 하위 모드의 변경을 허용합니다

                if (newMovementMode != MovementMode.Custom || newCustomMode == _customMovementMode)
                    return;
            }

            // 이동 모드 변경 수행

            MovementMode prevMovementMode = _movementMode;
            int prevCustomMode = _customMovementMode;

            _movementMode = newMovementMode;
            _customMovementMode = newCustomMode;

            OnMovementModeChanged(prevMovementMode, prevCustomMode);
        }

        /// <summary>
        /// MovementMode가 변경된 후 호출됩니다.
        /// 특정 모드 시작 시 특수 처리를 수행합니다, 예: 지면 제약 활성화 / 비활성화 등.
        /// 재정의된 경우 base 메서드를 호출해야 합니다.
        /// </summary>

        protected virtual void OnMovementModeChanged(MovementMode prevMovementMode, int prevCustomMode)
        {
            // 모드 변경 시 추가 작업 수행

            switch (movementMode)
            {
                case MovementMode.None:

                    // None 모드로 전환...

                    // 캐릭터의 이동을 비활성화하고 보류 중인 모든 힘을 제거합니다

                    characterMovement.velocity = Vector3.zero;
                    characterMovement.ClearAccumulatedForces();

                    break;

                case MovementMode.Walking:

                    // 걷기 모드로 전환...

                    // 점프 리셋

                    ResetJumpState();

                    // 날거나 수영 중이었으면 지면 제약을 활성화합니다

                    if (prevMovementMode == MovementMode.Flying || prevMovementMode == MovementMode.Swimming)
                        characterMovement.constrainToGround = true;

                    // 착지 이벤트 트리거

                    OnLanded(characterMovement.landedVelocity);

                    break;

                case MovementMode.Falling:

                    // 낙하 모드로 전환...

                    // 날거나 수영 중이었으면 착지할 수 있도록 지면 제약을 활성화합니다

                    if (prevMovementMode == MovementMode.Flying || prevMovementMode == MovementMode.Swimming)
                        characterMovement.constrainToGround = true;

                    break;

                case MovementMode.Flying:
                case MovementMode.Swimming:

                    // 비행 또는 수영 모드로 전환...

                    // 점프 리셋

                    ResetJumpState();

                    // 지면 제약 비활성화

                    characterMovement.constrainToGround = false;

                    break;
            }

            // 낙하 모드를 벗어났을 때 낙하 타이머 리셋

            if (!IsFalling())
                _fallingTime = 0.0f;

            // 이동 모드 변경 이벤트 트리거

            MovementModeChanged?.Invoke(prevMovementMode, prevCustomMode);
        }

        /// <summary>
        /// 캐릭터가 걷기 이동 모드에 있는지 여부를 반환합니다 (예: 걷을 수 있는 지면에 있는 경우).
        /// </summary>

        public virtual bool IsWalking()
        {
            return _movementMode == MovementMode.Walking;
        }

        /// <summary>
        /// 현재 낙하 중인지 여부를 반환합니다, 예: 공중에 있는 경우 (비행하지 않음) 또는 걷을 수 없는 지면에 있는 경우.
        /// </summary>

        public virtual bool IsFalling()
        {
            return _movementMode == MovementMode.Falling;
        }

        /// <summary>
        /// 현재 비행 중인지 여부를 반환합니다 (지면에 안착하지 않고 비중력 상태에서 이동 중인 경우).
        /// </summary>

        public virtual bool IsFlying()
        {
            return _movementMode == MovementMode.Flying;
        }

        /// <summary>
        /// 현재 수영 중인지 여부를 반환합니다 (물리 볼륨을 통해 이동 중인 경우).
        /// </summary>

        public virtual bool IsSwimming()
        {
            return _movementMode == MovementMode.Swimming;
        }

        /// <summary>
        /// 현재 이동 모드에 대한 최대 속도 (웅크린 상태 포함)를 반환합니다.
        /// </summary>

        public virtual float GetMaxSpeed()
        {
            switch (_movementMode)
            {
                case MovementMode.Walking:
                    return IsCrouched() ? maxWalkSpeedCrouched : maxWalkSpeed;

                case MovementMode.Falling:
                    return maxWalkSpeed;

                case MovementMode.Swimming:
                    return maxSwimSpeed;

                case MovementMode.Flying:
                    return maxFlySpeed;

                default:
                    return 0.0f;
            }
        }

        /// <summary>
        /// 최소 아날로그 스틱 기울기에서 걷기 시 가속할 지면 속도입니다.
        /// </summary>

        public virtual float GetMinAnalogSpeed()
        {
            switch (_movementMode)
            {
                case MovementMode.Walking:
                case MovementMode.Falling:
                    return minAnalogWalkSpeed;

                default:
                    return 0.0f;
            }
        }

        /// <summary>
        /// 현재 이동 모드에 대한 가속도입니다.
        /// </summary>

        public virtual float GetMaxAcceleration()
        {
            if (IsFalling())
                return maxAcceleration * airControl;

            return maxAcceleration;
        }

        /// <summary>
        /// 현재 이동 모드에 대한 제동 감속도입니다.
        /// </summary>

        public virtual float GetMaxBrakingDeceleration()
        {
            switch (_movementMode)
            {
                case MovementMode.Walking:
                    return brakingDecelerationWalking;

                case MovementMode.Falling:
                    {
                        // 낙하 중일 때,
                        // 하지만 걸을 수 없는 지면에서는 제동 감속을 무시하여 미끄러지도록 강제합니다

                        return characterMovement.isOnGround ? 0.0f : brakingDecelerationFalling;
                    }

                case MovementMode.Swimming:
                    return brakingDecelerationSwimming;

                case MovementMode.Flying:
                    return brakingDecelerationFlying;

                default:
                    return 0.0f;
            }
        }

        /// <summary>
        /// 현재 입력 벡터와 원하는 속도를 기반으로 아날로그 입력 수정자를 계산합니다 (0.0f에서 1.0f까지).
        /// </summary>

        protected virtual float ComputeAnalogInputModifier(Vector3 desiredVelocity)
        {
            float maxSpeed = GetMaxSpeed();

            if (desiredVelocity.sqrMagnitude > 0.0f && maxSpeed > 0.00000001f)
            {
                return Mathf.Clamp01(desiredVelocity.magnitude / maxSpeed);
            }

            return 0.0f;
        }

        /// <summary>
        /// 마찰과 제동 감속을 주어진 속도에 적용합니다.
        /// 수정된 입력 속도를 반환합니다.
        /// </summary>

        public virtual Vector3 ApplyVelocityBraking(Vector3 velocity, float friction, float maxBrakingDeceleration, float deltaTime)
        {
            const float kMinTickTime = 0.000001f;
            if (velocity.isZero() || deltaTime < kMinTickTime)
                return velocity;

            bool isZeroFriction = friction == 0.0f;
            bool isZeroBraking = maxBrakingDeceleration == 0.0f;
            if (isZeroFriction && isZeroBraking)
                return velocity;

            // 멈추기 위해 제동

            Vector3 oldVel = velocity;
            Vector3 revAccel = isZeroBraking ? Vector3.zero : -maxBrakingDeceleration * velocity.normalized;

            // 낮은 프레임 레이트에서 일관된 결과를 얻기 위해 제동을 세분화

            const float kMaxTimeStep = 1.0f / 33.0f;

            float remainingTime = deltaTime;
            while (remainingTime >= kMinTickTime)
            {
                // 제로 마찰은 일정한 감속을 사용하므로 반복이 필요하지 않음

                float dt = remainingTime > kMaxTimeStep && !isZeroFriction
                    ? Mathf.Min(kMaxTimeStep, remainingTime * 0.5f)
                    : remainingTime;

                remainingTime -= dt;

                // 마찰 및 제동 적용

                velocity += (-friction * velocity + revAccel) * dt;

                // 방향을 반전하지 않음

                if (Vector3.Dot(velocity, oldVel) <= 0.0f)
                    return Vector3.zero;
            }

            // 거의 제로인 경우 또는 제동 중일 때 최소 임계값 이하인 경우 제로로 클램프

            float sqrSpeed = velocity.sqrMagnitude;
            if (sqrSpeed <= 0.00001f || (!isZeroBraking && sqrSpeed <= 0.1f))
                return Vector3.zero;

            return velocity;
        }

        /// <summary>
        /// 마찰 또는 제동 마찰 및 가속도 또는 감속도의 효과를 적용하여 주어진 상태에 대한 새로운 속도를 계산합니다.
        /// </summary>

        public virtual Vector3 CalcVelocity(Vector3 velocity, Vector3 desiredVelocity, float friction, bool isFluid, float deltaTime)
        {
            const float kMinTickTime = 0.000001f;
            if (deltaTime < kMinTickTime)
                return velocity;

            // 요청된 이동 방향 계산

            float desiredSpeed = desiredVelocity.magnitude;
            Vector3 desiredMoveDirection = desiredSpeed > 0.0f ? desiredVelocity / desiredSpeed : Vector3.zero;

            // 요청된 가속도 (아날로그 입력을 고려함)

            float analogInputModifier = ComputeAnalogInputModifier(desiredVelocity);
            Vector3 inputAcceleration = GetMaxAcceleration() * analogInputModifier * desiredMoveDirection;

            // 실제 최대 속도 (아날로그 입력을 고려함)

            float actualMaxSpeed = Mathf.Max(GetMinAnalogSpeed(), GetMaxSpeed() * analogInputModifier);

            // 제동 또는 감속 적용

            bool isZeroAcceleration = inputAcceleration.isZero();
            bool isVelocityOverMax = velocity.isExceeding(actualMaxSpeed);

            // 가속도가 없거나 최대 속도를 초과하여 속도를 줄여야 하는 경우에만 제동을 적용합니다.

            if (isZeroAcceleration || isVelocityOverMax)
            {
                Vector3 oldVelocity = velocity;

                // 마찰 및 제동 적용

                float actualBrakingFriction = useSeparateBrakingFriction ? brakingFriction : friction;
                float actualBrakingAcceleration =
                    useSeparateBrakingDeceleration ? brakingDeceleration : GetMaxBrakingDeceleration();

                velocity = ApplyVelocityBraking(velocity, actualBrakingFriction, actualBrakingAcceleration, deltaTime);

                // 시작할 때 최대 속도를 초과한 경우 제동이 최대 속도 이하로 낮아지지 않도록 합니다.

                if (isVelocityOverMax && velocity.sqrMagnitude < actualMaxSpeed.square() && Vector3.Dot(inputAcceleration, oldVelocity) > 0.0f)
                    velocity = oldVelocity.normalized * actualMaxSpeed;
            }
            else
            {
                // 마찰, 이는 방향 변경 능력에 영향을 미칩니다

                Vector3 accelDir = inputAcceleration.normalized;
                float velMag = velocity.magnitude;

                velocity -= (velocity - accelDir * velMag) * Mathf.Min(friction * deltaTime, 1.0f);
            }

            // 유체 마찰 적용

            if (isFluid)
                velocity *= 1.0f - Mathf.Min(friction * deltaTime, 1.0f);

            // 입력 가속도 적용

            if (!isZeroAcceleration)
            {
                float newMaxSpeed = velocity.isExceeding(actualMaxSpeed) ? velocity.magnitude : actualMaxSpeed;

                velocity += inputAcceleration * deltaTime;
                velocity = velocity.clampedTo(newMaxSpeed);
            }

            return velocity;
        }

        /// <summary>
        /// 현재 이동 모드를 기준으로 입력 벡터에 대한 제약 조건을 적용합니다.
        /// 제약된 입력 벡터를 반환합니다.
        /// </summary>

        public virtual Vector3 ConstrainInputVector(Vector3 inputVector)
        {
            Vector3 worldUp = -GetGravityDirection();

            float inputVectorDotWorldUp = Vector3.Dot(inputVector, worldUp);
            if (!Mathf.Approximately(inputVectorDotWorldUp, 0.0f) && (IsWalking() || IsFalling()))
                inputVector = Vector3.ProjectOnPlane(inputVector, worldUp);

            return characterMovement.ConstrainVectorToPlane(inputVector);
        }

        /// <summary>
        /// 현재 이동 모드에 대한 원하는 속도를 계산합니다.
        /// </summary>

        protected virtual void CalcDesiredVelocity(float deltaTime)
        {
            // 현재 이동 방향

            Vector3 movementDirection = Vector3.ClampMagnitude(GetMovementDirection(), 1.0f);

            // 애니메이션에서 원하는 속도 (루트 모션을 사용하는 경우) 또는 입력 이동 벡터에서 원하는 속도

            Vector3 desiredVelocity = useRootMotion && rootMotionController
                ? rootMotionController.ConsumeRootMotionVelocity(deltaTime)
                : movementDirection * GetMaxSpeed();

            // 제약된 원하는 속도 반환

            _desiredVelocity = ConstrainInputVector(desiredVelocity);
        }

        /// <summary>
        /// 현재 이동 모드에 대한 원하는 속도를 계산합니다.
        /// </summary>

        public virtual Vector3 GetDesiredVelocity()
        {
            return _desiredVelocity;
        }

        /// <summary>
        /// 현재 이동 방향에 대한 기울기 각도를 계산합니다.
        /// 상향 경사 시 양수, 하향 경사 시 음수 또는 캐릭터가 지면에 없거나 움직이지 않는 경우 0입니다 (예: movementDirection == Vector3.zero).
        /// </summary>

        public float GetSignedSlopeAngle()
        {
            Vector3 movementDirection = GetMovementDirection();
            if (movementDirection.isZero() || !IsOnGround())
                return 0.0f;

            Vector3 projMovementDirection =
                Vector3.ProjectOnPlane(movementDirection, characterMovement.groundNormal).normalized;

            return Mathf.Asin(Vector3.Dot(projMovementDirection, -GetGravityDirection())) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 비키네틱 물리 객체의 상단에 서 있을 때 아래쪽 힘을 적용합니다 (applyStandingDownwardForce == true인 경우).
        /// 적용되는 힘은: 질량 * 중력 * standingDownwardForceScale입니다.
        /// </summary>

        protected virtual void ApplyDownwardsForce()
        {
            Rigidbody groundRigidbody = characterMovement.groundRigidbody;
            if (!groundRigidbody || groundRigidbody.isKinematic)
                return;

            Vector3 downwardForce = mass * GetGravityVector();
            groundRigidbody.AddForceAtPosition(downwardForce * standingDownwardForceScale, GetPosition());
        }

        /// <summary>
        /// 걷을 수 있는 표면에서 이동할 때 캐릭터의 속도를 업데이트합니다.
        /// </summary>

        protected virtual void WalkingMovementMode(float deltaTime)
        {
            // 루트 모션을 사용하는 경우 애니메이션 속도 사용

            if (useRootMotion && rootMotionController)
                characterMovement.velocity = GetDesiredVelocity();
            else
            {
                // 새로운 속도 계산

                characterMovement.velocity =
                    CalcVelocity(characterMovement.velocity, GetDesiredVelocity(), groundFriction, false, deltaTime);
            }

            // 아래쪽 힘 적용

            if (applyStandingDownwardForce)
                ApplyDownwardsForce();
        }

        /// <summary>
        /// 캐릭터가 현재 웅크리고 있는지 여부를 확인합니다.
        /// </summary>

        public virtual bool IsCrouched()
        {
            return _isCrouched;
        }

        /// <summary>
        /// 캐릭터가 웅크리기를 요청합니다.
        /// 요청은 다음 시뮬레이션 업데이트에서 처리됩니다.
        /// 입력 이벤트 (예: 버튼 '다운' 이벤트)에서 호출합니다.
        /// </summary>

        public virtual void Crouch()
        {
            crouchInputPressed = true;
        }

        /// <summary>
        /// 캐릭터가 웅크리기를 멈추도록 요청합니다.
        /// 요청은 다음 시뮬레이션 업데이트에서 처리됩니다.
        /// 입력 이벤트 (예: 버튼 '업' 이벤트)에서 호출합니다.
        /// </summary>

        public virtual void UnCrouch()
        {
            crouchInputPressed = false;
        }

        /// <summary>
        /// 현재 상태에서 캐릭터가 웅크릴 수 있는지 여부를 결정합니다.
        /// 기본적으로 걷기 모드만 해당됩니다.
        /// </summary>

        protected virtual bool IsCrouchAllowed()
        {
            return canEverCrouch && IsWalking();
        }

        /// <summary>
        /// 캐릭터가 웅크릴 수 있는지 여부를 결정합니다.
        /// 예: 캡슐 확장 공간이 있는지 확인 등.
        /// </summary>

        protected virtual bool CanUnCrouch()
        {
            bool overlapped = characterMovement.CheckHeight(_unCrouchedHeight);
            return !overlapped;
        }

        /// <summary>
        /// 웅크리기 입력을 확인하고 요청된 웅크리기를 수행하려고 시도합니다.
        /// </summary>

        protected virtual void CheckCrouchInput()
        {
            if (!_isCrouched && crouchInputPressed && IsCrouchAllowed())
            {
                _isCrouched = true;
                characterMovement.SetHeight(_crouchedHeight);

                OnCrouched();
            }
            else if (_isCrouched && (!crouchInputPressed || !IsCrouchAllowed()))
            {
                if (!CanUnCrouch())
                    return;

                _isCrouched = false;
                characterMovement.SetHeight(_unCrouchedHeight);

                OnUnCrouched();
            }
        }

        /// <summary>
        /// 낙하 중 속도를 업데이트합니다.
        /// 중력을 적용하고 단말 속도를 초과하지 않도록 합니다.
        /// </summary>

        protected virtual void FallingMovementMode(float deltaTime)
        {
            // 현재 목표 속도

            Vector3 desiredVelocity = GetDesiredVelocity();

            // 중력 방향에 의해 정의된 월드 업

            Vector3 worldUp = -GetGravityDirection();

            // 걷을 수 없는 지면에 있는 경우...

            if (IsOnGround() && !IsOnWalkableGround())
            {
                // '벽'으로 이동하는 경우 기여도 제한

                Vector3 groundNormal = characterMovement.groundNormal;

                if (Vector3.Dot(desiredVelocity, groundNormal) < 0.0f)
                {
                    // 벽과 평행하게 이동할 수는 있지만 벽으로 밀어내지 않도록 합니다

                    Vector3 groundNormal2D = Vector3.ProjectOnPlane(groundNormal, worldUp).normalized;
                    desiredVelocity = Vector3.ProjectOnPlane(desiredVelocity, groundNormal2D);
                }

                // 걷을 수 없는 표면에 평면 투영하여 속도 계산을 평면으로 만듭니다

                worldUp = Vector3.ProjectOnPlane(worldUp, groundNormal).normalized;
            }

            // 속도를 구성 요소로 분리

            Vector3 verticalVelocity = Vector3.Project(characterMovement.velocity, worldUp);
            Vector3 lateralVelocity = characterMovement.velocity - verticalVelocity;

            // 측면 속도 업데이트

            lateralVelocity = CalcVelocity(lateralVelocity, desiredVelocity, fallingLateralFriction, false, deltaTime);

            // 수직 속도 업데이트

            verticalVelocity += gravity * deltaTime;

            // 단말 속도를 초과하지 않도록 합니다.

            float actualFallSpeed = maxFallSpeed;
            if (physicsVolume)
                actualFallSpeed = physicsVolume.maxFallSpeed;

            if (Vector3.Dot(verticalVelocity, worldUp) < -actualFallSpeed)
                verticalVelocity = Vector3.ClampMagnitude(verticalVelocity, actualFallSpeed);

            // 새로운 속도 적용

            characterMovement.velocity = lateralVelocity + verticalVelocity;

            // 낙하 타이머 업데이트

            _fallingTime += deltaTime;
        }

        /// <summary>
        /// 캐릭터가 점프 중인지 여부를 확인합니다.
        /// </summary>

        public virtual bool IsJumping()
        {
            return _isJumping;
        }

        /// <summary>
        /// 캐릭터가 점프를 요청합니다. 요청은 다음 시뮬레이션 업데이트에서 처리됩니다.
        /// 입력 이벤트 (예: 버튼 '다운' 이벤트)에서 호출합니다.
        /// </summary>

        public virtual void Jump()
        {
            jumpInputPressed = true;
        }

        /// <summary>
        /// 캐릭터가 점프를 종료하도록 요청합니다. 요청은 다음 시뮬레이션 업데이트에서 처리됩니다.
        /// 입력 이벤트 (예: 버튼 '다운' 이벤트)에서 호출합니다.
        /// </summary>

        public virtual void StopJumping()
        {
            jumpInputPressed = false;
            jumpInputHoldTime = 0.0f;

            ResetJumpState();
        }

        /// <summary>
        /// 점프 관련 변수를 리셋합니다.
        /// </summary>

        protected virtual void ResetJumpState()
        {
            if (!IsFalling())
                jumpCurrentCount = 0;

            jumpForceTimeRemaining = 0.0f;

            _isJumping = false;
        }

        /// <summary>
        /// 점프가 힘을 제공하는지 여부를 확인합니다, 예: 점프 입력이 유지되고 있는 동안
        /// 점프 최대 유지 시간을 초과하지 않았습니다.
        /// </summary>

        public virtual bool IsJumpProvidingForce()
        {
            return jumpForceTimeRemaining > 0.0f;
        }

        /// <summary>
        /// jumpImpulse 속도와 중력을 기반으로 최대 점프 높이를 계산합니다.
        /// jumpMaxHoldTime은 고려하지 않습니다.
        /// </summary>

        public virtual float GetMaxJumpHeight()
        {
            float gravityMagnitude = GetGravityMagnitude();
            if (gravityMagnitude > 0.0001f)
            {
                return jumpImpulse * jumpImpulse / (2.0f * gravityMagnitude);
            }

            return 0.0f;
        }

        /// <summary>
        /// jumpImpulse 속도와 중력을 기반으로 최대 점프 높이를 계산합니다.
        /// jumpMaxHoldTime은 고려합니다.
        /// </summary>

        public virtual float GetMaxJumpHeightWithJumpTime()
        {
            float maxJumpHeight = GetMaxJumpHeight();
            return maxJumpHeight + jumpImpulse * jumpMaxHoldTime;
        }

        /// <summary>
        /// 현재 상태에서 캐릭터가 점프할 수 있는지 여부를 결정합니다.
        /// </summary>

        protected virtual bool IsJumpAllowed()
        {
            if (!canJumpWhileCrouching && IsCrouched())
                return false;

            return canEverJump && (IsWalking() || IsFalling());
        }

        /// <summary>
        /// 요청된 점프를 수행할 수 있는지 여부를 결정합니다.
        /// </summary>

        protected virtual bool CanJump()
        {
            // 캐릭터 상태가 유효한지 확인

            bool isJumpAllowed = IsJumpAllowed();
            if (isJumpAllowed)
            {
                // jumpCurrentCount 및 jumpInputHoldTime이 유효한지 확인

                if (!_isJumping || jumpMaxHoldTime <= 0.0f)
                {
                    if (jumpCurrentCount == 0)
                    {
                        // 첫 점프 시 jumpInputHoldTime은 jumpMaxPreGroundedTime 유예 기간 내에 있어야 합니다

                        isJumpAllowed = jumpInputHoldTime <= jumpMaxPreGroundedTime;

                        // 유효한 점프인 경우 jumpInputHoldTime을 리셋,
                        // 그렇지 않으면 jumpInputHoldTime이 0에서 시작되지 않아 점프 유지가 부정확해질 수 있습니다

                        if (isJumpAllowed)
                            jumpInputHoldTime = 0.0f;
                    }
                    else
                    {
                        // 연속 점프, 충분한 점프가 있어야 하며 새로 누른 상태여야 합니다 (즉, jumpInputHoldTime == 0.0f)

                        isJumpAllowed = jumpCurrentCount < jumpMaxCount && jumpInputHoldTime == 0.0f;
                    }
                }
                else
                {
                    // JumpInputHoldTime은 다음과 같은 경우에만 고려됩니다:
                    // A) 점프 제한이 충족되지 않았거나
                    // B) 점프 제한이 충족되었고 이미 점프 중인 경우

                    bool jumpInputHeld = jumpInputPressed && jumpInputHoldTime < jumpMaxHoldTime;

                    isJumpAllowed = jumpInputHeld && (jumpCurrentCount < jumpMaxCount || (_isJumping && jumpCurrentCount == jumpMaxCount));
                }
            }

            return isJumpAllowed;
        }

        /// <summary>
        /// jumpImpulse를 적용하여 점프를 수행합니다.
        /// 점프가 힘을 제공하는 경우 (예: 가변 높이 점프) 여러 번 호출될 수 있습니다.
        /// </summary>

        protected virtual bool DoJump()
        {
            // 중력 방향에 의해 결정된 월드 업

            Vector3 worldUp = -GetGravityDirection();

            // 위/아래로 움직일 수 없는 경우 점프하지 않음.

            if (characterMovement.isConstrainedToPlane &&
                Mathf.Approximately(Vector3.Dot(characterMovement.GetPlaneConstraintNormal(), worldUp), 1.0f))
            {
                return false;
            }

            // 중력 방향에 의해 정의된 월드 업을 따라 점프 임펄스를 적용

            float verticalSpeed = Mathf.Max(Vector3.Dot(characterMovement.velocity, worldUp), jumpImpulse);

            characterMovement.velocity =
                Vector3.ProjectOnPlane(characterMovement.velocity, worldUp) + worldUp * verticalSpeed;

            return true;
        }

        /// <summary>
        /// 점프 입력을 확인하고 요청된 점프를 수행하려고 시도합니다.
        /// </summary>

        protected virtual void CheckJumpInput()
        {
            if (!jumpInputPressed)
                return;

            // 첫 점프이고 이미 낙하 중인 경우, 점프MaxPostGroundedTime 유예 시간을 초과한 경우에만 jumpCurrentCount를 증가시킵니다.

            if (jumpCurrentCount == 0 && IsFalling() && fallingTime > jumpMaxPostGroundedTime)
                jumpCurrentCount++;

            bool didJump = CanJump() && DoJump();
            if (didJump)
            {
                // 점프 중이 아닌 상태에서 점프 중으로 전환

                if (!_isJumping)
                {
                    jumpCurrentCount++;
                    jumpForceTimeRemaining = jumpMaxHoldTime;

                    characterMovement.PauseGroundConstraint();
                    SetMovementMode(MovementMode.Falling);

                    OnJumped();
                }
            }

            _isJumping = didJump;
        }

        /// <summary>
        /// 점프 관련 타이머를 업데이트합니다
        /// </summary>

        protected virtual void UpdateJumpTimers(float deltaTime)
        {
            if (jumpInputPressed)
                jumpInputHoldTime += deltaTime;

            if (jumpForceTimeRemaining > 0.0f)
            {
                jumpForceTimeRemaining -= deltaTime;
                if (jumpForceTimeRemaining <= 0.0f)
                    ResetJumpState();
            }
        }

        /// <summary>
        /// notifyJumpApex가 true인 경우, 수직 속도 변화를 추적하여 ReachedJumpApex 이벤트를 트리거합니다.
        /// </summary>

        protected virtual void NotifyJumpApex()
        {
            if (!notifyJumpApex)
                return;

            float verticalSpeed = Vector3.Dot(GetVelocity(), -GetGravityDirection());
            if (verticalSpeed >= 0.0f)
                return;

            notifyJumpApex = false;
            OnReachedJumpApex();
        }

        /// <summary>
        /// 현재 물리 볼륨의 마찰 영향을 받는 '비행' 중 캐릭터의 이동을 결정합니다 (있는 경우).
        /// 중력 없는 이동과 함께 지면 제약이 없는 이동입니다.
        /// </summary>

        protected virtual void FlyingMovementMode(float deltaTime)
        {
            if (useRootMotion && rootMotionController)
                characterMovement.velocity = GetDesiredVelocity();
            else
            {
                float actualFriction = IsInWaterPhysicsVolume() ? physicsVolume.friction : flyingFriction;

                characterMovement.velocity
                    = CalcVelocity(characterMovement.velocity, GetDesiredVelocity(), actualFriction, true, deltaTime);
            }
        }

        /// <summary>
        /// 캐릭터가 물에 얼마나 잠겨 있는지를 계산합니다.
        /// 0.0 = 물에 없음, 1.0 = 완전히 잠긴 범위의 float를 반환합니다.
        /// </summary>

        public virtual float CalcImmersionDepth()
        {
            float depth = 0.0f;

            if (IsInWaterPhysicsVolume())
            {
                float height = characterMovement.height;
                if (height == 0.0f || buoyancy == 0.0f)
                    depth = 1.0f;
                else
                {
                    Vector3 worldUp = -GetGravityDirection();

                    Vector3 rayOrigin = GetPosition() + worldUp * height;
                    Vector3 rayDirection = -worldUp;

                    BoxCollider waterVolumeCollider = physicsVolume.boxCollider;
                    depth = !waterVolumeCollider.Raycast(new Ray(rayOrigin, rayDirection), out RaycastHit hitInfo, height)
                        ? 1.0f
                        : 1.0f - Mathf.InverseLerp(0.0f, height, hitInfo.distance);
                }
            }

            return depth;
        }

        /// <summary>
        /// 중력과 부력의 영향을 받으며 유체 볼륨을 통해 수영하는 동안 캐릭터의 이동을 결정합니다.
        /// 중력 없는 이동과 함께 지면 제약이 없는 이동입니다.
        /// </summary>

        protected virtual void SwimmingMovementMode(float deltaTime)
        {
            // 현재 침수 깊이를 고려한 실제 부력 계산

            float depth = CalcImmersionDepth();
            float actualBuoyancy = buoyancy * depth;

            // 새로운 속도 계산

            Vector3 desiredVelocity = GetDesiredVelocity();
            Vector3 newVelocity = characterMovement.velocity;

            Vector3 worldUp = -GetGravityDirection();
            float verticalSpeed = Vector3.Dot(newVelocity, worldUp);

            if (verticalSpeed > maxSwimSpeed * 0.33f && actualBuoyancy > 0.0f)
            {
                // 양의 수직 속도를 감쇠 (물 밖으로 나감)

                verticalSpeed = Mathf.Max(maxSwimSpeed * 0.33f, verticalSpeed * depth * depth);
                newVelocity = Vector3.ProjectOnPlane(newVelocity, worldUp) + worldUp * verticalSpeed;
            }
            else if (depth < 0.65f)
            {
                // 양의 수직 원하는 속도를 감쇠

                float verticalDesiredSpeed = Vector3.Dot(desiredVelocity, worldUp);

                desiredVelocity =
                    Vector3.ProjectOnPlane(desiredVelocity, worldUp) + worldUp * Mathf.Min(0.1f, verticalDesiredSpeed);
            }

            // 루트 모션을 사용하는 경우...

            if (useRootMotion && rootMotionController)
            {
                // 현재 수직 속도를 유지하여 중력의 영향을 유지합니다

                Vector3 verticalVelocity = Vector3.Project(newVelocity, worldUp);

                // 새로운 속도 업데이트

                newVelocity = Vector3.ProjectOnPlane(desiredVelocity, worldUp) + verticalVelocity;
            }
            else
            {
                // 실제 마찰

                float actualFriction = IsInWaterPhysicsVolume()
                    ? physicsVolume.friction * depth
                    : swimmingFriction * depth;

                newVelocity = CalcVelocity(newVelocity, desiredVelocity, actualFriction, true, deltaTime);
            }

            // 자유롭게 수영하는 경우, 중력 가속도를 적용하지만 (1.0f - 실제 부력)으로 스케일링합니다

            newVelocity += gravity * ((1.0f - actualBuoyancy) * deltaTime);

            // 속도 업데이트

            characterMovement.velocity = newVelocity;
        }

        /// <summary>
        /// 사용자 정의 이동 모드, 여러 하위 모드를 포함할 수 있습니다.
        /// MovementMode가 Custom으로 설정된 경우 호출됩니다.
        /// </summary>

        protected virtual void CustomMovementMode(float deltaTime)
        {
            // CustomMovementModeUpdate 이벤트 트리거

            OnCustomMovementMode(deltaTime);
        }

        /// <summary>
        /// 캐릭터의 현재 회전 모드를 반환합니다.
        /// </summary>

        public RotationMode GetRotationMode()
        {
            return _rotationMode;
        }

        /// <summary>
        /// 캐릭터의 현재 회전 모드를 설정합니다:
        /// -None: 회전 비활성화.
        /// -OrientRotationToMovement: 이동 방향으로 캐릭터를 회전시키며, rotationRate를 회전 변화 속도로 사용합니다.
        /// -OrientRotationToViewDirection: 카메라의 시선 방향으로 캐릭터를 부드럽게 회전시키며, rotationRate를 회전 변화 속도로 사용합니다.
        /// -OrientWithRootMotion: 루트 모션이 캐릭터 회전을 처리하도록 합니다.
        /// -Custom: 사용자 정의 회전 모드.
        /// </summary>

        public void SetRotationMode(RotationMode rotationMode)
        {
            _rotationMode = rotationMode;
        }

        /// <summary>
        /// 현재 RotationMode를 기반으로 캐릭터의 회전을 업데이트합니다.
        /// </summary>

        protected virtual void UpdateRotation(float deltaTime)
        {
            if (_rotationMode == RotationMode.None)
            {
                // 아무것도 하지 않음
            }
            else if (_rotationMode == RotationMode.OrientRotationToMovement)
            {
                // 회전이 캐릭터의 요(yaw)만 수정해야 하는지 확인

                bool shouldRemainVertical = IsWalking() || IsFalling();

                // 회전 변화를 회전 속도로 사용하여 이동 방향으로 캐릭터를 부드럽게 회전

                RotateTowards(_movementDirection, deltaTime, shouldRemainVertical);
            }
            else if (_rotationMode == RotationMode.OrientRotationToViewDirection && camera != null)
            {
                // 회전이 캐릭터의 요(yaw)만 수정해야 하는지 확인

                bool shouldRemainVertical = IsWalking() || IsFalling();

                // 회전 변화를 회전 속도로 사용하여 카메라의 시선 방향으로 캐릭터를 부드럽게 회전

                RotateTowards(cameraTransform.forward, deltaTime, shouldRemainVertical);
            }
            else if (_rotationMode == RotationMode.OrientWithRootMotion)
            {
                // 루트 모션이 캐릭터 회전을 처리하도록 함

                RotateWithRootMotion();
            }
            else if (_rotationMode == RotationMode.Custom)
            {
                CustomRotationMode(deltaTime);
            }
        }

        /// <summary>
        /// 사용자 정의 회전 모드.
        /// RotationMode가 Custom으로 설정된 경우 호출됩니다.
        /// </summary>

        protected virtual void CustomRotationMode(float deltaTime)
        {
            // CustomRotationModeUpdated 이벤트 트리거

            OnCustomRotationMode(deltaTime);
        }

        private void BeforeSimulationUpdate(float deltaTime)
        {
            // 캐릭터 이동 모드를 CharacterMovement 지면 상태에 기반하여 걷기 / 낙하 모드로 전환

            if (IsWalking() && !IsGrounded())
                SetMovementMode(MovementMode.Falling);

            if (IsFalling() && IsGrounded())
                SetMovementMode(MovementMode.Walking);

            // 활성 물리 볼륨 업데이트

            UpdatePhysicsVolumes();

            // 웅크리기 / 웅크리지 않기 처리

            CheckCrouchInput();

            // 점프 처리

            CheckJumpInput();
            UpdateJumpTimers(deltaTime);

            // BeforeSimulationUpdated 이벤트 트리거

            OnBeforeSimulationUpdate(deltaTime);
        }

        private void SimulationUpdate(float deltaTime)
        {
            // 현재 이동 모드에 대한 원하는 속도 계산

            CalcDesiredVelocity(deltaTime);

            // 현재 이동 모드 업데이트

            switch (_movementMode)
            {
                case MovementMode.None:
                    break;

                case MovementMode.Walking:
                    WalkingMovementMode(deltaTime);
                    break;

                case MovementMode.Falling:
                    FallingMovementMode(deltaTime);
                    break;

                case MovementMode.Flying:
                    FlyingMovementMode(deltaTime);
                    break;

                case MovementMode.Swimming:
                    SwimmingMovementMode(deltaTime);
                    break;

                case MovementMode.Custom:
                    CustomMovementMode(deltaTime);
                    break;
            }

            // 회전 업데이트

            UpdateRotation(deltaTime);

            // 입력 회전 추가 (예: AddYawInput 등)

            ConsumeRotationInput();
        }

        private void AfterSimulationUpdate(float deltaTime)
        {
            // 요청된 경우, 정점 도달을 확인하고 해당 이벤트 트리거

            NotifyJumpApex();

            // AfterSimulationUpdated 이벤트 트리거

            OnAfterSimulationUpdate(deltaTime);
        }

        private void CharacterMovementUpdate(float deltaTime)
        {
            // 이동 수행

            characterMovement.Move(deltaTime);

            // CharacterMovementUpdated 이벤트 트리거

            OnCharacterMovementUpdated(deltaTime);

            // 루트 모션을 사용하지 않는 경우, 루트 모션 누적 델타를 플러시합니다.
            // 캐릭터가 루트 모션을 토글하는 동안 누적을 방지합니다.

            if (!useRootMotion && rootMotionController)
                rootMotionController.FlushAccumulatedDeltas();
        }

        /// <summary>
        /// 이 캐릭터 시뮬레이션을 수행합니다, 즉: 속도, 위치, 회전 등을 업데이트합니다.
        /// enableAutoSimulation이 true일 때 자동으로 호출됩니다.
        /// </summary>

        public void Simulate(float deltaTime)
        {
            if (isPaused)
                return;

            BeforeSimulationUpdate(deltaTime);
            SimulationUpdate(deltaTime);
            AfterSimulationUpdate(deltaTime);
            CharacterMovementUpdate(deltaTime);
        }

        /// <summary>
        /// enableAutoSimulation이 true인 경우, 이 캐릭터 시뮬레이션을 수행합니다.
        /// </summary>

        private void OnLateFixedUpdate()
        {
            // 이 캐릭터 시뮬레이션 수행

            Simulate(Time.deltaTime);
        }

        /// <summary>
        /// 캐릭터가 현재 일시 중지 상태인지 여부를 확인합니다.
        /// </summary>

        public bool IsPaused()
        {
            return isPaused;
        }

        /// <summary>
        /// 캐릭터 일시 중지 / 재개.
        /// 일시 중지되면 캐릭터는 모든 상호 작용을 방지합니다 (이동 없음, 회전 없음, 충돌 없음 등).
        /// clearState가 true인 경우, 모든 보류 중인 이동, 힘 및 회전을 지웁니다.
        /// </summary>

        public void Pause(bool pause, bool clearState = true)
        {
            isPaused = pause;
            characterMovement.collider.enabled = !isPaused;

            if (clearState)
            {
                _movementDirection = Vector3.zero;
                _rotationInput = Vector3.zero;

                characterMovement.velocity = Vector3.zero;
                characterMovement.ClearAccumulatedForces();
            }
        }

        #endregion

        #region MONOBEHAVIOUR

        /// <summary>
        /// 재정의된 경우, 기본 메서드를 반드시 호출해야 합니다.
        /// </summary>

        protected virtual void Reset()
        {
            _rotationMode = RotationMode.OrientRotationToMovement;
            _rotationRate = 540.0f;

            _startingMovementMode = MovementMode.Walking;

            _maxWalkSpeed = 5.0f;
            _minAnalogWalkSpeed = 0.0f;
            _maxAcceleration = 20.0f;
            _brakingDecelerationWalking = 20.0f;
            _groundFriction = 8.0f;

            _canEverCrouch = true;
            _crouchedHeight = 1.25f;
            _unCrouchedHeight = 2.0f;
            _maxWalkSpeedCrouched = 3.0f;

            _maxFallSpeed = 40.0f;
            _brakingDecelerationFalling = 0.0f;
            _fallingLateralFriction = 0.3f;
            _airControl = 0.3f;

            _canEverJump = true;
            _canJumpWhileCrouching = true;
            _jumpMaxCount = 1;
            _jumpImpulse = 5.0f;
            _jumpMaxHoldTime = 0.0f;
            _jumpMaxPreGroundedTime = 0.0f;
            _jumpMaxPostGroundedTime = 0.0f;

            _maxFlySpeed = 10.0f;
            _brakingDecelerationFlying = 0.0f;
            _flyingFriction = 1.0f;

            _maxSwimSpeed = 3.0f;
            _brakingDecelerationSwimming = 0.0f;
            _swimmingFriction = 0.0f;
            _buoyancy = 1.0f;

            _gravity = new Vector3(0.0f, -9.81f, 0.0f);
            _gravityScale = 1.0f;

            _useRootMotion = false;

            _impartPlatformVelocity = false;
            _impartPlatformMovement = false;
            _impartPlatformRotation = false;

            _enablePhysicsInteraction = false;
            _applyPushForceToCharacters = false;
            _applyStandingDownwardForce = false;

            _mass = 1.0f;
            _pushForceScale = 1.0f;
            _standingDownwardForceScale = 1.0f;
        }

        /// <summary>
        /// 재정의된 경우, 기본 메서드를 반드시 호출해야 합니다.
        /// </summary>

        protected virtual void OnValidate()
        {
            rotationRate = _rotationRate;

            maxWalkSpeed = _maxWalkSpeed;
            minAnalogWalkSpeed = _minAnalogWalkSpeed;
            maxAcceleration = _maxAcceleration;
            brakingDecelerationWalking = _brakingDecelerationWalking;
            groundFriction = _groundFriction;

            crouchedHeight = _crouchedHeight;
            unCrouchedHeight = _unCrouchedHeight;
            maxWalkSpeedCrouched = _maxWalkSpeedCrouched;

            maxFallSpeed = _maxFallSpeed;
            brakingDecelerationFalling = _brakingDecelerationFalling;
            fallingLateralFriction = _fallingLateralFriction;
            airControl = _airControl;

            jumpMaxCount = _jumpMaxCount;
            jumpImpulse = _jumpImpulse;
            jumpMaxHoldTime = _jumpMaxHoldTime;
            jumpMaxPreGroundedTime = _jumpMaxPreGroundedTime;
            jumpMaxPostGroundedTime = _jumpMaxPostGroundedTime;

            maxFlySpeed = _maxFlySpeed;
            brakingDecelerationFlying = _brakingDecelerationFlying;
            flyingFriction = _flyingFriction;

            maxSwimSpeed = _maxSwimSpeed;
            brakingDecelerationSwimming = _brakingDecelerationSwimming;
            swimmingFriction = _swimmingFriction;
            buoyancy = _buoyancy;

            gravityScale = _gravityScale;

            useRootMotion = _useRootMotion;

            if (_characterMovement == null)
                _characterMovement = GetComponent<CharacterMovement>();

            impartPlatformVelocity = _impartPlatformVelocity;
            impartPlatformMovement = _impartPlatformMovement;
            impartPlatformRotation = _impartPlatformRotation;

            enablePhysicsInteraction = _enablePhysicsInteraction;
            applyPushForceToCharacters = _applyPushForceToCharacters;
            applyPushForceToCharacters = _applyPushForceToCharacters;

            mass = _mass;
            pushForceScale = _pushForceScale;
            standingDownwardForceScale = _standingDownwardForceScale;
        }

        /// <summary>
        /// 재정의된 경우, 기본 메서드를 반드시 호출해야 합니다.
        /// </summary>

        protected virtual void Awake()
        {
            // 구성 요소 캐시

            CacheComponents();

            // 시작 이동 모드 설정

            SetMovementMode(_startingMovementMode);
        }

        /// <summary>
        /// 재정의된 경우, 기본 메서드를 반드시 호출해야 합니다.
        /// </summary>

        protected virtual void OnEnable()
        {
            // CharacterMovement 이벤트 구독

            characterMovement.Collided += OnCollided;
            characterMovement.FoundGround += OnFoundGround;

            // 활성화된 경우, 자동 시뮬레이션을 수행하기 위해 LateFixedUpdate 코루틴 시작

            if (_enableAutoSimulation)
                EnableAutoSimulationCoroutine(true);
        }

        /// <summary>
        /// 재정의된 경우, 기본 메서드를 반드시 호출해야 합니다.
        /// </summary>

        protected virtual void OnDisable()
        {
            // CharacterMovement 이벤트 구독 해제

            characterMovement.Collided -= OnCollided;
            characterMovement.FoundGround -= OnFoundGround;

            // 활성화된 경우, 자동 시뮬레이션을 비활성화하기 위해 LateFixedUpdate 코루틴 중지

            if (_enableAutoSimulation)
                EnableAutoSimulationCoroutine(false);
        }

        /// <summary>
        /// 재정의된 경우, 기본 메서드를 반드시 호출해야 합니다.
        /// </summary>

        protected virtual void Start()
        {
            // 지면 상태를 업데이트하기 위해 지면 검사를 강제합니다,
            // 그렇지 않으면 캐릭터가 낙하 상태로 변경됩니다, 캐릭터 이동이 다음 Move 호출까지 지면 상태를 업데이트하지 않기 때문입니다.

            if (_startingMovementMode == MovementMode.Walking)
            {
                characterMovement.SetPosition(transform.position, true);
            }
        }

        /// <summary>
        /// 재정의된 경우, 기본 메서드를 반드시 호출해야 합니다.
        /// </summary>

        protected virtual void OnTriggerEnter(Collider other)
        {
            AddPhysicsVolume(other);
        }

        /// <summary>
        /// 재정의된 경우, 기본 메서드를 반드시 호출해야 합니다.
        /// </summary>

        protected virtual void OnTriggerExit(Collider other)
        {
            RemovePhysicsVolume(other);
        }

        /// <summary>
        /// enableAutoSimulation이 true인 경우, 이 코루틴은 캐릭터 시뮬레이션을 수행하는 데 사용됩니다.
        /// </summary>

        private IEnumerator LateFixedUpdate()
        {
            WaitForFixedUpdate waitTime = new WaitForFixedUpdate();

            while (true)
            {
                yield return waitTime;

                OnLateFixedUpdate();
            }
        }

        #endregion
    }
}