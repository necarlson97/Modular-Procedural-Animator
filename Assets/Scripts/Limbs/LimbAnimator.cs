using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using DitzelGames.FastIK;

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
    internal GameObject hint;
    internal GameObject skeleton;

    internal FastIKFabric fik;

    // The character/monster/etc that this limb belongs to
    internal Being being;
    internal LimbLandmarks landmarks;

    // Just some helper variables
    protected Vector3 _tipStartPos;
    protected Quaternion _tipStartRot;
    protected Vector3 _rootStartPos;
    protected Quaternion _rootStartRot;

    void Start() {
        being = transform.parent.GetComponent<Being>();
        skeleton = GetSkeleton();
        // TODO for now we don't actually memoize the mesh gameobject,
        // but we need to ensure it is a child of mine
        GetMesh();

        // Establish bounds before we setup any IK
        GetBounds();
        // Provide before/after logic hooks for the children
        BeforeStart();
        SetupIK();

        // Keep handy references to human understandable positions,
        // such as 'holster', 'extended', etc
        landmarks = new LimbLandmarks(this);

        AfterStart();
    }
    protected virtual void BeforeStart(){}
    protected virtual void AfterStart(){}

    protected GameObject GetSkeleton() {
        // Return this limb's skeleton, which may either be a child
        // placed in the editor, or inside of a sibling prefab
        // (the exire FBX exported from blender.)
        var skele = FindContains("Skeleton");
        if (skele != null) return skele;

        // For every sibling, search their children for skeletons
        // If there is a skeleton that contains this limbs name,
        // it is likey mine
        var allSkeletons = FindAllContains("Skeleton", transform.parent);
        foreach (GameObject s in allSkeletons) {
            if (s.name.Contains(name)) {
                s.transform.parent = transform;
                return s;
            }
        }
        Debug.LogError("Did not find skeleton for: "+name);
        return null;
    }

    protected GameObject GetMesh() {
        // Return this limb's mesh, which may either be a child
        // placed in the editor, or inside of a sibling prefab
        // (the exire FBX exported from blender)
        var mesh = FindContains("Mesh");
        if (mesh != null) return mesh;

        // For every sibling, search their children.
        // If a child contains this limbs name,
        // it is likey mine
        var allMeshes = FindAllContains("Mesh", transform.parent);
        foreach (GameObject m in allMeshes) {
            if (m.name.Contains(name)) {
                m.transform.parent = transform;
                return m;
            }
        }
        Debug.LogError("Did not find mesh for: "+name);
        return null;
    }

    internal Vector3 hintStart;
    void SetupIK() {
        // Create the objects / components needed for IK
        // (because Unity's IK setup is somewhat awkward,
        // and we want to do it, reapeatidly, 100s of times,
        // better just to do it programatically. Thanks chat-gpt)

        fik = SetupIK(GetRootBone().transform, GetTipBone().transform);
        target = fik.Target.gameObject;
        hint = fik.Pole.gameObject;
        hintStart = hint.transform.localPosition;
    }

    public FastIKFabric SetupIK(Transform root, Transform tip, bool shouldHint=true, string name="") {
        // Create the objects / components needed for IK
        // (because IK setup is somewhat awkward,
        // and we want to do it, reapeatidly, 100s of times,
        // better just to do it programatically. Thanks chat-gpt)
        // Root - root bone
        // Tip - tip bone
        // shouldHint - wheter to add a hint pole that orients  the 'elbow of the joint'

        // Create IK target
        var newTarget = CreateEmpty("Target"+name, tip);
        var newFik = tip.gameObject.AddComponent<FastIKFabric>();
        newFik.Target = newTarget.transform;
        newFik.Iterations = 20; // For now, increasing default

        if (shouldHint) {
            // For now, just create a hint empty right infront of the limb,
            // - then we can worry about changing it in a subscript
            var hintOffset = transform.forward * 0.8f * GetLength();
            var hintPos = GetMidBone().transform.position + hintOffset;
            var newHint = CreateEmpty("Hint"+name, hintPos);
            newFik.Pole = newHint.transform;
        }

        // Find out the chain length programatically,
        // but default to 2 if needed
        newFik.ChainLength = 2;
        var separation = TransSeparation(root, tip);
        if (separation == null) {
            Debug.LogError(
                "Did not find chain length between root: "
                +root.gameObject+" and tip: "+tip.gameObject
            );
        } else if (separation <= 0) {
            Debug.LogError(
                "Root was sibling/child of tip ("+separation+") root: "
                +root.gameObject+", tip: "+tip.gameObject
            );
        }
        // TODO remove log
        newFik.ChainLength = (int) separation;
        return newFik;
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

    public int? TransSeparation(Transform t1, Transform t2) {
        // Get the hierarchical separation of these two transforms, e.g:
        // if t1 is the parent of t2: return 1
        // if t2 is the grandparent of t1: return -2
        // if they are siblings: return 0
        // if they are not related: return null

        // We perform this by:
        // * Traverse up from each transform,
        //    counting the steps until you reach a common ancestor.
        // * The separation is the difference in the number of steps
        //    to the common ancestor for each transform.
        // If no common ancestor is found (other than the root),
        // they are not related, and the function returns null.

        int t1Depth = 0;
        int t2Depth = 0;
        Transform currentT1 = t1;
        Transform currentT2 = t2;

        // Find depth in the hierarchy for each transform
        while (currentT1.parent != null) {
            t1Depth++;
            currentT1 = currentT1.parent;
        }
        while (currentT2.parent != null) {
            t2Depth++;
            currentT2 = currentT2.parent;
        }
        int seperation = t2Depth - t1Depth;

        // Reset to start positions
        currentT1 = t1;
        currentT2 = t2;

        // Equalize the depth
        while (t1Depth > t2Depth) {
            currentT1 = currentT1.parent;
            t1Depth--;
        }
        while (t2Depth > t1Depth) {
            currentT2 = currentT2.parent;
            t2Depth--;
        }

        // Traverse up the hierarchy to find the common ancestor
        while (currentT1 != null && currentT2 != null) {
            if (currentT1 == currentT2) {
                return seperation; // Return the difference in depth
            }
            currentT1 = currentT1.parent;
            currentT2 = currentT2.parent;
        }

        // No common ancestor other than root
        return null;
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
        _length = Vector3.Distance(rootPos, _tipStartPos);
        return _length;
    }

    public void Place(GameObject obj, Vector3 destination, bool local=true) {
        // Just becasue we will use it often -
        // moving the target/root/whatever more smoothly with lerp
        // TODO the lerp speed will likely be depending on size and
        // 'personality' - like the idea for 'frenzy' or what-have-you
        // TODO should we just default to using LimbSpring and not
        // use this?
        var baseSpeed = 30f;
        var progress = baseSpeed * Time.deltaTime;
        if (local) {
            obj.transform.localPosition = Vector3.Lerp(
                obj.transform.localPosition, destination, progress);
        } else {
            obj.transform.position = Vector3.Lerp(
                obj.transform.position, destination, progress);
        }
    }
    public void Place(GameObject obj, Vector3 destination, Quaternion rotation, bool local=true) {
        var baseRotSpeed = 100f;
        var progress = baseRotSpeed * Time.deltaTime;
        Place(obj, rotation, local);
        Place(obj, destination, local);
    }
    public void Place(GameObject obj, Vector3 destination, Vector3 lookAt, bool local=true) {
        LookAt(obj, lookAt);
        Place(obj, destination, local);
    }
    public void Place(GameObject obj, Quaternion rotation, bool local=true) {
        var baseRotSpeed = 30f;
        var progress = baseRotSpeed * Time.deltaTime;
        if (local) {
            obj.transform.localRotation = Quaternion.Slerp(
                obj.transform.localRotation, rotation, progress);
        } else {
            obj.transform.rotation = Quaternion.Slerp(
                obj.transform.rotation, rotation, progress);
        }
    }

    public void Snap(GameObject obj, Vector3 destination, bool local=true) {
        // Instantly move a root/target/whatever - no smooth lerping
        // TODO maybe rename 'Place' to 'Lerp' or whatever
        if (local) obj.transform.localPosition = destination;
        else obj.transform.position = destination;
    }
    public void Snap(GameObject obj, Vector3 destination, Quaternion rotation, bool local=true) {
        if (local) obj.transform.localRotation = rotation;
        else obj.transform.rotation = rotation;
        Snap(obj, destination, local);
    }
    public void Snap(GameObject obj, Vector3 destination, Vector3 lookAt, bool local=true) {
        Quaternion rot;
        if (local) rot = Quaternion.LookRotation(lookAt, Vector3.up);
        else rot = Quaternion.LookRotation(lookAt, transform.up);
        Snap(obj, destination, rot, local);
    }

    // Methods for moving target
    public void SnapTarget(Vector3 destination, bool local=true) {
        Snap(target, destination, local);
    }
    public void SnapTarget(Vector3 destination, Quaternion rotation, bool local=true) {
        Snap(target, destination, rotation, local);
    }
    public void SnapTarget(Vector3 destination, Vector3 lookAt, bool local=true) {
        Snap(target, destination, lookAt, local);
    }
    public void PlaceTarget(Vector3 destination, bool local=true) {
        Place(target, destination, local);
    }
    public void PlaceTarget(Vector3 destination, Quaternion rotation, bool local=true) {
        Place(target, destination, rotation, local);
    }
    public void PlaceTarget(Vector3 destination, Vector3 lookAt, bool local=true) {
        Place(target, destination, lookAt, local);
    }
    public void PlaceTarget(Quaternion rotation, bool local=true) {
        Place(target, rotation, local);
    }

    // Simmilar methods, for changing root bone pos
    // (for, say, parenting. Should be a better way...)
    // TODO rename to something else? Even though it feels like snapping the root,
    // we are actually moving this gameovject - as we aren't
    // allowed to touch any actual bone, including the root and skeleton
    public void SnapRoot(Vector3 destination, bool local=true) {
        Snap(gameObject, destination, local);
    }
    public void SnapRoot(Vector3 destination, Quaternion rotation, bool local=true) {
        Snap(gameObject, destination, rotation, local);
    }
    public void SnapRoot(Vector3 destination, Vector3 lookAt, bool local=true) {
        Snap(gameObject, destination, lookAt, local);
    }
    public void PlaceRoot(Vector3 destination, bool local=true) {
        Place(gameObject, destination, local);
    }
    public void PlaceRoot(Vector3 destination, Quaternion rotation, bool local=true) {
        Place(gameObject, destination, rotation, local);
    }
    public void PlaceRoot(Vector3 destination, Vector3 lookAt, bool local=true) {
        Place(gameObject, destination, lookAt, local);
    }
    public void PlaceRoot(Quaternion rotation, bool local=true) {
        Place(gameObject, rotation, local);
    }

    public void LookAt(GameObject obj, Vector3 lookAt, bool local=true) {
        Quaternion rot = LookRotation(lookAt, local);
        Place(obj, rot, local);
    }
    public Quaternion LookRotation(Vector3 lookAt, bool local=true) {
        // Just shorithand for Quaternion.LookRotation
        if (local) return Quaternion.LookRotation(lookAt, Vector3.up);
        else return Quaternion.LookRotation(lookAt, transform.up);
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

        // We use these to calcualte length, so make sure they are set here
        var tipPos = GetTipBone().transform.position;
        _tipStartPos = transform.InverseTransformPoint(tipPos);
        _tipStartRot = GetTipBone().transform.localRotation;
        var rootPos = GetRootBone().transform.position;
        _rootStartPos = transform.InverseTransformPoint(rootPos);
        _rootStartRot = GetRootBone().transform.localRotation;

        var mesh = GetComponentInChildren<SkinnedMeshRenderer>();
        _bounds = mesh.bounds.size;
        return _bounds;
    }
    public Vector3 ResetBounds() {
        _bounds = default(Vector3);
        return GetBounds();
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
        _isLeft = GetRootBone().transform.localPosition.x < 0;
        return (bool) _isLeft;
    }

    public LegAnimator GetLeg(bool opposite=false) {
        // TODO is this leg-finding good?
        var legs = GetLegs();

        bool getLeft = IsLeft();
        if (opposite) getLeft = !getLeft;
        foreach (LegAnimator l in legs) {
            bool leftMatches = getLeft && l.gameObject.name.Contains(" L");
            bool rightMatches = !getLeft && l.gameObject.name.Contains(" R");
            if (leftMatches || rightMatches) return l;
        }
        return null;
    }
    public LegAnimator[] GetLegs() {
        // Return the array of all legs
        return transform.parent.GetComponentsInChildren<LegAnimator>();
    }

    TorsoAnimator _torso;
    public TorsoAnimator GetTorso() {
        // Get the torso most likely to be associated with this limb
        // (and memoize)
        if (_torso != null) return _torso;
        _torso = transform.parent.GetComponentInChildren<TorsoAnimator>();
        return _torso;
    }

    // Rotations for, say, hands/feet
    // TODO do these depend on start rot?
    // TODO verify all of these
    public Quaternion RotForward() { return Q(-90, 0, 90); }
    public Quaternion RotFlatForward() { return Q(0, 90, 0); }
    public Quaternion RotBackward() { return Q(90, 0, 90); }
    public Quaternion RotUp() { return Q(180, 0, 90); }
    public Quaternion RotDown() { return Q(0, 0, 90); }
    public Quaternion RotOut() { return Q(0, 0, 0); }
    public Quaternion RotIn() { return Q(0, 180, 0); }
    public Quaternion Q(float x, float y, float z) {
        var q = Quaternion.Euler(x, y, z);
        if (IsLeft()) return q;
        return CustomBehavior.ReflectQuaternionX(q);
    }
    public Quaternion Rotation(string name) {
        // Given the string name of a rotation (such as 'Up'),
        // use reflection to call that method
        var method = GetType().GetMethod("Rot" + name, Type.EmptyTypes);
        if (method == null) {
            Debug.LogError("Could not find method for " + name);
            return default(Quaternion);
        }
        return (Quaternion) method.Invoke(this, null);
    }

    public float GetProgress(float hertz=1f) {
        // Get progress 0-1 for a cyclic animation,
        // such as steping, or breathing
        return (Time.time * hertz) % 1;
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

        landmarks.OnDrawGizmos();
    }
}
