using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmAnimator : MonoBehaviour {
    Being being;

    // TODO doc just testing
    // TODO could do transforms
    GameObject handTargetR;
    GameObject handTargetL;
    Transform startingHandR;
    Transform startingHandL;

    Vector3 offsetPos;
    Quaternion offsetRot;

    public void Start() {
        being = FindObjectOfType<Player>();
        handTargetR = transform.Find("Rig/Right Arm IK/target").gameObject;
        startingHandR = handTargetR.transform;
        handTargetL = transform.Find("Rig/Left Arm IK/target").gameObject;
        startingHandL = handTargetL.transform;

        // TODO change weapon gameobject structure so we don't need these
        var wt = transform.parent.Find("weapon/sword");
        offsetPos = wt.position - startingHandR.position;
        offsetRot = wt.rotation * Quaternion.Inverse(startingHandR.rotation);
    }

    public void Update() {
        // Note: this could not be done with parenting, as the rig requires the
        // targets be children of itself
        var wt = transform.parent.Find("weapon/sword");
        handTargetR.transform.position = wt.position + offsetPos;
        handTargetR.transform.rotation = wt.rotation * offsetRot;
    }
}
