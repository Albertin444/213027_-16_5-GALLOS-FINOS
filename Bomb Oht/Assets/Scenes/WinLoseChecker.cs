using UnityEngine;
using UnityEngine;

public class WinLoseChecker : MonoBehaviour
{
    [Header("Referencias a los Canvas")]
    public GameObject victoryCanvas;
    public GameObject defeatCanvas;

    [Header("Etiquetas")]
    public string playerTag = "Player";
    public string npcTag = "NPC";
    public AudioSource Victoria;

    private GameObject player;

    void Start()
    {
        // Buscar el player por etiqueta
        player = GameObject.FindGameObjectWithTag(playerTag);

        // Asegurar que los Canvas estén ocultos al inicio
        if (victoryCanvas) victoryCanvas.SetActive(false);
        if (defeatCanvas) defeatCanvas.SetActive(false);
    }

    void Update()
    {
        CheckLoseCondition();
        CheckWinCondition();
    }

    // ---------------------------
    //    PLAYER DESTRUÍDO ? DERROTA
    // ---------------------------
    void CheckLoseCondition()
    {
        if (player == null)   // si ya no existe en la escena
        {
            ShowDefeat();
        }
    }

    // ---------------------------
    //    TODOS LOS NPC MUERTOS ? VICTORIA
    // ---------------------------
    void CheckWinCondition()
    {
        GameObject[] npcs = GameObject.FindGameObjectsWithTag(npcTag);

        if (npcs.Length == 0) // si no queda ninguno
        {
            if (player != null) // player todavía vivo
                ShowVictory();
        }
    }

    // ---------------------------
    void ShowVictory()
    {
        if (victoryCanvas && !victoryCanvas.activeSelf)
            victoryCanvas.SetActive(true);
        Victoria.Play();
    }

    void ShowDefeat()
    {
        if (defeatCanvas && !defeatCanvas.activeSelf)
            defeatCanvas.SetActive(true);
    }
}
