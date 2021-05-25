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
using UnityEngine.EventSystems;

public class CamMovement : MonoBehaviour
{
    [SerializeField] private new Light light = default;
    [SerializeField] private Camera cam = default;
    [SerializeField] private Transform target = default; 
    private Vector3 camInitPos;
    private Vector3 previousPosition;
    private bool exposedNode;
    private float highlightZoom;
    private Vector3 targetPos;


    // Start is called before the first frame update
    void Start()
    {
        targetPos = target.position;
        // camInitPos = new Vector3(0,9,-55);
        camInitPos = new Vector3(0,9,-55);

        // main camera Init
        // cam.transform.position = new Vector3(0,0,0);
        // cam.transform.Rotate(new Vector3(1,0,0));
        // cam.transform.Rotate(new Vector3(0,1,0));
        // cam.transform.Translate(camInitPos);

        // cam.transform.position = new Vector3(30.3f, 9.0f,-45.9f);
        // cam.transform.rotation = new Quaternion(0, -0.3f, 0, 1f);
        cam.transform.position = new Vector3(0f, 9.0f,-55f);
        cam.transform.rotation = new Quaternion(0, 0, 0, 1.0f);
        cam.fieldOfView = 60;

        // directional light init
        // light.transform.position = new Vector3(0,0,0);
        // light.transform.Rotate(new Vector3(1,0,0));
        // light.transform.Rotate(new Vector3(0,1,0));
        // light.transform.Translate(camInitPos);

        // light.transform.position = new Vector3(30.3f, 9.0f,-45.9f);
        // light.transform.rotation = new Quaternion(0, -0.3f, 0, 1f);
        light.transform.position = cam.transform.position;
        light.transform.rotation = cam.transform.rotation;

        Debug.Log("init CAMERA pos = " + cam.transform.position);
        Debug.Log("init CAMERA rot = " + cam.transform.rotation);

        // exposed nodes are false initially
        exposedNode = false;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // GetMouseButtonDown returns true when user press the mouse button 
        // This time record the previous position
        if(Input.GetMouseButtonDown(0)){
            if(DisableMouseEventOverUI() == true){
                return;
            }
            previousPosition = cam.ScreenToViewportPoint(Input.mousePosition);
        }
        // If mouse button is held down change the direction of camera accordingly
        if(Input.GetMouseButton(0)){
            if(DisableMouseEventOverUI() == true){
                return;
            }
            Vector3 direction = previousPosition - cam.ScreenToViewportPoint(Input.mousePosition);
            // Change camera position
            cam.transform.position = targetPos;     //new Vector3(0,0,0);
            cam.transform.Rotate(new Vector3(1,0,0), direction.y*180);
            cam.transform.Rotate(new Vector3(0,1,0), -direction.x*180, Space.World);
            cam.transform.Translate(camInitPos);

            // Change light position
            light.transform.position = targetPos;     //new Vector3(0,0,0);
            light.transform.Rotate(new Vector3(1,0,0), direction.y*180, Space.Self);
            light.transform.Rotate(new Vector3(0,1,0), -direction.x*180, Space.World);
            light.transform.Translate(camInitPos);

            previousPosition = cam.ScreenToViewportPoint(Input.mousePosition);
            // Debug.Log("Cam Pos = " + cam.transform.position + " : " + cam.transform.rotation);
        }

        
        if(exposedNode==true){
            if(cam.fieldOfView > highlightZoom){
                cam.fieldOfView--;
            }
            else if(cam.fieldOfView < highlightZoom){
                cam.fieldOfView++;
            }
            else{
                exposedNode = false;
            }
        }
        else{
            // Zooming in with mouse scroll
            if(Input.GetAxis("Mouse ScrollWheel")>0){
                cam.fieldOfView--;
            }

            // Zooming out with mouse scroll
            if(Input.GetAxis("Mouse ScrollWheel")<0){
                cam.fieldOfView++;
            }
        }
    }

    bool DisableMouseEventOverUI(){
        // METHOD 1: This method will disable mouse event over every UI element
        // if(EventSystem.current.IsPointerOverGameObject()){
        //         return;
        // }

        // METHOD 2: This method will disable mouse event on selective UI elements
        PointerEventData pointerData = new PointerEventData (EventSystem.current)
        {
            pointerId = -1,
        };

        pointerData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        
        foreach(var ui in results){
            // If UI element represents host then don't disable mouse event
            if(ui.gameObject.name == "HostText"){
                return false;
            }
        }
        if(results.Count != 0){
            return true;
        }
        return false;
    }
    public void MoveCamToNodes(List<GameObject> highlitedObjects){
        move(highlitedObjects);
        zoom(highlitedObjects);
        exposedNode = true;
    }
    private void zoom(List<GameObject> highlitedObjects){
        float zoomLimiter = 0.8f;
        float minZoom = 40f;
        float maxZoom = 10f;
        float newZoom = Mathf.Lerp(maxZoom, minZoom, GetGreatestDistance(highlitedObjects)/ zoomLimiter);
        // cam.fieldOfView = newZoom;
        highlightZoom = newZoom;
    }
    private float GetGreatestDistance(List<GameObject> targets){
        var bounds = new Bounds(targets[0].transform.position, Vector3.zero);
        foreach(var target in targets){
            bounds.Encapsulate(target.transform.position);
        }
        return bounds.size.x;
    }
    private void move(List<GameObject> highlitedObjects){
        Vector3 offset = new Vector3(0,0,0);
        Vector3 centerpoint = GetCenterPoint(highlitedObjects);
        cam.transform.LookAt(centerpoint, Vector3.up);
        light.transform.LookAt(centerpoint, Vector3.up);
    }
    private Vector3 GetCenterPoint(List<GameObject> targets){
        if(targets.Count == 1){
            return targets[0].transform.position;
        }
        var bounds = new Bounds(targets[0].transform.position, Vector3.zero);
        for(int i=0; i<targets.Count; i++){
            bounds.Encapsulate(targets[i].transform.position);

        }
        return bounds.center;
    }

    // float speed = 5f;
    // float minFOV = 35;
    // float maxFOV = 100;
    // float sensitivity = 17f;

    // void Update(){
    //     if(Input.GetMouseButton(1)){
    //         cam.transform.RotateAround(target.position, cam.transform.up, Input.GetAxis("Mouse X") * speed);
    //         cam.transform.RotateAround(target.position, cam.transform.right, Input.GetAxis("Mouse Y") * -speed);
    //     }

    //     float fov = cam.fieldOfView;
    //     fov += Input.GetAxis("Mouse ScrollWheel") * -sensitivity;
    //     fov = Mathf.Clamp(fov, minFOV, maxFOV);
    //     cam.fieldOfView = fov;
    // }
    
}
