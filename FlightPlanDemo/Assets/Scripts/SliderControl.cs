using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderControl : MonoBehaviour
{
    [SerializeField] AnimationControl anim = default;
    [SerializeField] Slider timeSlider = default;
    Global.SliderMode sliderMode;
    float lastTimeSliderPos = 0f;
    float lastPointerDownPos = 0f;
    float timeSliderLength;
    float timeSliderOffset;
    float timeSliderDifference;
    bool timeSliderValueChange;

    void Start(){
        timeSlider.minValue = lastTimeSliderPos;
        timeSlider.maxValue = 27891602f/Global.U_SEC;
        timeSlider.value = lastTimeSliderPos;
        Debug.Log("GVar = " + Global.gVar);
        RectTransform rect= timeSlider.GetComponent<RectTransform>();
        // Debug.Log("Slider size = " + rect.sizeDelta.ToString() + rect.rect.width + " - " + rect.rect.height + " : " + rect.offsetMax + " - " + rect.offsetMin);
        float xOffset = rect.offsetMin.x;
        float length = rect.rect.width;
        float margin = rect.sizeDelta.x;
        float canvasWidth = length + margin;
        timeSliderLength = length;
        timeSliderOffset = xOffset;
        timeSliderValueChange = false;
        
        // HPSliderRect.sizeDelta = new Vector2(PlayerMaxHP, HPSliderRect.sizeDelta.y);
    }

    public void SetSliderMode(Global.SliderMode mode){
        sliderMode = mode;
    }
    public Global.SliderMode GetSliderMode(){
        return sliderMode;
    }
    public void TimeSliderEvent(float position){
        timeSliderDifference = position - lastTimeSliderPos;
        // Debug.Log("POINTER DIFFERENCE EVENT = " + lastTimeSliderPos + " : " + position + " = " + timeSliderDifference);
        lastTimeSliderPos = position;

        // Change Elapsed time and remaining time on UI element
        timeSlider.gameObject.transform.parent.Find("ElapsedTime").gameObject.GetComponent<Text>().text = Math.Round((Decimal)position, 3, MidpointRounding.AwayFromZero).ToString();
        float timeRemain = timeSlider.maxValue - position;
        timeSlider.gameObject.transform.parent.Find("RemainingTime").gameObject.GetComponent<Text>().text = Math.Round((Decimal)timeRemain, 3, MidpointRounding.AwayFromZero).ToString();
        
        // Indicator if value has changed on slider
        timeSliderValueChange = true;
    }

    public void SetTimeSlider(float time){
        timeSlider.value = time;
    }

    public void TimeSliderPointerDown(){
        anim.SetLastAnimStatus();
        if(anim.GetAnimationStatus() != Global.AnimStatus.Pause){
            anim.Pause();
            // SetTimeSlider(((timeSlider.maxValue - timeSlider.minValue)/timeSliderLength * 1f)*(Input.mousePosition.x * 1f - timeSliderOffset * 1f));
        }
        // Debug.Log("DOWN SLIDER POS = " + timeSlider.value); 
        if(timeSliderValueChange == false){
            timeSliderDifference = 0f;
        } 
        // Debug.Log("DOWN POINTER DIFFERENCE = " + timeSliderDifference); 
        lastPointerDownPos = timeSlider.value;
        timeSliderValueChange = false;
    }

    public void TimeSliderPointerUp(){
        // Debug.Log("UP SLIDER POS = " + timeSlider.value); 
        Debug.Log("FINAL diff = " + (timeSliderDifference + timeSlider.value - lastPointerDownPos).ToString() );   
        anim.SetReferenceCounterThreshold(timeSliderDifference + timeSlider.value - lastPointerDownPos);
    }

}
