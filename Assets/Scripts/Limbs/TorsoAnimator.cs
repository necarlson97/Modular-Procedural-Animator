using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class TorsoAnimator : LimbAnimator {

    // Ratio of the total torso length that
    // they will lean forward when running
    float maxLeanRatio = 0.45f;
    // Simmilar for twist
    float twistRatio = 0.8f;
    // TODO config crouch ammount?

    // IK target for placing the head
    GameObject head;
    protected override void BeforeStart() {
        // For now, for testing, lets try target being chest,
        // rather than head - we may need both...
        tipBone = ChildOf(ChildOf(GetMidBone()));
        // TODO is this cleanest?
        ResetBounds();
    }
    protected override void AfterStart() {
        // Setup a second IK for the head
        // TODO is the head just another 'limb', and
        // we should rewrite some of the parenting logic?
        var neckBone = ChildOf(GetTipBone()).transform;
        var headBone = GetLastBone().transform;

        // Create this secondary IK with the suffix " Head"
        var headIK = SetupIK(neckBone, headBone, false, " Head");
        head = headIK.Target.gameObject;
    }

    void Update() {
        // Need more codified priority syste
        if (being.IsAttacking()) return;
        
        var chestOffset = GetLean() + GetWobble();
        var lookAt = GetRotation();
        // Chest target
        PlaceTarget(chestOffset + _tipStartPos, lookAt);

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

    float GetForwardsMotion(float min=0) {
        // Get a min-1 float of how much of our motion is
        // going forwards. For now, we have a 'min' value,
        // because we might want to, say, always lean a little
        // - but much more when we are leaning forward
        var forwardsMotion = being.ForwardRush();
        forwardsMotion = float.IsNaN(forwardsMotion) ? 0 : forwardsMotion;
        return Mathf.Max(min, forwardsMotion);
    }

    Vector3 GetLean() {
        // Given the characters velocity:
        // * tilt into the motion
        // * wobble & twist with arm pumping
        var leanDirection = being.WalkVelocity().normalized * being.Rush() * MaxLean();
        // Lean less if we are moving to the side
        leanDirection *= GetForwardsMotion(0.3f);

        if (being.IsCrouched())  leanDirection += transform.forward * .2f;
        return leanDirection;
    }

    Vector3 GetBounce() {
        // When running, move the torso up/down with the pace
        var leg = GetLeg();
        // Torso bounces once per step, so 'twice' per cycle
        // (Want hips to be dipping, rather than lifting off group)
        // TODO actually, should depend on 'number of legs' per step
        var numberOfLegs = GetLegs().Length;
        var progress = (leg.StepProgress() * numberOfLegs) % 1;

        var bounceCurve = Resources.Load<CurveData>("HipBounce").curve;
        var bounceHeight = bounceCurve.Evaluate(progress) * leg.StepHeight();

        // Regardless, we want to sink a bit to make our legs easier to reach,
        // depending on gait length
        bounceHeight -= leg.StepLength() * 0.1f;
        
        // Give height, but also a little forward motion
        return new Vector3(0, bounceHeight, bounceHeight/2);
    }

    Vector3 GetWobble() {
        // When running, move the torso right/left with the pace
        if (!being.IsRunning()) return Vector3.zero;

        var leg = GetLeg();
        var tau = Mathf.PI * 2;
        var progress = leg.StepProgress();
        
        var width = Mathf.Sin(progress * tau);
        width *= GetWidth() * maxLeanRatio * 0.15f * GetForwardsMotion();
        return Vector3.right * width;
    }

    Vector3 GetRotation() {
        // Keep torso pointed forwards,
        // but twist a little when running
        // TODO idle breathing?

        var vert = Vector3.zero;
        var horiz = Vector3.zero;

        // idle breathing
        // TODO make this cleaner
        var breathProgress = GetProgress(0.2f);
        var tau = Mathf.PI*2;
        var breathCycle = (Mathf.Cos(breathProgress * tau) + 1) * 0.5f;
        vert = Vector3.Lerp(Vector3.up, -Vector3.up, breathCycle) * .05f;

        if (being.IsWalking()) {
            var stepProgress = GetLeg().StepProgress();
            var twistProgress = (Mathf.Cos(stepProgress * tau) + 1) * 0.5f;

            horiz = Vector3.Lerp(Vector3.right, -Vector3.right, twistProgress);
            horiz *= GetWidth() * being.ForwardRush() * twistRatio;    
        }
        
        // If crouching, point down
        if (being.IsCrouched()) vert += -Vector3.up * 0.5f;

        return Vector3.forward + horiz + vert;
    }

    void FixHead() {
        // Have the head x/z follow the chest
        // (with a bit of running lean as well)
        var destination = target.transform.localPosition;
        destination.y = head.transform.localPosition.y;
        // TODO should be based on ratio of head length to torso length
        var forwardsLean = MaxLean() * .4f * GetForwardsMotion();
        destination.z = destination.z + forwardsLean;

        head.transform.localPosition = Vector3.Lerp(
            head.transform.localPosition,
            destination,
            Time.deltaTime * 30f
        );
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
