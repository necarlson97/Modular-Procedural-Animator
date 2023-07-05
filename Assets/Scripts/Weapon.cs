using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// An individual movement of the sword
// - from start to end
[System.Serializable]
public class Swing {
    public Vector3 startPos;
    public Quaternion startRot;
    public Vector3 endPos;
    public Quaternion endRot;
    public float speed = 1f;
    public float swingDelay = .1f;
}

[System.Serializable]
public class Attack {
    public List<Swing> swings;
    public float comboDelay;
}

public class Weapon : CustomBehavior {
    // base class for all weapons in the game
    // - including unarmed
    // Most weapons will simply change 
    // TODO gaurding / parry?
    // TODO two handed weapons??

    // TODO parse string shorthand or something to make
    // creating weapons easier?
    // TODO load most basic weapons
    // from yaml or something?

    public Attack lightAttack;
    public Attack heavyAttack;
    public Attack specialAttack;

    // TODO I can see a way to make this work
    // with legs as well
    // - but for now, ignoring kicks lol
    internal ArmAnimator limb;

    public void Start() {
        BeforeStart();
        limb = GetComponentInParent<ArmAnimator>();
        AfterStart();
    }
    protected virtual void BeforeStart(){}
    protected virtual void AfterStart(){}

    public void SetDefaults() {
        // TODO is this the best way to do this?
        foreach (Attack attack in new[] {lightAttack, heavyAttack, specialAttack}) {
            foreach (Swing swing in attack.swings) {
                swing.startPos = limb.RaisedPos();
                swing.endPos = limb.ExtendedPos();
                swing.startRot = RotForward();
                swing.endRot = RotForward();
            }
        }
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

    private IEnumerator PerformAttack(Attack attack) {
        foreach (Swing swing in attack.swings) {
            // Move the weapon from the start position to the end position over time
            // Here we are using a simple linear interpolation, but you later
            // we may want to use curves, or something involving the limb.GetLength(), etc
            float elapsedTime = 0;
            while (elapsedTime < swing.speed) {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / swing.speed;
                var pos = Vector3.Lerp(swing.startPos, swing.endPos, progress);
                var rot = Quaternion.Lerp(swing.startRot, swing.endRot, progress);
                limb.PlaceTarget(pos, rot, true);

                // TODO enable/disable hurtbox collider
                yield return null;
            }

            // Wait for the delay after the swing
            yield return new WaitForSeconds(swing.swingDelay);
        }

        // Wait for the delay until the attack combo sequance
        // can restart
        yield return new WaitForSeconds(attack.comboDelay);
    }
}
