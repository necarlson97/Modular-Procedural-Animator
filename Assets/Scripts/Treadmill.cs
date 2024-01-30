using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class Treadmill : Being {
    // A 'being' that is controlled by dev ui elements,
    // specifically for the purpose of viewing the animations in a vaccume
    // (for, say, comparison against a professional, traditional animation)

    void Start() {
        // Automatically find and setup buttons
        SetupButton("ToggleCrouchButton");
        SetupButton("ToggleRunButton");
        SetupButton("LightAttackButton");
        SetupButton("HeavyAttackButton");
        SetupButton("SpecialAttackButton");
        SetupButton("ToggleGuardButton");

        // For now, because there is no OnDown/OnUp, only OnClick
        // for buttons, we are ignoring prep-jump
        SetupButton("JumpButton");

        // Automatically find and setup sliders
        SetupSlider("MovementSliderX");
        SetupSlider("MovementSliderZ");

        // Zero movement
        SetupButton("ZeroButton");
        SetupButton("ToggleLockButton");

        // Have us moving by default
        SetMovement();
        ToggleRun();

        // Setup camera
        Slider slider = GameObject.Find("OrbitSlider").GetComponent<Slider>();
        slider.onValueChanged.AddListener(delegate { SetOrbit(); });
        SetOrbit();
    }

    void SetupButton(string buttonName) {
        Button button = GameObject.Find("UI/"+buttonName).GetComponent<Button>();
        string methodName = buttonName.Replace("Button", "");
        button.onClick.AddListener(delegate { Invoke(methodName, 0f); });
    }

    void SetupSlider(string sliderName) {
        Slider slider = GameObject.Find(sliderName).GetComponent<Slider>();
        slider.onValueChanged.AddListener(delegate { SetMovement(); });
    }

    void SetMovement() {
        Slider movementSliderX = GameObject.Find("MovementSliderX").GetComponent<Slider>();
        Slider movementSliderZ = GameObject.Find("MovementSliderZ").GetComponent<Slider>(); 
        // For now, just x/z
        var x = movementSliderX.value * 2 - 1;
        var z = movementSliderZ.value * 2 - 1;
        var movement = new Vector3(x, 0, z);
        SetMovement(movement);
    }

    void Zero() {
        Slider movementSliderX = GameObject.Find("MovementSliderX").GetComponent<Slider>();
        Slider movementSliderZ = GameObject.Find("MovementSliderZ").GetComponent<Slider>(); 
        movementSliderX.value = 0.5f;
        movementSliderZ.value = 0.5f;
        SetMovement();
    }

    bool _locked = false;
    void ToggleLock() {
        _locked = !_locked;
    }

    Vector3 startPos;
    protected override void AfterUpdate() {
        // Keep us locked in place
        if (startPos == default(Vector3)) {
            startPos = transform.position;
        }
        if (_locked) transform.position = startPos;
    }

    void SetOrbit() {
        // Orbit camera (and reference animation) around, based on slider
        Slider slider = GameObject.Find("OrbitSlider").GetComponent<Slider>();
        var angle = slider.value * 360;
        var rt = transform.Find("Rotator");
        rt.localRotation = Quaternion.Euler(0, angle, 0);

        foreach (var reference in transform.GetComponentsInChildren<Animator>()) {
            reference.transform.localRotation = Quaternion.Euler(0, -angle, 0);
        }
    }
}
