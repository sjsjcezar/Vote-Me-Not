using UnityEngine;

public class DrawerController : MonoBehaviour
{
    [SerializeField] private GameObject panelDrawer;

    // Assign this to your UI Drawer Button OnClick
    public void ShowPanelDrawer()
    {
        panelDrawer.SetActive(true);
    }

    // Assign this to your Back button OnClick
    public void HidePanelDrawer()
    {
        panelDrawer.SetActive(false);
    }
}
