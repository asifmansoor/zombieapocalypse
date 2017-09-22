using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PlayerMoveStatus { NotMoving, Crouching, Walking, Running, NotGrounded, Landing }
public enum CurveControlledBobCallbackType { Horizontal, Vertical }

// Delegates
public delegate void CurveControlledBobCallback();

/*
 * Class : CurveControlledBobEvent
 * Description : This class is to store the information for a curve control event used for the head bob functionality for the camera
 */
[System.Serializable]
public class CurveControlledBobEvent
{
    public float Time = 0.0f;
    public CurveControlledBobCallback Function = null;
    public CurveControlledBobCallbackType Type = CurveControlledBobCallbackType.Vertical;
}

/*
 * Class : CurveControlledBob
 * Description : This class handles the head bob functionality for the camera
 */
[System.Serializable]
public class CurveControlledBob 
{
    [SerializeField] AnimationCurve _bobcurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f),
                                                                    new Keyframe(1f, 0f), new Keyframe(1.5f, -1f),
                                                                    new Keyframe(2f, 0f));

    // Inspector assigned bob control variables
    [SerializeField] float _horizontalMultiplier = 0.01f;
    [SerializeField] float _verticalMultiplier = 0.02f;
    [SerializeField] float _verticalToHorizontalSpeedRatio = 2.0f;
    [SerializeField] private float _baseInterval = 1.0f;

    // Private
    private float _prevXPlayHead;
    private float _prevYPlayHead;
    private float _xPlayHead;
    private float _yPlayHead;
    private float _curveEndTime;
    private List<CurveControlledBobEvent> _events = new List<CurveControlledBobEvent>();

    public void Initialize()
    {
        _curveEndTime = _bobcurve[_bobcurve.length - 1].time;
        _xPlayHead = 0.0f;
        _yPlayHead = 0.0f;
        _prevXPlayHead = 0.0f;
        _prevYPlayHead = 0.0f;
    }

    /*
     * Method : RegisterEventCallback
     * Description : This method is to register a callback function for specific animation curve events
     */
    public void RegisterEventCallback (float time, CurveControlledBobCallback function, CurveControlledBobCallbackType type)
    {
        CurveControlledBobEvent ccbeEvent = new CurveControlledBobEvent();
        ccbeEvent.Time = time;
        ccbeEvent.Function = function;
        ccbeEvent.Type = type;
        _events.Add(ccbeEvent);
        _events.Sort(delegate (CurveControlledBobEvent t1, CurveControlledBobEvent t2)
        {
            return (t1.Time.CompareTo(t2.Time));
        });
    }

    /*
     * Method : GetVectorOffset
     * Description : 
     */
    public Vector3 GetVectorOffset(float speed)
    {
        _xPlayHead += (speed * Time.deltaTime)/_baseInterval;
        _yPlayHead += ((speed * Time.deltaTime) / _baseInterval) * _verticalToHorizontalSpeedRatio;

        if (_xPlayHead > _curveEndTime)
            _xPlayHead -= _curveEndTime;

        if (_yPlayHead > _curveEndTime)
            _yPlayHead -= _curveEndTime;

        for (int i = 0; i < _events.Count; i++)
        {
            CurveControlledBobEvent ev = _events[i];
            if (ev != null)
            {
                if (ev.Type == CurveControlledBobCallbackType.Vertical)
                {
                    if ((_prevYPlayHead < ev.Time && _yPlayHead >= ev.Time) || (_prevYPlayHead > _yPlayHead && (ev.Time > _prevYPlayHead || ev.Time <= _yPlayHead)))
                    {
                        // Call the registered function
                        ev.Function();
                    }
                }
                else
                {
                    if ((_prevXPlayHead < ev.Time && _xPlayHead >= ev.Time) || (_prevXPlayHead > _xPlayHead && (ev.Time > _prevXPlayHead || ev.Time <= _xPlayHead)))
                    {
                        // Call the registered function
                        ev.Function();
                    }
                }
            }
        }

        float xPos = _bobcurve.Evaluate(_xPlayHead) * _horizontalMultiplier;
        float yPos = _bobcurve.Evaluate(_yPlayHead) * _verticalMultiplier;

        _prevXPlayHead = _xPlayHead;
        _prevYPlayHead = _yPlayHead;

        return new Vector3(xPos, yPos, 0f);
    }
}

/*
 * Class : FPSController
 * Description : This class is to handle the FPS Controller of the game
 */
[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    public List<AudioSource> AudioSources = new List<AudioSource>();
    private int _audioToUse = 0;

    // Inspector assigned variables
    [SerializeField] private float _walkSpeed = 2.0f;
    [SerializeField] private float _runSpeed = 4.5f;
    [SerializeField] private float _jumpSpeed = 7.5f;
    [SerializeField] private float _crouchSpeed = 1.0f;
    [SerializeField] private float _stickToGroundForce = 5.0f;
    [SerializeField] private float _gravityMultiplier = 2.5f;
    [SerializeField] private float _runStepLengthen = 0.75f;

    [SerializeField] private CurveControlledBob _headBob = new CurveControlledBob();

    [SerializeField] private UnityStandardAssets.Characters.FirstPerson.MouseLook _mouseLook;

    [SerializeField] private GameObject _flashLight = null;
    [SerializeField] private Transform _spawnTransform = null;
    [SerializeField] private bool _useSpawnTransform = true;


    // Private
    private Camera _camera = null;
    private bool _jumpButtonPressed = false;
    private Vector2 _inputVector = Vector2.zero;
    private Vector3 _moveDirection = Vector3.zero;
    private bool _previouslyGrounded = false;
    private bool _isWalking = true;
    private bool _isJumping = false;
    private bool _isCrouching = false;
    private Vector3 _localSpaceCameraPos = Vector3.zero;
    private float _controllerHeight = 0.0f;

    // Timers
    private float _fallingTimer = 0.0f;

    private CharacterController _characterController = null;
    private PlayerMoveStatus _movementStatus = PlayerMoveStatus.NotMoving;

    // Public properties
    public PlayerMoveStatus movementStatus { get { return _movementStatus; } }
    public float walkSpeed { get { return _walkSpeed; } }
    public float runSpeed { get { return _runSpeed; } }

    public Transform SpawnTransform { get { return _spawnTransform; } }

    protected void Start()
    {
        if (_spawnTransform != null && _useSpawnTransform)
        {
            transform.position = _spawnTransform.position;
            transform.rotation = _spawnTransform.rotation;
        }
        _characterController = GetComponent<CharacterController>();
        _controllerHeight = _characterController.height;

        _camera = Camera.main;
        _localSpaceCameraPos = _camera.transform.localPosition;

        _movementStatus = PlayerMoveStatus.NotMoving;
        _fallingTimer = 0.0f;
        _mouseLook.Init(transform, _camera.transform);

        _headBob.Initialize();

        // Register the method to play footstep sound on the vertical events on the head bob animation curve
        _headBob.RegisterEventCallback(1.5f, PlayFootStepSound, CurveControlledBobCallbackType.Vertical);

        if (_flashLight) _flashLight.SetActive(false);
    }

    protected void Update()
    {
        if (_characterController.isGrounded) _fallingTimer = 0.0f;
        else _fallingTimer += Time.deltaTime;

        if (Time.timeScale > Mathf.Epsilon)
            _mouseLook.LookRotation(transform, _camera.transform);

        if (Input.GetButtonDown("FlashLight"))
        {
            // Toggle the flashllight
            if (_flashLight)
                _flashLight.SetActive(!_flashLight.activeSelf);
        }

        if (!_jumpButtonPressed && !_isCrouching)
            _jumpButtonPressed = Input.GetButtonDown("Jump");

        if (Input.GetButtonDown("Crouch"))
        {
            _isCrouching = !_isCrouching;

            _characterController.height = _isCrouching == true ? _controllerHeight / 2.0f : _controllerHeight;
        }

        if (!_previouslyGrounded && _characterController.isGrounded)
        {
            // The player just landed after a jump

            if (_fallingTimer > 0.5f)
            {
                // TODO: Play landing sound
            }

            _moveDirection.y = 0.0f;
            _isJumping = false;
            _movementStatus = PlayerMoveStatus.Landing;
        }
        else if (!_characterController.isGrounded)
        {
            _movementStatus = PlayerMoveStatus.NotGrounded;
        }
        else if (_characterController.velocity.sqrMagnitude < 0.01f)
        {
            _movementStatus = PlayerMoveStatus.NotMoving;
        }
        else if (_isCrouching)
            _movementStatus = PlayerMoveStatus.Crouching;
        else if (_isWalking)
            _movementStatus = PlayerMoveStatus.Walking;
        else
            _movementStatus = PlayerMoveStatus.Running;

        _previouslyGrounded = _characterController.isGrounded;
    }

    protected void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool wasWalking = _isWalking;
        _isWalking = !Input.GetKey(KeyCode.LeftShift);

        // Decide the player speed depending on whether the player is crouching, walking or running
        float speed = _isCrouching ? _crouchSpeed : _isWalking ? _walkSpeed : _runSpeed;
        _inputVector = new Vector2(horizontal, vertical);

        if (_inputVector.sqrMagnitude > 1)
            _inputVector.Normalize();

        // we only intend to move in the z and x direction and not in the y direction 
        Vector3 desiredMove = transform.forward * _inputVector.y + transform.right * _inputVector.x;

        // Check if the floor below if it is not flat then we need to move a little bit in the y direction too 
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, _characterController.radius, Vector3.down, out hitInfo, _characterController.height / 2, 1))
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

        _moveDirection.x = desiredMove.x * speed;
        _moveDirection.z = desiredMove.z * speed;

        if (_characterController.isGrounded)
        {
            // add a force to make the player stick to the ground if he is not jumping
            _moveDirection.y = -_stickToGroundForce;

            if (_jumpButtonPressed)
            {
                _moveDirection.y = _jumpSpeed;
                _jumpButtonPressed = false;
                _isJumping = true;
                // TODO: Play jumping sound
            }
        }
        else
        {
            // make the player jump and apply gravity to bring him down
            _moveDirection += Physics.gravity * _gravityMultiplier * Time.fixedDeltaTime;
        }

        _characterController.Move(_moveDirection * Time.fixedDeltaTime);

        // Apply the head bob to the camera. The bobbing would be relative to player speed
        Vector3 speedXZ = new Vector3(_characterController.velocity.x, 0.0f, _characterController.velocity.z);
        if (speedXZ.magnitude > 0.01f)
            _camera.transform.localPosition = _localSpaceCameraPos += _headBob.GetVectorOffset(speedXZ.magnitude * (_isCrouching || _isWalking ? 1.0f : _runStepLengthen));
        else
            _camera.transform.localPosition = _localSpaceCameraPos;
    }

    /*
     * Method : PlayFootStepSound
     * Description : This method is to play footstep sound when the animation curve tells it to. It picks from 1 of the 2 sounds to play randomly
     */
    void PlayFootStepSound()
    {
        if (_isCrouching)
            return;

        AudioSources[_audioToUse].Play();
        _audioToUse = (_audioToUse == 0 ? 1 : 0);
    }
}
