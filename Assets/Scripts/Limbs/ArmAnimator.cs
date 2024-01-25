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

    protected override void AfterStart() {
        // Elbows point 'behind' when starting in T-pose
        var hintPos = landmarks.Get("WideWaist");
        hintPos += transform.forward * -1f * GetDepth();
        hintPos += transform.up * 1f * GetLength();
        hint.transform.position = hintPos;
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
        // TODO could move to limb
        if (Player.IsDevMode()) return;
        if (testPos != "") {
            PlaceTarget(landmarks.Get(testPos));
            return;
        }

        // TODO we should have a more codified priority system
        // in the limb animator, for passing up what we think the limb
        // should be doing, then attack or whatever can override
        if (being.IsAttacking()) return;
        else if (being.IsGaurding()) Gaurd();
        else if (being.IsWalking()) RunCycle();
        else Rest();
    }

    void Rest() {
        // When standing still, bring arms down to sides
        PlaceTarget(landmarks.Get("Lowered"), RotDown());
    }

    void Gaurd() {
        // When gaurding, place hands where weapon gaurd
        // expects them to be
        // TODO for now - just keep fists by chin
        PlaceTarget(landmarks.Get("Face"), RotUp(IsLeft()));
    }

    void RunCycle() {
        // Cycle for 'pumping' arms when running

        // If we don't have opposing leg,
        // you wouldn't pump that arm... right?
        var leg = GetLeg(true);
        if (leg == null) return;

        // Where that leg is in its step, and we move more when moving faster
        var progress = leg.StepProgress();
        if (!being.MovingFoward()) progress = (progress + 0.5f) % 1;

        // TODO does using foor rot curve work?
        var zCurve = Resources.Load<CurveData>("HandZ").curve;
        var yCurve = Resources.Load<CurveData>("HandY").curve;
        var z = zCurve.Evaluate(progress) * GetLength();
        var y = yCurve.Evaluate(progress) * GetLength();

        var rotCurve = Resources.Load<CurveData>("HandRot").curve;
        var rot = rotCurve.Evaluate(progress);
        var runRot = Quaternion.Slerp(RotDown(), RotForward(), rot);

        // Interpolate depending on how fast we are running
        var restRot = RotDown();
        var currentRot = Quaternion.Lerp(restRot, runRot, being.Rush());

        var restPos = landmarks.Get("Waist");
        var runPos = restPos + new Vector3(0, y, z);
        var currentPos = Vector3.Lerp(restPos, runPos, being.Rush());

        PlaceTarget(currentPos, currentRot);
    }

    void WalkCycle() {
        // For now, just lifting arms a bit
        PlaceTarget(landmarks.Get("Waist"), RotForward());
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
        var shoulder = GetRootBone().transform;
        var chest = GetTorso().GetChestBone().transform;

        // Set offset if 1st time:
        if (_shoulderOffset == default(Vector3)) {
            _shoulderOffset = shoulder.position - chest.position;
        } 
        shoulder.position = chest.position + (chest.rotation * _shoulderOffset);
    }
}
