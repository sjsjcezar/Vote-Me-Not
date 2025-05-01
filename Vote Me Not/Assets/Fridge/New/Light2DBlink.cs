using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;


[RequireComponent(typeof(Light2D))]
public class Light2DBlink : MonoBehaviour
{
    [Header("Light Reference")]
    [Tooltip("The 2D Light to blink. If null, will use Light2D on this GameObject.")]
    [SerializeField] private Light2D light2D;

    [Header("Radius Settings")]
    [Tooltip("Outer radius when light is off.")]
    [SerializeField] private float offOuterRadius = 0f;
    [Tooltip("Outer radius when light is on (e.g. 0.15).")]
    [SerializeField] private float onOuterRadius = 0.15f;

    [Header("Blink Sequence Settings")]
    [Tooltip("Minimum time to wait between blink sequences.")]
    [SerializeField] private float minInterval = 2f;
    [Tooltip("Maximum time to wait between blink sequences.")]
    [SerializeField] private float maxInterval = 3f;
    [Tooltip("Number of quick blinks per sequence.")]
    [SerializeField] private int blinksPerSequence = 2;

    [Header("Blink Timing")]
    [Tooltip("Duration the light stays on during each blink.")]
    [SerializeField] private float blinkOnDuration = 0.1f;
    [Tooltip("Duration between blinks within a sequence.")]
    [SerializeField] private float blinkOffDuration = 0.1f;

    private Coroutine blinkCoroutine;

    private void Awake()
    {
        if (light2D == null)
            light2D = GetComponent<Light2D>();
        if (light2D == null)
            Debug.LogError("Light2DBlink requires a Light2D component or reference.");
    }

    private void OnEnable()
    {
        if (light2D != null)
        {
            // initialize to off radius
            light2D.pointLightOuterRadius = offOuterRadius;
            blinkCoroutine = StartCoroutine(BlinkLoop());
        }
    }

    private void OnDisable()
    {
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);
        if (light2D != null)
            light2D.pointLightOuterRadius = offOuterRadius;
    }

    private IEnumerator BlinkLoop()
    {
        while (true)
        {
            // random wait before next blink sequence
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            // perform the blink sequence
            for (int i = 0; i < blinksPerSequence; i++)
            {
                // on
                light2D.pointLightOuterRadius = onOuterRadius;
                yield return new WaitForSeconds(blinkOnDuration);

                // off
                light2D.pointLightOuterRadius = offOuterRadius;
                // wait between blinks, except after last
                if (i < blinksPerSequence - 1)
                    yield return new WaitForSeconds(blinkOffDuration);
            }
        }
    }
}
