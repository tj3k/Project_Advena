using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // Variables for using Rigidbody for movement instead.
    private Rigidbody _rigidbody;

    public float airDrag;
    public float groundDrag;

    [Header("GroundCheck")] 
    public float playerHeight;
    public LayerMask whatIsGround;
    private bool _isGrounded;
    
    // Declaring reference variables
    private PlayerInput _playerInput;
    private CharacterController _characterController;
    private Animator _animator;

    public Transform orientation;

    // variables to storing Animator parameter IDs
    private int _isWalkingHash;
    private int _isRunningHash;
    private int _isJumpingHash;

    // Variables for storing player input values
    private Vector2 _currentMovementInput;
    private Vector3 _currentMovement;
    private Vector3 _currentRunMovement;
    private Vector3 _appliedMovement;
    private bool _isMovementPressed;
    private bool _isRunPressed;
    private bool _isJumpAnimating;

    public float speed = 3.0f;
    public float runMultiplier = 2.5f;
    public float maxSpeed = 9;

    public float rotationFactorPerFrame = 12.0f;
    
    // State checks to enable more functions from picking up items in game.
    public bool hasHead;
    public bool hasLegs;
    public bool hasArms;
    
    // Constants
    private float _gravity = -7.0f;
    private float _groundedGravity = -.05f;
    private int _zero = 0;
    
    // Variables for Jumping
    private bool _isJumpPressed;
    private float _initialJumpVelocity;
    private float _maxJumpHeight = 0.6f;
    private float _maxJumpTime = 0.9f;
    private bool _isJumping;

    // Awake is called before Start
    private void Awake()
    {
        // Init set reference variables
        _playerInput = new PlayerInput();
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.freezeRotation = true;

        _isWalkingHash = Animator.StringToHash("isWalking");
        _isRunningHash = Animator.StringToHash("isRunning");
        _isJumpingHash = Animator.StringToHash("isJumping");

        // Checking for when input in sent from keyboard.
        _playerInput.Stage3.Move.started += OnMovementInput;
        // Checking for when input is released from keyboard.
        _playerInput.Stage3.Move.canceled += OnMovementInput;
        // Code for Analog Controllers that can range values between 0.0f - 1.0f
        _playerInput.Stage3.Move.performed += OnMovementInput;
        
        // Player Input callbacks for Running
        _playerInput.Stage3.Run.started += OnRun;
        _playerInput.Stage3.Run.canceled += OnRun;
        
        // Player Input callbacks for Jumping
        _playerInput.Stage3.Jump.started += OnJump;
        _playerInput.Stage3.Jump.canceled += OnJump;
        
        SetupJumpVariables();
    }

    void SetupJumpVariables()
    {
        float timeToApex = _maxJumpTime / 2;
        _gravity = (-2 * _maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        _initialJumpVelocity = (2 * _maxJumpHeight) / timeToApex;
    }

    void OnMovementInput(InputAction.CallbackContext context)
    {
        _currentMovementInput = context.ReadValue<Vector2>();
        _currentMovement.x = _currentMovementInput.x;
        _currentMovement.z = _currentMovementInput.y;
        _currentRunMovement.x = _currentMovementInput.x * runMultiplier;
        _currentRunMovement.z = _currentMovementInput.y * runMultiplier;
        
        _isMovementPressed = _currentMovementInput.x != 0 || _currentMovementInput.y != 0;
    }

    void OnRun(InputAction.CallbackContext context)
    {
        _isRunPressed = context.ReadValueAsButton();
    }

    void OnJump(InputAction.CallbackContext context)
    {
        _isJumpPressed = context.ReadValueAsButton();
    }

    void HandleAnimation()
    {
        bool isWalking = _animator.GetBool(_isWalkingHash);
        bool isRunning = _animator.GetBool(_isRunningHash);
        // bool isJumping = animator.GetBool(isJumpingHash);

        // Plays walking animation if movement input are pressed...
        if (_isMovementPressed && !isWalking)
        {
            _animator.SetBool(_isWalkingHash, true);
        }
        // ...Stops walking animation if there's no movement input being pressed.
        else if (!_isMovementPressed && isWalking)
        {
            _animator.SetBool(_isWalkingHash, false);
        }

        // Only play Jump animation if Player "gets legs" upgrade.
        if (hasLegs)
        {
            // Running animation if Run button is pressed but player wasn't running prior.
            if ((_isMovementPressed && _isRunPressed) && !isRunning)
            {
                _animator.SetBool(_isRunningHash, true);
            }
            // Stops running animation if either movement or run button were let go while "running" prior.
            else if ((!_isMovementPressed || !_isRunPressed) && isRunning)
            {
                _animator.SetBool(_isRunningHash,false);
            }
            
        }

    }
    
    void HandleRotation()
    {
        Vector3 positionToLookAt;
        
        // New rotation values for our player to be in based on forward movement.
        positionToLookAt.x = _currentMovement.x;
        positionToLookAt.y = 0.0f;
        positionToLookAt.z = _currentMovement.z;
        
        // Current rotation of our character copied to new Quaternion variable.
        Quaternion currentRotation = transform.rotation;
        
        // Creates new rotation based on movement inputs.
        if (_isMovementPressed)
        {
            // Creates new rotation performing with values from positionToLookAt.
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            // Smoothens rotation with slerp, from currentRotation (Prev) to targetRotation (New) with rotationFactorPerFrame determining the speed.
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFactorPerFrame * Time.deltaTime);
        }
    }

    void HandleGravity()
    {
        bool isFalling = _currentMovement.y <= 0.0f || (!_isJumpPressed);
        float fallMultiplier = 1.5f;
        
        // Creating reasonable gravity whenever player is in the air or on the ground.
        if (_characterController.isGrounded)
        {
            if (_isJumpAnimating)
            {
                _animator.SetBool(_isJumpingHash, false);
                _isJumpAnimating = false;
            }
            
            _currentMovement.y = _groundedGravity;
            _appliedMovement.y = _groundedGravity;
        }
        else if (isFalling) // Applying gravity once apex of jump is reached.
        {
            float prevYVelocity = _currentMovement.y;
            _currentMovement.y = _currentMovement.y + (_gravity * fallMultiplier * Time.deltaTime);
            _appliedMovement.y = Mathf.Max((prevYVelocity + _currentMovement.y) * 0.5f, -20.0f);
            
            // currentMovement.y = nextYVelocity;
            // currentRunMovement.y = nextYVelocity;
        }
        else
        {
            // Velocity Verlet method used to calculate consistent jumping.
            float prevYVelocity = _currentMovement.y;
            _currentMovement.y = _currentMovement.y + (_gravity * Time.deltaTime);
            _appliedMovement.y = (prevYVelocity + _currentMovement.y) * 0.5f;

            // currentMovement.y = nextYVelocity;
            // currentRunMovement.y = nextYVelocity;
            
            // Euler method easy but inconsistent with different frame rates.
            /*
            currentMovement.y += gravity * Time.deltaTime;
            currentRunMovement.y += gravity * Time.deltaTime;
            */
        }
    }

    void HandleJump()
    {
        if (hasLegs)
        {
            if (_isJumpPressed && !_isJumping && _characterController.isGrounded)
            {
                _animator.SetBool(_isJumpingHash, true);
                _isJumpAnimating = true;
                _isJumping = true;
                
                // // New Velocity Verlet Implementation 
                // float prevYVelocity = currentMovement.y;
                // float newYVelocity = currentMovement.y + initialJumpVelocity;
                // float nextYVelocity = (prevYVelocity + newYVelocity) * 0.5f;
                //
                // currentMovement.y = nextYVelocity;
                // currentRunMovement.y = nextYVelocity;
                
                // Old Inconsistent Method
                _currentMovement.y = _initialJumpVelocity;
                _appliedMovement.y = _initialJumpVelocity;
            }
            else if (!_isJumpPressed && _isJumping && _characterController.isGrounded)
            {
                _isJumping = false;
            }
        }
    }

    void HandleMove()
    {
        // Handling movement for the Rigidbody.
        // _rigidbody.AddForce(_appliedMovement.normalized * (speed * 100.0f), ForceMode.Force);
        
        // Applies run multiplier onto movement if player has legs & pressed Run button.
        // Applies normal movement otherwise.
        if (hasLegs && _isRunPressed)
        {
            // characterController.Move(currentRunMovement * (Time.deltaTime * runMultiplier));
            _appliedMovement.x = _currentRunMovement.x;
            _appliedMovement.z = _currentRunMovement.z;

            // Aligns Player movement with camera.
            _appliedMovement = orientation.forward * _currentRunMovement.z + orientation.right * _currentRunMovement.x;
            
            
            _rigidbody.AddForce(_appliedMovement.normalized * ((speed * runMultiplier) * 100.0f), ForceMode.Force);
        }
        else
        {
            // characterController.Move(currentMovement * (Time.deltaTime * speed));
            _appliedMovement.x = _currentMovement.x;
            _appliedMovement.z = _currentMovement.z;
            
            _appliedMovement = orientation.forward * _currentMovement.z + orientation.right * _currentMovement.x;
            
            _rigidbody.AddForce(_appliedMovement.normalized * (speed * 100.0f), ForceMode.Force);
        }
    }

    void HandleSpeed()
    {
        Vector3 flatVel = new Vector3(_rigidbody.velocity.x, 0.0f, _rigidbody.velocity.z);
        
        // Limiting velocity when breaching normal speed.
        if (flatVel.magnitude > maxSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * speed;
            _rigidbody.velocity = new Vector3(limitedVel.x, _rigidbody.velocity.y, limitedVel.z);
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        HandleAnimation();
        HandleRotation();

        // if (hasLegs && _isRunPressed)
        // {
        //     // characterController.Move(currentRunMovement * (Time.deltaTime * runMultiplier));
        //     _appliedMovement.x = _currentRunMovement.x;
        //     _appliedMovement.z = _currentRunMovement.z;
        // }
        // else
        // {
        //     // characterController.Move(currentMovement * (Time.deltaTime * speed));
        //     _appliedMovement.x = _currentMovement.x;
        //     _appliedMovement.z = _currentMovement.z;
        // }

        // Old Character Controller Movement
        // _characterController.Move(_appliedMovement * (Time.deltaTime * speed));

        _isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
        
        HandleMove();
        
        // Changing drag depending on whether in the air or on ground.
        if (_isGrounded)
        {
            _rigidbody.drag = groundDrag;
            // Debug.Log("XP-Lora-072 Ground State: " + _isGrounded);
        }
        else if (!_isGrounded)
        {
            _rigidbody.drag = airDrag;
            // Debug.Log("XP-Lora-072 Ground State: " + _isGrounded);
        }
        
        HandleGravity();
        HandleJump();
    }

    Vector3 ConvertToCameraSpace(Vector3 vectorToRotate)
    {
        // Getting forward and right directional vectors of main camera
        Vector3 cameraForward = UnityEngine.Camera.main.transform.forward;
        Vector3 cameraRight = UnityEngine.Camera.main.transform.right;

        // Removing the Y values to not invoke unwanted Y movement of the camera.
        cameraForward.y = 0;
        cameraRight.y = 0;

        // Re-Normalize both vectors to keep their magnitude of 1.
        cameraForward = cameraForward.normalized;
        cameraRight = cameraRight.normalized;

        // Rotate X and Z VectorToRotate values to camera space.
        Vector3 cameraForwardZProduct = vectorToRotate.z * cameraForward;
        Vector3 cameraRightXProduct = vectorToRotate.x * cameraRight;

        // Sums both products is the Vector3 of the Camera Space.
        Vector3 vectorRotatedToCameraSpace = cameraForwardZProduct + cameraRightXProduct;
        return vectorRotatedToCameraSpace;
    }

    private void OnEnable()
    {
        _playerInput.Stage3.Enable();
    }

    private void OnDisable()
    {
        _playerInput.Stage3.Disable();
    }
}
