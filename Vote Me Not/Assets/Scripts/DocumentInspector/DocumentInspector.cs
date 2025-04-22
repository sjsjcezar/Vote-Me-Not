using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DocumentInspector : MonoBehaviour
{
    [SerializeField] private GameObject documentPanel;

    // Assign this to your UI Drawer Button OnClick
    public void ShowDocumentPanel()
    {
        documentPanel.SetActive(true);
    }

    // Assign this to your Back button OnClick
    public void HideDocumentPanel()
    {
        documentPanel.SetActive(false);
    }
}
