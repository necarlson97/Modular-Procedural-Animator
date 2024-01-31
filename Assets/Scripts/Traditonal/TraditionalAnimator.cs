using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TraditionalAnimator : MonoBehaviour {
    // Update the 'animator' parameters
    public Being being;

    void Update() {
        var animator = GetComponent<Animator>();
        if (animator != null && being != null) {
            animator.SetFloat("MoveX", being.GetMovement().x);
            animator.SetFloat("MoveZ", being.GetMovement().z);
            animator.SetBool("IsCrouching", being.IsCrouched());
            animator.SetBool("IsRunning", being.IsRunning());
            animator.SetBool("IsMoving", being.IsWalking());
        }
    }
}
