using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimbLandmarks {
    // Class for handling the landmarks
    // - reference points that ease making the procedural animations for limbs:
    // a holster position, fully extended, fully at rest, etc

    LimbAnimator _limb;
    Transform transform;
    public LimbLandmarks(LimbAnimator limb) {
        _limb = limb;
        transform = limb.transform;
        Setup();
    }

    public void Setup() {
        // Initilize all of these positions,
        // ideally while we are in a 'neutral' position
        
        // TODO sloppy - can we leverage dict here?
        foreach (bool left in new bool[]{true, false}) {
            LoweredPos(left);
            ExtendedPos(left);
            RaisedPos(left);
            ChinPos(left);
            BoxerPos(left);
            WaistPos(left);
            HolsterPos(left);
        }
    }

    Dictionary<string, Vector3> _leftPositions = new Dictionary<string, Vector3>();
    Dictionary<string, Vector3> _rightPositions = new Dictionary<string, Vector3>();
    public Vector3 Get(string name, bool? left=null) {
        // Helper for 'get a known, names position'
        // Such as 'ChinPos()'
        // TODO not sure if these 'body sense'
        // methods should be on a different class
        var knownLeft = IsLeft(left);
        var positionDict = _rightPositions;
        if (knownLeft) positionDict = _leftPositions;

        // If we have it memozied, return that
        Vector3 pos = _GetMemoizedPos(name, knownLeft);
        if (pos != default(Vector3)) return pos;

        // Otherwize, call the method name to calcualte it
        var method = this.GetType().GetMethod(name+"Pos");
        if (method == null) Debug.LogError("Could not find method for "+name+"Pos");
        pos = (Vector3) method.Invoke(this, null);
        return pos;
    }
    public Vector3 _GetMemoizedPos(string name, bool? left=null) {
        // Check the memoized positions for one by this name
        var knownLeft = IsLeft(left);
        var positionDict = _rightPositions;
        if (knownLeft) positionDict = _leftPositions;
        // If we have it memozied, return that
        // For now, default to zero - but could switch to null(able)
        if (!positionDict.ContainsKey(name)) return default(Vector3);
        return positionDict[name];
    }
    public Vector3 SetPos(string name, Vector3 pos, bool? left=null) {
        // Memoize a known, named position, such as
        // 'Chin' for 'ChinPos'
        var knownLeft = IsLeft(left);
        var positionDict = _rightPositions;
        if (knownLeft) positionDict = _leftPositions;
        positionDict[name] = pos;
        return pos;
    }

    public Vector3 HolsterPos(bool? left=null) {
        // A space near the right hip, where one might
        // brace a spear, or sheathe a sword, etc
        var name = "Holster";
        var knownLeft = IsLeft(left);
        // If we have it memozied, return that
        Vector3 pos = _GetMemoizedPos(name, knownLeft);
        if (pos != default(Vector3)) return pos;

        var hipPos = GetTorso().GetRootBone().transform.position;
        var hipOffset = GetTorso().GetWidth() * .6f * transform.right;
        // TODO technically, we should use half waist width,
        // but leaving for now
        if (knownLeft) pos = hipPos - hipOffset;
        else pos = hipPos + hipOffset;

        // Memoize the results
        SetPos(name, pos, knownLeft);
        return pos;
    }
    public Vector3 WaistPos(bool? left=null) {
        // A space a by the waist, where
        // one might hold their arms comfortable - but not limp
        var name = "Waist";
        var knownLeft = IsLeft(left);
        // If we have it memozied, return that
        Vector3 pos = _GetMemoizedPos(name, knownLeft);
        if (pos != default(Vector3)) return pos;

        var hipPos = GetTorso().GetRootBone().transform.position;
        var hipOffset = GetTorso().GetWidth() * .6f * transform.right;
        var vertOffset = GetTorso().GetLength() * .1f * transform.up;
        pos = hipPos + vertOffset;
        if (knownLeft) pos -= hipOffset;
        else pos += hipOffset;

        // Memoize the results
        SetPos(name, pos, knownLeft);
        return pos;
    }
    public Vector3 WideWaistPos(bool? left=null) {
        // Simmilar to WaistPos, but arms a little more to the side
        var name = "WideWaist";
        var knownLeft = IsLeft(left);
        // If we have it memozied, return that
        Vector3 pos = _GetMemoizedPos(name, knownLeft);
        if (pos != default(Vector3)) return pos;

        var waistPos = WaistPos();
        var hipOffset = GetTorso().GetWidth() * .3f * transform.right;
        pos = waistPos;
        if (knownLeft) pos -= hipOffset;
        else pos += hipOffset;

        // Memoize the results
        SetPos(name, pos, knownLeft);
        return pos;
    }
    public Vector3 BoxerPos(bool? left=null) {
        // The space infront of the chest where a boxer
        // might hold their hands
        var name = "Boxer";
        var knownLeft = IsLeft(left);
        // If we have it memozied, return that
        Vector3 pos = _GetMemoizedPos(name, knownLeft);
        if (pos != default(Vector3)) return pos;

        var chest = GetTorso().GetChestBone().transform.position;
        var sideOffset = GetTorso().GetWidth() * .25f * transform.right;
        var frontOffset = GetTorso().GetDepth() * .5f * transform.forward;
        pos = chest + frontOffset;

        if (knownLeft) pos -= sideOffset;
        else pos += sideOffset;

        // Memoize the results
        SetPos(name, pos, knownLeft);
        return pos;
    }
    public Vector3 ChinPos(bool? left=null) {
        // The space infront of the chin,
        // where one might hold their hands
        var name = "Chin";
        var knownLeft = IsLeft(left);
        // If we have it memozied, return that
        Vector3 pos = _GetMemoizedPos(name, knownLeft);
        if (pos != default(Vector3)) return pos;

        var head = GetTorso().target.transform.position;
        var sideOffset = GetTorso().GetWidth() * .1f * transform.right;
        var frontOffset = GetTorso().GetDepth() * .5f * transform.forward;
        var vertOffset = GetTorso().GetLength() * -.2f * transform.up;
        pos = head + frontOffset + vertOffset;

        if (knownLeft) pos -= sideOffset;
        else pos += sideOffset;

        // Memoize the results
        SetPos(name, pos, knownLeft);
        return pos;
    }
    public Vector3 RaisedPos(bool? left=null) {
        // The space over the shoulder,
        // where someone might raise a weapon
        var name = "Raised";
        var knownLeft = IsLeft(left);
        // If we have it memozied, return that
        Vector3 pos = _GetMemoizedPos(name, knownLeft);
        if (pos != default(Vector3)) return pos;

        var head = GetTorso().target.transform.position;
        var sideOffset = GetTorso().GetWidth() * 1.1f * transform.right;
        var frontOffset = GetTorso().GetWidth() * -.2f * transform.forward;
        pos = head + frontOffset;

        if (knownLeft) pos -= sideOffset;
        else pos += sideOffset;

        // Memoize the results
        SetPos(name, pos, knownLeft);
        return pos;
    }
    public Vector3 ExtendedPos(bool? left=null) {
        // The space directly in front, where someone might
        // 'end' a punch
        var name = "Extended";
        var knownLeft = IsLeft(left);
        // If we have it memozied, return that
        Vector3 pos = _GetMemoizedPos(name, knownLeft);
        if (pos != default(Vector3)) return pos;

        var chest = GetTorso().GetChestBone().transform.position;
        var frontOffset = GetLength() * .8f * transform.forward;
        pos = chest + frontOffset;

        // Memoize the results
        SetPos(name, pos, knownLeft);
        return pos;
    }
    public Vector3 LoweredPos(bool? left=null) {
        // Hands all the way down by your sides
        var name = "Lowered";
        var knownLeft = IsLeft(left);
        // If we have it memozied, return that
        Vector3 pos = _GetMemoizedPos(name, knownLeft);
        if (pos != default(Vector3)) return pos;

        var root = GetRootBone().transform.position;
        var vertOffset = GetLength() * .85f * -transform.up;
        pos = root + vertOffset;

        // Memoize the results
        SetPos(name, pos, knownLeft);
        return pos;
    }

    // Shorthand
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
    
}
