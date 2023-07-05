using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Animations;

public class ArmAnimator : LimbAnimator {
    // TODO not exactly sure how we want to struture weapon animation - how that
    // meshes with
    // Might be a way to do it with a 
    // component-based programming methodology
    // - e.g., inheriting 'feindish-idle' code and 'dagger-thrust'
    // from elsewhere

    TorsoAnimator torso;

    protected override void BeforeStart() {
        // TODO is this gameObject structure gaurenteed?
        torso = transform.parent.GetComponentInChildren<TorsoAnimator>();

        // TODO for now, ignore shoulder, and move bicept
        // TODO is this ok?
        rootBone = GetRootBone().transform.GetChild(0).gameObject;
    }

    protected override void AfterStart() {
        // Elbows point 'behind' when starting in T-pose
        hint.transform.position = GetMidBone().transform.position - transform.forward * GetLength();
        // Correctly attach shoulders before toso has chance to move
        ParentShoulder();
    }

    public void Update() {
        // TODO for now
        // - have to figure how to make this easily extensible 
        // for both new weapon types, and individual animations
        // - like crushing an item, etc
        if (Player.IsDevMode()) return;
        if (being.Still()) Rest();
        else IdleCycle();
    }

    void Rest() {
        // When standing still, bring arms down to sides
        PlaceTarget(HolsterPos(IsLeft()), RotDown(), true);
    }

    void IdleCycle() {
        // Idle cycle for walking/running around
        // - 'pumping' arms

        // If we don't have opposing leg,
        // you wouldn't pump that arm, right?
        var leg = GetLeg(true);
        if (leg == null) return;

        // Use opposite leg to show us where we are
        // in the arm pump motion
        var legPos = leg.target.transform.localPosition;

        // TODO TESTING
        var downPos = HolsterPos(IsLeft());
        var upPos = BoxerPos(IsLeft());

        // Remap where the foot is to a 0-1 lerpable progress
        var stepRadius = leg.MaxStepLength();
        var progress = Remap(legPos.z, -stepRadius, stepRadius, .2f, .8f);
        var currentPos = Vector3.Lerp(downPos, upPos, progress);

        // Simmilarly, lets try lerping our rotation
        var currentRot = Quaternion.Lerp(RotDown(), RotUp(), progress);
        PlaceTarget(currentPos, currentRot, true);
    }

    public void LateUpdate() {
        ParentShoulder();
    }

    Vector3 _shoulderOffset;
    void ParentShoulder() {
        // Keep the shoulder attached to the torso as it leans
        // and whatnot
        // Note: we canot use ParentConstraint components as
        // it messes up IK targeting (for now)
        // Nor can we use just getting the info from an intermediary
        // bone - as strangley the localPosition appears unchained
        // for bones themselves - but does change on the
        // 'attachment' child we will create
        // We may be able to use typical parenting, but it seems
        // less clean for now

        // Shorthand for the bone transforms
        var shoulder = skeleton.transform.GetChild(0);
        var chest = torso.GetChestBone().transform;

        // Set offset if 1st time:
        if (_shoulderOffset == Vector3.zero) {
            _shoulderOffset = shoulder.position - chest.position;
        } 
        shoulder.position = chest.position + (chest.rotation * _shoulderOffset);
    }

    // TODO not sure if these 'body sense'
    // methods should be elsewhere - perhaps torso?
    // Or a BeingAnimator script on the parent gameObject?
    Vector3 _holsterPos;
    public Vector3 HolsterPos(bool left) {
        // A space near the right hip, where one might
        // brace a spear, or sheathe a sword, etc
        if (_holsterPos != Vector3.zero) return _holsterPos;
        var hipPos = torso.GetRootBone().transform.position;
        var hipOffset = torso.GetWidth() * .6f * transform.right;
        // TODO technically, we should use half waist width,
        // but leaving for now

        if (left) _holsterPos = hipPos - hipOffset;
        else _holsterPos = hipPos + hipOffset;
        return _holsterPos;
    }

    Vector3 _boxerPos;
    public Vector3 BoxerPos(bool left) {
        // The space infront of the chest where a boxer
        // might hold their hands
        if (_boxerPos != Vector3.zero) return _boxerPos;
        var chest = torso.GetChestBone().transform.position;
        var sideOffset = torso.GetWidth() * .25f * transform.right;
        var frontOffset = torso.GetWidth() * transform.forward;
        _boxerPos = chest + frontOffset;

        if (left) _boxerPos -= sideOffset;
        else _boxerPos += sideOffset;
        return _boxerPos;
    }

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
}
