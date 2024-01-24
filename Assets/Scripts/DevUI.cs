using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DevUI : MonoBehaviour {
    // Handles logic for dev tools on the screen
    Slider timeSlider;
    void Start() {
        timeSlider = transform.Find("TimeSlider").GetComponent<Slider>();
        Time.timeScale = timeSlider.value;

        CustomInput.GetAction("Inc Time").canceled += (
            ctx => { IncrementTime(); }
        );
        CustomInput.GetAction("Dec Time").canceled += (
            ctx => { DecrementTime(); }
        );
        timeSlider.onValueChanged.AddListener(delegate { SliderChanged(); });

        SliderChanged();
    }

    public void SliderChanged() {
        Time.timeScale = timeSlider.value;
    }

    public void IncrementTime() {
        Time.timeScale = Mathf.Min(1f, Time.timeScale + .1f);
        Slider timeSlider = GetComponentInChildren<Slider>();
        timeSlider.value = Time.timeScale;
    }
    public void DecrementTime() {
        Time.timeScale = Mathf.Max(0, Time.timeScale - .1f);
        Slider timeSlider = GetComponentInChildren<Slider>();
        timeSlider.value = Time.timeScale;
    }
}
