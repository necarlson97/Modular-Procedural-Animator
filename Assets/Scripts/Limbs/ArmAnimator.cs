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
    public string testRot;

    protected override void AfterStart() {
        // Elbows point 'behind' when starting in T-pose
        var hintPos = landmarks.Get("Waist");
        hintPos += transform.forward * -2f * GetDepth();
        hintPos += transform.up * 1f * GetLength();
        hint.transform.position = hintPos;
        // For now, swapping to using a springy connection
        SetupSpring();

        // Parent root to torso's chest
        skeleton.transform.SetParent(GetTorso().GetChestBone().transform);
    }

    public void Update() {
        // Handle normal 'default' behavior for arms
        // - when they arent using weapons, or items, etc
        // (have to figure how to make this easily extensible still)

        // Testing / debug
        // TODO could move to limb
        if (Player.IsDevMode()) return;
        if (testPos != "") PlaceTarget(landmarks.Get(testPos));
        if (testPos != "") PlaceTarget(Rotation(testRot));
        if (testPos != "" || testRot != "") return;

        // TODO we should have a more codified priority system
        // in the limb animator, for passing up what we think the limb
        // should be doing, then attack or whatever can override
        if (being.IsAttacking()) return;
        else if (being.IsGuarding()) Guard();
        else if (being.IsWalking()) RunCycle();
        else Rest();
    }

    void Rest() {
        // When standing still, bring arms down to sides
        PlaceTarget(landmarks.Get("Lowered"), RotDown());
    }

    void Guard() {
        // When guarding, place hands where weapon gaurd
        // expects them to be
        // TODO for now - just keep fists by chin
        PlaceTarget(landmarks.Get("Face"), RotUp());
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
}
