using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// An individual movement of the sword
// - from start to end
[System.Serializable]
public class Strike {
    public float prep = .5f;
    public float duration = .1f;
    
    // TODO functions that return the correct positions??
    // Because they are likely based on limb, not static
    // - but we want them to have a default
    public Vector3 startPos;
    public Quaternion startRot;
    public Vector3 endPos;
    public Quaternion endRot;

    [System.NonSerialized]
    internal Attack attack;
    internal string name;
    public Strike(Attack a, string strikeName) {
        // These will likely be overridden,
        // but just nice to have a default
        name = a.name + " " + strikeName;
        attack = a;
        startPos = Landmarks().WaistPos();
        startRot = CustomBehavior.RotForward();
        endPos = Landmarks().ExtendedPos();
        endRot = CustomBehavior.RotForward();
    }

    public LimbLandmarks Landmarks() {
        return Limb().landmarks;
    }
    public LimbAnimator Limb() { return attack.Limb(); }
    public Being Being() { return attack.Being(); }

    // Some default strike types - mostly for testing
    public static Strike Jab(Attack a) {
        return new Strike(a, "jab");
    }
    public static Strike Swipe(Attack a) {
        var swipe = new Strike(a, "swipe");
        swipe.startPos = a.Landmarks().RaisedPos();
        return swipe;
    }
}

[System.Serializable]
public class Attack {
    public List<Strike> strikes;
    public float comboDelay = .5f;
    
    [System.NonSerialized]
    internal Weapon weapon;
    internal string name;
    public Attack(Weapon w, string level) {
        // These will likely be overridden,
        // but just nice to have a default
        name = w.name + " " + level;
        weapon = w;
        strikes = new List<Strike>{
            Strike.Jab(this),
            Strike.Swipe(this)
        };
    }

    public LimbLandmarks Landmarks() { return Limb().landmarks; }
    public LimbAnimator Limb() { return weapon.Limb(); }
    public Being Being() { return weapon.being; }
}

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
        // TODO I can see a way to make this work
        // with legs as well
        // - but for now, ignoring kicks lol
        // TODO could memoize
        return being.GetWeaponLimb(this);
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
    private IEnumerator PerformAttack(Attack attack) {
        Debug.Log("Attacking: "+attack);
        _attacking = true;
        foreach (var strike in attack.strikes) {
            // Move the weapon from the start position to the end position over time
            // Here we are using a simple linear interpolation, but you later
            // we may want to use curves, or something involving the limb.GetLength(), etc
            float elapsedTime = 0;

            // Move hand from where it was previously,
            // to where the strike 'starts'
            // - prep is normally 'longer'
            var priorPos = Limb().transform.localPosition;
            var priorRot = Limb().transform.localRotation;
            while (elapsedTime < strike.prep) {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / strike.prep;

                var pos = Vector3.Lerp(priorPos, strike.startPos, progress);
                var rot = Quaternion.Lerp(priorRot, strike.startRot, progress);
                Limb().PlaceTarget(pos, rot, true);

                // TODO enable/disable hurtbox collider
                yield return null;
            }

            // Move the weapon through the actual strike itself
            // - usually fast
            while (elapsedTime < strike.prep + strike.duration) {
                elapsedTime += Time.deltaTime;
                float progress = (elapsedTime - strike.prep) / strike.duration;

                var pos = Vector3.Lerp(strike.startPos, strike.endPos, progress);
                var rot = Quaternion.Lerp(strike.startRot, strike.endRot, progress);
                Limb().PlaceTarget(pos, rot, true);

                // TODO enable/disable hurtbox collider
                yield return null;
            }
            // 'Snap' to end
            Limb().PlaceTarget(strike.endPos, strike.endRot, true);
            // For now, leaving a little after-strike delay
            yield return new WaitForSeconds(strike.prep / 10f);
        }
        // Wait for the delay until the attack combo sequence can restart
        yield return new WaitForSeconds(attack.comboDelay);
        _attacking = false;
    }
    public bool IsAttacking() { return _attacking; }
}
