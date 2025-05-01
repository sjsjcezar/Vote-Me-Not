using UnityEngine;
using UnityEngine.UI;


public enum SkillType { Speech, Scholar }

[System.Serializable]
public struct ConversationDialogue
{
    public DialogueContentSO initial;
    public DialogueContentSO[] repeatables;
}

[System.Serializable]
public struct QuestionNode
{
    public string[] optionTexts;
    public bool[]   isSkillCheck;
    public int[]    nextNodes;
    public bool     isExitNode;   // if this node ends the tree

    public SkillType[] skillCheckTypes;
}

[System.Serializable]
public struct QuestionDialogue
{
    [Tooltip("Dialogue when entering this node.")]
    public DialogueContentSO entryDialogue;

    [Tooltip("Dialogue responses for each option (non-skill-check). Indexed by option index.")]
    public DialogueContentSO[] responseDialogues;

    [Tooltip("Dialogue on skill-check success for each option. Indexed by option index.")]
    public DialogueContentSO[] skillSuccessDialogues;

    [Tooltip("Dialogue on skill-check failure for each option. Indexed by option index.")]
    public DialogueContentSO[] skillFailDialogues;
}

[System.Serializable]
public class QuestionTree
{
    public QuestionNode[] nodes;                // branching logic per node
    public QuestionDialogue[] nodeDialogues;    // per-node dialogue content
}

public class Politician : MonoBehaviour
{
    public string npcName;
    public AffiliationGlobalEnum affiliation;


    [Header("Skill Check Modifiers")]
    [Tooltip("Positive or negative modifier in % for Speech skill-checks (e.g. +10 or -20)")]
    public float speechModifierPercent = 0f;

    [Tooltip("Positive or negative modifier in % for Scholar skill-checks (e.g. +10 or -20)")]
    public float scholarModifierPercent = 0f;

    [Tooltip("Base NPC difficulty for skill checks; higher = tougher (e.g. 50–150)")]
    public float challengeLevel = 100f;

    [Header("Hard Skil Check Failure Debuff Settings")]
    [Tooltip("Chance (0-100) to apply failure debuff to player stats")]
    public float failureDebuffChance = 50f;
    [Tooltip("Percent reduction to player's speech skill on failure")]
    public float failureDebuffSpeechPercent = 15f;
    [Tooltip("Percent reduction to player's scholar skill on failure")]
    public float failureDebuffScholarPercent = 35f;


    [Header("Hard Skill Type")]
    [Tooltip("Which stat to use when the player hits the MAIN Skill-Check Button")]
    public SkillType hardSkillType = SkillType.Speech;

    [Header("Portrait Settings")]
    [Tooltip("The SpriteRenderer on your world-space politician object")]
    public SpriteRenderer portraitRenderer;
    [Tooltip("Default sprite to revert to")]
    public Sprite originalSprite;
    [Tooltip("Sprite to show on skill success")]
    public Sprite skillSuccessSprite;
    [Tooltip("Sprite to show on skill failure")]
    public Sprite skillFailSprite;
    [Tooltip("Animator controlling blinking on the portraitRenderer")]
    public Animator portraitAnimator;


    [Header("Dialogue Settings")]
    public DialogueContentSO initialDialogueContent;
    public DialogueContentSO[] claimDialogueContents;

    [Header("Skill-Check Dialogue (Claim-level)")]
    public DialogueContentSO[] skillSuccessDialogueContents;
    public DialogueContentSO[] skillFailDialogueContents;

    [Header("Agree/Disagree Responses")]
    public DialogueContentSO[] agreeResponseContents;
    public DialogueContentSO[] disagreeResponseContents;

    [Header("Conversation Dialogues")]
    public ConversationDialogue[] conversationDialogues;

    [Header("Button Text Customization")]
    public string[] claimButtonTexts;
    public string[] agreeButtonTexts;
    public string[] disagreeButtonTexts;
    public string[] skillCheckButtonTexts;
    public string[] conversationButtonTexts;

    [Header("Accept/Reject Button Dialogue")]
    public DialogueContentSO arEnableDialogueContent;
    public DialogueContentSO arDisableDialogueContent;

    [Header("Per-Claim Question Trees")]
    public QuestionTree[] questionTrees;

    // Tracks which claims have been unlocked
    public bool[] claimUnlocked;
    private bool[] initialConversationPlayed;

    void Awake()
    {
        // Initialize claim unlocks
        if (claimDialogueContents != null)
        {
            claimUnlocked = new bool[claimDialogueContents.Length];
            for (int i = 0; i < claimUnlocked.Length; i++)
                claimUnlocked[i] = false;
        }
        // Initialize conversation tracker
        initialConversationPlayed = new bool[conversationDialogues != null ? conversationDialogues.Length : 0];
        for (int i = 0; i < initialConversationPlayed.Length; i++)
            initialConversationPlayed[i] = false;

        
        if (portraitRenderer != null && originalSprite == null)
            originalSprite = portraitRenderer.sprite;
    }

    void Start()
    {
        TriggerInitialDialogue();
    }

    public void ShowSkillResult(bool success)
    {
        if (portraitAnimator != null)
            portraitAnimator.enabled = false;
        if (portraitRenderer != null)
            portraitRenderer.sprite = success ? skillSuccessSprite : skillFailSprite;
    }

    public void RevertPortrait()
    {
        if (portraitRenderer != null)
            portraitRenderer.sprite = originalSprite;
        if (portraitAnimator != null)
            portraitAnimator.enabled = true;
    }

    public void UnlockClaim(int claimIndex)
    {
        if (claimUnlocked != null && claimIndex >= 0 && claimIndex < claimUnlocked.Length)
        {
            claimUnlocked[claimIndex] = true;
            Debug.Log($"Claim {claimIndex+1} unlocked for {npcName}");
        }
    }

    public void TriggerInitialDialogue()
    {
        if (initialDialogueContent == null) return;
        var ui = FindObjectOfType<DialogueUI>();
        ui.SetSpeakerName(npcName);
        ui.onDialogueEnd = () => {
      //      if (claimUnlocked != null && claimUnlocked.Length > 0 && claimUnlocked[0])
      //          TriggerClaimDialogue(0);
        };
        ui.StartDialogue(initialDialogueContent);
    }

    public void TriggerClaimDialogue(int claimIndex)
    {
        if (claimDialogueContents == null || claimIndex < 0 || claimIndex >= claimDialogueContents.Length) return;
        var content = claimDialogueContents[claimIndex];
        var ui = FindObjectOfType<DialogueUI>();
        ui.SetSpeakerName(npcName);
        ui.StartDialogue(content);
    }

    public void TriggerAgreeDialogue(int claimIndex)
    {
        if (agreeResponseContents == null || claimIndex < 0 || claimIndex >= agreeResponseContents.Length) return;
        var ui = FindObjectOfType<DialogueUI>();
        ui.SetSpeakerName(npcName);
        ui.StartDialogue(agreeResponseContents[claimIndex]);
    }

    public void TriggerDisagreeDialogue(int claimIndex)
    {
        if (disagreeResponseContents == null || claimIndex < 0 || claimIndex >= disagreeResponseContents.Length) return;
        var ui = FindObjectOfType<DialogueUI>();
        ui.SetSpeakerName(npcName);
        ui.StartDialogue(disagreeResponseContents[claimIndex]);
    }

    public void TriggerSkillSuccessDialogue(int claimIndex)
    {
        if (skillSuccessDialogueContents == null || claimIndex < 0 || claimIndex >= skillSuccessDialogueContents.Length) return;
        var ui = FindObjectOfType<DialogueUI>();
        ui.SetSpeakerName(npcName);
        ui.StartDialogue(skillSuccessDialogueContents[claimIndex]);
    }

    public void TriggerSkillFailDialogue(int claimIndex)
    {
        if (skillFailDialogueContents == null || claimIndex < 0 || claimIndex >= skillFailDialogueContents.Length) return;
        var ui = FindObjectOfType<DialogueUI>();
        ui.SetSpeakerName(npcName);
        ui.StartDialogue(skillFailDialogueContents[claimIndex]);
    }

    public void TriggerConversationDialogue(int claimIndex)
    {
        if (conversationDialogues == null || claimIndex < 0 || claimIndex >= conversationDialogues.Length)
        {
            Debug.LogWarning($"Invalid conversation index for {npcName}: {claimIndex}");
            return;
        }
        var conv = conversationDialogues[claimIndex];
        var content = !initialConversationPlayed[claimIndex]
            ? conv.initial
            : conv.repeatables[Random.Range(0, conv.repeatables.Length)];
        initialConversationPlayed[claimIndex] = true;
        var ui = FindObjectOfType<DialogueUI>();
        ui.SetSpeakerName(npcName);
        ui.StartDialogue(content);
    }

    public void TriggerQuestionDialogue(int claimIndex, int nodeIndex)
    {
        var tree = questionTrees[claimIndex];
        var dialogue = tree.nodeDialogues[nodeIndex];
        var ui = FindObjectOfType<DialogueUI>();
        ui.SetSpeakerName(npcName);
        ui.StartDialogue(dialogue.entryDialogue);
    }

    public void TriggerQuestionResponse(int claimIndex, int nodeIndex, int optionIndex)
    {
        var tree = questionTrees[claimIndex];
        var dialogue = tree.nodeDialogues[nodeIndex];
        var content = dialogue.responseDialogues[optionIndex];
        var ui = FindObjectOfType<DialogueUI>();
        ui.SetSpeakerName(npcName);
        ui.StartDialogue(content);
    }

    public void TriggerSkillResponse(int claimIndex, int nodeIndex, int optionIndex, bool success)
    {
        var tree = questionTrees[claimIndex];
        var dialogue = tree.nodeDialogues[nodeIndex];
        var array = success ? dialogue.skillSuccessDialogues : dialogue.skillFailDialogues;
        var content = array[optionIndex];
        var ui = FindObjectOfType<DialogueUI>();
        ui.SetSpeakerName(npcName);
        ui.StartDialogue(content);
    }

    public void ResetConversationTracker(int claimIndex)
    {
        if (claimIndex >= 0 && claimIndex < initialConversationPlayed.Length)
            initialConversationPlayed[claimIndex] = false;
    }
}
