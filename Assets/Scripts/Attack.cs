using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Attack {
    // A sequence of strikes, that define a weapons's
    // light attack combo, or heavy attack combo, etc
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

[System.Serializable]
public class Strike {
    // An individual movement of the sword
    // - from start to end
    // TODO ideally, I think there could be,
    // more than just start/end - think of 
    // spinning with sword for example, that might
    // be 3 or 4 pos/rots there
    public float prep = .2f;
    public float duration = .08f;
    
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

    public LimbLandmarks Landmarks() { return Limb().landmarks; }
    public LimbAnimator Limb() { return attack.Limb(); }
    public Being Being() { return attack.Being(); }

    // Some default strike types - mostly for testing
    public static Strike Jab(Attack a) {
        var swipe = new Strike(a, "jab");
        swipe.startPos = a.Landmarks().ChinPos();
        var upRight = Vector3.up + Vector3.right;
        swipe.startRot = Quaternion.LookRotation(upRight, Vector3.up);
        return swipe;
    }
    public static Strike Swipe(Attack a) {
        var swipe = new Strike(a, "swipe");
        swipe.startPos = a.Landmarks().RaisedPos();
        return swipe;
    }
}

