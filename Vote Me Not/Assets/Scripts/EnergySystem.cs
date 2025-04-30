using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EnergySystem : MonoBehaviour
{
    [Header("Energy Settings")]
    public int maxEnergy = 10;
    [SerializeField] private int currentEnergy;

    [Header("UI References")]
    public Image energyImage;
    public TMP_Text energyText;

    private Coroutine blinkCoroutine;

    private readonly Color white     = new Color(1f, 1f, 1f);          // FFFFFF
    private readonly Color orange    = new Color(1f, 0.75294f, 0.5098f); // FFC092
    private readonly Color red       = new Color(1f, 0.45882f, 0.50196f); // FF7580
    private readonly Color grey      = new Color(0.64314f, 0.64314f, 0.64314f); // A4A4A4

    void Start()
    {
        currentEnergy = maxEnergy;
        UpdateDisplay();
    }

    public bool TryUseHardSkill()
    {
        if (currentEnergy < 3)
        {
            Debug.LogWarning("Not enough energy for hard skill check");
            return false;
        }
        currentEnergy -= 3;
        UpdateDisplay();
        return true;
    }

    public bool TryUseDNTSkill()
    {
        if (currentEnergy < 1)
        {
            Debug.LogWarning("Not enough energy for DNT skill check");
            return false;
        }
        currentEnergy -= 1;
        UpdateDisplay();
        return true;
    }

    public void AddEnergy(int amount)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (energyText) energyText.text = currentEnergy.ToString();

        if (currentEnergy >= 7)        energyImage.color = white;
        else if (currentEnergy >= 4)   energyImage.color = orange;
        else if (currentEnergy >= 1)   energyImage.color = red;
        else                            energyImage.color = grey;

        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        if (currentEnergy >= 1 && currentEnergy <= 3)
            blinkCoroutine = StartCoroutine(BlinkEnergy());
        else
            energyImage.enabled = true;
    }

    private IEnumerator BlinkEnergy()
    {
        while (true)
        {
            energyImage.enabled = !energyImage.enabled;
            yield return new WaitForSeconds(0.5f);
        }
    }
}