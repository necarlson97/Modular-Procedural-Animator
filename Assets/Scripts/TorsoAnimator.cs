using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorsoAnimator : MonoBehaviour {
    Being being; // The player or NPC
    GameObject head;
    Vector3 startingLocalHeadPos;

    float maxLean = 0.2f; // TODO set programatically?
    void Start() {
        being = FindObjectOfType<Player>();
        head = transform.Find("Rig/Head IK/target").gameObject;
        startingLocalHeadPos = head.transform.localPosition;
    }

    void Update() {
        LeanTorso();
    }

    void LeanTorso() {
        // Given the characters velocity, tilt
        // "into" the movement, as humans do
        // TODO programatic way to find lean size
        var leanDirection = being.WalkVelocity().normalized * being.Rush() * maxLean;

        if (being.IsCrouched())  leanDirection += transform.forward/4;

        // TODO ideally could make a smoother, teardrop shape or whatever
        if (!being.MovingFoward()) leanDirection /= 2;

        var leanFrom = head.transform.position;
        var leanTo = transform.position + startingLocalHeadPos + leanDirection;
        head.transform.position = Vector3.Lerp(leanFrom, leanTo, Time.deltaTime * 10);
    }
}
