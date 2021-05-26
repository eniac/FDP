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

public class PopUpControl : MonoBehaviour
{
    private IEnumerator coroutine = null;
    private Text messageText;
    public void PopUpInit(Text msgText){
        // Initialize parameters
        messageText = msgText;
        ShowDefaultMessage();
    }
    public void ShowErrorMessage(string message, int time, Color color){
        // Reset the parameters
        if(coroutine != null){
            StopCoroutine(coroutine);
        }
        // Set the error message
        messageText.text = message;
        messageText.fontSize = 20;
        messageText.fontStyle = FontStyle.Normal;
        messageText.color = color;

        // Wait for specified amount of time      
        coroutine = wait(time);
        StartCoroutine(coroutine);
    }
    // Wait for specified amount of time. hide the object when done
    private IEnumerator wait(int time){
        yield return new WaitForSeconds(time);
        ShowDefaultMessage();
    }
    // Set default message
    private void ShowDefaultMessage(){
        // messageText.text = Global.chosanExperimentName + "\n" + "<size=12><color=#00ff00>Click on nodes with red box for more information...</color></size>";
        if(Global.chosanExperimentName == null){
            messageText.text = "Flightplan Demo";
        }
        else{
            messageText.text = Global.chosanExperimentName.Replace('_',' ');
        }
        messageText.fontSize = 20;
        messageText.fontStyle = FontStyle.Bold;
        messageText.color = Color.white;
    }
}
