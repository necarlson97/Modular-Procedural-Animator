using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class LimbAnimator : MonoBehaviour {
    // Holds code for setting up IK for arm / leg / torso / etc
    // - intended to be extended (TODO abstract?)
    // Requires:
    // a child gameobject called 'target'
    // a child skeleton called 'skeleton'

    protected GameObject target;
    protected GameObject skeleton;

    public GameObject rootBone;
    public GameObject midBone;
    public GameObject tipBone;

    // Just some helper variables
    protected Vector3 _targetStartPos;
    protected Quaternion _targetStartRot;
    
    void Start() {
        SetupRig();
    }

    void SetupRig() {
        // Create the objects / components needed for IK
        // (because Unity's IK setup is somewhat awkward,
        // and we want to do it, reapeatidly, 100s of times,
        // better just to do it programatically. Thanks chat-gpt)
        target = transform.Find("target").gameObject;
        skeleton = transform.Find("skeleton").gameObject;

        _targetStartPos = target.transform.localPosition;
        _targetStartRot = target.transform.localRotation;

         // Setup animator
        var animator = gameObject.GetComponent<Animator>();
        if (animator == null) {
            animator = gameObject.AddComponent<Animator>();
        }

        // Setup rig builder on the root character GameObject
        var rigBuilder = gameObject.GetComponent<RigBuilder>();
        if (rigBuilder == null) {
            rigBuilder = gameObject.AddComponent<RigBuilder>();
        }

        // Setup rig
        var rig = skeleton.GetComponent<Rig>();
        if (rig == null) {
            rig = skeleton.AddComponent<Rig>();
            // Add the rig to the rig builder
            var rigLayer = new RigLayer(rig, true);
            rigBuilder.layers.Add(rigLayer);
        }

        // Setup IK
        var ik = skeleton.GetComponent<TwoBoneIKConstraint>();
        if (ik == null) {
            ik = skeleton.AddComponent<TwoBoneIKConstraint>();
        }

        // Setting IK information
        ik.data.target = target.transform;
        ik.data.root = GetRootBone().transform;
        ik.data.mid = GetMidBone().transform;
        ik.data.tip = GetTipBone().transform;

        ik.data.targetPositionWeight = 1;
        ik.data.targetRotationWeight = 1;
        
        // Workaround - see 'EnableRig'
        GetComponent<RigBuilder>().enabled = false;
        Invoke("EnableRig", 0);
    }

    void EnableRig() {
        // There is a bug where the ik does not work unless
        // RigBuilder was disabled/reenabled in editor
        // - perhaps has to do with targetPositionWeight not
        // being properly updated. For now, this works
        // TODO DOES IT??
        GetComponent<RigBuilder>().enabled = true;
    }

    GameObject GetRootBone() {
        // If the bone is not explicitly set,
        // we will assume the root bone for ik
        // is the 1st bone under skeleton
        if (rootBone != null) return rootBone;
        rootBone = skeleton.transform.GetChild(0).gameObject;
        return rootBone;
    }

    GameObject GetMidBone() {
        // If the bone is not explicitly set,
        // we will assume it is just after root
        if (midBone != null) return midBone;
        midBone = GetRootBone().transform.GetChild(0).gameObject;
        return midBone;
    }

    GameObject GetTipBone() {
        // If the bone is not explicitly set,
        // we will assume the skeleton is a linear
        // chain, and the tip is simply the last bone
        if (tipBone != null) return tipBone;
        Transform currentBone = GetRootBone().transform.GetChild(0);
        while (currentBone.childCount > 0) {
            currentBone = currentBone.GetChild(0);
        }
        tipBone = currentBone.gameObject;
        return tipBone;
    }

    float GetLength() {
        // Returns the 'length' of the limb,
        // ideally this would be calculated by adding
        // up bones or something, but for now, we assume
        // it started in T-pose or simmilar (thus stretched out)
        // so starting pos call tell us length
        return Vector3.Distance(GetRootBone().transform.localPosition, _targetStartPos);
    }

}
