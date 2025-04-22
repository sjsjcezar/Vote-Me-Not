using UnityEngine;
using UnityEngine.UI;

// Attach to your "file" UI element to unlock a specific claim on click.
public class ClaimUnlocker : MonoBehaviour
{
    [Tooltip("Assign the Politician whose claim you want to unlock.")]
    public Politician politician;

    [Tooltip("0-based index of the claim to unlock when this file is clicked.")]
    public int claimIndex;

    [Tooltip("Button component on the file UI (will hook up OnClick automatically)")]
    public Button fileButton;

    void Start()
    {
        if (fileButton == null)
            fileButton = GetComponent<Button>();

        if (fileButton != null)
            fileButton.onClick.AddListener(OnFileClicked);
        else
            Debug.LogWarning("ClaimUnlocker: No Button found on " + gameObject.name);
    }

    private void OnFileClicked()
    {
        if (politician != null)
        {
            politician.UnlockClaim(claimIndex);
            Debug.Log($"Unlocked Claim {claimIndex + 1} on {politician.npcName}");
        }
        else
        {
            Debug.LogError("ClaimUnlocker: Politician reference not set on " + gameObject.name);
        }
    }
}