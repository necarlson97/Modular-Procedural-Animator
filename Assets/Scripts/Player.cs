using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class Player : Being {

    protected override void AfterStart() {
        BindInput();
    }

    void BindInput() {
        // Intilize callbacks from our player input
        CustomInput.GetAction("Crouch").canceled += (
            ctx => { ToggleCrouch(); }
        );
        CustomInput.GetAction("Run").started += (
            ctx => { StartRun(); }
        );
        CustomInput.GetAction("Run").canceled += (
            ctx => { StopRun(); }
        );

        // TODO for now, test press/release jump
        CustomInput.GetAction("Jump").started += (
            ctx => { PrepJump(); }
        );
        CustomInput.GetAction("Jump").canceled += (
            ctx => { Jump(); }
        );

        CustomInput.GetAction("Light Attack").canceled += (
            ctx => { LightAttack(); }
        );
        CustomInput.GetAction("Heavy Attack").canceled += (
            ctx => { HeavyAttack(); }
        );
        CustomInput.GetAction("Special Attack").canceled += (
            ctx => { SpecialAttack(); }
        );

        CustomInput.GetAction("Guard").started += (
            ctx => { StartGuard(); }
        );
        CustomInput.GetAction("Guard").canceled += (
            ctx => { StopGuard(); }
        );
    }

    protected override void BeforeUpdate() {
        // Get keyboard / controller input
        var input = CustomInput.GetAxis("Movement");
        // Input we get as x/y, but we want to move on x/z
        var movement = new Vector3(input.x, 0, input.y);
        // Rotate that to move where the camera is looking
        movement = Camera.main.transform.rotation * movement;

        // TODO is this what we want?
        if (!InAir()) SetMovement(movement);
        

        // Look at our target lock, or just where the camera's pointing
        var camLook = transform.position + Camera.main.transform.forward;
        SetLook(target != null ? target.transform.position : camLook);

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

    public bool Pickup(Pickup pickup) {
        // Player picking up this item/weapon/etc
        // Return 'false' if it is not possible
        // TODO perhaps a Being method?

        var weap = pickup.transform.parent.GetComponent<Weapon>();
        if (weap != null) {
            // For now, just drop any weapon that is held
            GetWeapon().Drop();
            weap.Equip(this);
            weap.transform.parent = transform;
            return true;
        } else {
            Debug.Log("Don't know how to pickup "+pickup.transform.parent.name);
            return false;
        }
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
