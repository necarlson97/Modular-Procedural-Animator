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

    TorsoAnimator torso;

    protected override void BeforeStart() {
        // TODO is this gameObject structure gaurenteed?
        torso = transform.parent.GetComponentInChildren<TorsoAnimator>();

        // For now, arm defaults to using chain-ik
        // rather than two-bone - to utalize shoulder and whatnot
        // TODO is this the best way to code this?
        twoBone = false;
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
        var chest = torso.GetChestBone().transform;

        // TODO FIX REMOVE
        // Set offset if 1st time:
        if (_shoulderOffset == Vector3.zero) {
            _shoulderOffset = shoulder.position - chest.position;
        } 
        shoulder.position = chest.position + _shoulderOffset;
    }

    // TODO not sure if these 'body sense'
    // methods should be elsewhere - perhaps torso?
    // Or a BeingAnimator script on the parent gameObject?
    Vector3 _holsterPos;
    public Vector3 HolsterPos(bool left) {
        // A space near the right hip, where one might
        // brace a spear, or sheathe a sword, etc
        if (_holsterPos != Vector3.zero) return _holsterPos;
        // TODO how wide is torso? Use collider? Or?
        // TODO y'know, at some point, we are going to need to do mesh calculations
        var hipPos = torso.GetRootBone().transform.position;
        var hipOffset = torso.GetWidth() * .5f * transform.right;

        if (left) _holsterPos = hipPos + hipOffset;
        else _holsterPos = hipPos - hipOffset;

        return _holsterPos;
    }
}
