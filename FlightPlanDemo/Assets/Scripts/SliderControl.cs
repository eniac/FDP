using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderControl : MonoBehaviour
{
    // [SerializeField] AnimationControl anim = default;
    [SerializeField] AnimControl anim = default;
    [SerializeField] Slider timeSlider = default;
    [SerializeField] Slider speedSlider = default;
    Global.SliderMode sliderMode;
    float lastTimeSliderPos = 0f;
    float lastPointerDownPos = 0f;
    float timeSliderLength;
    float timeSliderOffset;
    float timeSliderDifference;
    bool timeSliderValueChange;
    Vector2 timeSliderInitPos;
    GameObject timeSliderHandle;

    void Start(){
        timeSliderHandle = timeSlider.gameObject.transform.Find("Handle Slide Area").transform.Find("Handle").gameObject;
        timeSlider.minValue = lastTimeSliderPos;  
        timeSlider.maxValue = 121.0f;
        timeSlider.value = lastTimeSliderPos;
        timeSliderInitPos = timeSliderHandle.transform.position;
        RectTransform rect= timeSlider.GetComponent<RectTransform>();
        // Debug.Log("Slider size = " + rect.sizeDelta.ToString() + rect.rect.width + " - " + rect.rect.height + " : " + rect.offsetMax + " - " + rect.offsetMin);
        float xOffset = rect.offsetMin.x;
        float length = rect.rect.width;
        float margin = rect.sizeDelta.x;
        float canvasWidth = length + margin;
        timeSliderLength = length;
        timeSliderOffset = xOffset;
        timeSliderValueChange = false;

        speedSlider.minValue = 0;
        speedSlider.maxValue = Global.MAX_SPEED;
        speedSlider.value = 1;
        
        // HPSliderRect.sizeDelta = new Vector2(PlayerMaxHP, HPSliderRect.sizeDelta.y);
    }

    public void SetSliderMaxValue(float value){
        anim.SetAnimTime(value);
        timeSlider.maxValue = value;
        timeSlider.gameObject.transform.parent.Find("RemainingTime").gameObject.GetComponent<Text>().text = Math.Round((Decimal)value, 3, MidpointRounding.AwayFromZero).ToString();
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
        timeSlider.gameObject.transform.parent.Find("ElapsedTime").gameObject.GetComponent<Text>().text = Math.Round((Decimal)position, 3, MidpointRounding.AwayFromZero).ToString()+"s";
        float timeRemain = timeSlider.maxValue - position;
        timeSlider.gameObject.transform.parent.Find("RemainingTime").gameObject.GetComponent<Text>().text = Math.Round((Decimal)timeRemain, 3, MidpointRounding.AwayFromZero).ToString()+"s";
        
        // Indicator if value has changed on slider
        timeSliderValueChange = true;
    }

    public void SetTimeSlider(float time){
        timeSlider.value = time;
        // Vector2 pos = new Vector2(timeSliderInitPos.x + timeSliderLength + timeSliderOffset - 20, timeSliderHandle.transform.position.y);
        // timeSliderHandle.transform.position = pos;
        // Debug.Log(timeSlider.value);
    }

    public void TimeSliderPointerDown(){
        // anim.SetLastAnimStatus();
        // if(anim.GetAnimationStatus() != Global.AnimStatus.Pause){
        //     anim.Pause();
        //     // SetTimeSlider(((timeSlider.maxValue - timeSlider.minValue)/timeSliderLength * 1f)*(Input.mousePosition.x * 1f - timeSliderOffset * 1f));
        // }
        // // Debug.Log("DOWN SLIDER POS = " + timeSlider.value); 
        // if(timeSliderValueChange == false){
        //     timeSliderDifference = 0f;
        // } 
        // // Debug.Log("DOWN POINTER DIFFERENCE = " + timeSliderDifference); 
        // lastPointerDownPos = timeSlider.value;
        // timeSliderValueChange = false;

        Debug.Log("Pointer Down");
        anim.SetAnimParamBeforeSliderJump();
        anim.Pause();
        if(timeSliderValueChange == false){
            timeSliderDifference = 0f;
        } 
        lastPointerDownPos = timeSlider.value;
        timeSliderValueChange = false;
    }

    public void TimeSliderPointerUp(){
        // Debug.Log("UP SLIDER POS = " + timeSlider.value); 
        Debug.Log("FINAL diff = " + (timeSliderDifference + timeSlider.value - lastPointerDownPos).ToString() );   

        anim.SetAnimParamBeforeSliderJump(timeSliderDifference + timeSlider.value - lastPointerDownPos);
    }

    public void AdjustSpeed(float speed){
        anim.AdjustSpeed(speed);
    }

    public void SetSpeedSliderDefault(){
        speedSlider.value = 1;
    }
}
