using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class LegAnimator : MonoBehaviour {
    Being being;

    // How fast to move the body, how 'fidgety'
    // (inversly, how slow/deliberate/imposing)
    // TODO set min/max, and have 0-1 slider
    float frantic = 0.4f;


    // TODO doc just testing
    // TODO could do transforms
    Vector3 startingFootR;
    GameObject footTargetR;
    Vector3 startingFootL;
    GameObject footTargetL;

    // TODO ideally set programatically
    float maxStepLength = .4f;
    float maxStepHeight = .3f;

    public void Start() {
        being = FindObjectOfType<Player>();
        footTargetR = transform.Find("Rig/Right Leg IK/target").gameObject;
        startingFootR = footTargetR.transform.localPosition;
        footTargetL = transform.Find("Rig/Left Leg IK/target").gameObject;
        startingFootL = footTargetL.transform.localPosition;
    }

    Vector3 lastPlacementL;
    Vector3 lastPlacementR;
    public void Update() {
        PlaceFeet();
        PlaceHips();
    }

    public void PlaceFeet(){
        // Find foot placements, and lerp to them
        var footPlacementR = GetFootPlacement(lastPlacementR, startingFootR);
        var footPlacementL = GetFootPlacement(lastPlacementL, startingFootL, true);

        var ftr = footTargetR.transform; // Shorthand
        var ftl = footTargetL.transform;
        ftr.position = Vector3.Lerp(ftr.position, footPlacementR, 30 * Time.deltaTime);
        ftl.position = Vector3.Lerp(ftl.position, footPlacementL, 30 * Time.deltaTime);

        lastPlacementR = footPlacementR;
        lastPlacementL = footPlacementL;
    }

    Vector3 startingHipPos;
    public void PlaceHips() {
        // Bounce hips along with lifting either foot

        // TODO would cause problem with starting at 0,0,0?
        if (startingHipPos == default(Vector3)) startingHipPos = transform.localPosition;

        var ry = footTargetR.transform.localPosition.y;
        var ly = footTargetL.transform.localPosition.y;
        var bounceY = Mathf.Max(ry, ly) / 10;
        transform.localPosition = new Vector3(0, bounceY, 0);
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
        var thisFootDegrees = offsetFoot ? degrees + 180 : degrees;

        var z = (StepLength() * Mathf.Cos(thisFootDegrees * Mathf.Deg2Rad));
        var y = (StepHeight() * Mathf.Sin(thisFootDegrees * Mathf.Deg2Rad));

        var ellipsePoint = new Vector3(0, y, z);

        // Rotate the elipses around the movement vector
        var angle = -Vector3.SignedAngle(being.WalkVelocity(), Vector3.forward, Vector3.up);
        return Quaternion.Euler(0, angle, 0) * ellipsePoint;
    }

    Vector3 GetFootPlacement(Vector3 lastPlacement, Vector3 startingPos, bool offsetFoot=false) {
        // For a specific foot, find a placement that is either:
        // * in the air, along an ellipse path
        // * stationary where it hit the ground

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
        if (belowGround) groundHit.point += new Vector3(0, maxStepHeight/10, 0);

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
        var airCrouch = transform.position + startingPos + transform.up * maxStepHeight/2;
        lastPlacement = Vector3.Lerp(lastPlacement, airCrouch, 20 * Time.deltaTime);
        // TODO could use velocity here to inform something
        return lastPlacement;
    }

    // Step size is a function of how fast we are running
    float StepLength() {
        // Longer steps when moving forward, shorter when sidestepping
        var forward = Mathf.Max(.2f, Mathf.Abs(being.ForwardRush()));
        return maxStepLength * being.Rush() * forward;
    }
    float StepHeight() { return maxStepHeight * being.Rush(); }

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
        var hips = HipPosition();
        var footDirection = landingPos-hips;

        // If we are at rest, we actually want to check below the foot,
        // 'in-line' with our normal resting point
        if (!being.IsWalking()) {
            footDirection = transform.position + startingPos - hips;
            footDirection *= 2;
        }

        Ray ray = new Ray(hips, footDirection);
        RaycastHit hit;
        Physics.Raycast(ray, out hit, footDirection.magnitude);
        return hit;
    }
    Vector3 HipPosition() {
        // TODO hips for non-anthro characters 
        return transform.Find("Armature").position;
    }

    // void OnDrawGizmos()  {
    //     Handles.Label(transform.position, "Degrees: "+degrees);
    // }
    
}
