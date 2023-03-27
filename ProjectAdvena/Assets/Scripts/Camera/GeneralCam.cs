using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralCam : MonoBehaviour
{
    [Header("References")] 
    public Transform orientation, player, playerObj;

    public Rigidbody rb;

    public float rotationSpeed;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Rotates Orientation of Camera
        Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        orientation.forward = viewDir.normalized;
        
        // Rotate Player Object, (1) Getting inputs via name and (2) 
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 inputDir = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (inputDir != Vector3.zero)
        {
            playerObj.forward = Vector3.Slerp(playerObj.forward,inputDir.normalized,Time.deltaTime * rotationSpeed);
        }

    }
}
