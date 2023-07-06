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

    // Unfortunatly, there is no drop in replacement
    // for GetKeyUp - but we can make our own
    static internal Dictionary<string, bool> _keyUp;
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

            // Add special callbacks to replicate GetUp
            action.canceled += ctx => { _keyUp[name] = true; };
            action.performed += ctx => { _keyUp[name] = false; };
        }
    }

    public static bool Get(string name) {
        // Get a specific button input
        // such as 'Run' or 'Jump'
        if (inputs == null) Setup();
        if (!inputs.ContainsKey(name)) Debug.LogError("Did not find button: "+name);
        return inputs[name].ReadValue<bool>();
    }

    public static  bool GetDown(string name) {
        // Get a specific button pressed down
        // (replacement for InputManager's GetKeyDown)
        if (inputs == null) Setup();
        if (!inputs.ContainsKey(name)) Debug.LogError("Did not find button: "+name);
        return inputs[name].triggered;
    }

    public static  bool GetUp(string name) {
        // Get a specific button pressed down
        // (replacement for InputManager's GetKeyDown)
        if (_keyUp == null) Setup();
        if (!_keyUp.ContainsKey(name)) Debug.LogError("Did not find key-up button: "+name);
        return _keyUp[name];
    }

    public static Vector2 GetAxis(string name) {
        // Get a specific axis input
        // such as 'Movement' or 'Look'\
        if (inputs == null) Setup();
        if (!inputs.ContainsKey(name)) Debug.LogError("Did not find axis: "+name);
        return inputs[name].ReadValue<Vector2>();
    }

    

}