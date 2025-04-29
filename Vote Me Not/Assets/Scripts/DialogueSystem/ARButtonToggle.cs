using UnityEngine;
using UnityEngine.UI;

public class ARButtonToggle : MonoBehaviour
{
    [Header("Button References")]
    public Button arButton;
    public Button acceptButton;
    public Button rejectButton;

    private bool isEnabled = false;
    private DialogueUI dialogueUI;
    private VoteManager voteManager;

    void Start()
    {
        // cache systems
        dialogueUI    = FindObjectOfType<DialogueUI>();
        voteManager   = FindObjectOfType<VoteManager>();
        if (dialogueUI == null)  Debug.LogError("No DialogueUI in scene!");
        if (voteManager == null) Debug.LogError("No VoteManager in scene!");

        // hide at start
        acceptButton?.gameObject.SetActive(false);
        rejectButton?.gameObject.SetActive(false);

        // hook toggle
        if (arButton != null)
            arButton.onClick.AddListener(OnARToggle);
        else
            Debug.LogError("ARButtonToggle: missing AR button ref!");
    }

    void OnARToggle()
    {
        isEnabled = !isEnabled;
        acceptButton?.gameObject.SetActive(isEnabled);
        rejectButton?.gameObject.SetActive(isEnabled);

        // grab the current NPC
        int idx = voteManager.currentIndex;
        var arr = voteManager.politicians;
        if (arr == null || idx < 0 || idx >= arr.Length) return;
        var currentNPC = arr[idx];

        // speaker + clip
        if (dialogueUI != null)
        {
            dialogueUI.SetSpeakerName(currentNPC.npcName);
            var clip = isEnabled 
                ? currentNPC.arEnableDialogueContent 
                : currentNPC.arDisableDialogueContent;

            if (clip != null)
                dialogueUI.StartDialogue(clip);
            else
                Debug.LogWarning($"[{currentNPC.npcName}] missing AR-{(isEnabled?"on":"off")} clip!");
        }
    }
}
