using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;

public static class CustomInput {
    // Class for managing the input action system
    // - For now, using a simple Get(string)
    // to get info

    static internal Dictionary<string, InputAction> inputs;
    // TODO can we get these from actionMap
    static string[] inputNames = {
        "Movement", "Look",
        "Run", "Jump", "Crouch",
        "Target Lock",
        "Light Attack", "Heavy Attack", "Special Attack",
        "Dev Key",
        "Mouse Click", "Escape"
    };

    private static void Setup() {
        // Load player inputs
        // (This may requoire more action maps
        // and whatnot in the future - but leaving simple for now)
        var actions = Resources.Load<InputActionAsset>("New Controls");
        var actionMap = actions.FindActionMap("Player Input");

        inputs = new Dictionary<string, InputAction>();
        foreach (string name in inputNames) {
            var action = actionMap.FindAction(name);
            if (action == null) Debug.LogError("Did not find action: "+name);
            inputs.Add(name, actionMap.FindAction(name));
            inputs[name].Enable();
        }
    }

    public static bool Get(string name) {
        // Get a specific button input
        // such as 'Run' or 'Jump'
        if (inputs == null) Setup();
        if (!inputs.ContainsKey(name)) Debug.LogError("Did not find button: "+name);
        return inputs[name].ReadValue<float>() > 0;
    }

    public static  bool GetDown(string name) {
        // Get a specific button pressed down
        // (replacement for InputManager's GetKeyDown)
        if (inputs == null) Setup();
        if (!inputs.ContainsKey(name)) Debug.LogError("Did not find button: "+name);
        return inputs[name].triggered;
    }

    public static Vector2 GetAxis(string name) {
        // Get a specific axis input
        // such as 'Movement' or 'Look'
        if (inputs == null) Setup();
        if (!inputs.ContainsKey(name)) Debug.LogError("Did not find axis: "+name);
        return inputs[name].ReadValue<Vector2>();
    }

    public static InputAction GetAction(string name) {
        // Returns the action itself, for assignment
        // of callbacks or whathave you, e.g.:
        // CustomInput.GetAction("Jump").started += ctx => { UpdatePreJump(true) };
        if (inputs == null) Setup();
        if (!inputs.ContainsKey(name)) Debug.LogError("Did not find action: "+name);
        return inputs[name];
    }

    

}