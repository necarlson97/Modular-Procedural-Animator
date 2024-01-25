using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LimbLandmarks {
    // Class for handling the landmarks
    // - reference points that ease making the procedural animations for limbs:
    // a holster position, fully extended, fully at rest, etc
    // So there will be an instance of this 'LimbLandmarks'
    // class on each limb - as their landmarks are unique to them

    LimbAnimator _limb;
    
    public LimbLandmarks(LimbAnimator limb) {
        _limb = limb;
        Setup();
    }

    // TODO ideally, we might have some kind of enumerator,
    // but leaving for now
    Dictionary<string, Landmark> _landmarks = new Dictionary<string, Landmark>();
    public void Setup() {
        // Initilize a dict of landmarks,
        // storing them to be referenced later
        if (_limb == null) {
            Debug.LogError("Limb is null when trying to set up landmarks");
            return;
        }
        // Load all of the defined landmarks we have with reflection
        var landmarkTypes = typeof(Landmark).Assembly.GetTypes().Where(
            type => type.IsSubclassOf(typeof(Landmark)));
        foreach (Type landmarkType in landmarkTypes) {
            // Initilize the landmark with the given _limb
            object[] args = new object[] { _limb };
            Landmark landmark = (Landmark) Activator.CreateInstance(landmarkType, args);
            _landmarks[landmarkType.Name] = landmark;
        }
    }

    public Vector3 Get(string name, bool? left=null) {
        // Helper for 'get a named landmark position'
        // Such as 'Chin()'
        if (!_landmarks.ContainsKey(name)) {
            Debug.LogError("Unrecognized landmark name: "+name);
            return default(Vector3);
        }
        return _landmarks[name].Get(left);
    }

    internal void OnDrawGizmos() {
        foreach (var l in _landmarks.Values) {
            l.OnDrawGizmos();
        }
    }
}

public abstract class Landmark {
    // An individual landmark, calculated once,
    // then memoized for future use
    
    // The memoized postions for right/left
    // (you have to calculate bothm as you may want
    //  a left hand to reach to the right breast)
    Vector3 _leftPosition;
    Vector3 _rightPosition;
    protected LimbAnimator _limb;
    protected Transform transform;  // Shorthand
    public Vector3 position { get; private set; }

    public Landmark(LimbAnimator limb) {
        _limb = limb;
        transform = _limb.transform;
        _leftPosition = Calcualte(true);
        _rightPosition = Calcualte(false);
    }

    // The method that each subclass will implement
    // TODO is the 'left' always just reflected
    // about the y axis? In which case, it can be
    // handled by the base class?
    protected abstract Vector3 Calcualte(bool left);

    public Vector3 Get(bool? left=null) {
        // Return the desired position
        var knownLeft = IsLeft(left);
        if (knownLeft) return _leftPosition;
        return _rightPosition;
    }
    public void Recalculate() {
        // If we ever need to reset landmarks on a being
        _leftPosition = Calcualte(true);
        _rightPosition = Calcualte(false);
    }

    // Helpers for use in the subclass's 'Calcualte'
    public TorsoAnimator GetTorso() { return _limb.GetTorso(); }
    public float GetLength() { return _limb.GetLength(); }
    public GameObject GetRootBone() { return _limb.GetRootBone(); }

    public bool IsLeft(bool? left) {
        // Helper method for when a method is called
        // with a bool? for left - use it if not null,
        // otherwise default to if we are on left
        if (left != null) return (bool) left;
        return _limb.IsLeft();
    }

    internal void OnDrawGizmos() {
        // Show the landmark's name in space
        var name = GetType().Name;
        Handles.Label(transform.position+Get(), name);
        Gizmos.color = Color.gray;
        Gizmos.DrawSphere(transform.position+Get(), .001f);
    }
}

public class Holster : Landmark {
    // A space near the right hip, where one might
    // brace a spear, or sheathe a sword, etc
    public Holster(LimbAnimator limb) : base(limb) {}
    protected override Vector3 Calcualte(bool left) {
        var pos = GetTorso().GetRootBone().transform.position;
        var sideOffset = GetTorso().GetWidth() * .5f * transform.right;
        if (left) return pos - sideOffset;
        else return pos + sideOffset;
    }
}
public class Waist : Landmark {
    // A space a by the waist, where
    // one might hold their arms comfortable
    // (not limp)
    public Waist(LimbAnimator limb) : base(limb) {}
    protected override Vector3 Calcualte(bool left) {
        var pos = GetTorso().GetRootBone().transform.position;
        var sideOffset = GetTorso().GetWidth() * .6f * transform.right;
        var vertOffset = GetTorso().GetLength() * .1f * transform.up;
        pos += vertOffset;
        if (left) return pos - sideOffset;
        else return pos + sideOffset;
    }
}
public class WideWaist : Landmark {
    // Simmilar to WaistPos, but arms a little more to the side
    public WideWaist(LimbAnimator limb) : base(limb) {}
    protected override Vector3 Calcualte(bool left) {
        var pos = new Waist(_limb).Get(left);
        var sideOffset = GetTorso().GetWidth() * .3f * transform.right;
        if (left) return pos - sideOffset;
        else return pos + sideOffset;
    }
}
public class Chest : Landmark {
    // The space infront of the chest
    public Chest(LimbAnimator limb) : base(limb) {}
    protected override Vector3 Calcualte(bool left) {
        var pos = GetTorso().GetChestBone().transform.position;
        var sideOffset = GetTorso().GetWidth() * .25f * transform.right;
        var frontOffset = GetTorso().GetDepth() * .5f * transform.forward;
        pos += frontOffset;
        if (left) return pos - sideOffset;
        else return pos + sideOffset;
    }
}
public class Face : Landmark {
    // The space infront of the face
    public Face(LimbAnimator limb) : base(limb) {}
    protected override Vector3 Calcualte(bool left) {
        var pos = GetTorso().target.transform.position;
        var sideOffset = GetTorso().GetWidth() * .1f * transform.right;
        var frontOffset = GetTorso().GetDepth() * .5f * transform.forward;
        // var vertOffset = GetTorso().GetLength() * -.2f * transform.up;
        pos += frontOffset;
        if (left) return pos - sideOffset;
        else return pos + sideOffset;
    }
}
public class Raised : Landmark {
    // The space over the shoulder,
    // where someone might raise a weapon
    public Raised(LimbAnimator limb) : base(limb) {}
    protected override Vector3 Calcualte(bool left) {
        var pos = GetTorso().target.transform.position;
        var sideOffset = GetTorso().GetWidth() * 1.1f * transform.right;
        var frontOffset = GetTorso().GetWidth() * -.2f * transform.forward;
        pos += frontOffset;
        if (left) return pos - sideOffset;
        else return pos + sideOffset;
    }
}
public class Extended : Landmark {
    // The space directly in front, where someone might
    // 'end' a punch
    public Extended(LimbAnimator limb) : base(limb) {}
    protected override Vector3 Calcualte(bool left) {
        var pos = GetTorso().GetChestBone().transform.position;
        var frontOffset = GetLength() * 1f * transform.forward;
        pos += frontOffset;
        // For now, no side offset
        return pos;
    }
}
public class ExtendedBack : Landmark {
    // The space directly behind, where someone rear back
    public ExtendedBack(LimbAnimator limb) : base(limb) {}
    protected override Vector3 Calcualte(bool left) {
        var pos = GetTorso().GetChestBone().transform.position;
        var frontOffset = GetLength() * -1.8f * transform.forward;
        pos += frontOffset;
        // For now, no side offset
        return pos;
    }
}
public class Lowered : Landmark {
    // Hands all the way down by your sides
    public Lowered(LimbAnimator limb) : base(limb) {}
    protected override Vector3 Calcualte(bool left) {
        var pos = GetRootBone().transform.position;
        var vertOffset = GetLength() * .85f * -transform.up;
        pos += vertOffset;
        return pos;
    }
}
public class LeanBack : Landmark {
    // Slght lean for torso target
    public LeanBack(LimbAnimator limb) : base(limb) {}
    protected override Vector3 Calcualte(bool left) {
        var pos = GetTorso().GetChestBone().transform.position;
        var frontOffset = GetTorso().GetDepth() * .1f * -transform.forward;
        pos += frontOffset;
        return pos;
    }
}
public class LeanForward : Landmark {
    // Slght lean for torso target
    public LeanForward(LimbAnimator limb) : base(limb) {}
    protected override Vector3 Calcualte(bool left) {
        var pos = GetTorso().GetChestBone().transform.position;
        var frontOffset = GetTorso().GetDepth() * .1f * transform.forward;
        pos += frontOffset;
        return pos;
    }
}