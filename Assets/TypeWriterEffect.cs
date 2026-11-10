using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TypeWriterEffect : MonoBehaviour
{
    public REDESCcutscenemrg interactionEventHandler;
    public TextMeshProUGUI textComponent;
    [TextArea]
    public string fullText = "This is typing text...";
    public float typingSpeed = 0.05f;
    public float startDelay = 1f;
    public float EventDelay = 1f;
    public int evntID;
    public AudioSource typingAudioSource;
    public AudioClip typingSound;

    public bool startTyping = false; // Trigger to begin typing
    public bool DoEvent = false; // Trigger to begin typing
    public GameObject objectToActivate; // Object to activate after delay

    private bool hasStarted = false;

    private void Start()
    {

    }
    void Update()
    {
        if (startTyping && !hasStarted)
        {
            typingAudioSource.PlayOneShot(typingSound);
            hasStarted = true;
            StartCoroutine(TypeText());
            if(DoEvent)
            {
                Invoke("ExecuteEventTyper", EventDelay);
            }
        }
    }

    IEnumerator TypeText()
    {
        
        yield return new WaitForSeconds(startDelay);
        textComponent.text = "";
        if (objectToActivate != null)
            objectToActivate.SetActive(true); // Activate object after delay

        foreach (char letter in fullText)
        {
            textComponent.text += letter;


            yield return new WaitForSeconds(typingSpeed);
        }
    }
    void ExecuteEventTyper()
    {
        interactionEventHandler.GlobalUexFunc(evntID);
    }
}