using UnityEngine;

public class Politician : MonoBehaviour
{
    // Politician's display name (set in the Inspector)
    public string npcName;

    public AffiliationGlobalEnum affiliation;

    [Header("Dialogue Settings")]
    public DialogueContentSO initialDialogueContent;    // Initial dialogue (claim presentation)
    public DialogueContentSO[] claimDialogueContents;     // Dialogue asset per claim (for interrogation)

    // Tracks which claims have been unlocked; initialized in Awake.
    public bool[] claimUnlocked;

    void Awake()
    {
        if(claimDialogueContents != null && claimDialogueContents.Length > 0)
        {
            claimUnlocked = new bool[claimDialogueContents.Length];
            for (int i = 0; i < claimUnlocked.Length; i++)
            {
                claimUnlocked[i] = false;
            }
        }
    }

    void Start()
    {
        TriggerInitialDialogue();
    }

    // Called by the file selection system to unlock a specific claim.
    public void UnlockClaim(int claimIndex)
    {
        if(claimUnlocked != null && claimIndex >= 0 && claimIndex < claimUnlocked.Length)
        {
            claimUnlocked[claimIndex] = true;
            Debug.Log("Claim " + (claimIndex + 1) + " unlocked for " + npcName);
        }
    }

    // Triggers the initial dialogue and sets a callback to trigger Claim 1 if unlocked.
    public void TriggerInitialDialogue()
    {
        if (initialDialogueContent == null)
        {
            Debug.LogWarning("No initial dialogue content assigned to " + npcName);
            return;
        }

        DialogueUI dialogueUI = FindObjectOfType<DialogueUI>();
        if (dialogueUI != null)
        {
            dialogueUI.SetSpeakerName(npcName);
            // When dialogue ends, check if Claim 1 is unlocked and trigger its dialogue.
            dialogueUI.onDialogueEnd = () =>
            {
                if (claimUnlocked != null && claimUnlocked.Length > 0 && claimUnlocked[0])
                {
                    TriggerClaimDialogue(0);
                }
            };
            Debug.Log("Triggering initial dialogue for " + npcName);
            dialogueUI.StartDialogue(initialDialogueContent);
        }
        else
        {
            Debug.LogError("DialogueUI not found in the scene.");
        }
    }

    // Triggers the interrogation dialogue for a specific claim.
    public void TriggerClaimDialogue(int claimIndex)
    {
        if (claimDialogueContents == null || claimIndex < 0 || claimIndex >= claimDialogueContents.Length)
        {
            Debug.LogWarning("Invalid claim dialogue content for " + npcName);
            return;
        }
        DialogueContentSO claimDialogue = claimDialogueContents[claimIndex];
        if (claimDialogue == null)
        {
            Debug.LogWarning("No dialogue content for claim " + claimIndex + " on " + npcName);
            return;
        }

        DialogueUI dialogueUI = FindObjectOfType<DialogueUI>();
        if (dialogueUI != null)
        {
            dialogueUI.SetSpeakerName(npcName);
            Debug.Log("Triggering claim dialogue for claim " + (claimIndex + 1) + " on " + npcName);
            dialogueUI.StartDialogue(claimDialogue);
        }
        else
        {
            Debug.LogError("DialogueUI not found in the scene.");
        }
    }
}
