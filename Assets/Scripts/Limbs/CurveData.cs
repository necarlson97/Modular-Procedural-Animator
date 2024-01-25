using UnityEngine;

[CreateAssetMenu(fileName = "CurveData", menuName = "Animation/CurveData", order = 1)]
public class CurveData : ScriptableObject {
    // Create a specific, complex curve that we can reference in code
    // for animations, e.g., 'the path that a foot travels during a step'
    public AnimationCurve curve;
}