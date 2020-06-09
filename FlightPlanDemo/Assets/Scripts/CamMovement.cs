using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamMovement : MonoBehaviour
{
    [SerializeField] private Light light;
    [SerializeField] private Camera cam;
    [SerializeField] private Transform target; 
    private Vector3 camInitPos;
    private Vector3 previousPosition;
    
    // Start is called before the first frame update
    void Start()
    {
        // main camera Init
        camInitPos = new Vector3(0,5,-40);
        cam.transform.position = new Vector3(0,0,0);
        cam.transform.Rotate(new Vector3(1,0,0));
        cam.transform.Rotate(new Vector3(0,1,0));
        cam.transform.Translate(camInitPos);
        // directional light init
        light.transform.position = new Vector3(0,0,0);
        light.transform.Rotate(new Vector3(1,0,0));
        light.transform.Rotate(new Vector3(0,1,0));
        light.transform.Translate(camInitPos);

    }

    // Update is called once per frame
    void Update()
    {
        // GetMouseButtonDown returns true when user press the mouse button 
        // This time record the previous position
        if(Input.GetMouseButtonDown(0)){
            previousPosition = cam.ScreenToViewportPoint(Input.mousePosition);
        }

        // If mouse button is held down change the direction of camera accordingly
        if(Input.GetMouseButton(0)){
            Vector3 direction = previousPosition - cam.ScreenToViewportPoint(Input.mousePosition);

            // Change camera position
            cam.transform.position = target.position;     //new Vector3(0,0,0);
            cam.transform.Rotate(new Vector3(1,0,0), direction.y*180);
            cam.transform.Rotate(new Vector3(0,1,0), -direction.x*180, Space.World);
            cam.transform.Translate(camInitPos);

            // Change light position
            light.transform.position = target.position;     //new Vector3(0,0,0);
            light.transform.Rotate(new Vector3(1,0,0), direction.y*180);
            light.transform.Rotate(new Vector3(0,1,0), -direction.x*180, Space.World);
            light.transform.Translate(camInitPos);

            previousPosition = cam.ScreenToViewportPoint(Input.mousePosition);
        }

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
