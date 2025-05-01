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
        openStatsButton.onClick.AddListener(OpenStatsPanel);
        closeStatsButton.onClick.AddListener(CloseStatsPanel);
        statsPanel.SetActive(false);
    }

    private void Start()
    {
        voteManager = FindObjectOfType<VoteManager>();
        if (voteManager == null)
            Debug.LogError("VoteManager not found in scene.");
        else
            voteManager.OnStatsUpdated += UpdateStatsUI; // Subscribe to the event
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (voteManager != null)
            voteManager.OnStatsUpdated -= UpdateStatsUI;
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
        // Get current politician's modifiers
        float politicianSpeechMod = voteManager.NpcSpeechModPercent;
        float politicianScholarMod = voteManager.NpcScholarModPercent;

        // Get active bottle modifier (0 if inactive)
        float bottleBoost = voteManager.bottleModPercent;

        // Calculate total modifiers
        float totalSpeechMod = politicianSpeechMod + bottleBoost;
        float totalScholarMod = politicianScholarMod + bottleBoost;

        // Update UI texts
        speechModText.text = $"{totalSpeechMod:+#;-#;0}%";
        scholarModText.text = $"{totalScholarMod:+#;-#;0}%";

        // Update effective stats
        speechBaseText.text = voteManager.speechSkill.ToString("0");
        scholarBaseText.text = voteManager.scholarSkill.ToString("0");

        // Color coding
        Color pos = Color.green, neg = Color.red, neu = Color.white;
        speechModText.color = totalSpeechMod > 0 ? pos : (totalSpeechMod < 0 ? neg : neu);
        scholarModText.color = totalScholarMod > 0 ? pos : (totalScholarMod < 0 ? neg : neu);
    }
}
