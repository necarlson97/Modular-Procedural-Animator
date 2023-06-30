using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DevUI : MonoBehaviour {
    // Handles logic for dev tools on the screen
    void Update() {
        Slider timeSlider = GetComponentInChildren<Slider>();
        Time.timeScale = timeSlider.value;
    }
}
