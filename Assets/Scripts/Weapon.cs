using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : CustomBehavior {
    // base class for all weapons in the game
    // - including unarmed
    // Most weapons will simply change 
    // TODO guarding / parry position? And timing?
    // TODO two handed weapons??

    // TOOD should we use scriptableobject here?

    // TODO parse string shorthand or something to make
    // creating weapons easier?
    // TODO load most basic weapons
    // from yaml or something?

    public Attack lightAttack;
    public Attack heavyAttack;
    public Attack specialAttack;

    // TODO should attack buffer be on Being?
    public AttackBuffer _attackBuffer;

    public void Start() {
        BeforeStart();
        _attackBuffer = new AttackBuffer();
        AfterStart();
    }
    protected virtual void BeforeStart(){}
    protected virtual void AfterStart(){}

    internal Being _being;
    public Being Being() { return _being; }
    bool _attacking;
    public bool IsAttacking() { return _attacking; }

    public void Equip(Being being) {
        // This weapon is currently being held,
        // initlizie it's attacks
        // TODO NOT CORRECT - need to make the
        // attacks easily overridable by the subclasse
        _being = being;
        lightAttack = new Attack(this, "light");
        heavyAttack = new Attack(this, "heavy");
        specialAttack = new Attack(this, "special");
    }
    public void Drop() {
        // Drop this weapon on the ground
        // (or destroy it, if it cannot be dropped,
        // like 'fists')
        Interrupt();
        _being = null;
        var pickup = GetComponentInChildren<Pickup>();
        if (pickup == null) {
            Destroy(gameObject);
            return;
        }
        pickup.Drop();
    }
    public bool IsEquiped() { return _being != null; }

    void Update() {
        // If we are ready to perform an attack,
        // and have one ready, perform it
        if (!IsEquiped() ||  IsAttacking()) return;
        var attack = _attackBuffer.Pop();
        if (attack != null) StartCoroutine(PerformAttack(attack));
    }
    
    public virtual void Light() {
        _attackBuffer.Add(lightAttack);
    }
    public virtual void Heavy() {
        _attackBuffer.Add(heavyAttack);
    }
    public virtual void Special() {
        _attackBuffer.Add(specialAttack);
    }

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

        // After the strike, check if there's a buffered attack of the same type,
        // and if it is of the same type, perform the next strike
        var nextAttack = _attackBuffer.Pop();
        bool sameAttack = nextAttack != null && nextAttack.name == attack.name;
        if (sameAttack && strikeIndex+1 < attack.strikes.Count) {
            yield return StartCoroutine(PerformAttack(nextAttack, strikeIndex + 1));
        }
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
            strike.Prep(progress, priorPos, priorRot);
            yield return null;
        }
    }

    private IEnumerator PerformStrike(Strike strike) {
        // Move the weapon swiftly through the strike
        float elapsedTime = 0;

        while (elapsedTime < strike.duration) {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / strike.duration;
            strike.Perform(progress);
            yield return null;
        }
        
        // For now, leaving a little after-strike delay
        strike.Perform(1);
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

    public LimbAnimator Limb() {
        return MajorLimb();
    }
    public LimbAnimator MajorLimb() {
        // The dominant hand - for now, right
        // TODO could memoize
        // TODO I can see a way to make this work
        // with legs as well
        // - but for now, ignoring kicks lol
        return Being().MajorLimb();
    }
    public LimbAnimator MinorLimb() {
        // TODO could rename major / minor, etc
        // TODO could memoize
        return Being().MinorLimb();
    }
    public TorsoAnimator Torso() {
        return Being().Torso();
    }
}
