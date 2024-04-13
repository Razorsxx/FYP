using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Control : MonoBehaviour
{
    public Transform player;
    public float mouseSens = 2f;
    float cameraVertRotation = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float inputX = Input.GetAxis("Mouse X") * mouseSens;
        float inputY = Input.GetAxis("Mouse Y") * mouseSens;

        cameraVertRotation -= inputY;
        cameraVertRotation = Mathf.Clamp(cameraVertRotation, -90f, 90f);
        transform.localEulerAngles = Vector3.right * cameraVertRotation;

        player.Rotate(Vector3.up * inputX);
    }
}
