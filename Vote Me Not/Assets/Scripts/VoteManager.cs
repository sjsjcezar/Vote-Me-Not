/* Authored by: Samuel Cezar
Company: Company Name
Project: Project Name
Feature: [NXR-002] Accept Reject System Feature
Description: If certain codes are being called here, it is because they are part of the Accept Reject System Feature.
 */


using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    private Image fadeImage;
    private bool isTransitioning = false;

    void Start()
    {
        fadeImage = fadePanel.GetComponent<Image>();
        if (fadeImage != null)
        {
            Color tempColor = fadeImage.color;
            tempColor.a = 0f;
            fadeImage.color = tempColor;
        }
        fadePanel.SetActive(false);

        for (int i = 0; i < politicians.Length; i++)
        {
            politicians[i].gameObject.SetActive(i == currentIndex);
        }
        Debug.Log("Game Started. Initial Ethics Meter: " + ethicsMeter);
    }

    public void OnAccept()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (currentIndex < politicians.Length)
        {
            Politician currentPolitician = politicians[currentIndex];
            Debug.Log("Accepted politician with affiliation: " + currentPolitician.affiliation);

            switch (currentPolitician.affiliation)
            {
                case AffiliationGlobalEnum.Good:
                    ethicsMeter += updateAmountGood;
                    Debug.Log("Good politician accepted. Ethics meter increased by " 
                        + updateAmountGood + " to " + ethicsMeter);
                    break;
                case AffiliationGlobalEnum.Neutral:
                    Debug.Log("Neutral politician accepted. Ethics meter remains " + ethicsMeter);
                    break;
                case AffiliationGlobalEnum.Evil:
                    ethicsMeter -= updateAmountEvil;
                    Debug.Log("Evil politician accepted. Ethics meter decreased by " 
                        + updateAmountEvil + " to " + ethicsMeter);
                    break;
            }
            if (ethicsMeterController != null)
            {
                ethicsMeterController.UpdateEthics(ethicsMeter);
            }
            StartCoroutine(FadeTransition());
        }
    }

    public void OnReject()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (currentIndex < politicians.Length)
        {
            Politician currentPolitician = politicians[currentIndex];
            Debug.Log("Politician rejected with affiliation: " + currentPolitician.affiliation);

            switch (currentPolitician.affiliation)
            {
                case AffiliationGlobalEnum.Good:
                    int penalty = Mathf.RoundToInt(updateAmountEvil * 0.25f);
                    ethicsMeter -= penalty;
                    Debug.Log("Good politician declined. Ethics meter decreased by " 
                        + penalty + " to " + ethicsMeter);
                    break;
                case AffiliationGlobalEnum.Neutral:
                    Debug.Log("Neutral politician declined. Ethics meter remains " + ethicsMeter);
                    break;
                case AffiliationGlobalEnum.Evil:
                    ethicsMeter += updateAmountGood;
                    Debug.Log("Evil politician declined. Ethics meter increased by " 
                        + updateAmountGood + " to " + ethicsMeter);
                    break;
            }
            if (ethicsMeterController != null)
            {
                ethicsMeterController.UpdateEthics(ethicsMeter);
            }
            StartCoroutine(FadeTransition());
        }
    }

    IEnumerator FadeTransition()
    {
        Debug.Log("Fade In started.");
        yield return StartCoroutine(FadeIn());

        Debug.Log("Transitioning to next politician at full opacity.");
        politicians[currentIndex].gameObject.SetActive(false);
        currentIndex++;
        if (currentIndex < politicians.Length)
        {
            politicians[currentIndex].gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("No more politicians to display.");
        }

        Debug.Log("Fade Out started.");
        yield return StartCoroutine(FadeOut());
        Debug.Log("Fade transition complete.");

        isTransitioning = false;
    }

    IEnumerator FadeIn()
    {
        fadePanel.SetActive(true);
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            if (fadeImage != null)
            {
                Color tempColor = fadeImage.color;
                tempColor.a = alpha;
                fadeImage.color = tempColor;
            }
            yield return null;
        }
        if (fadeImage != null)
        {
            Color tempColor = fadeImage.color;
            tempColor.a = 1f;
            fadeImage.color = tempColor;
        }
    }

    IEnumerator FadeOut()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            if (fadeImage != null)
            {
                Color tempColor = fadeImage.color;
                tempColor.a = alpha;
                fadeImage.color = tempColor;
            }
            yield return null;
        }
        if (fadeImage != null)
        {
            Color tempColor = fadeImage.color;
            tempColor.a = 0f;
            fadeImage.color = tempColor;
        }
        fadePanel.SetActive(false);
    }
}
