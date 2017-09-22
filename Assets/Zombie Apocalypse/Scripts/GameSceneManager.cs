using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Class : PlayerInfo
 * Description : This class is to capture information for the Player object. The information is stored in a dictionary for each collider on the player object 
 * so that when a collision is detected, all information can be extracted without expensive find object calls.
 */
public class PlayerInfo
{
    public Collider collider = null;
    public CharacterManager characterManager = null;
    public Camera camera = null;
    public CapsuleCollider meleeTrigger = null;
}

/*
 * Class : GameSceneManager
 * Description : This s the main manager class to manage the various object dictionaries for efficient object instance retrieval. Each NPC and player entity 
 * stores its state machine instances and player info instances respectively for each of their colliders with their collider as the key.
 */
public class GameSceneManager : MonoBehaviour
{
    // Inspector assigned variables
    [SerializeField] private ParticleSystem _bloodParticles = null;

    // Statics
    private static GameSceneManager _instance = null;

    public static GameSceneManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = (GameSceneManager) FindObjectOfType(typeof(GameSceneManager));
                ZombieManager.instance.SetupZombies();
            }
            return _instance;
        }
    }

    // Private
    private Dictionary<int, AIStateMachine> _stateMachines = new Dictionary<int, AIStateMachine>();
    private Dictionary<int, PlayerInfo> _playerInfos = new Dictionary<int, PlayerInfo>();
    private GameObject[] _zombies = null;

    // Properties
    public ParticleSystem bloodParticles { get { return _bloodParticles; } }

    // Public Methods
    public void RegisterAIStateMachines(int key, AIStateMachine stateMachine)
    {
        if (!_stateMachines.ContainsKey(key))
        {
            _stateMachines[key] = stateMachine;
        }
    }

    public AIStateMachine GetAIStateMachine(int key)
    {
        AIStateMachine machine = null;
        if (_stateMachines.TryGetValue(key, out machine))
        {
            return machine;
        }

        return null;
    }

    public void RegisterPlayerInfos(int key, PlayerInfo playerInfo)
    {
        if (!_playerInfos.ContainsKey(key))
        {
            _playerInfos[key] = playerInfo;
        }
    }

    public PlayerInfo GetPlayerInfo(int key)
    {
        PlayerInfo info = null;
        if (_playerInfos.TryGetValue(key, out info))
        {
            return info;
        }

        return null;
    }

}
