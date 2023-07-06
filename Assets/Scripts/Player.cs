using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class Player : Being {

    void Update() {
        // Get keyboard / controller input
        var input = CustomInput.GetAxis("Movement");
        // Rotate that to move where the camera is looking
        // TODO limit to just 1 axis?
        input = Camera.main.transform.rotation * input;

        // Look at our target lock, or just where the camera's pointing
        var camLook = transform.position + Camera.main.transform.forward;
        UpdateLook(target != null ? target.transform.position : camLook);

        // Move dependent on user input
        // TODO for now, test press/release jump
        UpdatePreJump(CustomInput.GetDown("Jump"));
        UpdateJump(CustomInput.GetUp("Jump"));
        UpdateCrouch(CustomInput.GetDown("Crouch"));
        UpdateWalk(input, CustomInput.Get("Run"));

        UpdateLightAttack(CustomInput.GetDown("Light Attack"));
        UpdateHeavyAttack(CustomInput.GetDown("Heavy Attack"));
        UpdateSpecialAttack(CustomInput.GetDown("Special Attack"));

        // Engage / disengage target lock
        // TODO for now, just a test object
        if (CustomInput.GetDown("Target Lock")) {
            if (target != null) target = null;
            else target = GameObject.Find("Player Target Lock");
        }

        // Just for testing, move target
        if (CustomInput.GetDown("Dev Key") && target) {
            target.transform.position = transform.position + transform.up + Camera.main.transform.forward;
        }

        SetDev();
    }

    // Togglable dev mode, and allow
    // other classes to check if we are in dev mode
    static bool _dev;
    void SetDev() {
        // TODO new input system
        if (CustomInput.GetDown("Dev Key")) _dev = !_dev;
    }
    public static bool IsDevMode() { return _dev; }

    void OnDrawGizmos()  {
        var s = "";
        Handles.Label(transform.position+Vector3.up, s);
        if (IsDevMode()) {
            Debug.DrawLine(transform.position, transform.position+WalkVelocity(), Color.white);
        }
    }
}
