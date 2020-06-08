using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamMovement : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Transform target; 
    Vector3 cam_init_pos;
    private Vector3 previousPosition;
    
    // Start is called before the first frame update
    void Start()
    {
        cam_init_pos = new Vector3(0,5,-40);
        cam.transform.position = new Vector3(0,0,0);
        cam.transform.Rotate(new Vector3(1,0,0));
        cam.transform.Rotate(new Vector3(0,1,0));
        cam.transform.Translate(cam_init_pos);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0)){
            previousPosition = cam.ScreenToViewportPoint(Input.mousePosition);
        }
        if(Input.GetMouseButton(0)){
            Vector3 direction = previousPosition - cam.ScreenToViewportPoint(Input.mousePosition);
            cam.transform.position = target.position;     //new Vector3(0,0,0);

            cam.transform.Rotate(new Vector3(1,0,0), direction.y*180);
            cam.transform.Rotate(new Vector3(0,1,0), -direction.x*180, Space.World);
            cam.transform.Translate(cam_init_pos);

            previousPosition = cam.ScreenToViewportPoint(Input.mousePosition);
        }
    }
}
