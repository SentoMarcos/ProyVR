using UnityEngine;
using UnityEngine.UI;

public class PanelSwitcher : MonoBehaviour
{
    [Header("Referencias a los paneles")]
    public GameObject[] panels;  // Lista de paneles (Empty con contenido)

    private GameObject currentPanel;

    void Start()
    {
        // Opcional: activa el primer panel por defecto
        if (panels.Length > 0)
        {
            currentPanel = panels[0];
            ActivatePanel(currentPanel);
        }
    }

    /// <summary>
    /// Llama a este método desde el botón, pasando el panel que quieres activar
    /// </summary>
    /// <param name="panelToActivate"></param>
    public void ActivatePanel(GameObject panelToActivate)
    {
        if (currentPanel != null)
            currentPanel.SetActive(false); // Desactiva panel actual

        panelToActivate.SetActive(true);   // Activa el nuevo panel
        currentPanel = panelToActivate;    // Actualiza el panel actual
    }
}
