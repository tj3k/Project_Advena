using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GeneralCam : MonoBehaviour
{
    public PlayerInput _playerInput;
    
    [Header("References")] 
    public Transform orientation, player, playerObj;    // Default Ones

    public Rigidbody rb;

    public GameObject playerGameObj;
    
    // Experimental arrays for live switching of the player model on pickup.
    public Transform[] playerStage, playerModel, playerOrientation;

    public Rigidbody[] playerRigidbody;

    public float rotationSpeed;

    public CameraStage currentStage;

    public GameObject[] CameraStageObj;
    private Vector2 _currentMovementInput;
    private Vector3 _currentMovement;

    public enum CameraStage
    {
        Stage1,
        Stage2,
        Stage3,
        Stage4
    }

    private void Awake()
    {
        _playerInput = new PlayerInput();
        _playerInput.Stage3.Move.started += OnMovementInput;
        // Checking for when input is released from keyboard.
        _playerInput.Stage3.Move.canceled += OnMovementInput;
        // Code for Analog Controllers that can range values between 0.0f - 1.0f
        _playerInput.Stage3.Move.performed += OnMovementInput;
        
    }
    
    void OnMovementInput(InputAction.CallbackContext context)
    {
        _currentMovementInput = context.ReadValue<Vector2>();
    }

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
        // float horizontalInput = _currentMovementInput.x;
        float verticalInput = Input.GetAxis("Vertical");
        // float verticalInput = _currentMovementInput.y;


        Vector3 inputDir = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (inputDir != Vector3.zero)
        {
            playerObj.forward = Vector3.Slerp(playerObj.forward,inputDir.normalized,Time.deltaTime * rotationSpeed);
        }

    }

    private void SwitchCamera(CameraStage newStage)
    {
        CameraStageObj[0].SetActive(false);
        CameraStageObj[1].SetActive(false);
        CameraStageObj[2].SetActive(false);
        CameraStageObj[3].SetActive(false);

        if (newStage == CameraStage.Stage1) CameraStageObj[0].SetActive(true);
        if (newStage == CameraStage.Stage2) CameraStageObj[1].SetActive(true);
        if (newStage == CameraStage.Stage3) CameraStageObj[2].SetActive(true);
        if (newStage == CameraStage.Stage4) CameraStageObj[3].SetActive(true);

        currentStage = newStage;
    }
}
