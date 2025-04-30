using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatsPanelToggle : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private Button openStatsButton;
    [SerializeField] private Button closeStatsButton;

    [Header("Stats Text References")]
    [SerializeField] private TMP_Text speechBaseText;
    [SerializeField] private TMP_Text scholarBaseText;
    [SerializeField] private TMP_Text speechModText;
    [SerializeField] private TMP_Text scholarModText;

    private VoteManager voteManager;

    private void Awake()
    {
        // Validate references
        if (statsPanel == null) Debug.LogError("StatsPanel reference is missing on " + name);
        if (openStatsButton == null) Debug.LogError("OpenStatsButton reference is missing on " + name);
        if (closeStatsButton == null) Debug.LogError("CloseStatsButton reference is missing on " + name);
        if (speechBaseText == null) Debug.LogError("SpeechBaseText reference is missing on " + name);
        if (scholarBaseText == null) Debug.LogError("ScholarBaseText reference is missing on " + name);
        if (speechModText == null) Debug.LogError("SpeechModText reference is missing on " + name);
        if (scholarModText == null) Debug.LogError("ScholarModText reference is missing on " + name);

        openStatsButton.onClick.AddListener(OpenStatsPanel);
        closeStatsButton.onClick.AddListener(CloseStatsPanel);
        statsPanel.SetActive(false);
    }

    private void Start()
    {
        voteManager = FindObjectOfType<VoteManager>();
        if (voteManager == null)
            Debug.LogError("VoteManager not found in scene.");
    }

    private void OpenStatsPanel()
    {
        UpdateStatsUI();
        statsPanel.SetActive(true);
    }

    private void CloseStatsPanel()
    {
        statsPanel.SetActive(false);
    }

    private void UpdateStatsUI()
    {
        int baseSpeech = voteManager.speechSkill;
        int baseScholar = voteManager.scholarSkill;

        var currentPolitician = voteManager.politicians[voteManager.currentIndex];
        float speechMod = currentPolitician.speechModifierPercent;
        float scholarMod = currentPolitician.scholarModifierPercent;

        float effectiveSpeech = baseSpeech * (1f + speechMod / 100f);
        float effectiveScholar = baseScholar * (1f + scholarMod / 100f);

        // Determine if there is a modifier
        bool speechHasMod = !Mathf.Approximately(speechMod, 0f);
        bool scholarHasMod = !Mathf.Approximately(scholarMod, 0f);

        // Show effective value in base text if modified, otherwise raw
        speechBaseText.text = (speechHasMod ? effectiveSpeech : baseSpeech).ToString("0");
        scholarBaseText.text = (scholarHasMod ? effectiveScholar : baseScholar).ToString("0");

        // Modifier text shows just the percentage
        speechModText.text = string.Format("{0}{1:0}%", speechMod >= 0 ? "+" : "", speechMod);
        scholarModText.text = string.Format("{0}{1:0}%", scholarMod >= 0 ? "+" : "", scholarMod);

        // Color-code both base and modifier
        Color posColor = Color.green;
        Color negColor = Color.red;
        Color neutralColor = Color.white;

        Color speechColor = speechMod > 0 ? posColor : (speechMod < 0 ? negColor : neutralColor);
        Color scholarColor = scholarMod > 0 ? posColor : (scholarMod < 0 ? negColor : neutralColor);

        speechBaseText.color = speechColor;
        speechModText.color = speechColor;
        scholarBaseText.color = scholarColor;
        scholarModText.color = scholarColor;
    }
}
