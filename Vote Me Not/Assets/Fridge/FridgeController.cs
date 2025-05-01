using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FridgeController : MonoBehaviour
{
    [Header("Fridge Sprites")]
    [SerializeField] private SpriteRenderer fridgeRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openFourSprite;
    [SerializeField] private Sprite openThreeSprite;
    [SerializeField] private Sprite openTwoSprite;
    [SerializeField] private Sprite openOneSprite;
    [SerializeField] private Sprite openNoneSprite;

    [Header("Buttons")]
    [SerializeField] private Button openFridgeButton;
    [SerializeField] private Button closeFridgeButton;
    [SerializeField] private Button getBottleButton;

    [Header("Consume UI Panel")]
    [SerializeField] private GameObject consumePanel;
    [SerializeField] private Button consumeButton;
    [SerializeField] private Button backButton;

    [Header("Timer UI")]
    [SerializeField] private TMP_Text timerText;

    [Header("Bottle Settings")]
    [Tooltip("Number of bottles initially available")] public int bottleCount = 4;
    [Tooltip("Speech & Scholar boost percent (e.g. 35 for +35%)")] public float boostPercent = 35f;
    [Tooltip("Boost duration in seconds")] public float boostDuration = 60f;

    private VoteManager voteManager;

    private void Awake()
    {
        voteManager = FindObjectOfType<VoteManager>();
        // Hook up buttons
        openFridgeButton.onClick.AddListener(OpenFridge);
        closeFridgeButton.onClick.AddListener(CloseFridge);
        getBottleButton.onClick.AddListener(ShowConsumePanel);
        consumeButton.onClick.AddListener(ConsumeBottle);
        backButton.onClick.AddListener(HideConsumePanel);
    }

    private void Start()
    {
        // Initial state: closed
        fridgeRenderer.sprite = closedSprite;
        openFridgeButton.gameObject.SetActive(true);
        closeFridgeButton.gameObject.SetActive(false);
        getBottleButton.gameObject.SetActive(false);
        consumePanel.SetActive(false);
        timerText.gameObject.SetActive(false);
    }

    private void OpenFridge()
    {
        // Show open state and controls
        UpdateFridgeSprite();
        openFridgeButton.gameObject.SetActive(false);
        closeFridgeButton.gameObject.SetActive(true);
        getBottleButton.gameObject.SetActive(true);
    }

    private void CloseFridge()
    {
        // Revert to closed state
        fridgeRenderer.sprite = closedSprite;
        openFridgeButton.gameObject.SetActive(true);
        closeFridgeButton.gameObject.SetActive(false);
        getBottleButton.gameObject.SetActive(false);
        consumePanel.SetActive(false);
    }

    private void ShowConsumePanel()
    {
        consumePanel.SetActive(true);
    }

    private void HideConsumePanel()
    {
        consumePanel.SetActive(false);
    }

    private void ConsumeBottle()
    {
        if (bottleCount <= 0) return;

        // Update bottle count and fridge sprite
        bottleCount--;
        UpdateFridgeSprite();
        consumePanel.SetActive(false);

        // Disable further get if empty
        if (bottleCount == 0)
            getBottleButton.gameObject.SetActive(false);

        // Apply effects
        if (voteManager != null)
        {
            // clear inflicted debuff if any and apply boost
            voteManager.ApplyBottleBoost(boostPercent, boostDuration);
        }

        // Start timer display
        StartCoroutine(ShowTimer(boostDuration));
    }

    private void UpdateFridgeSprite()
    {
        switch (bottleCount)
        {
            case 4: fridgeRenderer.sprite = openFourSprite; break;
            case 3: fridgeRenderer.sprite = openThreeSprite; break;
            case 2: fridgeRenderer.sprite = openTwoSprite; break;
            case 1: fridgeRenderer.sprite = openOneSprite; break;
            default: fridgeRenderer.sprite = openNoneSprite; break;
        }
    }

    private IEnumerator ShowTimer(float duration)
    {
        timerText.gameObject.SetActive(true);
        float remaining = duration;
        while (remaining > 0f)
        {
            timerText.text = Mathf.CeilToInt(remaining).ToString() + "s";
            remaining -= Time.deltaTime;
            yield return null;
        }
        timerText.gameObject.SetActive(false);
    }
}
