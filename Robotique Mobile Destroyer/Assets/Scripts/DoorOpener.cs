using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorOpener : MonoBehaviour
{
    public Transform door;

    Vector3 originalPosition;

    public Vector3 openedOffset;

    public float openedRatio;

    public float openingSpeed;

    Vector3 openedPosition;

    public int peopleInVincinity;

    bool shouldOpen;

    bool isMoving;

    public AudioSource source;

    public AudioClip movementSound;
    public AudioClip stoppingSound;

    // Start is called before the first frame update
    void Start()
    {
        originalPosition = door.position;
        openedPosition = originalPosition + door.TransformVector(openedOffset);
        isMoving = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(door.position, door.position - door.TransformVector(openedOffset));
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!other.isTrigger && ( other.tag == "Scientist"|| other.tag == "Robot" ))
        {
            peopleInVincinity++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.isTrigger && (other.tag == "Scientist" || other.tag == "Robot"))
        {
            StartCoroutine(decreasePeopleAfterSeconds(5f));
        }
        IEnumerator decreasePeopleAfterSeconds(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            peopleInVincinity--;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(peopleInVincinity > 0 && shouldOpen == false)
        {
            shouldOpen = true;
            PlayMovementSound();
        }
        if(peopleInVincinity == 0 && shouldOpen == true)
        {
            shouldOpen = false;
            PlayMovementSound();
        }

        if(shouldOpen)
        {
            if(openedRatio < 1f)
            {
                isMoving = true;
                openedRatio += + Time.fixedDeltaTime * openingSpeed;
            } else
            {
                openedRatio = 1f;
                if(isMoving)
                {
                    PlayEndOfMovementSound();
                    isMoving = false;
                }
            }
        }
        else
        {
            if (openedRatio > 0f)
            {
                isMoving = true;
                openedRatio -= Time.fixedDeltaTime * openingSpeed;
            }
            else
            {
                openedRatio = 0f;
                if (isMoving)
                {
                    PlayEndOfMovementSound();
                    isMoving = false;
                }
            }
        }

        door.position = Vector3.Lerp(originalPosition, openedPosition, openedRatio);
    }

    void PlayMovementSound()
    {
        source.Stop();
        source.clip = movementSound;
        source.Play();
    }

    void PlayEndOfMovementSound()
    {
        source.Stop();
        source.clip = stoppingSound;
        source.pitch = Random.Range(0.95f, 1.05f);
        source.Play();
    }
}