using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class LimbAnimator : MonoBehaviour {
    // Holds code for setting up IK for arm / leg / torso / etc
    // - intended to be extended (TODO abstract?)
    // Requires:
    // a child gameobject with the word 'Target'
    // a child skeleton with the word 'Skeleton'
    // Ideally:
    // skeleton is mostly a single long chain (see GetRootBone, etc)
    // this limb is the child of a 'being'
    // leg/arms have 'parners' that are named:
    // 'Leg L' and 'Leg R'

    // The character/monster/etc that this limb belongs to
    protected Being being;

    // Typically, set programatically - but can be overridden
    public GameObject rootBone;
    public GameObject midBone;
    public GameObject tipBone;

    // Set programatically
    [System.NonSerialized]
    public GameObject target;
    [System.NonSerialized]
    public GameObject skeleton;
    [System.NonSerialized]
    public GameObject hint;

    // Just some helper variables
    protected Vector3 _targetStartPos;
    protected Quaternion _targetStartRot;
    protected Vector3 _rootStartPos;
    protected Quaternion _rootStartRot;

    // Wheter to setup twoBone or chain
    public bool twoBone = true;
    
    void Start() {
        being = transform.parent.GetComponent<Being>();
        target = FindContains("Target");
        skeleton = FindContains("Skeleton");

        // Provide before/after logic hooks for the children
        BeforeStart();
        SetupRig();
        if (twoBone) SetupTwoBone();
        else SetupChain();
        AfterStart();
    }
    protected virtual void BeforeStart(){}
    protected virtual void AfterStart(){}


    void SetupRig() {
        // Create the objects / components needed for IK
        // (because Unity's IK setup is somewhat awkward,
        // and we want to do it, reapeatidly, 100s of times,
        // better just to do it programatically. Thanks chat-gpt)

        _targetStartPos = target.transform.localPosition;
        _targetStartRot = target.transform.localRotation;
        _rootStartPos = GetRootBone().transform.localPosition;
        _rootStartRot = GetRootBone().transform.localRotation;

         // Setup animator
        var animator = gameObject.GetComponent<Animator>();
        if (animator == null) {
            animator = gameObject.AddComponent<Animator>();
        }

        // Setup rig builder on the root character GameObject
        var rigBuilder = gameObject.GetComponent<RigBuilder>();
        if (rigBuilder == null) {
            rigBuilder = gameObject.AddComponent<RigBuilder>();
        }

        // Setup rig
        var rig = skeleton.GetComponent<Rig>();
        if (rig == null) {
            rig = skeleton.AddComponent<Rig>();
            // Add the rig to the rig builder
            var rigLayer = new RigLayer(rig, true);
            rigBuilder.layers.Add(rigLayer);
        }
    }

    protected void SetupChain() {
        // Setup IK chain between root and tip,
        // (ignoring midbone and hint)
        var ik = skeleton.GetComponent<ChainIKConstraint>();
        if (ik == null)  {
            ik = skeleton.AddComponent<ChainIKConstraint>();
        }
        
        ik.data.root = GetRootBone().transform;
        ik.data.tip = GetTipBone().transform;
        ik.data.target = target.transform;
        ik.data.chainRotationWeight = 1f;
        ik.data.tipRotationWeight = 1f;
        ik.data.maintainTargetPositionOffset = true;
        ik.data.maintainTargetRotationOffset = true;
        ik.data.maxIterations = 10;
        ik.data.tolerance = 0.001f;

        // Workaround - see 'EnableRig'
        GetComponent<RigBuilder>().enabled = false;
        Invoke("EnableRig", 0);
    }

    protected void SetupTwoBone() {
        // Set up a two bone constraint, using midbone,
        // where everything before/after is basically rigid
        // (which may be more useful for some limbs
        // - and more perfomant)
         var ik = skeleton.GetComponent<TwoBoneIKConstraint>();
        if (ik == null) {
            ik = skeleton.AddComponent<TwoBoneIKConstraint>();
        }
        // For now, just create a hint empty right infront of the limb,
        // - then we can worry about changing it in a subscript
        hint = new GameObject("Hint");
        hint.transform.SetParent(transform);
        var hintOffset = transform.forward * 0.2f * GetLength();
        hint.transform.position = GetMidBone().transform.position + hintOffset;

        // Setting IK information
        ik.data.target = target.transform;
        ik.data.hint = hint.transform;
        ik.data.root = GetRootBone().transform;
        ik.data.mid = GetMidBone().transform;
        ik.data.tip = GetTipBone().transform;
        ik.data.targetPositionWeight = 1;
        ik.data.targetRotationWeight = 1;
        ik.data.hintWeight = 1;

        // Workaround - see 'EnableRig'
        GetComponent<RigBuilder>().enabled = false;
        Invoke("EnableRig", 0);
    }

    void EnableRig() {
        // There is a bug where the ik does not work unless
        // RigBuilder was disabled/reenabled in editor
        // - perhaps has to do with targetPositionWeight not
        // being properly updated. For now, this works
        // TODO DOES IT??
        GetComponent<RigBuilder>().enabled = true;
    }

    public GameObject GetRootBone() {
        // If the bone is not explicitly set,
        // we will assume the root bone for ik
        // is the 1st bone under skeleton
        if (rootBone != null) return rootBone;
        // TODO for now, assume 'last'
        // - mostly because 'thigh' later alpha than 'pelvis'
        var last = skeleton.transform.childCount - 1;
        rootBone = skeleton.transform.GetChild(last).gameObject;
        return rootBone;
    }

    public GameObject GetMidBone() {
        // If the bone is not explicitly set,
        // we will assume it is just after root
        if (midBone != null) return midBone;
        midBone = GetRootBone().transform.GetChild(0).gameObject;
        return midBone;
    }

    public GameObject GetTipBone() {
        // If the bone is not explicitly set,
        // we will assume the skeleton is a linear
        // chain, and the tip is near the end
        if (tipBone != null) return tipBone;
        tipBone = GetLastBone();
        return tipBone;
    }

    public GameObject GetLastBone() {
        // Get the last bone - which is the
        // penultimate child in the root's descendents
        // (Note: you can use the last child,
        //  but that is the bone's tail rather than head)

        Transform currentBone = GetRootBone().transform;
        while (currentBone.childCount > 0) {
            var last = currentBone.childCount - 1;
            currentBone = currentBone.GetChild(last);
        }
        return currentBone.parent.gameObject;
    }

    float _length = -1;
    public float GetLength() {
        // Returns the 'length' of the limb,
        // ideally this would be calculated by adding
        // up bones or something, but for now, we assume
        // it started in T-pose or simmilar (thus stretched out)
        // so starting pos call tell us length
        if (_length != -1) return _length;
        var rootPos = GetRootBone().transform.position - transform.position;
        _length = Vector3.Distance(rootPos, _targetStartPos);
        return _length;
    }

    public Vector3 TargetOffset() {
        return _targetStartPos - target.transform.localPosition;
    }

    public GameObject FindContains(string query) {
        // Find a child gameobject if it contains a string
        // (recursive)
        // TODO could be utility
        return FindContains(query, transform);
    }
    public GameObject FindContains(string query, Transform t) {
        if (t.name.Contains(query)){
            return t.gameObject;
        }
        foreach (Transform child in t){
            var found = FindContains(query, child);
            if (found != null) return found;
        }
        return null;
    }

    public void PlaceTarget(Vector3 destination, bool local=false) {
        // Just becasue we will use it often -
        // moving the target more smoothly with lerp
        // TODO the lerp speed will likely be depending on size and
        // 'personality' - like the idea for 'frenzy' or what-have-you
        var baseSpeed = 30f;
        var progress = baseSpeed * Time.deltaTime;
        if (local) {
            target.transform.localPosition = Vector3.Lerp(
                target.transform.localPosition, destination, progress);
        } else {
            target.transform.position = Vector3.Lerp(
                target.transform.position, destination, progress);
        }
    }
    public void PlaceTarget(Vector3 destination, Quaternion rotation, bool local=false) {
        var baseRotSpeed = 100f;
        var progress = baseRotSpeed * Time.deltaTime;
        if (local) {
            target.transform.localRotation = Quaternion.Slerp(
                target.transform.localRotation, rotation, progress);
        } else {
            target.transform.localRotation = Quaternion.Slerp(
                target.transform.localRotation, rotation, progress);
        }
        PlaceTarget(destination, local);
    }
    public void PlaceTarget(Vector3 destination, Vector3 lookAt, bool local=false) {
        Quaternion rot;
        if (local) {
            rot = Quaternion.LookRotation(lookAt, Vector3.up);
        } else {
            rot = Quaternion.LookRotation(lookAt, transform.up);
        }
        PlaceTarget(destination, rot, local);
    }

    // For now, finding proper rotations was not producing
    // expected results with LookAt, and so I'll just provide
    // some hardcoded shorthands for now
    // Note: some might be 1 or so degrees 'off'
    // to encourage lerp to 'choose a desired direction'
    // TODO bit sloppy - and belongs in utility?
    public static Quaternion RotForward() { return Q(0, 90, 90); }
    public static Quaternion RotBackward() { return Q(0, -90, 90); }
    public static Quaternion RotUp() { return Q(0, 90, 1); }
    public static Quaternion RotDown() { return Q(0, 90, 180); }
    public static Quaternion RotLeft() { return Q(0, 0, 90); }
    public static Quaternion RotRight() { return Q(0, 0, -90); }
    public static Quaternion Q(float x, float y, float z) {
        return Quaternion.Euler(x, y, z);   
    }

    public static float Remap(float value, float inStart, float inEnd, float outStart, float outEnd) {
        // Take a value in an expected range, and remap it to a new range
        // TODO move to utility class
        var v = (value - inStart) / (inEnd - inStart) * (outEnd - outStart) + outStart;
        v = Mathf.Max(Mathf.Min(v, outEnd), outStart);
        return v;
    }

    void OnDrawGizmos() {
        if (target == null) return;
        // Translucent red
        var size = .1f;
        var dimensions = new Vector3(size, size, size);
        Gizmos.color = new Color(1f, 0f, 0f, .5f);
        Gizmos.DrawCube(target.transform.position, dimensions);
        if (hint == null) return;
        // Translucent blue
        Gizmos.color = new Color(0f, 0f, 1f, .5f);
        Gizmos.DrawCube(hint.transform.position, dimensions);
    }

}
