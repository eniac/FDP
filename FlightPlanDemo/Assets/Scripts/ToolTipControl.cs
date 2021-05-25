/*
Copyright 2021 Heena Nagda

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolTipControl : MonoBehaviour
{
    private static ToolTipControl instance;
    Text toolTipText;
    RectTransform backgroundRectTransform;

    void Start(){
        instance = this;
        backgroundRectTransform = transform.Find("Background").GetComponent<RectTransform>();
        toolTipText = backgroundRectTransform.Find("Text").GetComponent<Text>();
        HideToolTip();
        // ShowToolTip("Random Text on");
    }
    void ShowToolTip(string toolTipString){
        gameObject.SetActive(true);
        toolTipText.text = toolTipString;   
        float textPaddingSize = 4f;     
        Vector2 backgroundSize = new Vector2(toolTipText.preferredWidth + textPaddingSize*2f, toolTipText.preferredHeight + textPaddingSize*2f);
        backgroundRectTransform.sizeDelta = backgroundSize;
    }

    void HideToolTip(){
        gameObject.SetActive(false);
    }

    public static void ShowToolTip_Static(string toolTipString){
        instance.ShowToolTip(toolTipString);
    }

    public static void HideToolTip_Static(){
        instance.HideToolTip();
    }
    void Update(){
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent.GetComponent<RectTransform>(), Input.mousePosition, null, out localPoint);
        transform.localPosition = localPoint;
    }
}
