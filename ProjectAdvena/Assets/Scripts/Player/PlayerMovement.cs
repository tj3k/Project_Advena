using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    public AudioManager playerAudio;
    
    // Variables for using Rigidbody for movement instead.
    private Rigidbody _rigidbody;

    public float airDrag;
    public float groundDrag;

    // Variables for jumping
    public float jumpForce, jumpCooldown, airMultiplier;
    private bool _readyToJump;

    [Header("GroundCheck")] 
    public float playerHeight;
    public LayerMask whatIsGround;
    private bool _isGrounded;

    [Header("Slope Handling")] 
    public float maxSlopeAngle;
    private RaycastHit _slopeHit;
    private bool _exitingSlope;
    
    // Declaring reference variables
    private PlayerInput _playerInput;
    // private CharacterController _characterController;
    private Animator _animator;

    public Transform orientation;

    // variables to storing Animator parameter IDs
    private int _isWalkingHash;
    private int _isRunningHash;
    private int _isJumpingHash;
    private int _isFallingHash;

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
    private bool _isPickupPressed;

    // Awake is called before Start
    private void Awake()
    {
        // Init set reference variables
        _playerInput = new PlayerInput();
        // _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.freezeRotation = true;

        _isWalkingHash = Animator.StringToHash("isWalking");
        _isRunningHash = Animator.StringToHash("isRunning");
        _isJumpingHash = Animator.StringToHash("isJumping");
        _isFallingHash = Animator.StringToHash("isFalling");

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

        _playerInput.Stage3.Pickup.started += OnPickup;
        _playerInput.Stage3.Pickup.canceled += OnPickup;
        
        SetupJumpVariables();
        
        OnEnable();
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

    void OnPickup(InputAction.CallbackContext context)
    {
        _isPickupPressed = context.ReadValueAsButton();
    }

    void HandleAudio()
    {
        if (_isMovementPressed && _isGrounded)
        {
            if (!hasLegs)
            {
                playerAudio.PlayHoverSfx();
            }
            else if (hasLegs)
            {
                playerAudio.PlayWalkSfx();
            }
        }
        else if (!_isMovementPressed || !_isGrounded)
        {
            playerAudio.StopHoverWalkSfx();
        }
    }
    
    void HandleAnimation()
    {
        bool isWalking = _animator.GetBool(_isWalkingHash);
        bool isRunning = _animator.GetBool(_isRunningHash);
        // bool isFalling = _animator.GetBool(_isFallingHash);
        // bool isJumping = _animator.GetBool(_isJumpingHash);

        // Plays walking animation if movement input are pressed...
        if (_isMovementPressed && _isGrounded && !isWalking)
        {
            _animator.SetBool(_isWalkingHash, true);
            
            // // Plays different audio depending on player's stage.
            // if (!hasLegs)
            // {
            //     playerAudio.PlayHoverSfx();
            // }

        }
        // ...Stops walking animation if there's no movement input being pressed.
        else if ((!_isMovementPressed || !_isGrounded) && isWalking)
        {
            _animator.SetBool(_isWalkingHash, false);
        }

        // Only play Jump animation if Player "gets legs" upgrade.
        if (hasLegs)
        {
            // Running animation if Run button is pressed but player wasn't running prior.
            if ((_isMovementPressed && _isRunPressed && _isGrounded) && !isRunning && hasArms)
            {
                _animator.SetBool(_isRunningHash, true);
            }
            // Stops running animation if either movement or run button were let go while "running" prior.
            else if ((!_isMovementPressed || !_isRunPressed || !_isGrounded) && isRunning && hasArms)
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
        float fallMultiplier = 2.0f;
        
        // Creating reasonable gravity whenever player is in the air or on the ground.
        if (_isGrounded)
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
            _appliedMovement.y = Mathf.Max((prevYVelocity + _currentMovement.y) * 0.5f, -40.0f);
            
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
            if (_isJumpPressed && !_isJumping && _isGrounded)
            {
                // Changes animation to be jumping.
                _animator.SetBool(_isJumpingHash, true);
                _isJumpAnimating = true;
                _isJumping = true;
                _exitingSlope = true;
                
                // Reset Y Velocity
                // _rigidbody.velocity = new Vector3(_rigidbody.maxAngularVelocity, 0.0f, _rigidbody.velocity.z);
                
                // Performs jump
                _rigidbody.AddForce(transform.up * jumpForce, ForceMode.Impulse);
                
                playerAudio.PlayJumpSfx();
                
                // // New Velocity Verlet Implementation 
                // float prevYVelocity = currentMovement.y;
                // float newYVelocity = currentMovement.y + initialJumpVelocity;
                // float nextYVelocity = (prevYVelocity + newYVelocity) * 0.5f;
                //
                // currentMovement.y = nextYVelocity;
                // currentRunMovement.y = nextYVelocity;
                
                // Old Inconsistent Method
                // _currentMovement.y = _initialJumpVelocity;
                // _appliedMovement.y = _initialJumpVelocity;

            }
            else if (!_isJumpPressed && _isJumping && !_isGrounded)
            {
                // _animator.SetBool(_isJumpingHash, false);
                // _isJumpAnimating = false;
                _isJumping = false;
                _exitingSlope = false;
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

            if (OnSlope() && !_exitingSlope)
            {
                _rigidbody.AddForce(GetSlopeMoveDirection() * ((speed * runMultiplier) * 100.0f), ForceMode.Force);
            }

            if (_isGrounded)
            {
                _rigidbody.AddForce(_appliedMovement.normalized * ((speed * runMultiplier) * 100.0f), ForceMode.Force);
            } 
            else if (!_isGrounded)
            {
                _rigidbody.AddForce(_appliedMovement.normalized * ((speed * runMultiplier) * 100.0f * airMultiplier), ForceMode.Force);
                
            }

            // Turns off gravity when on slopes to prevent sliding.
            _rigidbody.useGravity = !OnSlope();
        }
        else
        {
            // characterController.Move(currentMovement * (Time.deltaTime * speed));
            _appliedMovement.x = _currentMovement.x;
            _appliedMovement.z = _currentMovement.z;
            
            _appliedMovement = orientation.forward * _currentMovement.z + orientation.right * _currentMovement.x;

            if (OnSlope() && !_exitingSlope)
            {
                _rigidbody.AddForce(GetSlopeMoveDirection() * (speed * 100.0f), ForceMode.Force);

                if (_rigidbody.velocity.y > 0)
                    _rigidbody.AddForce(Vector3.down * 100.0f, ForceMode.Force);
            }
            
            if (_isGrounded)
            {
                _rigidbody.AddForce(_appliedMovement.normalized * (speed * 100.0f), ForceMode.Force);
            } 
            else if (!_isGrounded)
            {
                _rigidbody.AddForce(_appliedMovement.normalized * (speed * 100.0f * airMultiplier), ForceMode.Force);
                
            }
            
            // Turns off gravity when on slopes to prevent sliding.
            _rigidbody.useGravity = !OnSlope();
        }
    }

    void HandleSpeed()
    {
        
        // Limiting max speed on and off slopes.
        if (OnSlope())
        {
            if (_rigidbody.velocity.magnitude > maxSpeed)
            {
                _rigidbody.velocity = _rigidbody.velocity.normalized * speed;
            }
        }
        else
        {
            var velocity = _rigidbody.velocity;
            Vector3 flatVel = new Vector3(velocity.x, 0.0f, velocity.z);

            // Limiting velocity when breaching normal speed.
            if (flatVel.magnitude > maxSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * speed;
                _rigidbody.velocity = new Vector3(limitedVel.x, _rigidbody.velocity.y, limitedVel.z);
            }
        }
        

    }

    private void OnTriggerStay(Collider other)
    {
        if (_isPickupPressed)
        {
            if (other.CompareTag("PlayerHead") && !hasHead)
            {

                // PlayUIFadeAnimation
                StartCoroutine(PickupHead());
            } 
            else if (other.CompareTag("PlayerLegs") && hasHead && !hasLegs)
            {
                OnDisable();
                hasLegs = true;
                playerAudio.PlayPickup2Sfx();
                // PlayUIFadeAnimation
                StartCoroutine(PickupLegs());
            } 
            else if (other.CompareTag("PlayerArms") && hasHead && hasLegs && !hasArms)
            {
                OnDisable();
                hasArms = true;
                playerAudio.PlayPickup3Sfx();
                // PlayUIFadeAnimation
                StartCoroutine(PickupArms());
            }
            else if (other.CompareTag("PlayerFinal"))
            {
                OnDisable();
                hasArms = true;
                playerAudio.PlayPickupFinalSfx();
                // PlayUIFadeAnimation
                StartCoroutine(PickupFinal());
            }
        }
        else
        {
            return;
        }
    }

    public IEnumerator PickupHead()
    {
        OnDisable();
        hasHead = true;
        playerAudio.PlayPickup1Sfx();
        playerAudio.MuteWorldMusic();
        yield return new WaitForSeconds(5.0f);
        SceneManager.LoadScene("World1_PlayerStage2");
    }
    
    public IEnumerator PickupLegs()
    {
        OnDisable();
        hasHead = true;
        playerAudio.PlayPickup2Sfx();
        playerAudio.MuteWorldMusic();
        yield return new WaitForSeconds(5.0f);
        SceneManager.LoadScene("World1_PlayerStage3");
    }
    
    public IEnumerator PickupArms()
    {
        OnDisable();
        hasHead = true;
        playerAudio.PlayPickup3Sfx();
        playerAudio.MuteWorldMusic();
        yield return new WaitForSeconds(5.0f);
        OnEnable();
        SceneManager.LoadScene("World1_PlayerStage4");
    }
    
    public IEnumerator PickupFinal()
    {
        OnDisable();
        hasHead = true;
        playerAudio.PlayPickupFinalSfx();
        playerAudio.MuteWorldMusic();
        yield return new WaitForSeconds(7.5f);
        OnEnable();
        SceneManager.LoadScene("EndScene");
    }

    void HandlePickup()
    {
        // if(!hasHead && collides with head && _isPickupPressed)
        // {
        //     Instantiate(Stage2robot);
        //     hasHead = true;
        // } else if(!hasLegs && collides with legs && _isPickupPressed)
        // {
        //     insintsflegs verio;
        //     hasLegs = true;
        // } else if(!hasArms && collides with arms && _isPickupPressed)
        // {
        //     Instantiate(stage4);
        // }
        
    }

    private void FixedUpdate()
    {
        HandleMove();
        
    }

    // Update is called once per frame
    void Update()
    {
        HandleSpeed();
        HandleAnimation();
        HandleAudio();
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

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position,
                Vector3.down, out _slopeHit,
                playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(_appliedMovement, _slopeHit.normal);
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
