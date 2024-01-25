using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Being : CustomBehavior {
    // A creature that can walk, jump, etc
    // - like the player, or an npc monster

    // These are just defaults - they should likely
    // be overriden by the subclass
    protected float walkSpeed = 2f;
    protected float runSpeed = 8f;
    protected float jumpForce = 250f;
    protected float accelSpeed = 600f;

    // What is the being target locked / attacking / talking to / etc
    internal GameObject target;

    // Want easy methods for player/monster classes
    // to call to set movement/behavior/etc
    Vector3 _lookAt;
    public void SetLook(Vector3 look) { _lookAt = look; }
    Vector3 _movement;
    public void SetMovement(Vector3 move) { _movement = move; }
    bool _crouching;
    public void ToggleCrouch() { _crouching = !_crouching; }
    public void StartCrouch() { _crouching = true; }
    public void StopCrouch() { _crouching = false; }
    public bool IsCrouched() { return _crouching; }
    bool _running;
    public void ToggleRun() { _running = !_running; }
    public void StartRun() { _running = true; }
    public void StopRun() { _running = false; }
    // Note - we don't use bool here, as we may be trying to run, but failing
    public bool IsRunning() { return WalkVelocity().magnitude > walkSpeed; }

    void Start() {
        // Lifecycle hooks for subclasses
        BeforeStart();
        AfterStart();
    }
    protected virtual void BeforeStart(){}
    protected virtual void AfterStart(){}

    void Update() {
        // This unity built-in method takes no parameters of course,
        // but the being will use 'SetLook', 'SetMovement', 'Crouch', etc
        // to change it's behavior.
        BeforeUpdate(); // Lifecycle hook for children

        UpdateLook(_lookAt);
        UpdateJump();
        UpdateCrouch();
        UpdateWalk(_movement, _running);

        AfterUpdate();
    }
    protected virtual void BeforeUpdate(){}
    protected virtual void AfterUpdate(){}

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

    protected void LightAttack() {
        // TODO 'hitbox', attack sequences, input buffer, etc, etc
        GetWeapon().Light();
    }
    protected void HeavyAttack() {
        GetWeapon().Heavy();
    }
    protected void SpecialAttack() {
        GetWeapon().Special();
    }

    // How much time after the gaurd starts that we are
    // able to parry the blow
    float _parryMax = .1f;
    float _gaurdTime;
    protected void UpdateGaurd() {
        if (!_gaurding) _gaurdTime = 0;
        else {
            _gaurdTime += Time.deltaTime;
        }
    }
    bool _gaurding;
    public void ToggleGaurd() { _gaurding = !_gaurding; }
    public void StartGaurd() { _gaurding = true; }
    public void StopGaurd() { _gaurding = false; }
    public bool IsGaurding() { return _gaurding; }
    public bool CanParry() { return _gaurdTime < _parryMax; }


    public Weapon GetWeapon() {
        // Return what weapon this being is holding,
        // creating the default weapon if it is not equiped
        var weap = GetComponentInChildren<Weapon>();
        if (weap != null) return weap;
        var weapObject = CreateWeapon();
        return weapObject.GetComponent<Weapon>();
    }
    Type defaultWeaponType = typeof(Fists);
    GameObject CreateWeapon(Type weaponType) {
        // Create an empty weapon object,
        // and equpt it (useful for default weapons)
        var weapObject = new GameObject("Weapon - "+weaponType);
        weapObject.transform.SetParent(transform);
        var weap = (Weapon) weapObject.AddComponent(weaponType);
        weap.Equip(this);
        return weapObject;
    }
    GameObject CreateWeapon() { return CreateWeapon(defaultWeaponType); }
    public LimbAnimator GetWeaponLimb(Weapon weapon) { 
        // Return the limb holding this equiped weapon
        // TODO for now, just returning major arm
        return MajorLimb();
    }
    public LimbAnimator MajorLimb() { 
        // Return the 'dominant' limb
        // TODO for now, just right arm
        return FindContains("Arm R").GetComponent<LimbAnimator>();
    }
    public LimbAnimator MinorLimb() {
        // The limb opposite to the major limb,
        // e.g. the non-dominant arm
        // TODO for now, just left arm
        return FindContains("Arm L").GetComponent<LimbAnimator>();
    }
    public TorsoAnimator Torso() {
        return GetComponentInChildren<TorsoAnimator>();
    }

    public void Jump() {
        // Being wishes to jump
        // TODO should buffer/queue this stuff
        _prejump = false;
        if (!CanJump()) return;
        _jumpTimer = 0;
        GetComponent<Rigidbody>().AddForce(transform.up * jumpForce); 
    }
    // Time before a player can jump again
    float _jumpMax = 1f;
    float _jumpTimer;
    // Time after a ledge a player can still jump
    float _coyoteMax = 0.5f;
    float _coyoteTimer;
    public bool CanJump() {
        // Returns true if player is on ground
        // (or coyote time) and hasn't just jumped
        return _coyoteTimer < _coyoteMax && _jumpTimer > _jumpMax;
    }
    public bool InAir() {
        // Return true when the player is airborn
        var distance = .1f;
        var start = BottomPoint();
        var direction = new Vector3(0, -distance, 0);
        RaycastHit hit;

        var rc = GetComponent<Rigidbody>();
        if (Physics.Raycast(start, direction, out hit, distance)) {
            return false;
        }
        return true;
    }
    protected void UpdateJump() {
        // Check to see if the being wishes to jump,
        // and handle duplicate jumps, coyote jumps, etc

        // Whenever we walk on the ground, we get coyote back
        if (InAir()) { _coyoteTimer += Time.deltaTime; }
        else _coyoteTimer = 0;
        // For now, jump recharges in air and on ground
        _jumpTimer += Time.deltaTime;
    }
    bool _prejump;
    protected void PrepJump(float prepTime=-1) {
        // Before jump, do a bit of squatting down
        _prejump = true;
        // If we want the being to 'automatically'
        // jump after a set time
        if (prepTime >= 0) Invoke("Jump", prepTime);
    }

    protected void UpdateCrouch() {
        // Handle crouching, squatting before jump, etc
        var collider = transform.Find("Walk Collider");
        if (_prejump) collider.localScale = new Vector3(1, .65f, 1);
        else if (_crouching) collider.localScale = new Vector3(1, .75f, 1);
        else collider.localScale= new Vector3(1, 1, 1);
    }

    public Vector3 BottomPoint() {
        // Find a low point in the center, just above the ground
        // (e.g. telling if we are airborne)
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
        // - can be positive or negative
        return Vector3.Dot(WalkVelocity(), transform.forward);
    }
    public bool MovingFoward() { return ForwardVelocity() > 0; }
    // The % of the controllers max speed they are moving
    public float Rush() { return WalkVelocity().magnitude / runSpeed; }
    public float ForwardRush() { return ForwardVelocity() / runSpeed; }
    // TODO or also running, better name... Ambulating?
    public bool IsWalking() { return WalkVelocity().magnitude > 0.001f; }
    public bool IsAttacking() { return GetWeapon().IsAttacking(); }

    Vector3 _prevVelocity;
    public Vector3 AccelerationToDisplacement(Vector3 acceleration) {
        // We want the player to move with constant,
        // frame independant acceleration - so we apply
        // acceleration, and return the movement
        Vector3 currentVelocity = _prevVelocity + acceleration * Time.deltaTime; 
        Vector3 displacement = (
            _prevVelocity * Time.deltaTime + (currentVelocity - _prevVelocity) / 2 * Time.deltaTime
        );
        // TODO is this correct?
        _prevVelocity = currentVelocity *= 0.95f; // Dampening
        return displacement;
    }

    public Vector3 AccelerationToVelocity(Vector3 acceleration, float maxVelocity) {
        // We want the player to move with constant,
        // frame independant acceleration - so we apply
        // acceleration, and return the velocity
        Vector3 currentVelocity = _prevVelocity + acceleration * Time.deltaTime; 
        _prevVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, 50 * Time.deltaTime); // Dampening

        // TODO technically I'd like running to not slow
        // suddenly when they release shift - but leaving for now
        if (currentVelocity.magnitude > maxVelocity) return currentVelocity.normalized * maxVelocity;
        return currentVelocity;
    }
}
