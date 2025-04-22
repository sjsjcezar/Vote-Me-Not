using UnityEngine;

[System.Serializable]
public struct ConversationDialogue
{
    public DialogueContentSO initial;
    public DialogueContentSO[] repeatables;
}

public class Politician : MonoBehaviour
{
    // Politician's display name (set in the Inspector)
    public string npcName;

    public AffiliationGlobalEnum affiliation;
    

    [Header("Dialogue Settings")]
    public DialogueContentSO initialDialogueContent;    // Initial dialogue (claim presentation)
    public DialogueContentSO[] claimDialogueContents;     // Dialogue asset per claim (for interrogation)

    [Header("Skill-Check Dialogue")]
    [Tooltip("Played when speech skill check succeeds for each claim index.")]
    public DialogueContentSO[] skillSuccessDialogueContents;
    [Tooltip("Played when speech skill check fails for each claim index.")]
    public DialogueContentSO[] skillFailDialogueContents;

    [Header("Response Dialogues")]
    [Tooltip("What the NPC says when you AGREE with Claim #i")]
    public DialogueContentSO[] agreeResponseContents;
    [Tooltip("What the NPC says when you DISAGREE with Claim #i")]
    public DialogueContentSO[] disagreeResponseContents;

    [Header("Conversation Dialogues")]
    public ConversationDialogue[] conversationDialogues;


    [Header("Button Text Customization")]
    [Tooltip("Text for claim buttons (index 0 = Claim 1)")]
    public string[] claimButtonTexts = new string[3];

    [Header("Dialogue Option Texts")]
    [Tooltip("Texts for agree button per claim (index 0 = Claim 1)")]
    public string[] agreeButtonTexts = new string[3];
    [Tooltip("Texts for disagree button per claim")]
    public string[] disagreeButtonTexts = new string[3];
    [Tooltip("Texts for skill check button per claim")]
    public string[] skillCheckButtonTexts = new string[3];
    [Tooltip("Texts for conversation button per claim")]
    public string[] conversationButtonTexts = new string[3];

    private bool[] initialConversationPlayed;



    // Tracks which claims have been unlocked; initialized in Awake.
    public bool[] claimUnlocked;

    void Awake()
    {
        if (claimDialogueContents != null && claimDialogueContents.Length > 0)
        {
            claimUnlocked = new bool[claimDialogueContents.Length];
            for (int i = 0; i < claimUnlocked.Length; i++)
                claimUnlocked[i] = false;
        }

        // Initialize conversation tracker
        initialConversationPlayed = new bool[conversationDialogues != null ? conversationDialogues.Length : 0];
        for (int i = 0; i < initialConversationPlayed.Length; i++)
        {
            initialConversationPlayed[i] = false;
        }
    }

    void Start()
    {
        TriggerInitialDialogue();
    }

    // Called by the file selection system to unlock a specific claim.
    public void UnlockClaim(int claimIndex)
    {
        if (claimUnlocked != null && claimIndex >= 0 && claimIndex < claimUnlocked.Length)
        {
            claimUnlocked[claimIndex] = true;
            Debug.Log("Claim " + (claimIndex + 1) + " unlocked for " + npcName);
        }
    }

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
            dialogueUI.onDialogueEnd = () =>
            {
                if (claimUnlocked != null && claimUnlocked.Length > 0 && claimUnlocked[0])
                    TriggerClaimDialogue(0);
            };
            Debug.Log("Triggering initial dialogue for " + npcName);
            dialogueUI.StartDialogue(initialDialogueContent);
        }
        else
        {
            Debug.LogError("DialogueUI not found in the scene.");
        }
    }

    public void TriggerClaimDialogue(int claimIndex)
    {
        if (claimDialogueContents == null || claimIndex < 0 || claimIndex >= claimDialogueContents.Length)
        {
            Debug.LogWarning("Invalid claim dialogue content for " + npcName);
            return;
        }
        var claimDialogue = claimDialogueContents[claimIndex];
        if (claimDialogue == null)
        {
            Debug.LogWarning($"No dialogue content for claim {claimIndex} on {npcName}");
            return;
        }

        var dialogueUI = FindObjectOfType<DialogueUI>();
        if (dialogueUI != null)
        {
            dialogueUI.SetSpeakerName(npcName);
            Debug.Log($"Triggering claim dialogue for claim {claimIndex + 1} on {npcName}");
            dialogueUI.StartDialogue(claimDialogue);
        }
        else
        {
            Debug.LogError("DialogueUI not found in the scene.");
        }
    }

    public void TriggerConversationDialogue(int claimIndex)
    {
        if (conversationDialogues == null || claimIndex < 0 || claimIndex >= conversationDialogues.Length)
        {
            Debug.LogWarning("Invalid conversation index for " + npcName);
            return;
        }

        ConversationDialogue conv = conversationDialogues[claimIndex];
        DialogueContentSO content = null;

        if (!initialConversationPlayed[claimIndex])
        {
            content = conv.initial;
            initialConversationPlayed[claimIndex] = true;
        }
        else
        {
            if (conv.repeatables == null || conv.repeatables.Length == 0)
            {
                Debug.LogWarning("No repeatable dialogues for claim " + claimIndex + " on " + npcName);
                return;
            }
            int randomIdx = Random.Range(0, conv.repeatables.Length);
            content = conv.repeatables[randomIdx];
        }

        if (content == null)
        {
            Debug.LogWarning("Conversation content missing for claim " + claimIndex + " on " + npcName);
            return;
        }

        DialogueUI dialogueUI = FindObjectOfType<DialogueUI>();
        if (dialogueUI != null)
        {
            dialogueUI.SetSpeakerName(npcName);
            dialogueUI.StartDialogue(content);
        }
    }

        
    public void TriggerAgreeDialogue(int claimIndex)
    {
        TriggerCustomDialogue(agreeResponseContents, claimIndex, "agree");
    }

    public void TriggerDisagreeDialogue(int claimIndex)
    {
        TriggerCustomDialogue(disagreeResponseContents, claimIndex, "disagree");
    }


    public void TriggerSkillSuccessDialogue(int claimIndex)
    {
        TriggerCustomDialogue(skillSuccessDialogueContents, claimIndex, "success");
    }

    public void TriggerSkillFailDialogue(int claimIndex)
    {
        TriggerCustomDialogue(skillFailDialogueContents, claimIndex, "failure");
    }

    private void TriggerCustomDialogue(DialogueContentSO[] dialogues, int index, string type)
    {
        if (dialogues == null || index < 0 || index >= dialogues.Length)
        {
            Debug.LogWarning($"Missing skill-check dialogue ({type}) at index {index} for {npcName}");
            return;
        }
        var content = dialogues[index];
        if (content == null)
        {
            Debug.LogWarning($"Null DialogueContentSO for skill-check ({type}) at index {index} on {npcName}");
            return;
        }
        var dialogueUI = FindObjectOfType<DialogueUI>();
        if (dialogueUI != null)
        {
            dialogueUI.SetSpeakerName(npcName);
            Debug.Log($"Triggering skill-check {type} dialogue for claim {index + 1} on {npcName}");
            dialogueUI.StartDialogue(content);
        }
        else
        {
            Debug.LogError("DialogueUI not found in the scene.");
        }
    }


    public void ResetConversationTracker(int claimIndex)
    {
        if (claimIndex >= 0 && claimIndex < initialConversationPlayed.Length)
        {
            initialConversationPlayed[claimIndex] = false;
        }
    }
}
