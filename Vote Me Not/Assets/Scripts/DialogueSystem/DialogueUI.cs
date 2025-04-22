/* Authored by: Samuel Cezar
Company: Company Name
Project: Project Name
Feature: [NXR-006] Dialogue System Feature
Description: certain codes are being called here, it is because they are part of the Dialogue System Feature
 */


using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialogueBox;                // Container for the dialogue UI
    public TextMeshProUGUI dialogueText;          // TMP component for dialogue text
    public TextMeshProUGUI speakerNameText;       // TMP component for the speaker’s name

    [Header("Typewriter Settings")]
    public float typewriterSpeed = 0.05f;

    [Header("Dialogue Pop-out Settings")]
    public float dialoguePopDelay = 0.5f;
    public float linePauseDelay = 1f;

    [Header("Auto Progress Settings")]
    // When true, dialogue will auto-progress after linePauseDelay.
    public bool autoProgress = true;

    [Header("Audio")]
    public AudioSource voiceSource;

    private Queue<DialogueContentSO.DialogueLine> dialogueLines;
    private bool isTyping = false;

    public Action onDialogueEnd;

    private bool forceComplete = false;
    private Coroutine currentDialogueCoroutine;

    void Update()
    {
        // Handle tap to skip typing effect
        if (isTyping && (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)))
        {
            forceComplete = true;
        }
    }


    public void SetSpeakerName(string name)
    {
        if (speakerNameText != null)
        {
            speakerNameText.text = name;
            Debug.Log("Speaker Name set to: " + name);
        }
        else
        {
            Debug.LogWarning("SpeakerNameText is not assigned in DialogueUI.");
        }
    }

    public void StartDialogue(DialogueContentSO dialogueContent)
    {
        // Stop any existing dialogue first
        if (currentDialogueCoroutine != null)
        {
            StopCoroutine(currentDialogueCoroutine);
            ClearDialogue();
        }

        if(dialogueContent == null || dialogueContent.dialogueLines.Count == 0)
        {
            Debug.LogWarning("Dialogue content is null or empty.");
            return;
        }
        
        dialogueBox.SetActive(true);
        dialogueLines = new Queue<DialogueContentSO.DialogueLine>(dialogueContent.dialogueLines);
        currentDialogueCoroutine = StartCoroutine(PlayDialogue());
    }

    IEnumerator PlayDialogue()
    {
        while (dialogueLines.Count > 0)
        {
            var line = dialogueLines.Dequeue();
            yield return StartCoroutine(TypeText(line));

            // Play voice clip if available.
            if (line.voiceClip != null && voiceSource != null)
            {
                voiceSource.clip = line.voiceClip;
                voiceSource.Play();
                yield return new WaitForSeconds(line.voiceClip.length);
            }
            yield return new WaitForSeconds(linePauseDelay);

            if(autoProgress)
            {
                // Automatically progress after a brief delay.
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                // Wait until player input (mouse click or touch) to continue.
                yield return new WaitUntil(() => Input.GetMouseButtonDown(0) || Input.touchCount > 0);
            }
        }
        EndDialogue();
    }

    IEnumerator TypeText(DialogueContentSO.DialogueLine line)
    {
        isTyping = true;
        forceComplete = false;
        dialogueText.text = "";
        
        string processedText = line.text;
        if (!string.IsNullOrEmpty(line.textHighlight))
        {
            processedText = processedText.Replace(
                line.textHighlight,
                $"<color=#{ColorUtility.ToHtmlStringRGB(line.highlightColor)}>{line.textHighlight}</color>"
            );
        }

        dialogueText.text = processedText;
        dialogueText.maxVisibleCharacters = 0;
        int totalCharacters = processedText.Length;

        for (int i = 0; i < totalCharacters; i++)
        {
            if (forceComplete)
            {
                dialogueText.maxVisibleCharacters = totalCharacters;
                break;
            }
            
            dialogueText.maxVisibleCharacters = i + 1;
            yield return new WaitForSeconds(typewriterSpeed);
        }

        isTyping = false;
        forceComplete = false;
    }

    public void ClearDialogue()
    {
        if (currentDialogueCoroutine != null)
        {
            StopCoroutine(currentDialogueCoroutine);
            currentDialogueCoroutine = null;
        }
        
        dialogueText.text = "";
        dialogueText.maxVisibleCharacters = int.MaxValue;
        dialogueBox.SetActive(false);
        isTyping = false;
        forceComplete = false;
        
        // Clear any remaining lines
        if (dialogueLines != null)
            dialogueLines.Clear();

        onDialogueEnd?.Invoke();
        onDialogueEnd = null;
    }

    void EndDialogue()
    {
        ClearDialogue();
        Debug.Log("Dialogue ended.");
    }
    
}
