using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("Движение")]
    public Joystick moveJoystick;
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float crouchSpeed = 2.5f;

    [Header("Бег")]
    public float maxRunTime = 10f;
    public float runCooldown = 5f;
    public Button runButton;
    public Image runButtonImage;
    public Color runActiveColor = Color.white;
    public Color runInactiveColor = Color.gray;

    [Header("Присед")]
    public Button crouchButton;
    public Image crouchButtonImage;
    public float crouchHeight = 1f;
    public float crouchCameraY = 0.3f;
    public float standCameraY = 0.6f;
    public float crouchTransitionSpeed = 8f;
    public Color crouchActiveColor = Color.cyan;
    public Color crouchInactiveColor = Color.white;

    [Header("Камера")]
    public Camera playerCamera;
    public float lookSensitivity = 1.2f;
    public float maxLookUp = 80f;
    public float maxLookDown = 80f;

    [Header("Эффекты камеры")]
    public float walkBobSpeed = 10f;
    public float walkBobHeight = 0.05f;
    public float walkBobWidth = 0.04f;
    public float walkTilt = 1.5f;

    public float runBobSpeed = 18f;
    public float runBobHeight = 0.12f;
    public float runBobWidth = 0.08f;
    public float runTilt = 3f;

    private CharacterController controller;
    private float currentRunTime;
    private float currentCooldown;
    private bool isRunning;
    private bool isCrouching;
    private float cameraVerticalAngle = 0f;
    private float playerYaw = 0f;
    private float currentCameraY;
    private float bobTimer;
    private bool isMoving;
    private int touchId = -1;
    private float currentSpeed;
    private float originalControllerHeight;
    private Vector3 originalControllerCenter;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (runButton != null)
            runButton.onClick.AddListener(ToggleRun);

        if (crouchButton != null)
            crouchButton.onClick.AddListener(ToggleCrouch);

        originalControllerHeight = controller.height;
        originalControllerCenter = controller.center;
        currentCameraY = standCameraY;

        currentRunTime = maxRunTime;
        currentCooldown = 0;
        isRunning = false;
        isCrouching = false;

        UpdateRunButtonVisual();
        UpdateCrouchButtonVisual();
    }

    void Update()
    {
        HandleRunCooldown();
        HandleCrouch();
        HandleMovement();
        HandleTouchLook();
        HandleCameraEffects();
        UpdateRunButtonVisual();
        UpdateCrouchButtonVisual();
    }

    void HandleRunCooldown()
    {
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown <= 0)
            {
                currentRunTime = maxRunTime;
                UpdateRunButtonVisual();
            }
        }

        if (isRunning)
        {
            currentRunTime -= Time.deltaTime;
            if (currentRunTime <= 0)
            {
                isRunning = false;
                currentCooldown = runCooldown;
                UpdateRunButtonVisual();
            }
        }
    }

    void HandleCrouch()
    {
        float targetHeight = isCrouching ? crouchHeight : originalControllerHeight;
        Vector3 targetCenter = isCrouching ? new Vector3(0, crouchHeight / 2f, 0) : originalControllerCenter;
        float targetCameraY = isCrouching ? crouchCameraY : standCameraY;

        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        controller.center = Vector3.Lerp(controller.center, targetCenter, Time.deltaTime * crouchTransitionSpeed);
        currentCameraY = Mathf.Lerp(currentCameraY, targetCameraY, Time.deltaTime * crouchTransitionSpeed);
    }

    void HandleMovement()
    {
        float x = moveJoystick.Horizontal();
        float z = moveJoystick.Vertical();

        Vector3 move = transform.right * x + transform.forward * z;

        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (isRunning && currentRunTime > 0)
            currentSpeed = runSpeed;
        else
            currentSpeed = walkSpeed;

        controller.Move(move * currentSpeed * Time.deltaTime);
        isMoving = (x != 0 || z != 0);
    }

    void HandleTouchLook()
    {
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                float screenHalf = Screen.width / 2f;

                if (touch.position.x > screenHalf)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        touchId = touch.fingerId;
                    }
                    else if (touch.fingerId == touchId && touch.phase == TouchPhase.Moved)
                    {
                        float deltaX = touch.deltaPosition.x * lookSensitivity * 0.1f;
                        float deltaY = touch.deltaPosition.y * lookSensitivity * 0.1f;

                        playerYaw += deltaX;
                        cameraVerticalAngle -= deltaY;
                        cameraVerticalAngle = Mathf.Clamp(cameraVerticalAngle, -maxLookDown, maxLookUp);

                        transform.rotation = Quaternion.Euler(0f, playerYaw, 0f);
                        playerCamera.transform.localRotation = Quaternion.Euler(cameraVerticalAngle, 0f, 0f);
                    }
                    else if (touch.phase == TouchPhase.Ended && touch.fingerId == touchId)
                    {
                        touchId = -1;
                    }
                    break;
                }
            }
        }
    }

    void HandleCameraEffects()
    {
        if (playerCamera == null) return;

        Vector3 camPos = playerCamera.transform.localPosition;
        camPos.y = currentCameraY;

        if (isMoving && !isCrouching)
        {
            bool isPlayerRunning = (isRunning && currentRunTime > 0);

            float bobSpeed = isPlayerRunning ? runBobSpeed : walkBobSpeed;
            float bobHeight = isPlayerRunning ? runBobHeight : walkBobHeight;
            float bobWidth = isPlayerRunning ? runBobWidth : walkBobWidth;
            float tiltAngle = isPlayerRunning ? runTilt : walkTilt;

            float speedFactor = currentSpeed / walkSpeed;
            bobTimer += Time.deltaTime * bobSpeed * speedFactor;

            float bobX = Mathf.Sin(bobTimer) * bobWidth;
            float bobY = Mathf.Sin(bobTimer * 2f) * bobHeight;
            float tilt = Mathf.Sin(bobTimer) * tiltAngle;

            camPos.x = bobX;
            camPos.y = currentCameraY + bobY;

            Quaternion targetRot = Quaternion.Euler(cameraVerticalAngle, 0, tilt);
            playerCamera.transform.localRotation = Quaternion.Slerp(playerCamera.transform.localRotation, targetRot, Time.deltaTime * 12f);
        }
        else
        {
            camPos.x = 0;
            bobTimer = 0;

            if (!isCrouching)
            {
                Quaternion targetRot = Quaternion.Euler(cameraVerticalAngle, 0, 0);
                playerCamera.transform.localRotation = Quaternion.Slerp(playerCamera.transform.localRotation, targetRot, Time.deltaTime * 10f);
            }
        }

        playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, camPos, Time.deltaTime * 15f);
    }

    void ToggleRun()
    {
        if (currentCooldown > 0) return;
        if (isCrouching) return;

        if (isRunning && currentRunTime > 0)
        {
            isRunning = false;
        }
        else if (currentRunTime > 0)
        {
            isRunning = true;
        }
    }

    void ToggleCrouch()
    {
        if (isRunning)
        {
            isRunning = false;
        }

        isCrouching = !isCrouching;
    }

    void UpdateRunButtonVisual()
    {
        if (runButtonImage != null)
        {
            if (isRunning && currentRunTime > 0)
                runButtonImage.color = runActiveColor;
            else if (currentCooldown > 0 || currentRunTime <= 0 || isCrouching)
                runButtonImage.color = runInactiveColor;
            else
                runButtonImage.color = Color.white;
        }

        if (runButton != null)
        {
            runButton.interactable = !isCrouching && currentCooldown <= 0 && currentRunTime > 0;
        }
    }

    void UpdateCrouchButtonVisual()
    {
        if (crouchButtonImage != null)
        {
            crouchButtonImage.color = isCrouching ? crouchActiveColor : crouchInactiveColor;
        }
    }
}