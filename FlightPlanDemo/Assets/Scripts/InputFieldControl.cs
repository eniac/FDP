using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldControl : MonoBehaviour
{
    private string nodeString;
    [SerializeField] private InputField inputField = default;

    public void GetNodeString(){
        // nodeString = inputField.GetComponent<Text>().text;
        // Debug.Log("Node string = " + nodeString);
        nodeString = inputField.text;
        Debug.Log("Node string = " + nodeString);
        inputField.text = "";
        // nodeString = inputField.GetComponent<textComponent>().text;
        // Debug.Log("Node string = " + nodeString);
    }


}
