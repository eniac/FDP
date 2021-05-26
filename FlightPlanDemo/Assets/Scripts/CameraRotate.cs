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

public class CameraRotate : MonoBehaviour
{
    [SerializeField] private new Light light = default;
    private const float camSpeed = 5f;
    bool doRotate = false;
    Quaternion rotateMax;

    public void DoRotate(Quaternion rotateMax){
        doRotate = true;
        this.rotateMax = rotateMax;
    }
    void Update()
    {
        if(Input.GetKey(KeyCode.R)){
            RotateCamera();
        }
        else if(doRotate){
            // Debug.Log("ROTATION = " + rotateMax + " : "  + transform.rotation);
            // Debug.Log("ROTATION = " + " [ " + rotateMax.x + " : " + rotateMax.y + " : " + rotateMax.z + " : " + rotateMax.w  + " ] " + " [ " + Round(transform.rotation.x) + " : " + Round(transform.rotation.y) + " : " + Round(transform.rotation.z) + " : " + Round(transform.rotation.w) + " ] ");
            if(rotateMax.x == Round(transform.rotation.x)
                && rotateMax.y >= Round(transform.rotation.y) 
                && rotateMax.z == Round(transform.rotation.z) 
                && rotateMax.w >= Round(transform.rotation.w)){
                    doRotate = false;
            }
            else{
                Camera.main.fieldOfView -= 0.04f;
                RotateCamera();
            }
        }
    }

    void RotateCamera(){
        transform.Rotate(0, camSpeed * Time.deltaTime * (-1), 0);
        light.transform.Rotate(0, camSpeed * Time.deltaTime * (-1), 0);

        // Debug.Log("CAMERA pos = " + transform.position);
        // Debug.Log("CAMERA rot = " + transform.rotation);
    }

    float Round(float unrounded){
        return Mathf.Round(unrounded*10)/10;
    }
}
