using UnityEngine;
using System.Collections;

/*
 * Class : AIDamageTrigger
 * Description : This class is to manage the blood particle effects being played when the NPC attacks the player
 */
public class AIDamageTrigger : MonoBehaviour
{
    // Inspector assigned variables
    [SerializeField] private string _parameter = "";
    [SerializeField] private int _bloodParticlesBurstAmount = 10;
    [SerializeField] private float _damageAmount = 0.1f;
    [SerializeField] private AudioSource _damageSound = null;

    // Private
    private AIStateMachine _stateMachine = null;
    private Animator _animator = null;
    private int _parameterHash = -1;
    private GameSceneManager _gameSceneManager = null;

    void Start()
    {
        //Debug.Log("Damage TRigger Start!!!!!");
        _stateMachine = transform.root.GetComponentInChildren<AIStateMachine>();
        if (_stateMachine != null)
            _animator = _stateMachine.animator;

        // Get the hash for the animation parameter to check if the correct NPC body part is hitting the player
        _parameterHash = Animator.StringToHash(_parameter);

        _gameSceneManager = GameSceneManager.instance;
    }

    void OnTriggerStay(Collider col)
    {
        //Debug.Log("Register damage!!!!!");
        if (!_animator)
            return;

        //Debug.Log("Register damage : Animator present!!!!!");
        //Debug.Log(_parameter);
        //Debug.Log(_animator.GetFloat(_parameterHash));
        //Debug.Log(col.gameObject.tag);

        /* Each attack animation has animation curves defined which set the parameters "Mouth", "LeftHand" and "RightHand" according to the part hitting in the animation.
           This script is attached to the relavent body part and it defines the body part which we check if matching with the parameter received to only attack as per the
           body part*/ 
        if (col.gameObject.CompareTag("Player") && _animator.GetFloat(_parameterHash) > 0.9f)
        {
            Debug.Log("Register damage : attack mode on!!!!!");
            if (GameSceneManager.instance && GameSceneManager.instance.bloodParticles)
            {
                ParticleSystem system = GameSceneManager.instance.bloodParticles;

                system.transform.position = transform.position;
                system.transform.rotation = Camera.main.transform.rotation;
                ParticleSystem.MainModule main = system.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                //system.simulationSpace = ParticleSystemSimulationSpace.World;
                system.Emit(_bloodParticlesBurstAmount);
            }

            if (_gameSceneManager != null)
            {
                PlayerInfo info = _gameSceneManager.GetPlayerInfo(col.GetInstanceID());
                if (info != null && info.characterManager != null)
                {
                    // If the collider is the player then process player damage
                    info.characterManager.TakeDamage(_damageAmount);
                    if (_damageSound != null && !_damageSound.isPlaying)
                    {
                        _damageSound.Play();
                    }
                }
            }
        }
    }
}
