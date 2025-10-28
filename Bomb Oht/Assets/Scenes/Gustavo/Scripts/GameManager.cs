using UnityEngine;
using UnityEngine.UI;
using System;
#if TMP_PRESENT
using TMPro;
#endif

/// <summary>
/// GameManager: controla el tiempo de la ronda y decide el perdedor
/// (quien tenga la bomba cuando el tiempo llegue a 0).
/// Se integra con BombManager.Instance para obtener el dueño actual.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Ronda")]
    [Tooltip("Duración de la ronda en segundos")]
    public float roundDuration = 30f;

    [Header("UI (opcional)")]
    [Tooltip("Componente Text para mostrar el temporizador (opcional)")]
    public Text roundTimerText;

#if TMP_PRESENT
    [Tooltip("Componente TMP_Text para mostrar el temporizador (opcional)")]
    public TMP_Text roundTimerTMP;
#endif

    [Header("Opciones")]
    [Tooltip("Si true la ronda empieza automáticamente en Start()")]
    public bool startAutomatically = true;

    // Evento que notifica el fin de la ronda y entrega el GameObject poseedor (puede ser null)
    public event Action<GameObject> onRoundEnded;

    private float timer;
    private bool roundRunning = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        timer = roundDuration;
        UpdateTimerUI();

        if (startAutomatically)
            StartRound();
    }

    void Update()
    {
        if (!roundRunning) return;

        timer -= Time.unscaledDeltaTime; // usar unscaled por si se pausa el timeScale
        if (timer <= 0f)
        {
            timer = 0f;
            EndRound();
        }

        UpdateTimerUI();
    }

    /// <summary>
    /// Inicia la ronda (resetea timer y reanuda timeScale)
    /// </summary>
    public void StartRound()
    {
        timer = roundDuration;
        roundRunning = true;
        Time.timeScale = 1f; // asegurar que el juego corra
        UpdateTimerUI();
    }

    /// <summary>
    /// Pausa la ronda sin declarar ganador (útil para menús).
    /// </summary>
    public void PauseRound()
    {
        roundRunning = false;
    }

    /// <summary>
    /// Reanuda la ronda (tras PauseRound).
    /// </summary>
    public void ResumeRound()
    {
        roundRunning = true;
    }

    /// <summary>
    /// Finaliza la ronda, determina el perdedor (quien tenga la bomba) y lanza evento.
    /// </summary>
    public void EndRound()
    {
        roundRunning = false;
        Time.timeScale = 0f;

        GameObject loser = null;
        if (BombManager.Instance != null)
            loser = BombManager.Instance.currentOwner;

        if (loser != null)
            BombManager.Instance.currentOwner = null; // 🔒 Limpia referencia antes de destruir


        // Notificar por log y por el evento
        if (loser == null)
        {
            Debug.Log("[GameManager] Ronda terminada: nadie tenía la bomba. (Empate?)");
        }
        else
        {
            if (loser.CompareTag("Player"))
                Debug.Log("[GameManager] Ronda terminada: ¡El Player perdió por tener la bomba!");
            else if (loser.CompareTag("NPC"))
                Debug.Log($"[GameManager] Ronda terminada: ¡{loser.name} perdió por tener la bomba!");
            else
                Debug.Log("[GameManager] Ronda terminada: dueño desconocido perdió.");
        }

        // Llamar a subscriptores
        onRoundEnded?.Invoke(loser);
    }

    /// <summary>
    /// Reinicia la ronda: resetea timer y reposiciona la bomba (si BombManager lo permite).
    /// </summary>
    public void RestartRound()
    {
        // Reactivar el timeScale si estaba a 0
        Time.timeScale = 1f;

        // Intentar reasignar dueño aleatorio si hay BombManager
        if (BombManager.Instance != null)
        {
            // Si BombManager tiene un método público AssignRandomOwner lo usaremos.
            // En caso de que tu BombManager tenga otro nombre, cámbialo aquí.
            BombManager.Instance.AssignRandomOwner();
        }

        // Reiniciar timer y bandera
        timer = roundDuration;
        roundRunning = true;
        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        // Formato mm:ss
        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);
        string text = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (roundTimerText != null)
            roundTimerText.text = text;

#if TMP_PRESENT
        if (roundTimerTMP != null)
            roundTimerTMP.text = text;
#endif
    }

    /// <summary>
    /// Devuelve el tiempo restante (lectura).
    /// </summary>
    public float GetTimeRemaining()
    {
        return Mathf.Max(0f, timer);
    }

    /// <summary>
    /// Indica si la ronda está corriendo.
    /// </summary>
    public bool IsRoundRunning()
    {
        return roundRunning;
    }
}
