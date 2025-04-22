using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/* Authored by: Samuel Cezar
   Company: Company Name
   Project: Project Name
   Feature: [NXR-002] Accept Reject System Feature
*/
public class VoteManager : MonoBehaviour
{
    [Header("Politicians Setup")]
    public Politician[] politicians;
    private int currentIndex = 0;

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
    public GameObject dialogueOptionsPanel;
    public Button agreeButton;
    public Button disagreeButton;
    public Button skillCheckButton;

    [Header("Skill Check Settings")]
    [Tooltip("Chance (0-100) to succeed the speech skill check.")]
    public int speechSkill = 50;

    private DialogueUI dialogueUI;
    private Image fadeImage;
    private bool isTransitioning = false;
    private int currentClaimIndex = -1;

    void Start()
    {
        // Fade initialization
        fadeImage = fadePanel.GetComponent<Image>();
        if (fadeImage != null)
        {
            var temp = fadeImage.color;
            temp.a = 0f;
            fadeImage.color = temp;
        }
        fadePanel.SetActive(false);

        // Activate only the first politician
        for (int i = 0; i < politicians.Length; i++)
            politicians[i].gameObject.SetActive(i == currentIndex);

        // Hook up Interrogate button
        interrogateButton.onClick.AddListener(OnInterrogate);

        // Prepare Claim selection UI (initially hidden)
        claimSelectionPanel.SetActive(false);
        for (int i = 0; i < claimButtons.Length; i++)
        {
            int idx = i;
            claimButtons[i].gameObject.SetActive(false);
            claimButtons[i].onClick.AddListener(() => OnSelectClaim(idx));
        }

        // Prepare Dialogue options UI (initially hidden)
        dialogueOptionsPanel.SetActive(false);
        agreeButton.onClick.AddListener(OnAgree);
        disagreeButton.onClick.AddListener(OnDisagree);
        skillCheckButton.onClick.AddListener(OnSkillCheck);

        // Cache DialogueUI
        dialogueUI = FindObjectOfType<DialogueUI>();
    }

    /// <summary>
    /// Toggles the claim selection panel each time Interrogate is clicked.
    /// </summary>
    public void OnInterrogate()
    {
        if (isTransitioning || currentIndex >= politicians.Length)
            return;

        // Toggle panel visibility
        bool isOpen = claimSelectionPanel.activeSelf;
        claimSelectionPanel.SetActive(!isOpen);
        if (isOpen)
            return;

        // Populate unlocked claim buttons
        var current = politicians[currentIndex];
        for (int i = 0; i < claimButtons.Length; i++)
        {
            bool unlocked = current.claimUnlocked != null
                            && i < current.claimUnlocked.Length
                            && current.claimUnlocked[i];
            claimButtons[i].gameObject.SetActive(unlocked);
        }
    }

    private void OnSelectClaim(int idx)
    {
        currentClaimIndex = idx;
        claimSelectionPanel.SetActive(false);
        dialogueUI.onDialogueEnd = ShowDialogueOptions;
        politicians[currentIndex].TriggerClaimDialogue(idx);
    }

    private void ShowDialogueOptions()
    {
        dialogueOptionsPanel.SetActive(true);
        agreeButton.gameObject.SetActive(true);
        disagreeButton.gameObject.SetActive(true);
        skillCheckButton.gameObject.SetActive(true);
        SetDialogueOptionsInteractable(true);
    }

    /// <summary>
    /// Enables or disables all dialogue option buttons to prevent multiple submissions.
    /// </summary>
    private void SetDialogueOptionsInteractable(bool interactable)
    {
        agreeButton.interactable = interactable;
        disagreeButton.interactable = interactable;
        skillCheckButton.interactable = interactable;
    }

    private void OnAgree()
    {
        var current = politicians[currentIndex];
        // Disable further option clicks
        SetDialogueOptionsInteractable(false);
        // After dialogue ends, hide options and disable this claim
        dialogueUI.onDialogueEnd = () =>
        {
            dialogueOptionsPanel.SetActive(false);
            if (current.claimUnlocked != null && currentClaimIndex >= 0 && currentClaimIndex < current.claimUnlocked.Length)
                current.claimUnlocked[currentClaimIndex] = false;
        };
        current.TriggerAgreeDialogue(currentClaimIndex);
    }

    private void OnDisagree()
    {
        var current = politicians[currentIndex];
        // Disable further option clicks
        SetDialogueOptionsInteractable(false);
        // After dialogue ends, hide options and disable this claim
        dialogueUI.onDialogueEnd = () =>
        {
            dialogueOptionsPanel.SetActive(false);
            if (current.claimUnlocked != null && currentClaimIndex >= 0 && currentClaimIndex < current.claimUnlocked.Length)
                current.claimUnlocked[currentClaimIndex] = false;
        };
        current.TriggerDisagreeDialogue(currentClaimIndex);
    }

    private void OnSkillCheck()
    {
        var current = politicians[currentIndex];
        // Disable further option clicks
        SetDialogueOptionsInteractable(false);
        // Hide dialogue options panel
        dialogueOptionsPanel.SetActive(false);
        bool success = Random.Range(0, 100) < speechSkill;

        // After skill-check dialogue ends, hide options and disable this claim
        dialogueUI.onDialogueEnd = () =>
        {
            dialogueOptionsPanel.SetActive(false);
            if (current.claimUnlocked != null && currentClaimIndex >= 0 && currentClaimIndex < current.claimUnlocked.Length)
                current.claimUnlocked[currentClaimIndex] = false;
        };

        if (success)
            current.TriggerSkillSuccessDialogue(currentClaimIndex);
        else
            current.TriggerSkillFailDialogue(currentClaimIndex);
    }

    public void OnAccept()
    {
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
