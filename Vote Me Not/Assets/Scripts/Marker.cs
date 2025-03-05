using UnityEngine;
using UnityEngine.UI;

public enum MarkerState
{
    Evil,
    Neutral,
    Good
}

public class Marker : MonoBehaviour
{
    [Header("Marker Sprites")]
    public Sprite evilSprite;
    public Sprite neutralSprite;
    public Sprite goodSprite;

    private Image markerImage;

    void Awake()
    {
        markerImage = GetComponent<Image>();
    }

    public void SetState(MarkerState state)
    {
        switch (state)
        {
            case MarkerState.Evil:
                if (evilSprite != null) markerImage.sprite = evilSprite;
                break;
            case MarkerState.Neutral:
                if (neutralSprite != null) markerImage.sprite = neutralSprite;
                break;
            case MarkerState.Good:
                if (goodSprite != null) markerImage.sprite = goodSprite;
                break;
        }
    }
}
