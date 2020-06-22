using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopUpControl : MonoBehaviour
{
    [SerializeField] private Text errorMessageText = default;
    private IEnumerator coroutine;

    void Awake(){
        // Initialize parameters
        ShowDefaultMessage();
        coroutine = null;
    }
    public void ShowErrorMessage(string message, int time, Color color){
        // Reset the parameters
        if(coroutine != null){
            StopCoroutine(coroutine);
        }
        // Set the error message
        errorMessageText.text = message;
        errorMessageText.fontSize = 20;
        errorMessageText.fontStyle = FontStyle.Normal;
        errorMessageText.color = color;

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
        errorMessageText.text = "FlightPlan Demo";
        errorMessageText.fontSize = 28;
        errorMessageText.fontStyle = FontStyle.Bold;
        errorMessageText.color = Color.white;
    }
}
