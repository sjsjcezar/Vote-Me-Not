/* Authored by: Samuel Cezar
   Company: Company Name
   Project: Project Name
   Feature: [NXR-002] Accept Reject System Feature
*/

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

[System.Serializable]
public class DialogueNode
{
    public string[] optionTexts;
    public bool[] isSkillCheck;
    public int[] nextNodes;
    public bool isExitNode;
}

public class VoteManager : MonoBehaviour
{
    [Header("Politicians Setup")]
    public Politician[] politicians;
    public int currentIndex = 0;

    [Header("Ethics Meter Settings")]
    public int ethicsMeter = 50;
    public int goodThreshold = 60;
    public int evilThreshold = 40;
    public int updateAmountGood = 10;
    public int updateAmountEvil = 10;

    [Header("References")]
    public EthicsMeterController ethicsMeterController;

    [Header("Fade Transition Settings")]
    public GameObject fadePanel;
    public float fadeDuration = 1.0f;

    [Header("Interrogation UI")]
    public Button interrogateButton;
    public GameObject claimSelectionPanel;
    public Button[] claimButtons;
    public TMP_Text[] claimButtonTextElements;

    [Header("Dialogue Options UI")]
    public GameObject dialogueOptionsPanel;
    public Button agreeButton;
    public Button disagreeButton;
    public Button skillCheckButton;
    public Button conversationButton;
    public TMP_Text agreeButtonText;
    public TMP_Text disagreeButtonText;
    public TMP_Text skillCheckButtonText;
    public TMP_Text conversationButtonText;

    [Header("Question Tree UI")]
    public GameObject questionOptionsPanel;
    public Button questionAgreeButton;
    public Button questionDisagreeButton;
    public Button questionSkillCheckButton;
    public Button questionConversationButton;
    public TMP_Text questionAgreeButtonText;
    public TMP_Text questionDisagreeButtonText;
    public TMP_Text questionSkillCheckButtonText;
    public TMP_Text questionConversationButtonText;
    public Button[] questionButtons;

    [Header("Skill Check Settings")]
    [Tooltip("Chance (0-100) to succeed the speech skill check.")]
    public int speechSkill = 50;

    [Header("Skill Check Feedback Text")]
    public TMP_Text successText;
    public TMP_Text failText;

    [Header("Question Dialogue Tree State")]
    private int currentNodeIndex = -1;

    private DialogueUI dialogueUI;
    private Image fadeImage;
    private bool isTransitioning = false;
    private int currentClaimIndex = -1;

    private bool isOption2ButtonPressed = false;

    private bool[] skillCheckDisabled;

    void Start()
    {
        // Fade init
        fadeImage = fadePanel.GetComponent<Image>();
        if (fadeImage != null)
        {
            var temp = fadeImage.color;
            temp.a = 0f;
            fadeImage.color = temp;
        }
        fadePanel.SetActive(false);

        // Activate first politician only
        for (int i = 0; i < politicians.Length; i++)
            politicians[i].gameObject.SetActive(i == currentIndex);

        // Setup Interrogate
        interrogateButton.onClick.AddListener(OnInterrogate);

        // Setup claim buttons (hidden)
        claimSelectionPanel.SetActive(false);
        for (int i = 0; i < claimButtons.Length; i++)
        {
            int idx = i;
            claimButtons[i].gameObject.SetActive(false);
            claimButtons[i].onClick.AddListener(() => OnSelectClaim(idx));
        }

        // Setup main options (hidden)
        dialogueOptionsPanel.SetActive(false);
        agreeButton.onClick.AddListener(OnAgree);
        disagreeButton.onClick.AddListener(OnQuestion);
        skillCheckButton.onClick.AddListener(OnSkillCheck);
        conversationButton.onClick.AddListener(OnConversation);

        // Setup question-tree options (hidden)
        questionOptionsPanel.SetActive(false);
        questionAgreeButton.onClick.AddListener(() => HandleQuestionResponse(0));
        questionDisagreeButton.onClick.AddListener(() => HandleQuestionResponse(1));
        questionSkillCheckButton.onClick.AddListener(() => HandleQuestionResponse(2));
        questionConversationButton.onClick.AddListener(() => HandleQuestionResponse(3));

        // DialogueUI cache
        dialogueUI = FindObjectOfType<DialogueUI>();

        // Hide feedback
        successText?.gameObject.SetActive(false);
        failText?.gameObject.SetActive(false);

        questionButtons = new[] {
            questionAgreeButton,
            questionDisagreeButton,
            questionSkillCheckButton,
            questionConversationButton
        };
    }

    public void OnInterrogate()
    {
        if (isTransitioning || currentIndex >= politicians.Length)
            return;

        bool open = claimSelectionPanel.activeSelf;
        claimSelectionPanel.SetActive(!open);
        if (open) return;

        var current = politicians[currentIndex];
        for (int i = 0; i < claimButtons.Length; i++)
        {
            if (i < current.claimButtonTexts.Length)
                claimButtonTextElements[i].text = current.claimButtonTexts[i];

            bool unlocked = current.claimUnlocked != null && i < current.claimUnlocked.Length && current.claimUnlocked[i];
            claimButtons[i].gameObject.SetActive(unlocked);
        }
    }

    private void OnSelectClaim(int idx)
    {
        dialogueUI?.ClearDialogue();
        currentClaimIndex = idx;
        claimSelectionPanel.SetActive(false);
        dialogueUI.onDialogueEnd = ShowDialogueOptions;

        // Set up main buttons text
        var curr = politicians[currentIndex];
        agreeButtonText.text = curr.agreeButtonTexts[idx];
        disagreeButtonText.text = curr.disagreeButtonTexts[idx];
        skillCheckButtonText.text = curr.skillCheckButtonTexts[idx];
        conversationButtonText.text = curr.conversationButtonTexts[idx];

        curr.TriggerClaimDialogue(idx);
    }

    private void ShowDialogueOptions()
    {
        // hide question UI
        questionOptionsPanel.SetActive(false);

        // show main UI
        dialogueOptionsPanel.SetActive(true);
        SetMainButtons(true);
    }

    private void SetMainButtons(bool canInteract)
    {
        agreeButton.interactable = canInteract;
        disagreeButton.interactable = canInteract;
        skillCheckButton.interactable = canInteract;
        conversationButton.interactable = canInteract;
    }

    private void ShowQuestionOptions()
    {
        dialogueOptionsPanel.SetActive(false);
        questionOptionsPanel.SetActive(true);
        SetQuestionButtons(true);
        UpdateQuestionButtons();
    }

    private void SetQuestionButtons(bool canInteract)
    {
        questionAgreeButton.interactable = canInteract;
        questionDisagreeButton.interactable = canInteract;
        questionSkillCheckButton.interactable = canInteract;
        questionConversationButton.interactable = canInteract;
    }

    private void UpdateQuestionButtons()
    {
        var curr = politicians[currentIndex];
        var node = curr.questionTrees[currentClaimIndex].nodes[currentNodeIndex];

        // reset disabled-flags on node change
        if (skillCheckDisabled == null || skillCheckDisabled.Length != questionButtons.Length)
            skillCheckDisabled = new bool[questionButtons.Length];
        else
            for (int i = 0; i < skillCheckDisabled.Length; i++)
                skillCheckDisabled[i] = false;

        // hide all
        for (int i = 0; i < questionButtons.Length; i++)
            questionButtons[i].gameObject.SetActive(false);

        // show + label relevant ones
        for (int i = 0; i < node.optionTexts.Length; i++)
        {
            questionButtons[i].gameObject.SetActive(true);
            switch (i)
            {
                case 0: questionAgreeButtonText.text       = node.optionTexts[i]; break;
                case 1: questionDisagreeButtonText.text    = node.optionTexts[i]; break;
                case 2: questionSkillCheckButtonText.text  = node.optionTexts[i]; break;
                case 3: questionConversationButtonText.text= node.optionTexts[i]; break;
            }

            // if it’s a skill-check, apply per-node disabled state
            if (node.isSkillCheck[i])
                questionButtons[i].interactable = !skillCheckDisabled[i];
            else
                questionButtons[i].interactable = true;
        }
    }

    private void HandleQuestionResponse(int optionIndex)
    {
        var curr = politicians[currentIndex];
        var node = curr.questionTrees[currentClaimIndex].nodes[currentNodeIndex];

        // 1) If skill-check, disable *this* button only
        if (node.isSkillCheck[optionIndex])
        {
            bool success = Random.Range(0, 100) < speechSkill;
            ShowSkillFeedback(success);
            curr.TriggerSkillResponse(currentClaimIndex, currentNodeIndex, optionIndex, success);

            questionButtons[optionIndex].interactable = false;
            skillCheckDisabled[optionIndex] = true;
        }
        else
        {
            curr.TriggerQuestionResponse(currentClaimIndex, currentNodeIndex, optionIndex);
        }

        // 2) exit-node logic only on option 0
        if (node.isExitNode && optionIndex == 0)
        {
            questionOptionsPanel.SetActive(false);
            dialogueOptionsPanel.SetActive(true);
            SetMainButtons(true);
            disagreeButton.interactable = false;
            return;
        }

        // 3) move to next or stay
        int next = node.nextNodes[optionIndex];
        if (next >= 0)
            currentNodeIndex = next;

        dialogueUI.onDialogueEnd = ShowQuestionOptions;
    }

    private void OnAgree()
    {
        var curr = politicians[currentIndex];
        SetMainButtons(false);
        dialogueUI.onDialogueEnd = () => {
            dialogueOptionsPanel.SetActive(false);
            if (curr.claimUnlocked != null && currentClaimIndex >=0 && currentClaimIndex < curr.claimUnlocked.Length)
                curr.claimUnlocked[currentClaimIndex] = false;
            curr.ResetConversationTracker(currentClaimIndex);
        };
        curr.TriggerAgreeDialogue(currentClaimIndex);
    }

    private void OnQuestion()
    {
        isOption2ButtonPressed = true;
        var curr = politicians[currentIndex];
        SetMainButtons(false);
        if (currentClaimIndex >=0)
            curr.claimUnlocked[currentClaimIndex] = false;

        currentNodeIndex = 0;
        dialogueUI.onDialogueEnd = ShowQuestionOptions;
        curr.TriggerQuestionDialogue(currentClaimIndex, currentNodeIndex);
    }

    private void OnSkillCheck()
    {
        var curr = politicians[currentIndex];
        SetMainButtons(false);
        dialogueOptionsPanel.SetActive(false);

        bool success = Random.Range(0,100) < speechSkill;
        ShowSkillFeedback(success);

        dialogueUI.onDialogueEnd = () => {
            dialogueOptionsPanel.SetActive(false);
            if (curr.claimUnlocked != null && currentClaimIndex >=0 && currentClaimIndex < curr.claimUnlocked.Length)
                curr.claimUnlocked[currentClaimIndex] = false;
            curr.ResetConversationTracker(currentClaimIndex);
        };

        if (success) curr.TriggerSkillSuccessDialogue(currentClaimIndex);
        else curr.TriggerSkillFailDialogue(currentClaimIndex);
    }

    private void ShowSkillFeedback(bool success)
    {
        if (success) successText?.gameObject.SetActive(true);
        else failText?.gameObject.SetActive(true);
        StartCoroutine(HideSkillFeedback(success));
    }

    private IEnumerator HideSkillFeedback(bool success)
    {
        yield return new WaitForSeconds(2f);
        if (success) successText?.gameObject.SetActive(false);
        else failText?.gameObject.SetActive(false);
    }

    private void OnConversation()
    {
        // only call if a claim is active
        if (currentClaimIndex < 0) return;

        var curr = politicians[currentIndex];
        ButtonDisabler();

        dialogueUI.onDialogueEnd = () => ButtonActivator();

        curr.TriggerConversationDialogue(currentClaimIndex);
    }

    public void ButtonActivator()
    {
        agreeButton.gameObject.SetActive(true);
        disagreeButton.gameObject.SetActive(true);
        skillCheckButton.gameObject.SetActive(true);
        conversationButton.gameObject.SetActive(true);
    }

    public void ButtonDisabler()
    {
        agreeButton.gameObject.SetActive(false);
        disagreeButton.gameObject.SetActive(false);
        skillCheckButton.gameObject.SetActive(false);
        conversationButton.gameObject.SetActive(false);
    }

    public void OnAccept()
    {
        FindObjectOfType<DialogueUI>()?.ClearDialogue();
        if (isTransitioning) return;
        isTransitioning = true;

        if (currentIndex < politicians.Length)
        {
            var currentPolitician = politicians[currentIndex];
            switch (currentPolitician.affiliation)
            {
                case AffiliationGlobalEnum.Good:
                    ethicsMeter += updateAmountGood;
                    break;
                case AffiliationGlobalEnum.Evil:
                    ethicsMeter -= updateAmountEvil;
                    break;
            }
            ethicsMeterController?.UpdateEthics(ethicsMeter);
            StartCoroutine(FadeTransition());
        }
    }

    public void OnReject()
    {
        FindObjectOfType<DialogueUI>()?.ClearDialogue();
        if (isTransitioning) return;
        isTransitioning = true;

        if (currentIndex < politicians.Length)
        {
            var currentPolitician = politicians[currentIndex];
            switch (currentPolitician.affiliation)
            {
                case AffiliationGlobalEnum.Good:
                    int penalty = Mathf.RoundToInt(updateAmountEvil * 0.25f);
                    ethicsMeter -= penalty;
                    break;
                case AffiliationGlobalEnum.Evil:
                    ethicsMeter += updateAmountGood;
                    break;
            }
            ethicsMeterController?.UpdateEthics(ethicsMeter);
            StartCoroutine(FadeTransition());
        }
    }

    private IEnumerator FadeTransition()
    {
        yield return StartCoroutine(FadeIn());
        politicians[currentIndex].gameObject.SetActive(false);
        currentIndex++;
        if (currentIndex < politicians.Length)
            politicians[currentIndex].gameObject.SetActive(true);
        yield return StartCoroutine(FadeOut());
        isTransitioning = false;
    }

    private IEnumerator FadeIn()
    {
        fadePanel.SetActive(true);
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            var color = fadeImage.color;
            color.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }
    }

    private IEnumerator FadeOut()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            var color = fadeImage.color;
            color.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }
        fadePanel.SetActive(false);
    }
}
 


