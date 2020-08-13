using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotate : MonoBehaviour
{
    [SerializeField] private new Light light = default;
    private const float camSpeed = 2f;
    void Update()
    {
        if(Input.GetKey(KeyCode.R)){
            transform.Rotate(0, camSpeed * Time.deltaTime, 0);
            light.transform.Rotate(0, camSpeed * Time.deltaTime, 0);
        }
    }
}
