using UnityEngine;
using UnityEngine.UI;

public class EthicsMeterController : MonoBehaviour
{
    [Header("Slider Settings")]
    public Slider ethicsSlider;
    public float minEthics = 0f;
    public float maxEthics = 100f;

    [Header("Thresholds")]
    public float evilThreshold = 40f;
    public float goodThreshold = 60f;

    [Header("Marker")]
    public Marker marker;

    [Header("Smoothing Settings")]
    public float smoothingSpeed = 5f;

    private float targetEthics;
    private float displayEthics;
    private MarkerState lastState = MarkerState.Neutral;

    void Start()
    {
        ethicsSlider.minValue = minEthics;
        ethicsSlider.maxValue = maxEthics;
        
        targetEthics = 50f;
        displayEthics = 50f;
        ethicsSlider.value = displayEthics;
        
        UpdateMarkerState(); 
        Debug.Log("Initial Ethics Meter is in NEUTRAL territory.");
    }

    public void UpdateEthics(float newEthics)
    {
        targetEthics = Mathf.Clamp(newEthics, minEthics, maxEthics);
    }

    void Update()
    {
        if (Mathf.Abs(displayEthics - targetEthics) > 0.01f)
        {
            displayEthics = Mathf.Lerp(displayEthics, targetEthics, Time.deltaTime * smoothingSpeed);
            ethicsSlider.value = displayEthics;
            UpdateMarkerState();
        }
    }

    private void UpdateMarkerState()
    {
        if (displayEthics <= evilThreshold)
        {
            if (lastState != MarkerState.Evil)
            {
                Debug.Log("Ethics Meter is in the EVIL territory.");
                lastState = MarkerState.Evil;
            }
            marker.SetState(MarkerState.Evil);
        }
        else if (displayEthics >= goodThreshold)
        {
            if (lastState != MarkerState.Good)
            {
                Debug.Log("Ethics Meter is in the GOOD territory.");
                lastState = MarkerState.Good;
            }
            marker.SetState(MarkerState.Good);
        }
        else
        {
            if (lastState != MarkerState.Neutral)
            {
                Debug.Log("Ethics Meter is in the NEUTRAL territory.");
                lastState = MarkerState.Neutral;
            }
            marker.SetState(MarkerState.Neutral);
        }
    }
}
