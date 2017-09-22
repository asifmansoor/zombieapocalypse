using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Class : ZombieManager
 * Description : This class is to manage the initial creation of Zombie NPCs
 */
public class ZombieManager : MonoBehaviour
{
    // Inspector assigned variables
    //[SerializeField] private Transform[] _zombieSpawnTransforms = null;
    [SerializeField] private GameObject[] _zombiesPrefabs = null;
    [SerializeField] private GameObject _waypointNetworksObject = null;

    // Statics
    private static ZombieManager _instance = null;

    // Private
    private GameObject[] _zombies;

    public static ZombieManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = (ZombieManager)FindObjectOfType(typeof(ZombieManager));
            }
            return _instance;
        }
    }

    /*
     * Method : SetupZombies
     * Description : This method is to place random zombie characters at each waypoint network
     */
    public void SetupZombies()
    {
        if (_waypointNetworksObject == null || _zombiesPrefabs == null) return;
        AIWaypointNetwork[] waypointNetworks = _waypointNetworksObject.GetComponentsInChildren<AIWaypointNetwork>();
        if (waypointNetworks != null)
        {
            // Create zombie game objects equal to the number of waypoint networks
            _zombies = new GameObject[waypointNetworks.Length];
            Transform waypoint = null;
            AIZombieStateMachine _stateMachine = null;
            for (int i = 0; i < waypointNetworks.Length; i++)
            {
                // Pick the last waypoint in the waypoint network to place the zombie as a start so that it traverses to the starting waypoint upon game start
                waypoint = waypointNetworks[i].Waypoints[waypointNetworks[i].Waypoints.Count - 1];

                // Instantiate a Zombie object picking up randomly from the list of all zombie characters
                _zombies[i] = Instantiate(_zombiesPrefabs[Random.Range(0, _zombiesPrefabs.Length)], waypoint.position,
                    waypoint.rotation);

                // Get the zombie state machine
                _stateMachine = _zombies[i].GetComponentInChildren<AIZombieStateMachine>();

                // set the waypoint network in the zombie state machine
                _stateMachine.waypointNetwork = waypointNetworks[i];

                /* set the saisfaction value randomly so that half the times the zombie is fully satisfied i.e. 1.0f and will not start feeding at the start of the game and half 
                   of the times they will have varying level of satisfaction value and might start feeding */
                _stateMachine.satisfaction = Random.Range(0, 2) == 0 ? (Random.Range(0, 0.90f)) : 1.0f;

                /* set the lower body damage value randomly so that 30% of the time the zombie is damaged from the lower body i.e. 76 and will crawl at the start of the game and 
                   70% of the times they will have 0 lower body damage and will start normally in upright position */
                _stateMachine.lowerBodyDamage = Random.Range(0, 10) > 7 ? 76 : 0;
            }
        }

    }

}
