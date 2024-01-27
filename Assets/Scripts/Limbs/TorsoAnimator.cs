using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class TorsoAnimator : LimbAnimator {

    // Ratio of the total torso length that
    // they will lean forward when running
    float maxLeanRatio = 0.45f;
    // Simmilar for twist / bounce
    float twistRatio = 0.8f;
    float bounceRatio = 0.1f;
    // TODO config crouch ammount?

    // IK target for placing the head
    GameObject head;
    protected override void BeforeStart() {
        // For now, for testing, lets try target being chest,
        // rather than head - we may need both...
        tipBone = ChildOf(ChildOf(GetMidBone()));
    }
    protected override void AfterStart() {
        // Setup a second IK for the head
        // TODO is the head just another 'limb', and
        // we should rewrite some of the parenting logic?
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
        
        var chestOffset = GetLean() + GetWobble();
        var lookAt = GetRotation();
        // Chest target
        PlaceTarget(chestOffset + _targetStartPos, lookAt);

        // Hips
        var hipsOffset = GetBounce();
        PlaceRoot(hipsOffset);
        
        // TODO I think we want to lean keep head focused
        // on lock-on pr what enemy you would target,
        // or what pickups are near, etc
        // UpdateLook();
        // Head target

        FixHead();
    }

    Vector3 GetLean() {
        // Given the characters velocity:
        // * tilt into the motion
        // * wobble & twist with arm pumping
        var leanDirection = being.WalkVelocity().normalized * being.Rush() * MaxLean();
        // Lean less if we are moving to the side
        var forwardsMotion = being.ForwardRush() / being.Rush();
        forwardsMotion = float.IsNaN(forwardsMotion) ? 0 : forwardsMotion;
        leanDirection *= Mathf.Max(.2f, forwardsMotion);

        if (being.IsCrouched())  leanDirection += transform.forward * .5f;
        return leanDirection;
    }

    Vector3 GetBounce() {
        // When running, move the torso up/down with the pace
        var leg = GetLeg();
        // Torso bounces once per step, so 'twice' per cycle
        // (Want hips to be dipping, rather than lifting off group)
        // TODO actually, should be 'number of legs' per step
        var tau = Mathf.PI * 2;
        var progress = leg.StepProgress();

        // Delay by a bit TODO DOC
        progress += 0.1f;

        var stepHeight = Mathf.Sin(progress * tau * 2) - 1.65f;
        // Torso does not move as high as feet, as it is dampened
        stepHeight *= leg.StepHeight() * bounceRatio;
        
        // Give height, but also a little forward motion
        return new Vector3(0, stepHeight, stepHeight/2);
    }

    Vector3 GetWobble() {
        // When running, move the torso right/left with the pace
        var leg = GetLeg();
        var tau = Mathf.PI * 2;
        var progress = leg.StepProgress();
        
        var width = Mathf.Sin(progress * tau * 2);
        width *= GetWidth() * maxLeanRatio * 0.2f * being.Rush();
        return Vector3.right * width;
    }

    Vector3 GetRotation() {
        // Keep torso pointed forwards,
        // but twist a little when running
        // TODO idle breathing?
        if (!being.IsRunning()) return Vector3.forward;

        var stepProgress = GetLeg().StepProgress();
        var tau = Mathf.PI*2;
        var twistProgress = (Mathf.Cos(stepProgress * tau) + 1) * 0.5f;

        var horiz = Vector3.Lerp(Vector3.right, -Vector3.right, twistProgress);
        horiz *= GetWidth() * being.Rush() * twistRatio;
        return Vector3.forward + horiz;
    }

    void FixHead() {
        // Have the head x/z follow the chest
        // TODO use SurrogateChild for this?
        var pos = Vector3.Lerp(head.transform.position, target.transform.position, Time.deltaTime * 10);
        pos.y = head.transform.position.y;
        head.transform.position = pos;
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
