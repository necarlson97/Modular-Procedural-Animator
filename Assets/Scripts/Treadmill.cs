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

        // Have us moving by default
        SetMovement();
        ToggleRun();
    }

    void SetupButton(string buttonName) {
        Debug.Log("Finding "+buttonName);
        Button button = GameObject.Find("UI/"+buttonName).GetComponent<Button>();
        string methodName = buttonName.Replace("Button", "");
        button.onClick.AddListener(delegate { Invoke(methodName, 0f); });
    }

    void SetupSlider(string sliderName) {
        Debug.Log("Finding "+sliderName);
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

    Vector3 startPos;
    protected override void AfterUpdate() {
        // Keep us locked in place
        if (startPos == default(Vector3)) {
            startPos = transform.position;
        }
        // transform.position = startPos;
    }
}
