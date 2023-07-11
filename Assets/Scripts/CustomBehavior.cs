using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class CustomBehavior : MonoBehaviour {
    // Our project specific utilities, shorthanded as 'U'
    // TODO is that to unreadable a name?
    // TODO should I extend 'Monobehavior' for some of these? All?

    // For now, finding proper rotations was not producing
    // expected results with LookAt, and so I'll just provide
    // some hardcoded shorthands for now
    // Note: some might be 1 or so degrees 'off'
    // to encourage lerp to 'choose a desired direction'
    public static Quaternion RotForward() { return Q(0, 90, 90); }
    public static Quaternion RotBackward() { return Q(0, -90, 90); }
    public static Quaternion RotUp() { return Q(0, 90, 1); }
    public static Quaternion RotUp(bool left) { 
        if (left) return Q(0, -90, 1);
        else return Q(0, 90, 1);
    }
    public static Quaternion RotDown() { return Q(0, 90, 180); }
    public static Quaternion RotLeft() { return Q(0, 0, 90); }
    public static Quaternion RotRight() { return Q(0, 0, -90); }
    public static Quaternion Q(float x, float y, float z) {
        return Quaternion.Euler(x, y, z);   
    }
    public static Quaternion Rotation(string name) {
        // Given the string name of a rotation (such as 'Up'),
        // use reflection to call that method
        var method = typeof(CustomBehavior).GetMethod("Rot"+name, Type.EmptyTypes);
        if (method == null) {
            Debug.LogError("Could not find method for "+name+"Pos");
            return default(Quaternion);
        }
        return (Quaternion) method.Invoke(null, null);
    }

    public static GameObject FindContains(string query, Transform t) {
        // Find a child gameobject if it contains a string
        // (recursive)
        if (t.name.Contains(query)){
            return t.gameObject;
        }
        foreach (Transform child in t){
            var found = FindContains(query, child);
            if (found != null) return found;
        }
        return null;
    }
    public GameObject FindContains(string query) {
        return FindContains(query, transform);
    }

    protected GameObject CreateEmpty(string emptyName) {
        // Helper for creating an empty child
        // (For IK targets, IK hints, etc)
        var go = new GameObject(emptyName);
        go.transform.SetParent(transform);
        return go;
    }
    protected GameObject CreateEmpty(string emptyName, Vector3 pos) {
        // Use Vector3 to set starting point
        var go = CreateEmpty(emptyName);
        go.transform.position = pos;
        return go;
    }
    protected GameObject CreateEmpty(string emptyName, Vector3 pos, Quaternion rot) {
        // Use Vector3 to set starting point
        var go = CreateEmpty(emptyName, pos);
        go.transform.rotation = rot;
        return go;
    }
    protected GameObject CreateEmpty(string emptyName, Vector3 pos, Vector3 lookAt) {
        // Use Vector3 to set starting point
        var go = CreateEmpty(emptyName, pos);
        go.transform.rotation = Quaternion.LookRotation(lookAt, Vector3.up);
        return go;
    }
    protected GameObject CreateEmpty(string emptyName, Transform t) {
        // Use transform position and rotation to set starting point
        return CreateEmpty(emptyName, t.position, t.rotation);
    }
    protected GameObject CreateEmpty(string emptyName, GameObject go) {
        // Use a gameObject's position to set starting point
        return CreateEmpty(emptyName, go.transform.position);
    }

    public static float Remap(float value, float inStart, float inEnd, float outStart, float outEnd) {
        // Take a value in an expected range, and remap it to a new range
        // TODO move to utility class
        var v = (value - inStart) / (inEnd - inStart) * (outEnd - outStart) + outStart;
        v = Mathf.Max(Mathf.Min(v, outEnd), outStart);
        return v;
    }

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

    public List<SerializableKV> dict;
    public void ViewDict(Dictionary<string, Vector3> dictionary) {
        // Just for viewing Dictionaries in unity's editor
        // Note: wanted to be able to view a bunch of dicts at once,
        // but cannot serlialize list of lists
        List<SerializableKV> list = new List<SerializableKV>();
        foreach (KeyValuePair<string, Vector3> pair in dictionary) {
            list.Add(new SerializableKV(pair.Key, pair.Value));
        }
        dict = list;
    }
}

[System.Serializable]
public struct SerializableKV {
    public string Key;
    public Vector3 Value;
    public SerializableKV(string key, Vector3 value) {
        Key = key;
        Value = value;
    }
}