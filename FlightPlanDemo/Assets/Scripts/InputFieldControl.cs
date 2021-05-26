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
