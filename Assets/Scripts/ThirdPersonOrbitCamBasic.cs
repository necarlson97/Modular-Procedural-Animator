﻿using UnityEngine;
using UnityEngine.InputSystem;

// This class corresponds to the 3rd person camera features.
public class ThirdPersonOrbitCamBasic : MonoBehaviour 
{
    public Transform player;
    internal Vector3 pivotOffset;
    internal Vector3 camOffset;

    // Speed of camera responsiveness.
    public float smooth = 10f;
    internal float horizontalAimingSpeed = 2f;
    internal float verticalAimingSpeed = 2f;

    public float maxVerticalAngle = 30f;
    public float minVerticalAngle = -60f;

    // How to get player input from the new InputAction system
    internal string lookInputName = "Look";
    InputAction lookInput;

    private float angleH = 0;
    private float angleV = 0;
    private Transform cam;
    private Vector3 smoothPivotOffset;
    private Vector3 smoothCamOffset;
    private Vector3 targetPivotOffset;
    private Vector3 targetCamOffset;
    private float defaultFOV;
    private float targetFOV;
    private float targetMaxVerticalAngle;
    private bool isCustomOffset; // If the script is overrriding the desired offset

    // Get the camera horizontal angle.
    public float GetH { get { return angleH; } }

    void Awake() {
        // Load the inputs
        var actions = Resources.Load<InputActionAsset>("New Controls");
        var actionMap = actions.FindActionMap("Player Input");
        lookInput = actionMap.FindAction("Look");

        // Reference to the camera transform.
        cam = transform;

        // Use the camera's scene position as it's default
        camOffset = transform.localPosition;
        // Pivot based off the y pos
        pivotOffset = new Vector3(0, camOffset.y, 0);
        camOffset = new Vector3(camOffset.x, 0, camOffset.z);

        // Set up references and default values.
        smoothPivotOffset = pivotOffset;
        smoothCamOffset = camOffset;
        defaultFOV = cam.GetComponent<Camera>().fieldOfView;
        angleH = player.eulerAngles.y;
        angleV = -cam.eulerAngles.x;

        ResetTargetOffsets();
        ResetFOV();
        ResetMaxVerticalAngle();

        // Check for no vertical offset.
        if (camOffset.y > 0)
            Debug.LogWarning(
                "Vertical Cam Offset (Y) will be ignored during collisions!\n" +
                "It is recommended to set all vertical offset in Pivot Offset."
            );
    }

    void Update() {
        // Get mouse/gamepad movement to orbit the camera.
        // If the cursor is free, only use the "Gamepad" bindings for look
        
        // TODO this will need to be more complex with any other logic
        // TODO should we handle this here? In CustomInput?
        // Enable mouse control if they click on screen,
        // disable if escaped
        if (CustomInput.GetDown("Mouse Click")) {
            Cursor.lockState = CursorLockMode.Locked;
        }
        if (CustomInput.GetDown("Escape")) {
            Cursor.lockState = CursorLockMode.None;
        }
        // If mouse is locked, use mouse and gamepad,
        // if mouse is 'free' - just gamepad
        if (Cursor.lockState == CursorLockMode.Locked) {
            lookInput.bindingMask = null;
        } else {
            lookInput.bindingMask = new InputBinding { groups = "Gamepad" };
        }

        var input = lookInput.ReadValue<Vector2>();
        angleH += Mathf.Clamp(input.x, -1, 1) * 60 * horizontalAimingSpeed * Time.deltaTime;
        angleV -= Mathf.Clamp(input.y, -1, 1) * 60 * verticalAimingSpeed * Time.deltaTime;

        // Set vertical movement limit.
        angleV = Mathf.Clamp(angleV, minVerticalAngle, targetMaxVerticalAngle);

        // Set camera orientation.
        Quaternion camYRotation = Quaternion.Euler(0, angleH, 0);
        Quaternion aimRotation = Quaternion.Euler(-angleV, angleH, 0);
        cam.rotation = aimRotation;

        // Set FOV.
        cam.GetComponent<Camera>().fieldOfView = Mathf.Lerp (cam.GetComponent<Camera>().fieldOfView, targetFOV,  Time.deltaTime);

        // Test for collision with the environment based on current camera position.
        Vector3 baseTempPosition = player.position + camYRotation * targetPivotOffset;
        Vector3 noCollisionOffset = targetCamOffset;
        while (noCollisionOffset.magnitude >= 0.2f) {
            if (DoubleViewingPosCheck(baseTempPosition + aimRotation * noCollisionOffset))
                break;
            noCollisionOffset -= noCollisionOffset.normalized * 0.2f;
        }
        if (noCollisionOffset.magnitude < 0.2f)
            noCollisionOffset = Vector3.zero;

        // No intermediate position for custom offsets, go to 1st person.
        bool customOffsetCollision = isCustomOffset && noCollisionOffset.sqrMagnitude < targetCamOffset.sqrMagnitude;

        // Repostition the camera.
        smoothPivotOffset = Vector3.Lerp(smoothPivotOffset, customOffsetCollision ? pivotOffset : targetPivotOffset, smooth * Time.deltaTime);
        smoothCamOffset = Vector3.Lerp(smoothCamOffset, customOffsetCollision ? Vector3.zero : noCollisionOffset, smooth * Time.deltaTime);
        
        cam.position =  player.position + camYRotation * smoothPivotOffset + aimRotation * smoothCamOffset;
    }

    public void SetTargetOffsets(Vector3 newPivotOffset, Vector3 newCamOffset) {
        // Set camera offsets to custom values.
        targetPivotOffset = newPivotOffset;
        targetCamOffset = newCamOffset;
        isCustomOffset = true;
    }

    public void ResetTargetOffsets() {
        // Reset camera offsets to default values.
        targetPivotOffset = pivotOffset;
        targetCamOffset = camOffset;
        isCustomOffset = false;
    }

    public void ResetYCamOffset() {
        // Reset the camera vertical offset.
        targetCamOffset.y = camOffset.y;
    }

    public void SetYCamOffset(float y) {
        // Set camera vertical offset.
        targetCamOffset.y = y;
    }

    public void SetXCamOffset(float x) {
        // Set camera horizontal offset.
        targetCamOffset.x = x;
    }

    public void SetFOV(float customFOV) {
        // Set custom Field of View.
        this.targetFOV = customFOV;
    }

    public void ResetFOV() {
        // Reset Field of View to default value.
        this.targetFOV = defaultFOV;
    }

    public void SetMaxVerticalAngle(float angle) {
        // Set max vertical camera rotation angle.
        this.targetMaxVerticalAngle = angle;
    }

    public void ResetMaxVerticalAngle() {
        // Reset max vertical camera rotation angle to default value.
        this.targetMaxVerticalAngle = maxVerticalAngle;
    }

    bool DoubleViewingPosCheck(Vector3 checkPos) {
        // Double check for collisions: concave objects doesn't detect hit from outside,
        // so cast in both directions.
        return ViewingPosCheck (checkPos) && ReverseViewingPosCheck (checkPos);
    }

    bool ViewingPosCheck (Vector3 checkPos) {
        // Check for collision from camera to player.

        // Cast target and direction.
        Vector3 target = player.position + pivotOffset;
        Vector3 direction = target - checkPos;
        // If a raycast from the check position to the player hits something...
        if (Physics.SphereCast(checkPos, 0.2f, direction, out RaycastHit hit, direction.magnitude)) {
            // ... if it is not the player...
            if(hit.transform != player && !hit.transform.GetComponent<Collider>().isTrigger) {
                // This position isn't appropriate.
                return false;
            }
        }
        // If we haven't hit anything or we've hit the player, this is an appropriate position.
        return true;
    }

    bool ReverseViewingPosCheck(Vector3 checkPos) {
        // Check for collision from player to camera.

        // Cast origin and direction.
        Vector3 origin = player.position + pivotOffset;
        Vector3 direction = checkPos - origin;
        if (Physics.SphereCast(origin, 0.2f, direction, out RaycastHit hit, direction.magnitude))
        {
            if(hit.transform != player && hit.transform != transform && !hit.transform.GetComponent<Collider>().isTrigger)
            {
                return false;
            }
        }
        return true;
    }

    public float GetCurrentPivotMagnitude(Vector3 finalPivotOffset) {
        // Get camera magnitude.
        return Mathf.Abs ((finalPivotOffset - smoothPivotOffset).magnitude);
    }
}
