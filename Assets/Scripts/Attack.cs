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
    public Being Being() { return weapon.Being(); }
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
        startPos = Landmarks().Get("Waist");
        startRot = CustomBehavior.RotForward();
        endPos = Landmarks().Get("Extended");
        endRot = CustomBehavior.RotForward();
    }

    public LimbLandmarks Landmarks() { return Limb().landmarks; }
    public LimbAnimator Limb() { return attack.Limb(); }
    public Being Being() { return attack.Being(); }

    // Some default strike types - mostly for testing
    public static Strike Jab(Attack a) {
        var swipe = new Strike(a, "jab");
        swipe.startPos = a.Landmarks().Get("Face");
        var upRight = Vector3.up + Vector3.right;
        swipe.startRot = Quaternion.LookRotation(upRight, Vector3.up);
        return swipe;
    }
    public static Strike Swipe(Attack a) {
        var swipe = new Strike(a, "swipe");
        swipe.startPos = a.Landmarks().Get("Raised");
        return swipe;
    }
}

public class AttackBuffer {
    // Buffer attacks, so a player's
    // input is still captured while their
    // being is busy performing the action
    // TODO I think this will need to be expanded
    // to a general input buffer, perhaps
    // made a part of CustomInput - but leaving for now

    // How old an input can be until it is ignored (seconds)
    private const float maxTime = 0.5f;
    // How many inputs can be in queue until new ones are ignored
    private const int maxInputs = 5;

    // Hold the buffered attacks alongside their time
    public Queue<AttackTime> _queue = new Queue<AttackTime>();
    public class AttackTime {
        public Attack attack { get; }
        public float time { get; }
        public AttackTime(Attack attack, float time) {
            this.attack = attack;
            this.time = time;
        }
    }

    public void Add(Attack attack) {
        // Enqueue attack, (so long as the queue is not full)
        // attaching the time it was added,
        // - so it can later be discarded if it becomes old
        if (_queue.Count < maxInputs) {
            _queue.Enqueue(new AttackTime(attack, Time.time));
        }
    }

    public Attack Pop() {
        // Get a ready, (non-stale) attack from the queue,
        // returning null if it is empty
        if (_queue.Count == 0) return null;
        RemoveStale();
        return _queue.Dequeue().attack;
    }

    public Attack Peek() {
        // Return the next attack,
        // but do not remove from queue
        if (_queue.Count == 0) return null;
        RemoveStale();
        return _queue.Peek().attack;
    }

    private void RemoveStale() {
        // Remove stale inputs
        if (_queue.Count == 0) return;
        while (Time.time - _queue.Peek().time > maxTime) {
            _queue.Dequeue();
        }
    }

    public void Clear() {
        // Clear the queue
        _queue.Clear();
    }
}