using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Class : AIDoorControl
 * Description : This class is to handle the door opening and closing animations
 */
public class AIDoorControl : MonoBehaviour
{
    public float doorOpenAngle = 90.0f;
    public float doorCloseAngle = 0.0f;
    public float doorAnimSpeed = 2.0f;
    public bool doorStatus = false; //false is close, true is open
    public AudioSource _doorOpenSound = null;


    private Quaternion doorOpen = Quaternion.identity;
    private Quaternion doorClose = Quaternion.identity;
    private bool doorGo = false; //for Coroutine, when start only one
    private bool _inProximity = false;

    void Start()
    {
        doorStatus = false; //door is open, maybe change
                            //Initialization your quaternions
        doorOpen = Quaternion.Euler(0, doorOpenAngle, 0);
        doorClose = Quaternion.Euler(0, doorCloseAngle, 0);
    }


    void OnTriggerEnter(Collider col)
    {
        if (GameSceneManager.instance == null) return;
        PlayerInfo info = GameSceneManager.instance.GetPlayerInfo(col.GetInstanceID());
        if (info != null)
        {
            // If the collider was a player object then set the proximity flag
            _inProximity = true;
        }
        /*if (col.gameObject.CompareTag("Player"))
        {
            _inProximity = true;
        }*/
    }

    void OnTriggerExit(Collider col)
    {
        if (GameSceneManager.instance == null) return;
        PlayerInfo info = GameSceneManager.instance.GetPlayerInfo(col.GetInstanceID());
        if (info != null)
        {
            // If the collider was a player object then unset the proximity flag
            _inProximity = false;
        }
        /*if (col.gameObject.CompareTag("Player"))
        {
            _inProximity = false;
        }*/
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O) && !doorGo && _inProximity)
        {
            // If the door opening/closing button was pressed and door not already in opening or closing state and player is in proximity then we can open or close door

            if (doorStatus)
            { //close door
                if (_doorOpenSound != null && !_doorOpenSound.isPlaying)
                    _doorOpenSound.Play();
                StartCoroutine(this.moveDoor(doorClose));
            }
            else
            { //open door
                if (_doorOpenSound != null && !_doorOpenSound.isPlaying)
                    _doorOpenSound.Play();
                StartCoroutine(this.moveDoor(doorOpen));
            }
        }
    }

    /*
     * Coroutine : moveDoor
     * Description : Coroutine to rotate the door object for opening or closing
     */
    public IEnumerator moveDoor(Quaternion dest)
    {
        doorGo = true;
        //Check if close/open, if angle less 4 degree, or use another value more 0
        while (Quaternion.Angle(transform.localRotation, dest) > 4.0f)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, dest, Time.deltaTime * doorAnimSpeed);
            //UPDATE 1: add yield
            yield return null;
        }
        //Change door status
        doorStatus = !doorStatus;
        doorGo = false;
        //UPDATE 1: add yield
        yield return null;
    }
}
