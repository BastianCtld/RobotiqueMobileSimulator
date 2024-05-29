using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ScientistController : MonoBehaviour
{

    public NavMeshAgent agent;
    public Animator animator;
    public ScientistMouth mouth;

    public float animationRotationLayerWeightGain;

    float lastAngle;
    float lastAngularSpeed;

    public ConversationActor conversation;

    // Start is called before the first frame update
    void Start()
    {
        lastAngle = transform.rotation.eulerAngles.y;
        StartCoroutine(BehaviorCoroutine());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Scientist" && !other.isTrigger)
        {
            Debug.Log("You are a scientist ! " + other.name);
            if(Random.value > 0.95f)
            {
                ConversationActor interlocutor = other.GetComponent<ConversationActor>();
                if(!interlocutor.isInConversation && !conversation.isInConversation) {
                    Debug.Log("Dear "+other.name+", you are not in a convo, let's talk. From "+gameObject.name);
                    conversation.InitiateConversation(interlocutor);
                }
            }
        }

        if (other.tag == "Scientist" || other.tag == "Robot")
        {
            if(Random.value > 0.99f)
            {
                StartCoroutine(conversation.Greet());
            }
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if(!conversation.isFlinching)
        {
            animator.SetTrigger("flinch");
            StartCoroutine(conversation.Flinch());
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float angularSpeed = Mathf.Lerp(lastAngularSpeed, transform.rotation.eulerAngles.y - lastAngle, 0.1f);
        animator.SetFloat("movementSpeed", agent.velocity.magnitude);
        animator.SetFloat("angularSpeed", angularSpeed*animationRotationLayerWeightGain);
        animator.SetLayerWeight(1, Mathf.Min(Mathf.Abs(angularSpeed * animationRotationLayerWeightGain), 1f));
        lastAngle = transform.rotation.eulerAngles.y;
        lastAngularSpeed = angularSpeed;
    }

    IEnumerator BehaviorCoroutine()
    {
        while(true)
        {
            yield return new WaitForSeconds(Random.Range(3, 10));

            while(conversation.isInConversation)
            {
                yield return new WaitForSeconds(2f);
            }

            print("I'm going ");
            NavMeshHit hit;
            NavMesh.SamplePosition(new Vector3(Random.Range(-4, 30f), 0f, Random.Range(1, 45f)), out hit, 100, 1);
            agent.SetDestination(hit.position);

            while (agent.remainingDistance > 1f)
            {
                yield return new WaitForSeconds(2f);
            }
        }
    }
}
