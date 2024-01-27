using UnityEngine;

public class SurrogateChild : MonoBehaviour {
    // Create a parent/child physical relationship
    // for the location/rotation, without
    // having to use the Unity parent/child
    // gameobject relationship.
    // This allows us to preserve the desired
    // body setup of seperate limbs.

    // TODO might be a way to have 1 skeleon
    // and find a way to use seperate meshes on that
    // rig for each lib. Then use scaling and whatnot
    // to remove unwanted limbs, and extra useless limbs
    // Can be maybe a seperate 'VestigalLimbAnimator'
    // or something, idk

    public Transform surrogateParent;
    private Vector3 localPositionOffset;
    private Quaternion localRotationOffset;

    void Start() {
        if (surrogateParent != null) {
            // TODO
        }
    }

    public static void Setup(Transform surrogateChild, Transform surrogateParent) {
        // Can't parent the limb object, nor the rig, nor the root or any bone,
        // so for now, create a new object to modify
        // TODO this breaks organization. Isn't that what we are trying to prevent?

        // TODO does this work?
        // var target = surrogateChild.gameObject.GetComponent<LimbAnimator>().target;
        // target.transform.SetParent(surrogateChild.parent);
        surrogateChild.SetParent(surrogateParent);
    }

    void Update() {
        if (surrogateParent == null) return;
        // Update position and rotation based on the surrogate parent's transform
        transform.position = surrogateParent.TransformPoint(localPositionOffset);
        transform.rotation = surrogateParent.rotation * localRotationOffset;
    }
}
