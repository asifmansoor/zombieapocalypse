using UnityEngine;
using System.Collections;
using System.Security.AccessControl;
using UnityEngine.UI;

/*
 * Class : CharacterManager
 * Description : This class is for handling player attributes
 */
public class CharacterManager : MonoBehaviour
{
    // Inspector assigned variables
    [SerializeField] private CapsuleCollider _meleeTrigger = null;
    [SerializeField] private CameraBloodEffect _cameraBloodEffect = null;
    [SerializeField] private Camera _camera = null;
    [SerializeField] private float _health = 100.0f;
    [SerializeField] private Transform _mainTransform = null;
    [SerializeField] private float _deathSpeed = 5.0f;
    [SerializeField] private Slider _healthSlider = null;
    [SerializeField] private AudioSource _gunShot = null;
    [SerializeField] private AudioSource _gunReload = null;

    // Private
    private Collider _collider = null;
    private FPSController _fpsController = null;
    private CharacterController _characterController = null;
    private GameSceneManager _gameSceneManager = null;
    private int _aiBodyPartLayer = -1;
    private float _volume = 0.0f;
    private bool _gunShotInProgress = false;

	// Use this for initialization
	void Start ()
	{
	    _collider = GetComponent<Collider>();
	    _fpsController = GetComponent<FPSController>();
	    _characterController = GetComponent<CharacterController>();
        _gameSceneManager = GameSceneManager.instance;
	    _aiBodyPartLayer = LayerMask.NameToLayer("AI Body Part");
	    _gunShotInProgress = false;

	    if (_gameSceneManager != null)
	    {
            // Create and fill a player info object to be stored in the Dictionary will all player colliders
	        PlayerInfo info = new PlayerInfo();
	        info.camera = _camera;
	        info.characterManager = this;
	        info.collider = _collider;
	        info.meleeTrigger = _meleeTrigger;
            
            // Add the player information to the dictionary with its collider as the key
            _gameSceneManager.RegisterPlayerInfos(_collider.GetInstanceID(), info);
	    }
	}

    /*
     * Method : Respawn
     * Description : This method respawns the player once it's health is depleted to 0. It reduces the volume so that all sounds are killed then
     *               repositions the player at the spawn position and rotation and then calls the Coroutine to restore the volume to its original level
     */
    private void Respawn()
    {
        _volume = AudioListener.volume;
        AudioListener.volume = 0;
        /*if (_gameSceneManager.soundManager != null)
            _gameSceneManager.soundManager.MuteZombieSoundsOnPlayerDeath();*/
        _mainTransform.position = _fpsController.SpawnTransform.position;
        _mainTransform.rotation = _fpsController.SpawnTransform.rotation;

        // Restore health value
        _health = 100.0f;

        // Update heath slider HUD
        _healthSlider.value = _health;
        StartCoroutine(RestoreVolume());
    }

    /*
     * Coroutine : RestoreVolume
     * Description : This coroutine is to restore the volume after a wait time
     */
    IEnumerator RestoreVolume()
    {
        // wait for 2 seconds before restoring the volume
        yield return new WaitForSeconds(2);
        AudioListener.volume = _volume;
    }

    /*
     * Method : PlayDeath
     * Description : This method is to simulate the player fall down upon its death
     */
    private void PlayDeath()
    {
        Vector3 finalPos = _mainTransform.position;
        finalPos.y = 0.0f;
        Vector3.Lerp(_mainTransform.position, finalPos, Time.deltaTime * _deathSpeed);
    }

    /*
     * Method : TakeDamage
     * Description : This method is to register player damage, play the camera blood effect and initiate respawn if the player's
     *               health goes below 0
     */
    public void TakeDamage(float amount)
    {
        Debug.Log("Taking damage!!!!!!");

        // Ensure that the player's health does not go below 0
        _health = Mathf.Max(_health - (amount * Time.deltaTime), 0.0f);

        // Update the health slider HUD property 
        _healthSlider.value = _health;
        if (_health < 0.1f)
        {
            // If health is approximately 0 the play death and respawn
            PlayDeath();
            Respawn();
        }

        if (_cameraBloodEffect != null)
        {
            /* setup the minimum blood to be displayed on camera after the player is injured. This is calculated by scaling the health value to
               amount value between 0 and 1*/
            _cameraBloodEffect.minBloodAmount = 1.0f - (_health / 100.0f);

            // Play the blood effect by temporarily increasing it by 30% and then reducing it back to the min amout set above
            _cameraBloodEffect.bloodAmount = Mathf.Min(_cameraBloodEffect.minBloodAmount + 0.3f, 1.0f);
        }
    }

    /*
     * Method : DoDamage
     * Description : This method is to simulate gun shot and then calculating if the NPC is damaged or not
     */
    public void DoDamage(int hitDirection = 0)
    {
        if (_camera == null) return;
        if (_gameSceneManager == null) return;

        // Set the flag true so that another shot cannot be fired till this one is complete
        _gunShotInProgress = true;

        Ray ray;
        RaycastHit hit;
        bool isSomethingHit = false;

        // cast a ray from the middle of the screen as that is where the crosshair is to find if some zombie is on target to be hit
        ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        isSomethingHit = Physics.Raycast(ray, out hit, 1000.0f, 1 << _aiBodyPartLayer); // the last parameter is the layer mask instead of the id

        if (_gunShot != null)
        {
            // Play the gun shot sound irrespective of whether something was hit or not
            _gunShot.Play();
            StartCoroutine(PlayGunReloadSound());
        }

        if (isSomethingHit)
        {
            // If we did hit an object then assuming it was a zombie NPC try to get the state machine from the disctionary
            AIStateMachine stateMachine = _gameSceneManager.GetAIStateMachine(hit.rigidbody.GetInstanceID());
            if (stateMachine != null)
            {
                // If we did find a state machine then it was indeed an NPC. Instruct its state machine to process the damage as per the direction and force
                stateMachine.TakeDamage(hit.point, ray.direction * 35.0f, 25, hit.rigidbody, this, hitDirection);
            }
        }
    }

    /*
     * Coroutine : PlayGunReloadSound
     * Description : This coroutine is to play the gun reload sound after a  wait once the gun shot sound is played and 
     *               then add another wait before the gun shot sound can be played again
     */
    IEnumerator PlayGunReloadSound()
    {
        // wait for gun shot sound to complete
        yield return new WaitForSeconds(_gunShot.clip.length);
        if (_gunReload != null && !_gunReload.isPlaying)
            _gunReload.Play();  // Play the reload sound

        // Wait for the reload sound to complete
        yield return new WaitForSeconds(_gunReload.clip.length); 

        // set the flag false so that the next gun shot button press could be acknowledged
        _gunShotInProgress = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !_gunShotInProgress)
        {
            // Process damage to NPC if the fire button was pressed and no other fire event is under process
            DoDamage();
        }
    }
}
