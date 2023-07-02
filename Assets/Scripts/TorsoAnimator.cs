using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorsoAnimator : LimbAnimator {

    // Ratio of the total torso length that
    // they will lean forward when running
    float maxLeanRatio = 0.3f;

    protected override void AfterStart() {
        // Set with before we have deformity / rotation (?)
        GetWidth();
    }

    void Update() {

        // TODO I think we want to lean with chest,
        // but keep head focused on lock-on
        LeanTorso();
    }

    void LeanTorso() {
        // Given the characters velocity, tilt
        // "into" the movement, as humans do
        // TODO programatic way to find lean size
        var leanDirection = being.WalkVelocity().normalized * being.Rush() * MaxLean();

        if (being.IsCrouched())  leanDirection += transform.forward * .2f;

        // TODO ideally could make a smoother, teardrop shape or whatever
        // TODO forward rush? Something
        if (!being.MovingFoward()) leanDirection *= .5f;

        var leanFrom = target.transform.position;
        var leanTo = transform.position + _targetStartPos + leanDirection;
        target.transform.position = Vector3.Lerp(leanFrom, leanTo, Time.deltaTime * 10);
    }

    float MaxLean() {
        return GetLength() * maxLeanRatio;
    }

    float _width;
    public float GetWidth() {
        // TODO is this implementation bad?
        // Is it good? Should we be using mesh bound elsewhere?
        // How likely is it to be wrong because of width variability
        // along torso length?
        var mesh = GetComponentInChildren<SkinnedMeshRenderer>();
        _width = mesh.bounds.size.x;
        return _width;
    }

    public GameObject GetChestBone() {
        // Return the bone we would want arms parented too
        // - for now, assuming one 'source' of arms
        // (but this could be extended with a param for, say
        // lower sets of arms)
        // For now, just assume '2 after midbone'
        return GetMidBone().transform.GetChild(0).GetChild(0).gameObject;
    }
}
