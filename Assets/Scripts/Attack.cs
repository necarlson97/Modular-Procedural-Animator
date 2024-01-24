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
            // TODO better defaults
            new FistJab(this),
            new FistCross(this)
        };
    }

    public LimbLandmarks Landmarks() { return Limb().landmarks; }
    public LimbAnimator Limb() { return weapon.Limb(); }
    public Being Being() { return weapon.Being(); }
}

[System.Serializable]
public class StrikeTarget {
    // Just a packaging of position & rotation,
    // as unity doesn't let us construct transforms
    // - with some handy helpers for defining strikes
    // TODO could make more general, as we could use these
    // for more animations than just strikes
    public Vector3 pos;
    public Quaternion rot;
    public StrikeTarget(Vector3 pos, Quaternion rot) {
        this.pos = pos;
        this.rot = rot;
    }
    public StrikeTarget(LimbAnimator limb, string landmarkName, string rotName) {
        // Can also create with the limb and given names,
        // using the landmark system and the CustomBehavior
        // handy rotations method
        // Debug.Log(landmarkName+" strike target: "+limb);
        // TODO
        this.pos = limb.landmarks.Get(landmarkName);
        this.rot = CustomBehavior.Rotation(rotName);
    }
    public StrikeTarget(LimbAnimator limb, string landmarkName, Vector3 lookAt) {
        // And can create with a landmark and a lookAt rotation
        this.pos = limb.landmarks.Get(landmarkName);
        this.rot = limb.LookRotation(lookAt);
    }
}

[System.Serializable]
public abstract class Strike {
    // An individual movement of the sword
    // - from start to end
    // TODO ideally, I think there could be,
    // more than just start/end - think of 
    // spinning with sword for example, that might
    // be 3 or 4 pos/rots there
    public float prep = .2f;
    public float duration = .08f;

    // Where the 'dominant' hand preps/ends strike
    // TODO could end up being array
    public StrikeTarget majorStart;
    public StrikeTarget majorEnd;
    // Same for 'non-domininat' hand
    public StrikeTarget minorStart;
    public StrikeTarget minorEnd;
    // Simmilar for torso
    public StrikeTarget torsoStart;
    public StrikeTarget torsoEnd;

    internal string name;
    [System.NonSerialized]
    internal Attack attack;    
    public Strike(Attack attack, string strikeName) {
        this.attack = attack;
        name = attack.name + " " + strikeName;
        Setup();
    }
    public Strike(Attack attack) {
        this.attack = attack;
        name = attack.name + " " + this.GetType().Name;
        Setup();
    }

    protected virtual void Setup() {
        // The likely overriden step that sets up
        // our target positions. Can set a StrikeTarget to null
        // if we don't care what happens there
        // (TODO ensure)

        // TODO have different defaults, this is for fists,
        // but defaults should probs assume 1 handed sword
        // or whatever
        majorStart = new StrikeTarget(MajorLimb(), "Waist", "Forward");
        minorStart = new StrikeTarget(MinorLimb(), "Waist", "Forward");
        torsoStart = new StrikeTarget(Torso(), "LeanBack", Vector3.right);

        majorEnd = new StrikeTarget(MajorLimb(), "Raise", "Up");
        minorEnd = new StrikeTarget(MinorLimb(), "WideWaist", "Down");
        torsoEnd = new StrikeTarget(Torso(), "LeanForward", -Vector3.right);
    }

    public virtual void Prep(float progress, Vector3 priorPos, Quaternion priorRot) {
        // Raise the weapon, prep the torso, etc
        // (with progress being [0-1] how far along we are)
        // Most subclasees will just override where these pos/rots
        // bring the limbs to, but can also override the behavior entirley
        // TODO do we have to have priorPos/Rot - is there something cleaner? 
        var pos = Vector3.Lerp(priorPos, majorStart.pos, progress);
        var rot = Quaternion.Lerp(priorRot, majorStart.rot, progress);
        Limb().SnapTarget(pos, rot);

        // TODO right now we 'snap' major, but 'place' minor/torso
        // - but we many need to snap others, which requires
        // all limb prior pos. If so - we need something cleaner
        // TODO could add defintions for PlaceTarget that take StrikeTarget
        MinorLimb().PlaceTarget(minorStart.pos, minorStart.rot);
        Torso().PlaceTarget(torsoStart.pos, torsoStart.rot);
    }

    public virtual void Perform(float progress) {
        // Move the weapon swiftly through the strike
        // (with progress being [0-1] how far along we are)
        var pos = Vector3.Lerp(majorStart.pos, majorEnd.pos, progress);
        var rot = Quaternion.Lerp(majorStart.rot, majorEnd.rot, progress);
        Limb().SnapTarget(pos, rot);
        
        MinorLimb().PlaceTarget(minorEnd.pos, minorEnd.rot);
        Torso().PlaceTarget(torsoEnd.pos, torsoEnd.rot);
        // TODO enable/disable hurtbox collider
    }

    public LimbLandmarks Landmarks() { return Limb().landmarks; }
    public LimbAnimator Limb() { return attack.Limb(); }
    public LimbAnimator MajorLimb() { return Being().MajorLimb(); }
    public LimbAnimator MinorLimb() { return Being().MinorLimb(); }
    public LimbAnimator Torso() { return Being().Torso(); }
    public Being Being() { return attack.Being(); }
}

// Some default strike types
public class FistJab : Strike {
    public FistJab(Attack attack) : base(attack) {}
    protected override void Setup() {
        majorStart = new StrikeTarget(MajorLimb(), "Face", "Up");
        minorStart = new StrikeTarget(MinorLimb(), "Chest", "Up");
        var backRight = (Vector3.right + Vector3.forward).normalized;
        torsoStart = new StrikeTarget(Torso(), "LeanBack", backRight);

        majorEnd = new StrikeTarget(MajorLimb(), "Extended", "Forward");
        minorEnd = new StrikeTarget(MinorLimb(), "Face", "Forward");
        var backLeft = (-Vector3.right + Vector3.forward).normalized;
        torsoEnd = new StrikeTarget(Torso(), "LeanForward", backLeft);
    }
}
public class FistCross : Strike {
    public FistCross(Attack attack) : base(attack) {}
    protected override void Setup() {
        majorStart = new StrikeTarget(MajorLimb(), "Raised", "Forward");
        minorStart = new StrikeTarget(MinorLimb(), "Chest", "Up");
        var backRight = Vector3.right + -Vector3.forward;
        torsoStart = new StrikeTarget(Torso(), "LeanBack", backRight);

        majorEnd = new StrikeTarget(MajorLimb(), "Extended", "Forward");
        minorEnd = new StrikeTarget(MinorLimb(), "Face", "Up");
        var backLeft = -Vector3.right + -Vector3.forward;
        torsoEnd = new StrikeTarget(Torso(), "LeanForward", backLeft);
    }
}