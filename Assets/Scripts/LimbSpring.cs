using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimbSpring : CustomBehavior {
    // A more simplified spring component
    // than 'spring joint' - created to allow
    // easy secondary motion for limb's ik targets

    // How forcefully our target catches up
    // to our spring root
    public float stiffness = 500f;
    // How close our damening gets to
    // elimintation oscillations
    // - so just <1 means 'a little bit of overshoot'
    public float dampRatio = .5f;

    internal GameObject target;
    internal GameObject springRoot;
    internal LimbAnimator limb;
    
    Vector3 velocity = Vector3.zero;

    public void Start() {
        Setup();
    }

    public GameObject Setup() {
        // Create the springRoot, assign the target
        limb = GetComponent<LimbAnimator>();
        target = FindContains("Target");

        // if we already run setup...
        springRoot = transform.Find("Spring Root")?.gameObject;
        if (springRoot != null) return springRoot;

        springRoot = new GameObject("Spring Root");
        springRoot.transform.SetParent(transform);
        springRoot.transform.position = target.transform.position;
        return springRoot;
    }

    void FixedUpdate() {
        HandlePos();
        HandleRot();
    }

    void HandlePos() {
        // Perform a simple calcualtion to spring our
        // 'target' to our 'spring root'

        
        // For now, assume mass of 1
        var mass = 1;
        // TODO doesn't change, could memoize
        var dampingConstant = dampRatio * (2 * Mathf.Sqrt(mass * stiffness));

        var goalPos = springRoot.transform.position;
        Vector3 displacement = target.transform.position - goalPos;
        Vector3 dampingForce = -dampingConstant * velocity;

        Vector3 springForce = -stiffness * displacement;
        Vector3 netForce = springForce + dampingForce;
        Vector3 acceleration = netForce * mass;

        velocity += acceleration * Time.deltaTime;
        target.transform.position += velocity * Time.deltaTime;

        // TODO clamp displacement, and 'snap' when close
        // Add a bit of the parent rigidbody into the calculation
        // TODO is this a good way to do it?
        velocity += limb.being.GetComponent<Rigidbody>().velocity * .05f;
    }

    void HandleRot() {
        // TODO for now, just passing rotation through directly
        target.transform.rotation = springRoot.transform.rotation;
    }

    void OnDrawGizmos() {
        if (springRoot == null) return;
        var size = .08f;
        var dimensions = new Vector3(size, size, size);
        // Translucent orange
        Gizmos.color = new Color(1f, .64f, 0f, .5f);
        Gizmos.DrawCube(target.transform.position, dimensions);
    }
}