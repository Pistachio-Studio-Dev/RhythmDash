// [ Written by Yuxuan Liao, Modified by Lindsey ]

using Event;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCharacterController : CharacterController
{
    public static PlayerCharacterController Instance { get; private set; }
    
    [Header("Input")]
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string lookActionName = "Look";

    [Header("Camera Spring")]
    [Tooltip("If not set, automatically fetches Camera.main")]
    [SerializeField] private Camera followCamera;
    [SerializeField] private Vector3 baseLocalOffset = new Vector3(0f, 2.2f, -5.2f);
    [SerializeField] private float maxLift = 2f;
    [SerializeField] private float maxExtraBack = 2.4f;
    [SerializeField] private float speedOffsetStrength = 1f;
    [SerializeField] private float cameraDampTime = 0.08f;
    [SerializeField] private float cameraLookDampSpeed = 16f;
    [SerializeField] private float lookSensitivity = 10.0f;
    [SerializeField] private float minPitch = -70f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private float autoCenterDelay = 2.0f;

    [SerializeField] private float autoCenterYawSpeed = 120f;

    [SerializeField] private float cameraFocusHeight = 1.2f;

    [SerializeField] private float beatToleranceTime = 0.1f;//time before a beat's sprint effect starts to degrade
    [SerializeField] private float beatDegradeTime = 0.1f;//time between the start and the end of a beat's sprint degrade

    [SerializeField] private bool useDebugSprint = false;


    public Transform VehicleTransform => vehicle.transform;



    private InputAction moveAction;
    private InputAction lookAction;
    //TO REMOVE
    private InputAction debugSprintAction;
    //

    private Vector3 cameraPositionVelocity;
    private Vector3 dynamicOffset;
    private Transform cameraTarget;
    private double cameraCenterTime;
    private float manualPitch;
    private float manualYaw;
    private double beatExpireTime;//time stamp when a beat's sprint effect starts to degrade

    protected void OnDestroy()
    {
        Instance = null;
    }

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        if (followCamera == null)
            followCamera = Camera.main;

        cameraTarget = vehicle != null ? vehicle.transform : transform;

        dynamicOffset = baseLocalOffset;

        beatExpireTime = - beatToleranceTime - beatDegradeTime;// at start there should be no valid bit;

        cameraCenterTime = GameplayClock.TimeAsDouble + autoCenterDelay;
        manualYaw = 0f;
        manualPitch = 0f;

    }

    protected override void OnEnable()
    {
        base.OnEnable();
        moveAction = InputSystem.actions?.FindAction(moveActionName);
        lookAction = InputSystem.actions?.FindAction(lookActionName);
        debugSprintAction = InputSystem.actions?.FindAction("DebugSprint");

        if (moveAction == null)
            Debug.LogWarning($"[{name}] Input action '{moveActionName}' not found in InputSystem.actions.", this);

        moveAction?.Enable();
        lookAction?.Enable();
        debugSprintAction?.Enable();

        EventManager.StartListening<BeatHit>(HandleBeat);
        EventManager.StartListening<BeatHitFailed>(HandleBeatFailed);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        moveAction?.Disable();
        lookAction?.Disable();
        debugSprintAction?.Disable();

        EventManager.StopListening<BeatHit>(HandleBeat);
        EventManager.StopListening<BeatHitFailed>(HandleBeatFailed);
    }

    private void Update()
    {
        if (!isGameplayPaused && currentSessionState == SessionState.Racing) {
            HandleInput();
        }
    }

    private void HandleInput() {
        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        if (useDebugSprint)
        {
            float debugTrottleInput = debugSprintAction != null ? debugSprintAction.ReadValue<float>() : 0.0f;
            SetVehicleInput(debugTrottleInput, moveInput.x);
        }
        else
        {
            float throttleInput = Mathf.Lerp(1f, 0f, (float)(GameplayClock.TimeAsDouble - beatExpireTime) / beatDegradeTime);
            // Directional control is left/right only (Move.x).
            SetVehicleInput(throttleInput, moveInput.x);
        }

        Vector2 lookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;

        if (followCamera != null && lookInput.sqrMagnitude > float.Epsilon)
        {
            cameraCenterTime = GameplayClock.TimeAsDouble + autoCenterDelay;
            manualYaw += lookInput.x * lookSensitivity * Time.deltaTime;
            manualPitch -= lookInput.y * lookSensitivity * Time.deltaTime;

            manualPitch = Mathf.Clamp(manualPitch, minPitch, maxPitch);
        }
    } // override for boost powerup

    private void FixedUpdate()
    {
        if (isGameplayPaused)
        {
            return;
        }

        UpdateCameraSpring();
    }

    //TODO: switch to cinamachine
    private void UpdateCameraSpring()
    {
        if (followCamera == null || cameraTarget == null || vehicle == null)
            return;

        float throttleFactor = Mathf.Max(vehicle.Throttle, 0f);
        float speedFactor = Mathf.InverseLerp(vehicle.AutoSpeed, vehicle.MaxSpeed, vehicle.CurrentSpeed);

        Vector3 targetOffset = baseLocalOffset;
        targetOffset.y += maxLift * (throttleFactor * 0.2f + speedFactor * 0.8f) * speedOffsetStrength;
        targetOffset.z -= maxExtraBack * speedFactor;

        if (GameplayClock.TimeAsDouble >= cameraCenterTime)
        {
            manualYaw = Mathf.MoveTowardsAngle(manualYaw, 0f, autoCenterYawSpeed * Time.deltaTime);
            manualPitch = Mathf.MoveTowards(manualPitch, 0f, autoCenterYawSpeed * 0.5f * Time.deltaTime);
        }

        Quaternion orbitRotation = cameraTarget.rotation * Quaternion.Euler(manualPitch, manualYaw, 0f);
        Vector3 desiredPosition = cameraTarget.position + orbitRotation * targetOffset;

        followCamera.transform.position = Vector3.SmoothDamp(
            followCamera.transform.position,
            desiredPosition,
            ref cameraPositionVelocity,
            cameraDampTime
        );

        Vector3 lookAt = cameraTarget.position + cameraTarget.up * cameraFocusHeight;
        Quaternion targetRot = Quaternion.LookRotation(lookAt - followCamera.transform.position, cameraTarget.up);

        followCamera.transform.rotation = Quaternion.Slerp(
            followCamera.transform.rotation,
            targetRot,
            1f - Mathf.Exp(-cameraLookDampSpeed * Time.deltaTime)
        );
    }

    private void HandleBeat()
    {
        //each successful secures some time for sprinting
        beatExpireTime = GameplayClock.TimeAsDouble + beatToleranceTime;
        SetVehicleOnBeat();
    }

    private void HandleBeatFailed()
    {
        //each failure causes early degrading
        beatExpireTime = System.Math.Min(beatExpireTime, GameplayClock.TimeAsDouble);
    }

    protected override void OnGameplayPauseStateChanged(bool paused)
    {
        if (!paused)
        {
            return;
        }

        cameraPositionVelocity = Vector3.zero;
        SetVehicleInput(0f, 0f);
    }
}
