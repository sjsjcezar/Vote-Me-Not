using UnityEngine;
using TMPro;

public class TMPColorBreathing : MonoBehaviour
{
    [Header("Text Settings")]
    [SerializeField] private TextMeshProUGUI tmpText;

    [Header("Color Settings")]
    [SerializeField] private Color colorA = Color.white;
    [SerializeField] private Color colorB = Color.red;

    [Header("Breathing Settings")]
    [SerializeField] private float breathSpeed = 1f;

    private void Awake()
    {
        if (tmpText == null)
        {
            tmpText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * breathSpeed) + 1f) / 2f;
        tmpText.color = Color.Lerp(colorA, colorB, t);
    }
}
