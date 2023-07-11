using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class LimbAnimator : CustomBehavior {
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

    // Typically, set programatically
    // - but can be overridden in prefab editor
    public GameObject rootBone;
    public GameObject midBone;
    public GameObject tipBone;

    // Set programatically
    internal GameObject target;
    internal GameObject skeleton;
    internal GameObject hint;
    // The character/monster/etc that this limb belongs to
    internal Being being;
    internal LimbLandmarks landmarks;

    // Just some helper variables
    protected Vector3 _targetStartPos;
    protected Quaternion _targetStartRot;
    protected Vector3 _rootStartPos;
    protected Quaternion _rootStartRot;

    // Wheter to setup twoBone or chain
    public bool twoBone = true;

    // TODO REMOVE
    // just for testing
    public string testPos;
    
    void Start() {
        being = transform.parent.GetComponent<Being>();
        skeleton = FindContains("Skeleton");
        target = CreateEmpty("Target", GetTipBone());

        // Establish bounds before we setup any IK
        GetBounds();
        // Provide before/after logic hooks for the children
        BeforeStart();
        SetupRig();
        if (twoBone) SetupTwoBone();
        else SetupChain();

        // Keep handy references to human understandable positions,
        // such as 'holster', 'extended', etc
        landmarks = new LimbLandmarks(this);

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
        var hintOffset = transform.forward * 0.2f * GetLength();
        var hintPos = GetMidBone().transform.position + hintOffset;
        hint = CreateEmpty("Hint", hintPos);

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

    protected void SetupSpring() {
        // It may be that some limbs, we want 'secondary metion'
        // - like arms. In this case, instead of moving the target itself,
        // we will move a spring root that is elasticly connected to the target
        var spring = gameObject.AddComponent<LimbSpring>();
        // For now, we will have our 'target' be the spring root,
        // and just trust the ik target to follow
        // TODO is this sloppy?
        target = spring.Setup();
    }

    public GameObject GetRootBone() {
        // If the bone is not explicitly set,
        // we will assume the root bone for ik
        // is the 1st bone under skeleton
        if (rootBone != null) return rootBone;
        rootBone = ChildOf(skeleton);
        return rootBone;
    }

    public GameObject GetMidBone() {
        // If the bone is not explicitly set,
        // we will assume it is just after root
        if (midBone != null) return midBone;
        midBone = ChildOf(GetRootBone());
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
            currentBone = ChildOf(currentBone).transform;
        }
        return currentBone.parent.gameObject;
    }

    public GameObject ChildOf(Transform t, bool last=true) {
        // Helper method to get child of transform (or gameObject)
        var idx = 0;
        if (last) idx = t.childCount - 1;
        return t.GetChild(idx).gameObject;
    }
    public GameObject ChildOf(GameObject g, bool last=true) {
        return ChildOf(g.transform, last);
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

    public void PlaceTarget(Vector3 destination, bool local=true) {
        // Just becasue we will use it often -
        // moving the target more smoothly with lerp
        // TODO the lerp speed will likely be depending on size and
        // 'personality' - like the idea for 'frenzy' or what-have-you
        // TODO should we just default to using LimbSpring and not
        // use this?
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
    public void PlaceTarget(Vector3 destination, Quaternion rotation, bool local=true) {
        var baseRotSpeed = 100f;
        var progress = baseRotSpeed * Time.deltaTime;
        PlaceTarget(rotation, local);
        PlaceTarget(destination, local);
    }
    public void PlaceTarget(Vector3 destination, Vector3 lookAt, bool local=true) {
        TargetLookAt(lookAt);
        PlaceTarget(destination, local);
    }
    public void PlaceTarget(Quaternion rotation, bool local=true) {
        var baseRotSpeed = 30f;
        var progress = baseRotSpeed * Time.deltaTime;
        if (local) {
            target.transform.localRotation = Quaternion.Slerp(
                target.transform.localRotation, rotation, progress);
        } else {
            target.transform.rotation = Quaternion.Slerp(
                target.transform.rotation, rotation, progress);
        }
    }
    public void TargetLookAt(Vector3 lookAt, bool local=true) {
        Quaternion rot = LookRotation(lookAt, local);
        PlaceTarget(rot, local);
    }
    public Quaternion LookRotation(Vector3 lookAt, bool local=true) {
        // Just shorithand for Quaternion.LookRotation
        if (local) return Quaternion.LookRotation(lookAt, Vector3.up);
        else return Quaternion.LookRotation(lookAt, transform.up);
    }

    public void SnapTarget(Vector3 destination, bool local=true) {
        // Instantly move the IK target - no smooth lerping
        // TODO maybe rename PlaceTarget to LerpTarget or whatever
        if (local) target.transform.localPosition = destination;
        else target.transform.position = destination;
    }
    public void SnapTarget(Vector3 destination, Quaternion rotation, bool local=true) {
        if (local) target.transform.localRotation = rotation;
        else target.transform.rotation = rotation;
        SnapTarget(destination, local);
    }
    public void SnapTarget(Vector3 destination, Vector3 lookAt, bool local=true) {
        Quaternion rot;
        if (local) rot = Quaternion.LookRotation(lookAt, Vector3.up);
        else rot = Quaternion.LookRotation(lookAt, transform.up);
        SnapTarget(destination, rot, local);
    }
    
    public Vector3 TargetPos(bool local=true) {
        // Return the current position of the target
        if (local) return target.transform.localPosition;
        return target.transform.position;
    }
    public Quaternion TargetRot(bool local=true) {
        // Return the current rotation of the target
        if (local) return target.transform.localRotation;
        return target.transform.rotation;
    }

    Vector3 _bounds;
    public Vector3 GetBounds() {
        // TODO is this implementation bad?
        // How likely is it to be wrong because of width variability
        // along limb length?
        // TODO should we rotate to account for T-pose and whatnot?
        // Maybe just make 'Height' the longest and go from there?
        if (_bounds != default(Vector3)) return _bounds;
        var mesh = GetComponentInChildren<SkinnedMeshRenderer>();
        _bounds = mesh.bounds.size;
        return _bounds;
    }
    public float GetWidth() { return GetBounds().x; }
    public float GetDepth() { return GetBounds().z; }
    public float GetHeight() { return GetBounds().y; }

    bool? _isLeft = null;
    public bool IsLeft() {
        // is arm is on the left or right of the torso
        // TODO maybe refactor to have a stronger idea of
        // 'neither' - in which case make it limb method
        if (_isLeft != null) return (bool) _isLeft;
        _isLeft = GetRootBone().transform.localPosition.x > 0;
        return (bool) _isLeft;
    }

    public LegAnimator GetLeg(bool opposite=false) {
        // TODO should this be limb method?
        // TODO is this leg-finding good?
        var legs = transform.parent.GetComponentsInChildren<LegAnimator>();

        bool getLeft = IsLeft();
        if (opposite) getLeft = !getLeft;
        foreach (LegAnimator l in legs) {
            bool leftMatches = getLeft && l.gameObject.name.Contains(" L");
            bool rightMatches = !getLeft && l.gameObject.name.Contains(" R");
            if (leftMatches || rightMatches) return l;
        }
        return null;
    }

    TorsoAnimator _torso;
    public TorsoAnimator GetTorso() {
        // Get the torso most likely to be associated with this limb
        // (and memoize)
        if (_torso != null) return _torso;
        _torso = transform.parent.GetComponentInChildren<TorsoAnimator>();
        return _torso;
    }

    void OnDrawGizmos() {
        if (target == null) return;
        var size = .1f;
        var dimensions = new Vector3(size, size, size);
        // Translucent yellow
        Gizmos.color = new Color(.1f, 1f, 1f, .5f);
        Gizmos.DrawCube(target.transform.position, dimensions);
        if (hint == null) return;
        // Translucent blue
        Gizmos.color = new Color(0f, 0f, 1f, .5f);
        Gizmos.DrawCube(hint.transform.position, dimensions);
    }
}
