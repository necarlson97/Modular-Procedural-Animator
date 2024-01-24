using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class TorsoAnimator : LimbAnimator {

    // Ratio of the total torso length that
    // they will lean forward when running
    float maxLeanRatio = 0.5f;

    // IK target for placing the head
    GameObject head;
    protected override void BeforeStart() {
        // For now, for testing, lets try target being shoulders,
        // rather than head - we may need both...
        tipBone = ChildOf(ChildOf(GetMidBone()));
    }
    protected override void AfterStart() {
        // Setup a second IK for the head
        var headIK = skeleton.AddComponent<ChainIKConstraint>();
        var shoulderBone = GetTipBone().transform;
        var headBone = GetLastBone().transform;

        head = CreateEmpty("Target Head", ChildOf(headBone));
        
        headIK.data.root = shoulderBone;
        headIK.data.tip = headBone;
        headIK.data.target = head.transform;
        headIK.data.chainRotationWeight = 1f;
        headIK.data.tipRotationWeight = 1f;
        headIK.data.maintainTargetPositionOffset = true;
        headIK.data.maintainTargetRotationOffset = true;
        headIK.data.maxIterations = 10;
        headIK.data.tolerance = 0.001f;

        // Workaround - see LimbAnimator 'EnableRig'
        GetComponent<RigBuilder>().enabled = false;
        Invoke("EnableRig", 0);
    }

    void Update() {
        // Need more codified priority system
        if (Player.IsDevMode()) return;
        if (testPos != "") {
            PlaceTarget(landmarks.Get(testPos));
            return;
        }
        if (being.IsAttacking()) return;
        
        LeanTorso();
        BounceTorso();
        // Keep torso pointed forwards
        TargetLookAt(Vector3.forward);
        // TODO idle breathing?
        
        // TODO I think we want to lean keep head focused
        // on lock-on pr what enemy you would target,
        // or what pickups are near, etc
        UpdateLook();
        FixParenting();
    }

    void UpdateLook() {
        // TODO look at something nearby?
        // What you could pick up?
    }

    void LeanTorso() {
        // Given the characters velocity, tilt
        // "into" the movement, as humans do
        var leanDirection = being.WalkVelocity().normalized * being.Rush() * MaxLean();
        // Lean less if we are moving to the side
        var forwardsMotion = being.ForwardRush() / being.Rush();
        forwardsMotion = float.IsNaN(forwardsMotion) ? 0 : forwardsMotion;
        leanDirection *= Mathf.Max(.2f, forwardsMotion);


        if (being.IsCrouched())  leanDirection += transform.forward * .2f;

        // TODO use PlaceTarget
        var leanFrom = target.transform.position;
        var leanTo = transform.position + _targetStartPos + leanDirection;
        target.transform.position = Vector3.Lerp(leanFrom, leanTo, Time.deltaTime * 10);
    }

    void BounceTorso() {
        // When running, move the torso up/down with the pace

        // TODO using max of all leg's step height
        // was giving cycloid motion, but I think I want sin motion,
        // so using this for now
        // Get a random leg
        var la = transform.parent.GetComponentInChildren<LegAnimator>();
        // What 'part' of it's walk cycle is it in?
        var degrees = la.degrees;
        // Torso bounces once per step, so 'twice' per cycle
        // (Want hips to be dipping, rather than lifting off group)
        // TODO actually, should be 'number of legs' per step
        var stepHeight = Mathf.Sin(degrees * Mathf.Deg2Rad * 2) - 2;
        // Torso does not move as high as feet, as it is dampened
        // TODO does this need config?
        stepHeight *= la.StepHeight() * 0.08f;
        
        // Give height, but also a little forward motion
        transform.localPosition = new Vector3(0, stepHeight, stepHeight/2);
    }

    void FixParenting() {
        // Have the head x/z follow the chest,
        // and the thighs folow the hips
        var pos = Vector3.Lerp(head.transform.position, target.transform.position, Time.deltaTime * 10);
        pos.y = head.transform.position.y;
        head.transform.position = pos;

        // TOOD legs might have offsets (?)
        foreach (var la in transform.parent.GetComponentsInChildren<LegAnimator>()) {
            la.transform.localPosition = transform.localPosition;
        }
    }

    float MaxLean() {
        return GetLength() * maxLeanRatio;
    }

    public GameObject GetChestBone() {
        // Return the bone we would want arms parented too
        // - for now, assuming one 'source' of arms
        // (but this could be extended with a param for, say
        // lower sets of arms)
        // For now, just assume '2 after midbone'
        return ChildOf(ChildOf(GetMidBone()));
    }
}
