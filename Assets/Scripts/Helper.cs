using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Helper {

    public static Vector3 ClosestOnLine(Vector3 point, Vector3 start, Vector3 end) {
        // Find the closest point on a line segment

        // Get heading
        Vector3 heading = (end - start);
        float maxLength = heading.magnitude;
        heading.Normalize();

        // Do projection from the point but clamp it
        float dotP = Vector3.Dot(point - start, heading);
        float length = Mathf.Clamp(dotP, 0f, maxLength);
        return start + heading * length;
    }

    public static Vector3 ProjectToLine(Vector3 point, Vector3 start, Vector3 end) {
        // Find how far off a line is from a point, returning
        // a vector which projects twoards the line
        return point - ClosestOnLine(point, start, end);
    }

    public static float DistanceToLine(Vector3 point, Vector3 start, Vector3 end) {
        // Calculate the distance between a point and a line segment
        return ProjectToLine(point, start, end).magnitude;
    }

    public static float LeftOrRight(Vector3 forward, Vector3 up, Vector3 targetDirection) {
        // Is the vector to the left or right of (0,0), accounting for rotation
        // (negative is left, positive is right)
        Vector3 right = Vector3.Cross(forward, targetDirection);
        float dotP = Vector3.Dot(right, up);
        return dotP;
    }

    public static float LeftOrRight(Transform t, Vector3 target) {
        // Is the point to the left or right of the transform
        // (negative is left, positive is right)
        return LeftOrRight(t.forward, t.up, target-t.position);
    }
}
