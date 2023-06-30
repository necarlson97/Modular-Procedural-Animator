using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Being : MonoBehaviour {
    // A creature that can walk, jump, etc
    // - like the player, or an npc monster

    // These are just defaults - they should likely
    // be overriden by the subclass
    protected float walkSpeed = 2f;
    protected float runSpeed = 5f;
    protected float jumpForce = 250f;
    protected float accelSpeed = 600f;

    // What is the being target locked / attacking / talking to / etc
    internal GameObject target;

    protected void UpdateLook(Vector3? lookAt=null) {
        // Update the players body position, if it should be
        // looking at a specific target
        // (note, doesn't animate head or anything, just rotates GameObject)

        // If we aren't given something specific, default to target
        Vector3 lookTarget = default(Vector3);
        if (target != null) lookTarget = target.transform.position;
        if (lookAt != null) lookTarget = (Vector3) lookAt;
        // If there is nothing for us to change, leave
        if (lookTarget == default(Vector3)) return;

        // We only actually care about x & z - don't rotate y pos
        // TODO would we ever care? For a flying enemy? Or would their
        // head pos handle that too?
        lookTarget.y = transform.position.y;

        Debug.DrawLine(transform.position, lookTarget, Color.yellow);

        // Turn to look - but only if we are actually moving
        var lookRot = Quaternion.LookRotation(lookTarget - transform.position);
        var lookSpeed = 10 * Rush() * Time.deltaTime;
        transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, lookSpeed);
    }

    protected void UpdateWalk(Vector3 direction, bool running) {
        // Given the direction the being wants to move,
        // and wether it is running, change the
        // rigidbodies velocity accordingly
        var rc = GetComponent<Rigidbody>(); // Shorthand
        float moveSpeed = running ? runSpeed : walkSpeed;

        // Normalize vector, and scale by speed
        var accel = direction.magnitude * direction.normalized * accelSpeed;
        var walkVelocity = AccelerationToVelocity(accel, moveSpeed);
        rc.velocity = new Vector3(walkVelocity.x, rc.velocity.y, walkVelocity.z);
        
        // TODO once we have looking / targeting down,
        // the player should move faster when moving twoards look, and slower
        // when backpeadling
    }

    private Animator weaponAnim;
    protected void UpdateAttack(bool attacked) {
        // TODO 'hitbox', attack sequences, etc, etc
        if (!attacked) return;
        if (weaponAnim == null) {
            weaponAnim = transform.Find("weapon").GetComponentInChildren<Animator>();
        }
        weaponAnim.SetTrigger("slash");
        Debug.Log("slash");
    }

    float jumpTimer;
    protected void UpdateJump(bool jumped) {
        // Check to see if the being wishes to jump,
        // and handle duplicate jumps, coyote jumps, etc
        jumpTimer -= Time.deltaTime;
        if (jumped && !InAir()) Jump();
    }
    void Jump() {
        GetComponent<Rigidbody>().AddForce(transform.up * jumpForce);
        jumpTimer = 1f; // Time before a grounded player can jump again
    }

    bool crouched; // Crouching is toggled
    protected void UpdateCrouch(bool toggleCrouch) {
        // Check to see if being is crouched
        if (toggleCrouch) crouched = !crouched;
        
        var collider = transform.Find("Walk Collider");
        if (crouched) collider.localScale = new Vector3(1, .65f, 1);
        else collider.localScale= new Vector3(1, 1, 1);
    }

    public bool IsWalking() { return Rush() > 0.001f; }
    public bool IsCrouched() { return crouched; }

    float coyoteMax = 0.5f;
    float coyoteTimer;
    public bool InAir() {
        // Return true when the player is considered
        // airborn - both for animation sake, and jumping sake
        // TODO sloppy, changes dependent on # of times called
        var distance = .1f;
        var start = BottomPoint();
        var direction = new Vector3(0, -distance, 0);
        RaycastHit hit;

        var rc = GetComponent<Rigidbody>();
        coyoteTimer = - Time.deltaTime;
        if (Physics.Raycast(start, direction, out hit, distance)) {
            // If we are at walking on the ground, we get coyote back
            coyoteTimer = 0;
        } else coyoteTimer += Time.deltaTime;
        return coyoteTimer > coyoteMax || jumpTimer > 0;
    }
    public Vector3 BottomPoint() {
        // Find a low point in the center, just above the ground, to use for
        // telling if we are airborne
        var y = transform.Find("Walk Collider").GetComponent<Collider>().bounds.min.y;
        return new Vector3(transform.position.x, y+.001f, transform.position.z);
    }

    // Helper methods for animation classes to get an idea
    // of how the player is moving
    public Vector3 WalkVelocity() {
        // We might change how players move,
        // but the animator will want our movment velocity
        // - but wont care about jumping/falling
        return Vector3.Scale(GetComponent<Rigidbody>().velocity, new Vector3(1, 0, 1));;
    }
    public float ForwardVelocity() {
        // How much of this player's velocity is moving forward,
        // becomes a positive or negative velocity float 
        return Vector3.Dot(WalkVelocity(), transform.forward);
    }
    public bool MovingFoward() { return ForwardVelocity() > 0; }
    // The % of the controllers max speed they are moving
    public float Rush() { return WalkVelocity().magnitude / runSpeed; }
    public float ForwardRush() { return ForwardVelocity() / runSpeed; }

    Vector3 prevVelocity;
    public Vector3 AccelerationToDisplacement(Vector3 acceleration) {
        // We want the player to move with constant,
        // frame independant acceleration - so we apply
        // acceleration, and return the 
        Vector3 currentVelocity = prevVelocity + acceleration * Time.deltaTime; 
        Vector3 displacement = prevVelocity * Time.deltaTime + (currentVelocity - prevVelocity) / 2 * Time.deltaTime;

        // TODO is this correct?
        prevVelocity = currentVelocity *= 0.95f; // Dampening
        return displacement;
    }

    public Vector3 AccelerationToVelocity(Vector3 acceleration, float maxVelocity) {
        // We want the player to move with constant,
        // frame independant acceleration - so we apply
        // acceleration, and return the

        Vector3 currentVelocity = prevVelocity + acceleration * Time.deltaTime; 
        prevVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, 50 * Time.deltaTime); // Dampening

        // TODO technically I'd like running to not slow
        // suddenly when they release shift - but leaving for now
        if (currentVelocity.magnitude > maxVelocity) return currentVelocity.normalized * maxVelocity;
        return currentVelocity;
    }
}
