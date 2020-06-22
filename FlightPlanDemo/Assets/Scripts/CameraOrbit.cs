using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    protected Transform xFormCamera;
    protected Transform xFormParent;
    protected Vector3 localRotation;
    protected float cameraDistance = 10f;
    public float mouseSensitivity = 4f;
    public float scrollSensitivity = 2f;
    public float orbitDampening = 10f;
    public float scrollDampening = 6f;
    public bool cameraDisabled = false;

    // Start is called before the first frame update
    void Start()
    {
        this.xFormCamera = this.transform;   
        this.xFormParent = this.transform.parent;
    }

    // Update is called once per frame, after update() every game object in scene
    void LateUpdate()
    {
        if(Input.GetKeyDown(KeyCode.LeftShift)){
            cameraDisabled = !cameraDisabled;
        }

        if(!cameraDisabled){
            // Rotation of the camera based on mouse coordinates
            if(Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0){
                localRotation.x += Input.GetAxis("Mouse X") * mouseSensitivity;
                localRotation.y -= Input.GetAxis("Mouse Y") * mouseSensitivity;

                // Clamp the Y rotation to horizon and not flipping over at the top 
                // if(localRotation.y < 0f){
                //     localRotation.y = 0f;
                // }
                // else if(localRotation > 90f){
                //     localRotation.y = 90;
                // }
                localRotation.y = Mathf.Clamp(localRotation.y, 0f, 90f);
            }
            
            //Zooming input from mouse scroll wheel
            if(Input.GetAxis("Mouse ScrollWheel") != 0){
                float scrollAmount = Input.GetAxis("Mouse ScrollWheel") * scrollSensitivity;

                // Makes the camera zoom faster the further away it is from the target 
                scrollAmount *= (this.cameraDistance * 0.3f);
                this.cameraDistance += scrollAmount * -1f;

                // This makes camera go no closer than 1.5 meters from target, and no further than 100
                this.cameraDistance = Mathf.Clamp(this.cameraDistance, 1.5f, 100f); 
            }

        }

        // Actual Camera Rig Transformation
        Quaternion qt = Quaternion.Euler(localRotation.y, localRotation.x, 0);
        // Linear interpolation between current rotation of camera at the start of the frame animate towards the traget rotation
        this.xFormParent.rotation = Quaternion.Lerp(this.xFormParent.rotation, qt, Time.deltaTime * orbitDampening);

        if(this.xFormCamera.localPosition.z != this.cameraDistance * -1f){
            // Optimization for quaternion
            // Set the scroll distance, rotation of camera
            // Animating towards target values

            this.xFormCamera.localPosition = new Vector3(0f, 0f, Mathf.Lerp(this.xFormCamera.localPosition.z, this.cameraDistance * -1f, Time.deltaTime * scrollDampening));
        }
    }
}
