/* Authored by: Samuel Cezar
   Company: Company Name
   Project: Project Name
   Feature: [NXR-002] Accept Reject System Feature
*/

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

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

    [Header("Energy System")]
    public EnergySystem energySystem;
    
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
    public int speechSkill = 100;
    [Tooltip("Chance (0-100) to succeed the scholar skill check.")]
    public int scholarSkill = 100;

    [Header("Base Player Stats")]
    [Tooltip("Base speech skill value.")]
    [SerializeField] private int baseSpeechSkill;
    [Tooltip("Base scholar skill value.")]
    [SerializeField] private int baseScholarSkill;

    // percent mods
    private float npcSpeechModPercent = 0f;
    private float npcScholarModPercent = 0f;
    public float bottleModPercent   = 0f;

    [Header("Skill Check Feedback Text")]
    public TMP_Text successText;
    public TMP_Text failText;


    [Header("Main UI Skill-Icon")]
    [Tooltip("Image you’ve placed beside the MAIN Skill-Check Button")]
    public Image mainSkillIcon;
    [Tooltip("Sprite to show when using Speech")]
    public Sprite speechIcon;
    [Tooltip("Sprite to show when using Scholar")]
    public Sprite scholarIcon;

    [Header("Question UI Skill-Icons")]
    [Tooltip("One Image per question-button, in the same order as questionButtons[]")]
    public Image[] questionSkillIcons;

    [Header("Question Dialogue Tree State")]
    private int currentNodeIndex = -1;


    [Header("Post-Processing Settings")]
    public Volume postProcessVolume;
    private ChromaticAberration chromatic;
    private Vignette vignette;

    // --- Debuff State ---
    public bool hasDebuff = false;
    private int originalSpeechSkill;
    private int originalScholarSkill;
    public float NpcSpeechModPercent => npcSpeechModPercent;
    public float NpcScholarModPercent => npcScholarModPercent;

    private bool hasBoost = false;
    public bool HasBottleBoost => hasBoost;
    private float currentBottleBoostPct = 0f;
    public float BottleBoostPercent => currentBottleBoostPct;
    private int boostOriginalSpeech, boostOriginalScholar;
    private float boostRemainingTime = 0f;
    private Coroutine boostTimerCoroutine;

    private DialogueUI dialogueUI;
    private Image fadeImage;
    private bool isTransitioning = false;
    private int currentClaimIndex = -1;

    private bool[] skillCheckDisabled;

    private bool hasBeenInteracted = false;
    public event Action OnStatsUpdated;

    void Awake()
    {
        baseSpeechSkill  = speechSkill;
        baseScholarSkill = scholarSkill;
    }
    void Start()
    {
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

        if (postProcessVolume != null)
        {
            var profile = postProcessVolume.profile;
            profile.TryGet<ChromaticAberration>(out chromatic);
            profile.TryGet<Vignette>(out vignette);
            // ensure defaults
            if (chromatic != null) chromatic.intensity.value = 0f;
            if (vignette != null)
            {
                vignette.intensity.value = 0.212f;
                vignette.rounded.value = true;
            }
        }

        ApplyCurrentPoliticianMods();
    }


    private void ApplyCurrentPoliticianMods() 
    {
        var npc = politicians[currentIndex];
        npcSpeechModPercent   = npc.speechModifierPercent;
        npcScholarModPercent  = npc.scholarModifierPercent;
        RecalculateStats();
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
        UpdateMainSkillIcon();
    }

    private void UpdateMainSkillIcon()
    {
        var curr = politicians[currentIndex];
        if (mainSkillIcon != null)
        {
            mainSkillIcon.sprite = (curr.hardSkillType == SkillType.Speech)
                ? speechIcon
                : scholarIcon;
        }
        else Debug.LogWarning("MainSkillIcon reference is missing on VoteManager.");
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

    private void EnableQuestionButtons()
    {
        // grab the current question node
        var curr  = politicians[currentIndex];
        var tree  = curr.questionTrees[currentClaimIndex];
        var node  = tree.nodes[currentNodeIndex];
        int count = node.optionTexts.Length; // 2, 3 or 4

        // enable only as many as the node actually uses
        for (int i = 0; i < questionButtons.Length; i++)
        {
            questionButtons[i].gameObject.SetActive(i < count);
        }
    }

    private void DisableQuestionButtons()
    {
        questionAgreeButton.gameObject.SetActive(false);
        questionDisagreeButton.gameObject.SetActive(false);
        questionSkillCheckButton.gameObject.SetActive(false);
        questionConversationButton.gameObject.SetActive(false);
    }

    private void UpdateQuestionButtons()
    {
        if (questionSkillIcons == null)
        {
            Debug.LogWarning("QuestionSkillIcons is null in VoteManager.");
            return;
        }

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

        for (int i = 0; i < questionSkillIcons.Length; i++)
        {
            var iconImage = questionSkillIcons[i];
            bool isSkill = (node.isSkillCheck != null && i < node.isSkillCheck.Length && node.isSkillCheck[i]);

            if (isSkill && node.skillCheckTypes != null && i < node.skillCheckTypes.Length)
            {
                // Set correct sprite
                var type = node.skillCheckTypes[i];
                Sprite spriteToUse = (type == SkillType.Speech) ? speechIcon : scholarIcon;

                if (iconImage != null)
                {
                    iconImage.sprite = spriteToUse;
                    iconImage.gameObject.SetActive(true);
                }
                else Debug.LogWarning($"questionSkillIcons[{i}] is null.");
            }
            else
            {
                // Hide non-skill icons
                if (iconImage != null)
                    iconImage.gameObject.SetActive(false);
            }
        }

    }

    private void HandleQuestionResponse(int optionIndex)
    {
        var curr = politicians[currentIndex];
        var node = curr.questionTrees[currentClaimIndex].nodes[currentNodeIndex];

        // 1) If skill-check, disable *this* button only
        if (node.isSkillCheck[optionIndex])
        {
            if (!energySystem.TryUseDNTSkill())
                return;

            SkillType type = node.skillCheckTypes[optionIndex];
            float chance = GetSkillChance(type);

            if (hasBoost)
            {
                chance += 25f;
                chance = Mathf.Clamp(chance, 0f, 100f);
            }

            // DEBUG log for DNT
            Debug.LogError(
                $"[DNTSkillCheck] type={type} | "
            + $"chance={chance:0.00}% "
            + $"(base={(type==SkillType.Speech?speechSkill:scholarSkill)}, "
            + $"mod={(type==SkillType.Speech?curr.speechModifierPercent:curr.scholarModifierPercent)}%, "
            + $"challenge={curr.challengeLevel}) | hasBoost={hasBoost}");

            bool success = UnityEngine.Random.value * 100f < chance;

            DisableQuestionButtons();

            ShowSkillFeedback(success);
            curr.ShowSkillResult(success);

            dialogueUI.onDialogueEnd = () => {
                curr.RevertPortrait();
                ContinueQuestionFlow(optionIndex);
            };

            questionButtons[optionIndex].interactable = false;
            skillCheckDisabled[optionIndex] = true;
            curr.TriggerSkillResponse(currentClaimIndex, currentNodeIndex, optionIndex, success);
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
            if(hasBeenInteracted)
            {
                skillCheckButton.interactable = false;
            }

            return;
        }

        // 3) move to next or stay
        int next = node.nextNodes[optionIndex];
        if (next >= 0)
            currentNodeIndex = next;

        dialogueUI.onDialogueEnd = ShowQuestionOptions;
    }


    private void ContinueQuestionFlow(int optionIndex)
    {
        EnableQuestionButtons();
        var node = politicians[currentIndex].questionTrees[currentClaimIndex].nodes[currentNodeIndex];
        if (node.isExitNode && optionIndex == 0)
        {
            questionOptionsPanel.SetActive(false);
            dialogueOptionsPanel.SetActive(true);
            SetMainButtons(true);
            disagreeButton.interactable = false;
            return;
        }
        int next = node.nextNodes[optionIndex];
        if (next >= 0) currentNodeIndex = next;
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
            hasBeenInteracted = false;
        };
        curr.TriggerAgreeDialogue(currentClaimIndex);
    }

    private void OnQuestion()
    {
        var curr = politicians[currentIndex];
        SetMainButtons(false);
        if (currentClaimIndex >=0)
            curr.claimUnlocked[currentClaimIndex] = false;

        currentNodeIndex = 0;
        dialogueUI.onDialogueEnd = ShowQuestionOptions;
        curr.TriggerQuestionDialogue(currentClaimIndex, currentNodeIndex);
    }

    private void SkillCheckButtonDisabler()
    {
        skillCheckButton.interactable = false;
    }

    private void OnSkillCheck()
    {
        if (!energySystem.TryUseHardSkill())
            return;

        var curr = politicians[currentIndex];
        Debug.Log("Skill Check is true");
        hasBeenInteracted = true;
        ButtonDisabler();
        SkillCheckButtonDisabler();

        // 1) compute raw chance
        float rawChance = GetSkillChance(curr.hardSkillType);

        // 2) apply the hard‐check penalty
        float penalizedChance = Mathf.Clamp(rawChance - 20f, 0f, 100f);


        if (hasBoost)
        {
            if (penalizedChance < 50f)
            {
                penalizedChance += 30f;
            }
            else
            {
                penalizedChance += 15f;
            }
            penalizedChance = Mathf.Clamp(penalizedChance, 0f, 100f);
        }

        // DEBUG log for main skill-check
        Debug.LogError(
            $"[MainSkillCheck] type={curr.hardSkillType} | raw={rawChance:0.00}% | penalized={penalizedChance:0.00}% | hasBoost={hasBoost}");

        // 3) roll against penalized chance
        bool success = UnityEngine.Random.value * 100f < penalizedChance;

        // show basic feedback
        ShowSkillFeedback(success);
        curr.ShowSkillResult(success);

        // only apply debuff if not boosted
        if (!success && !hasDebuff && !hasBoost)
        {
            originalSpeechSkill = speechSkill;
            originalScholarSkill = scholarSkill;
            speechSkill = Mathf.RoundToInt(speechSkill * (1f - curr.failureDebuffSpeechPercent / 100f));
            scholarSkill = Mathf.RoundToInt(scholarSkill * (1f - curr.failureDebuffScholarPercent / 100f));
            hasDebuff = true;
            StartCoroutine(ApplyDebuffEffects());
        }

        // restore UI and portrait after dialogue
        dialogueUI.onDialogueEnd = () => {
            ButtonActivator();
            curr.RevertPortrait();
        };

        // trigger appropriate dialogue
        if (success)
            curr.TriggerSkillSuccessDialogue(currentClaimIndex);
        else
            curr.TriggerSkillFailDialogue(currentClaimIndex);
    }


    private IEnumerator ApplyDebuffEffects()
    {
        float elapsed = 0f;
        float duration = 2f;
        // disable vignette rounding immediately
        if (vignette != null) vignette.rounded.value = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (chromatic != null) chromatic.intensity.value = Mathf.Lerp(0f, 1f, t);
            if (vignette != null) vignette.intensity.value = Mathf.Lerp(0.212f, 0.556f, t);
            yield return null;
        }

        // ensure final values
        if (chromatic != null) chromatic.intensity.value = 1f;
        if (vignette != null) vignette.intensity.value = 0.556f;
    }


    private IEnumerator ClearDebuffEffects()
    {
        float elapsed = 0f;
        float duration = 5f;

        float startVig = vignette != null ? vignette.intensity.value : 0.556f;
        float startChrom = chromatic != null ? chromatic.intensity.value : 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (chromatic != null) chromatic.intensity.value = Mathf.Lerp(startChrom, 0f, t);
            if (vignette != null) vignette.intensity.value = Mathf.Lerp(startVig, 0.212f, t);
            yield return null;
        }

        // restore defaults
        if (chromatic != null) chromatic.intensity.value = 0f;
        if (vignette != null)
        {
            vignette.intensity.value = 0.212f;
            vignette.rounded.value = true;
        }

        // restore stats
        speechSkill = baseSpeechSkill;
        scholarSkill = baseScholarSkill;

        hasDebuff = false;
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
        energySystem.AddEnergy(2);
        if (hasDebuff)
            StartCoroutine(ClearDebuffEffects());
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
        energySystem.AddEnergy(2);
        if (hasDebuff)
            StartCoroutine(ClearDebuffEffects());
    }


    private float GetSkillChance(SkillType type)
    {
        // base player stat
        int baseStat = (type == SkillType.Speech) ? speechSkill : scholarSkill;
        // grab NPC‐side modifier
        var npc = politicians[currentIndex];
        float modPercent = (type == SkillType.Speech)
            ? npc.speechModifierPercent
            : npc.scholarModifierPercent;
        // apply % buff/debuff
        float effectiveSkill = baseStat * (1f + modPercent / 100f);
        // challenge scales difficulty
        float challenge = npc.challengeLevel;
        // formula → more skill vs. challenge means higher chance
        float chance = effectiveSkill / (effectiveSkill + challenge) * 100f;
        return Mathf.Clamp(chance, 0f, 100f);
    }


    public void ApplyBottleBoost(float boostPct, float duration)
    {
        // Clear any debuff effects first
        if (hasDebuff)
            StartCoroutine(ClearDebuffEffects());

        // Set modifiers (override previous bottle)
        hasBoost = true;
        bottleModPercent = boostPct; // Replace += with = to prevent stacking
        currentBottleBoostPct = boostPct;
        RecalculateStats();

        // Handle boost timer
        if (boostTimerCoroutine != null)
        {
            StopCoroutine(boostTimerCoroutine);
        }
        boostTimerCoroutine = StartCoroutine(BoostTimer(duration));
    }

    private IEnumerator BoostTimer(float duration)
    {
        boostRemainingTime = duration;
        while (boostRemainingTime > 0f)
        {
            boostRemainingTime -= Time.deltaTime;
            yield return null;
        }

        // Reset modifiers when expired
        bottleModPercent = 0f;
        currentBottleBoostPct = 0f;
        hasBoost = false;
        boostTimerCoroutine = null;
        RecalculateStats();
    }

    private IEnumerator ClearBottleBoostAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        // restore stats
        speechSkill = boostOriginalSpeech;
        scholarSkill = boostOriginalScholar;
        hasBoost = false;
    }



    private void RecalculateStats()
    {
        float speechTotal = npcSpeechModPercent + bottleModPercent;
        float scholarTotal = npcScholarModPercent + bottleModPercent;
        speechSkill  = Mathf.RoundToInt(baseSpeechSkill  * (1f + speechTotal  / 100f));
        scholarSkill = Mathf.RoundToInt(baseScholarSkill * (1f + scholarTotal / 100f));

        // Notify subscribers that stats have updated
        OnStatsUpdated?.Invoke();
    }

    private IEnumerator FadeTransition()
    {
        yield return StartCoroutine(FadeIn());
        politicians[currentIndex].gameObject.SetActive(false);
        currentIndex++;
        if (currentIndex < politicians.Length)
        {
            politicians[currentIndex].gameObject.SetActive(true);
            ApplyCurrentPoliticianMods(); // Add this line
        }
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
 


