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

    // TODO REMOVE
    // just for testing
    public string testPos;

    protected override void BeforeStart() {
        // TODO for now, ignore shoulder, and move bicept
        // TODO is this ok?
        rootBone = GetRootBone().transform.GetChild(0).gameObject;
    }

    protected override void AfterStart() {
        // Elbows point 'behind' when starting in T-pose
        hint.transform.position = GetMidBone().transform.position - transform.forward * GetLength();
        // Correctly attach shoulders before toso has chance to move
        ParentShoulder();
        // For now, swapping to using a springy connection
        SetupSpring();
    }

    public void Update() {
        // Handle normal 'default' behavior for arms
        // - when they arent using weapons, or items, etc
        // (have to figure how to make this easily extensible still)

        // Testing / debug
        if (Player.IsDevMode()) return;
        if (testPos != "") {
            PlaceTarget(GetPos(testPos), true);
            return;
        }

        if (being.IsRunning()) RunCycle();
        else if (being.IsWalking()) WalkCycle();
        else Rest();
    }

    void Rest() {
        // When standing still, bring arms down to sides
        PlaceTarget(DownPos(IsLeft()), RotDown(), true);
    }

    void RunCycle() {
        // Cycle for 'pumping' arms when running

        // If we don't have opposing leg,
        // you wouldn't pump that arm, right?
        var leg = GetLeg(true);
        if (leg == null) return;

        // Use opposite leg to show us where we are
        // in the arm pump motion
        var legPos = leg.target.transform.localPosition;

        var downPos = HolsterPos(IsLeft());
        var upPos = BoxerPos(IsLeft());

        // Remap where the foot is to a 0-1 lerpable progress
        var stepRadius = leg.MaxStepLength();
        var progress = Remap(legPos.z, -stepRadius, stepRadius, .2f, .8f);
        var currentPos = Vector3.Lerp(downPos, upPos, progress);

        // Rotate the placement around the movement vector
        // Limiting to 'forward' angles
        var angleLimit = 35;
        var moveEuler = (
            Quaternion.LookRotation(being.WalkVelocity())
            * Quaternion.Inverse(transform.rotation)
        ).eulerAngles;
        if (moveEuler.y > 180) moveEuler.y -= 360;
        if (moveEuler.y > -angleLimit && moveEuler.y < angleLimit) {
            currentPos = Quaternion.Euler(moveEuler) * currentPos;
        }
        
        // Simmilarly, lets try lerping our rotation
        var currentRot = Quaternion.Lerp(RotForward(), RotUp(), progress);
        currentRot = Quaternion.Euler(moveEuler) * currentRot;
        PlaceTarget(currentPos, currentRot, true);
    }

    void WalkCycle() {
        // For now, just lifting arms a bit
        PlaceTarget(WaistPos(IsLeft()), RotForward(), true);
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
        var chest = GetTorso().GetChestBone().transform;

        // Set offset if 1st time:
        if (_shoulderOffset == default(Vector3)) {
            _shoulderOffset = shoulder.position - chest.position;
        } 
        shoulder.position = chest.position + (chest.rotation * _shoulderOffset);
    }
}
