using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LegAnimator : LimbAnimator {
    
    
    // Limb 'length' is known - these values
    // are a ratio of the limb's total length
    // TODO how do these look?
    protected float stepLengthRatio = 2.5f;
    protected float stepHeightRatio = .5f;
    protected float strideHertz = 1.5f;

    // Path that the foor takes during a single step
    // (will use a default if none is defined)
    // TODO do we need x vs y?
    public CurveData footCurve;

    protected override void AfterStart() {
        // TODO not sure if this is the cleanest way
        // - method on parent?
        _targetStartRot = RotFlatForward();
        target.transform.localRotation = _targetStartRot;

        // Load the default step curve if we aren't given a specifc one
        if (footCurve == null) {
            footCurve = Resources.Load<CurveData>("Foot");
        }
    }

    public void Update() {
        // Testing / debug
        // TODO could move to limb
        if (Player.IsDevMode()) return;
        if (testPos != "") {
            PlaceTarget(landmarks.Get(testPos));
            return;
        }

        PlaceFoot();
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

        var footPlacement = GetFootPlacement(lastPlacement, _targetStartPos);

        // Place foot in world position - not local
        PlaceTarget(footPlacement, false);
        // But rotation is local
        PlaceTarget(GetFootRotation());

        lastPlacement = footPlacement;
        // To prevent knee rotation when strafing, move hint
        // along with ellipse for now
        var hintOffsetX = GetEllipsePoint().x / 2;
        hint.transform.localPosition = new Vector3(
            hintStart.x + hintOffsetX, hintStart.y, hintStart.z);
    }

    internal float StepProgress() {
        // 0-1, how far along are we in the step
        var p = (Time.time * strideHertz) % 1;
        // Offset one foot by half-step
        return IsOffset() ? p + 0.5f : p;
    }

    internal Vector3 GetEllipsePoint() {
        // Given where on the ellipse the foot would currently by,
        // return a point on that ellipse, keeping it rotated in the
        // direction of travel
        // Note: Now that we use AnimationCurve, not a true 'ellipse',
        // could rename

        // How much to move foot along in its progress
        // TODO define gait duration, perhaps in hz?
        // var speed = being.WalkVelocity().magnitude;


        var z = StepLength(StepProgress());
        var y = StepHeight(StepProgress());

        var ellipsePoint = new Vector3(0, y, z);

        // Rotate the elipses around the movement vector
        var angle = -Vector3.SignedAngle(being.WalkVelocity(), Vector3.forward, Vector3.up);
        return Quaternion.Euler(0, angle, 0) * ellipsePoint;
    }

    // TODO for gizmo, remove
    RaycastHit gizmoGroundHit;
    Vector3 GetFootPlacement(Vector3 lastPlacement, Vector3 startingPos) {
        // For a specific foot, find a placement that is either:
        // * in the air, along an ellipse path
        // * stationary where it hit the ground

        // (if first frame)
        if (lastPlacement == default(Vector3)) {
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
        var ellipsePos = ellipseCenter + GetEllipsePoint();
        // and see if that spot is mid-step, or 'under the ground'
        var groundHit = GetGroundPlacement(ellipsePos, startingPos);
        bool belowGround = groundHit.transform != null;
        
        // TODO might have to have programatic offsets or something
        // For now, target bone is actually in foot, rather than on bottom of it
        // if (belowGround) {
        //     // groundHit.point += new Vector3(0, MaxStepHeight() * .01f, 0);
        //     // groundHit.point += new Vector3(0, .2f, 0);
        // }

        Debug.DrawLine(ellipseCenter, ellipsePos, Color.blue);
        gizmoGroundHit = groundHit;
        Debug.DrawLine(groundHit.point, target.transform.position, Color.yellow);

        // TODO we should likely have an in-air posing algorythm
        if (being.InAir()) return InAirPos(lastPlacement, startingPos, groundHit);
        // If foot is mid-step, move it
        if (!belowGround) return ellipsePos;
        // Otherwise, leave it on ground
        return groundHit.point;

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

    Quaternion GetFootRotation() {
        // Foot 'looks down at' the ground
        // for now, use these values and preset curve
        var curve = Resources.Load<CurveData>("FootRot").curve;
        var progress = StepProgress();
        if (!being.MovingFoward()) progress *= -1;
        var x = curve.Evaluate(progress);

        // Interpolate depending on how fast we are running
        var runRot = Quaternion.Euler(x * 180, 0, 0);
        var restRot = Quaternion.Euler(90, 0, 0);
        return Quaternion.Lerp(restRot, runRot, being.Rush());
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
    internal float StepLength(float stepProgress) {
        // Get exact stride displacement for this foot,
        // given 'angle' in walk cicle elipse
        // TODO could use curve, but for now, using Cos
        var tau = Mathf.PI * 2;
        return StepLength() * -Mathf.Cos(stepProgress * tau);
    }
    internal float StepHeight(float stepProgress) {
        // Get exact step height for this foot,
        // given 'angle' in walk cicle elipse
        // TODO use curve, but for now, using Sin
        return StepHeight() * footCurve.curve.Evaluate(stepProgress);
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
        var root = GetRootBone().transform.position;
        var footDirection = landingPos-root;
        var distance = GetLength() * 1.5f;

        // If we are at rest, we actually want to check below the foot,
        // 'in-line' with our normal resting point
        if (!being.IsWalking()) {
            footDirection = transform.position + startingPos - root;
            footDirection *= distance;
        }

        Debug.DrawLine(root, root+footDirection, Color.green);

        // Only register hits on ground
        // TODO could make this smarter for
        // walking over ragdolls, etc
        // Also - I fuggen hate layer mask syntax.
        LayerMask groundLayer = 1 << LayerMask.NameToLayer("Ground");
        Ray ray = new Ray(root, footDirection);
        RaycastHit hit;
        Physics.Raycast(ray, out hit, (float) footDirection.magnitude, groundLayer);
        return hit;
    }

    // TODO debug key
    void OnDrawGizmos()  {
        Handles.Label(transform.position, "stepProgress: "+StepProgress().ToString("F2"));
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(gizmoGroundHit.point, .02f);
    }
    
}
