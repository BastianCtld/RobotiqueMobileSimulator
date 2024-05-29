using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ConversationActor : MonoBehaviour
{
    public ScientistMouth mouth;

    public List<AudioClip> greetings;

    public List<AudioClip> questions;

    public List<AudioClip> answers;

    public List<AudioClip> flinches;

    public NavMeshAgent agent;

    public Animator animator;

    public bool isInConversation;

    public bool isFlinching;

    // Start is called before the first frame update
    void Start()
    {
        isInConversation = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitiateConversation(ConversationActor interlocutor)
    {
        isInConversation = true;
        interlocutor.isInConversation = true;

        Debug.Log("Conversation started.");

        agent.SetDestination(Vector3.Lerp(transform.position, interlocutor.agent.transform.position, 0.3f));

        interlocutor.agent.SetDestination(Vector3.Lerp(transform.position, interlocutor.agent.transform.position, 0.7f));

        StartCoroutine(AskQuestion(interlocutor));
        
    }

    public IEnumerator AskQuestion(ConversationActor interlocutor)
    {
        yield return new WaitForSeconds(Random.Range(0f, 2f));
        mouth.Say(questions[(int)(Random.value * questions.Count)]);

        animator.SetBool("talking", true);
        while (mouth.source.isPlaying)
        {
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("I have asked my question. It's your turn.");
        animator.SetBool("talking", false);

        StartCoroutine(interlocutor.AnswerQuestion(this));
    }

    public IEnumerator AnswerQuestion(ConversationActor interlocutor)
    {
        Debug.Log("I will answer.");
        yield return new WaitForSeconds(0.5f);

        mouth.Say(answers[(int)(Random.value * answers.Count)]);

        animator.SetBool("talking", true);
        while (mouth.source.isPlaying)
        {
            yield return new WaitForSeconds(0.1f);
        }
        animator.SetBool("talking", false);

        Debug.Log("I have answered.");

        if (Random.value > 0.5f)
        {
            StartCoroutine(AskQuestion(interlocutor));
        }
        else
        {
            EndConversation(interlocutor);
        }
    }

    public void EndConversation(ConversationActor interlocutor)
    {
        isInConversation = false;
        interlocutor.isInConversation = false;
        Debug.Log("End of conversation.");
    }

    public IEnumerator Greet()
    {
        yield return new WaitForSeconds(Random.Range(0f, 1f));
        mouth.Say(greetings[(int)(Random.value * greetings.Count)]);
    }

    public IEnumerator Flinch()
    {
        isFlinching = true;
        mouth.Say(flinches[(int)(Random.value * flinches.Count)]);
        yield return new WaitForSeconds(5f);
        isFlinching = false;
    }
}
