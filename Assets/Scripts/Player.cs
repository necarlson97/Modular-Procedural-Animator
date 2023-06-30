using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class Player : Being {
    internal List<KeyCode> runningKeys = new List<KeyCode>{KeyCode.LeftShift, KeyCode.JoystickButton0};
    internal List<KeyCode> jumpKeys = new List<KeyCode>{KeyCode.Space, KeyCode.JoystickButton3};
    internal List<KeyCode> crouchKeys = new List<KeyCode>{KeyCode.LeftControl, KeyCode.JoystickButton8};
    internal List<KeyCode> targetLockKeys = new List<KeyCode>{KeyCode.F, KeyCode.JoystickButton9};
    internal List<KeyCode> lightAttackKeys = new List<KeyCode>{KeyCode.Mouse0, KeyCode.JoystickButton5};
    internal List<KeyCode> devKeys = new List<KeyCode>{KeyCode.G, KeyCode.JoystickButton2};

    void Update() {
        // Get keyboard / controller input
        var input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        // Rotate that to move where the camera is looking
        // TODO limit to just 1 axis?
        input = Camera.main.transform.rotation * input;

        // Look at our target lock, or just where the camera's pointing
        var camLook = transform.position + Camera.main.transform.forward;
        UpdateLook(target != null ? target.transform.position : camLook);

        // Move dependent on user input
        UpdateJump(jumpKeys.Any(k => Input.GetKeyDown(k)));
        UpdateCrouch(crouchKeys.Any(k => Input.GetKeyDown(k)));
        UpdateWalk(input, runningKeys.Any(k => Input.GetKey(k)));
        UpdateAttack(lightAttackKeys.Any(k => Input.GetKeyDown(k)));

        // Engage / disengage target lock
        // TODO for now, just a test object
        if (targetLockKeys.Any(k => Input.GetKeyDown(k))) {
            if (target != null) target = null;
            else target = GameObject.Find("Player Target Lock");
        }

        // Just for testing, move target
        if (devKeys.Any(k => Input.GetKeyDown(k)) && target) {
            target.transform.position = transform.position + transform.up + Camera.main.transform.forward;
        }

        Debug.DrawLine(transform.position, transform.position+WalkVelocity(), Color.white);
    }


    void OnDrawGizmos()  {
        var s = "";
        Handles.Label(transform.position+Vector3.up, s);
    }
}
