using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour {
    // Script for picking up an gameObject:
    // a weapon, a consumable item, etc

    void Start() {
        // Ensure our collider is set properly
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        // TODO could we create the pickup collider programatically?
        // Using bounds * 5 or some such thing?
        // TODO we should try to reszie programatically
        // - making sure the min bound is still large enough
    }

    void Update() {
        if (!IsHeld()) GroundAnimation();
    }

    internal float rotateSpeed = 30;
    void GroundAnimation() {
        // Gentle animation for this object sitting on the ground
        // TODO enticing particles
        // - maybe different depending on the
        // pickup type
        // TODO do we want them to fall?
        // Hover in place? Roll around? etc

        var pt = transform.parent;  // Shorthand
        // Tilt it back slightly
        // TODO the tilt could result in the collider
        // comming in and out of range
        // - maybe just tilt model??
        // But then need to put back to how it was...
        pt.eulerAngles = new Vector3(5f, pt.eulerAngles.y, pt.eulerAngles.z);
        // Rotate gently
        pt.Rotate(new Vector3(0,  rotateSpeed* Time.deltaTime, 0));
    }

    public bool PickingUp(Player player) {
        // Give this pickup to the player,
        // returning false if it is not possible
        if (IsHeld()) return false;
        var wasPickedUp = player.Pickup(this);
        if (!wasPickedUp) return false;
        _held = true;
        GetComponent<Collider>().enabled = false;
        // This is a sub-object to the thing actually picked up
        transform.parent.parent = player.transform;
        return true;
    }

    public void Drop() {
        _held = false;
        // This is a sub-object to the thing actually being dropped
        transform.parent.parent = null;
        // For now, waiting a second to reenable pickup trigger
        Invoke("EnableTrigger", 1f);
    }

    void EnableTrigger() { GetComponent<Collider>().enabled = true; }

    bool _held;
    public bool IsHeld() { return _held; }

    private void OnTriggerEnter(Collider other) {
        var player = other.transform.parent.GetComponent<Player>();
        // TODO when in range, need to just show the prompt
        // of 'press A to pickup' or whatever
        // - not actually pick up
        if (player != null) PickingUp(player);
    }
}
