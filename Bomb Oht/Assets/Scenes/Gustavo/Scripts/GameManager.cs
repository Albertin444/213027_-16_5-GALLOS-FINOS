using UnityEngine;
using UnityEngine.UI;
using System;
#if TMP_PRESENT
using TMPro;
#endif

/// <summary>
/// GameManager: controla el tiempo de la ronda y decide el perdedor.
/// Si la bomba está en el suelo, pierde el último dueño registrado.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Ronda")]
    [Tooltip("Duración de la ronda en segundos")]
    public float roundDuration = 30f;

    [Header("UI (opcional)")]
    public Text roundTimerText;
#if TMP_PRESENT
    public TMP_Text roundTimerTMP;
#endif

    [Header("Opciones")]
    public bool startAutomatically = true;

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

        timer -= Time.unscaledDeltaTime;
        if (timer <= 0f)
        {
            timer = 0f;
            EndRound();
        }

        UpdateTimerUI();
    }

    public void StartRound()
    {
        timer = roundDuration;
        roundRunning = true;
        Time.timeScale = 1f;
        UpdateTimerUI();
    }

    public void PauseRound() => roundRunning = false;
    public void ResumeRound() => roundRunning = true;

    public void EndRound()
    {
        roundRunning = false;
        Time.timeScale = 0f;

        GameObject loser = BombManager.Instance?.currentOwner;

        // ✅ Si no hay dueño actual, usar el último dueño registrado
        if (loser == null)
            loser = BombManager.Instance?.lastOwner;

        BombManager.Instance?.ClearOwner();

        if (loser == null)
        {
            Debug.Log("[GameManager] Ronda terminada: error, no se pudo determinar perdedor.");
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

        onRoundEnded?.Invoke(loser);
    }

    public void RestartRound()
    {
        Time.timeScale = 1f;

        if (BombManager.Instance != null)
            BombManager.Instance.AssignRandomOwner();

        timer = roundDuration;
        roundRunning = true;
        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
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

    public float GetTimeRemaining() => Mathf.Max(0f, timer);
    public bool IsRoundRunning() => roundRunning;
}