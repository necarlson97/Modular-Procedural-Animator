using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class LegAnimator : LimbAnimator {
    
    
    // Limb 'length' is known - these values
    // are a ratio of the limb's total length
    // TODO how do these look?
    protected float stepLengthRatio = .4f;
    protected float stepHeightRatio = .3f;

    public void Update() {
        PlaceFoot();
        PlaceHips();
    }

    LegAnimator _partner;
    public LegAnimator GetPartner() {
        // Programatically set leg that this
        // is 'partnered' with (attatched to the same hip)
        // (can be None)
        // TODO make 'limb' method that takes Leg/Arm name
        // TODO 'memoize' is recalculated for unpaired legs
        if (_partner != null) return _partner;
        for (int i = 0; i < transform.parent.childCount; i++) {
            Transform sibling = transform.parent.GetChild(i);
            if (sibling != transform && sibling.name.Contains("Leg")) {
                _partner = sibling.GetComponent<LegAnimator>();
                return _partner;
            }
        }
        return null;
    }
    bool IsOffset() {
        // return true if this is the 'offset'
        // foot - e.g., the left foot in a pair,
        // so a pair stays 'out of phase'
        // TODO using name is a bit crude here
        return GetPartner() != null && name.Contains(" L");
    }

    Vector3 lastPlacement;
    public void PlaceFoot(){
        // Find foot placements, and lerp to them

        var footPlacement = GetFootPlacement(
            lastPlacement, _targetStartPos, IsOffset());

        PlaceTarget(footPlacement);
        lastPlacement = footPlacement;
    }

    public void PlaceHips() {
        // Bounce hips along with lifting either foot
        // TODO actually, torso should handle this, yes?
        var footHeight = TargetOffset().z;
        var partnerHeight = 0f;
        if (GetPartner() != null) {
            partnerHeight = GetPartner().TargetOffset().z;
        }

        var bounceHeight = Mathf.Max(footHeight, partnerHeight) * (MaxStepHeight() * 1f);
        var bounceOffset = transform.forward * bounceHeight;
        // TOOD not sure if I like setting our pos vs setting root bone...
        GetRootBone().transform.localPosition = _rootStartPos + bounceOffset;
    }

    float degrees;
    Vector3 GetEllipsePoint(bool offsetFoot=false) {
        // Given where on the ellipse the foot would currently by,
        // return a point on that ellipse, keeping it rotated in the
        // direction of travel

        // How much to move foot along ellipse
        // TODO imperfect - but seems close enough for now
        var gaitLength = Mathf.Max(2 * EllipsePerimiter() / being.WalkVelocity().magnitude, 0.01f);
        var d = 360 / gaitLength * Time.deltaTime;
        degrees = (degrees - d) % 360;
        // Offset one of the feet by 180 degrees
        var footDegrees = offsetFoot ? degrees + 180 : degrees;

        var z = StepLength(footDegrees);
        var y = StepHeight(footDegrees);

        var ellipsePoint = new Vector3(0, y, z);

        // Rotate the elipses around the movement vector
        var angle = -Vector3.SignedAngle(being.WalkVelocity(), Vector3.forward, Vector3.up);
        return Quaternion.Euler(0, angle, 0) * ellipsePoint;
    }

    Vector3 GetFootPlacement(Vector3 lastPlacement, Vector3 startingPos, bool offsetFoot=false) {
        // For a specific foot, find a placement that is either:
        // * in the air, along an ellipse path
        // * stationary where it hit the ground

        // (if first frame)
        if (lastPlacement == Vector3.zero) {
            lastPlacement = target.transform.position;
        }
        // TODO currently 'lastPlacement' will always be one frame 'above'
        // the ground - ideally, it wouldn't

        // Our resting position and whatnot needs to account for rotation
        var ellipseCenterOffset = transform.rotation * new Vector3(startingPos.x, 0, 0);
        startingPos = transform.rotation * startingPos;

        // Center our walk cycle at the bottom of the character,
        // offseting the x pos for this foot - which could be rotated
        var ellipseCenter = being.BottomPoint();
        ellipseCenter += ellipseCenterOffset;

        // Find the spot along the ellipse this foot should be at 
        var ellipsePos = ellipseCenter + GetEllipsePoint(offsetFoot);
        // and see if that spot is mid-step, or 'under the ground'
        var groundHit = GetGroundPlacement(ellipsePos, startingPos);
        bool belowGround = groundHit.transform != null;
        
        // TODO might have to have programatic offsets or something
        // For now, target bone is actually in foot, rather than on bottom of it
        // if (belowGround) groundHit.point += new Vector3(0, MaxStepHeight() * .01f, 0);

        Debug.DrawLine(ellipseCenter, ellipsePos, Color.blue);

        // TODO we should likely have an in-air posing algorythm
        if (being.InAir()) return InAirPos(lastPlacement, startingPos, groundHit);
        // If foot is mid-step, move it
        if (!belowGround) return ellipsePos;
        // If we are at rest, make sure feet are near ground
        if (!being.IsWalking()) return groundHit.point;
        // Otherwise, leave it where it was - near the ground
        return lastPlacement;

        // TODO need to rotate foot to ground, but limit the angles
        // footTarget.transform.rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(0, 0, 180);
    }
    Vector3 InAirPos(Vector3 lastPlacement, Vector3 startingPos, RaycastHit groundHit) {
        // Where should the feet be placed if the character is in flight?

        // If we are about to hit ground, extend twoards it
        if (groundHit.transform != null) return groundHit.point;
        // Otherwise, move feet slowly twoards a resting state
        var airCrouch = transform.position + startingPos + transform.up * MaxStepHeight()/2;
        lastPlacement = Vector3.Lerp(lastPlacement, airCrouch, 20 * Time.deltaTime);
        // TODO could use velocity here to inform something
        return lastPlacement;
    }

    // Step size is a function of how fast we are running
    public float StepLength() {
        // Get the general stride length for this walk/run speed
        // (Longer steps when moving forward, shorter when sidestepping)
        var forward = Mathf.Min(.2f, Mathf.Abs(being.ForwardRush()));
        return MaxStepLength() * being.Rush() * forward;
    }
    public float StepHeight() {
        // Get the general step height for this walk/run speed
        return MaxStepHeight() * being.Rush();
    }
    float StepLength(float footDegrees) {
        // Get exact stride displacement for this foot,
        // given 'angle' in walk cicle elipse
        return StepLength() * Mathf.Cos(footDegrees * Mathf.Deg2Rad);
    }
    float StepHeight(float footDegrees) {
        // Get exact step height for this foot,
        // given 'angle' in walk cicle elipse
        return StepHeight() * Mathf.Sin(footDegrees * Mathf.Deg2Rad);
    }
    public float MaxStepLength() { return GetLength() * stepLengthRatio; }
    public float MaxStepHeight() { return GetLength() * stepHeightRatio; }

    float EllipsePerimiter() {
        // The walk elipse perimiter allows us to determine
        // how fast to walk - as we want the feet to not slide
        // TODO maybe we should just leave the foot on the ground wherever it hit,
        // until the permiter goes above ground again?

        // The 'minor axis' and 'major axis' of the ellipse
        // are dependent on our step
        var a = StepLength();
        var b = StepHeight();
        return Mathf.PI * (3*(a+b) - Mathf.Sqrt( (3*a + b) * (a + 3*b) ));
    }

    public RaycastHit GetGroundPlacement(Vector3 landingPos, Vector3 startingPos) {
        // If the given foot target would end below the ground,
        // allign it with the ground

        // TODO once a foot hits the ground, it should really
        // stay there until target is above ground again

        // Cast from center of character
        // TOOD layermask
        var root = GetRootBone().transform.position;
        var footDirection = landingPos-root;

        // If we are at rest, we actually want to check below the foot,
        // 'in-line' with our normal resting point
        if (!being.IsWalking()) {
            footDirection = transform.position + startingPos - root;
            footDirection *= 1.5f;
        }

        Ray ray = new Ray(root, footDirection);
        RaycastHit hit;
        Physics.Raycast(ray, out hit, footDirection.magnitude);
        return hit;
    }

    // TODO debug key
    // void OnDrawGizmos()  {
    //     Handles.Label(transform.position, "Degrees: "+degrees);
    // }
    
}
