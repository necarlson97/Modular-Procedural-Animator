using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : CustomBehavior {
    // base class for all weapons in the game
    // - including unarmed
    // Most weapons will simply change 
    // TODO gaurding / parry position? And timing?
    // TODO two handed weapons??

    // TOOD should we use scriptableobject here?

    // TODO parse string shorthand or something to make
    // creating weapons easier?
    // TODO load most basic weapons
    // from yaml or something?

    public Attack lightAttack;
    public Attack heavyAttack;
    public Attack specialAttack;

    internal Being being;
    public void Start() {
        BeforeStart();
        being = GetComponentInParent<Being>();
        lightAttack = new Attack(this, "light");
        heavyAttack = new Attack(this, "heavy");
        specialAttack = new Attack(this, "special");
        AfterStart();
    }
    protected virtual void BeforeStart(){}
    protected virtual void AfterStart(){}

    public LimbAnimator Limb() {
        return MajorLimb();
    }

    public LimbAnimator MajorLimb() {
        // The dominant hand - for now, right
        // TODO could memoize
        // TODO I can see a way to make this work
        // with legs as well
        // - but for now, ignoring kicks lol
        return being.MajorLimb();
    }

    public LimbAnimator MinorLimb() {
        // TODO could rename major / minor, etc
        // TODO could memoize
        return being.MinorLimb();
    }

    public TorsoAnimator Torso() {
        return being.Torso();
    }

    public virtual void Light() {
        StartCoroutine(PerformAttack(lightAttack));
    }

    public virtual void Heavy() {
        StartCoroutine(PerformAttack(heavyAttack));
    }

    public virtual void Special() {
        StartCoroutine(PerformAttack(specialAttack));
    }

    bool _attacking;
    public bool IsAttacking() { return _attacking; }

    private IEnumerator PerformAttack(Attack attack, int strikeIndex=0) {
        // Perform an attack, starting with the 1st strike,
        // but if the same attack command comes in a small window,
        // increment to the next

        // TODO buffer system
        var strike = attack.strikes[strikeIndex];
        _attacking = true;

        // Bring the weapon back to ready the strike (slower)
        yield return StartCoroutine(PerformPrep(strike));
        // Swiftly lash the weapon out to perform the strike (faster)
        yield return StartCoroutine(PerformStrike(strike));

        // After the strike, check if there's a buffered attack of the same type
        // and if it's still within the window of opportunity
        // if (_attackBuffer.TryGetAttack(attack, out Attack bufferedAttack)) {
        //     yield return StartCoroutine(PerformAttack(bufferedAttack, strikeIndex + 1));
        // } else {
        //     _attacking = false;
        // }

        _attacking = false;

        yield return new WaitForSeconds(attack.comboDelay);
    }

    private IEnumerator PerformPrep(Strike strike) {
        // Draw the weapon back, preping to strike
        float elapsedTime = 0;

        // Move hand from whereever we started, to
        // where we will begin our strike
        var priorPos = Limb().TargetPos();
        var priorRot = Limb().TargetRot();
        while (elapsedTime < strike.prep) {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / strike.prep;

            var pos = Vector3.Lerp(priorPos, strike.startPos, progress);
            var rot = Quaternion.Lerp(priorRot, strike.startRot, progress);
            Limb().SnapTarget(pos, rot);

            // For now, have other hand just come to gaurd position,
            // and rotate shoulders - but this will likely have to
            // be based on attack
            MinorLimb().PlaceTarget(MinorLimb().landmarks.BoxerPos(), RotUp());
            Torso().TargetLookAt(Vector3.right);

            // TODO REMOVE
            Debug.DrawLine(transform.position+priorPos, transform.position+strike.startPos, Color.yellow);
            yield return null;
        }
    }

    private IEnumerator PerformStrike(Strike strike) {
        // Move the weapon swiftly through the strike
        float elapsedTime = 0;

        while (elapsedTime < strike.duration) {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / strike.duration;

            var pos = Vector3.Lerp(strike.startPos, strike.endPos, progress);
            var rot = Quaternion.Lerp(strike.startRot, strike.endRot, progress);
            Limb().SnapTarget(pos, rot);
            // For now, have other hand move back,
            // and shoulders rotate forwards
            MinorLimb().PlaceTarget(MinorLimb().landmarks.BoxerPos(), RotUp());
            Torso().TargetLookAt(-Vector3.right);
            // TODO enable/disable hurtbox collider
            // TODO REMOVE
            Debug.DrawLine(transform.position+strike.startPos, transform.position+strike.endPos, Color.red);
            Debug.DrawLine(pos, Limb().TargetPos(), Color.blue);
            yield return null;
        }
        
        // For now, leaving a little after-strike delay
        Limb().SnapTarget(strike.endPos, strike.endRot);
        yield return new WaitForSeconds(strike.prep * .6f);
    }


    public void Interrupt() {
        // Cancel any ongoing attack
        if (_attacking) {
            // _attackBuffer.Clear();
            _attacking = false;
            StopAllCoroutines();
            // Possibly return weapon to idle position, disable hurtbox, etc. here
        }
    }
}

public class AttackBuffer {
    // Buffer attacks, so as 
    // TODO I think this will need to be expanded
    // to a general input buffer, perhaps
    // made a part of CustomInput - but leaving for now

    // TODO what exactly is thuis?
    private const float bufferTime = 0.5f;

    private float _lastBufferTime = -bufferTime;
    // TODO do we want this as a list?
    private Attack _bufferedAttack;

    public void BufferAttack(Attack attack) {
        _bufferedAttack = attack;
        _lastBufferTime = Time.time;
    }

    public bool TryGetAttack(Attack type, out Attack attack) {
        if (_bufferedAttack != null && _bufferedAttack == type && Time.time - _lastBufferTime <= bufferTime) {
            attack = _bufferedAttack;
            Clear();
            return true;
        } else {
            attack = null;
            return false;
        }
    }

    public void Clear() {
        _bufferedAttack = null;
        _lastBufferTime = -bufferTime;
    }
}