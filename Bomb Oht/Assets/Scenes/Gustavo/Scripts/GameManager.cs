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

    public GameObject explosionPrefab;
    public GameObject monstruoPrefab;

    [Header("Tiempo global")]
    public Game_Time gameTime;
    public GameObject bomb; // referencia a la bomba actual
    public AudioSource Convertision_a_moutnro; 


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (gameTime == null)
            gameTime = FindObjectOfType<Game_Time>();

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
        Time.timeScale = 1f;

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

        
        SpawnExplosionAndMonster(loser);

        // Reiniciar el tiempo
        // Reiniciar timer interno del GameManager
        timer = roundDuration;
        roundRunning = true;

        // 🔥 Reiniciar tiempo del sistema global Game_Time
        if (gameTime != null)
        {
            gameTime.GameTime = gameTime.GameTimeCompleted;
            gameTime.ResumeTimer();
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
    void SpawnExplosionAndMonster(GameObject loser)
    {
        if (loser == null) return;

        Vector3 pos = loser.transform.position;

        // Crear explosión
        Instantiate(explosionPrefab, pos, Quaternion.identity);

        // Crear monstruo
        Instantiate(monstruoPrefab, pos, Quaternion.identity);
        // 3. Obtener bomba real desde BombManager
        GameObject bombaActual = BombManager.Instance.bomb.gameObject;


        Convertision_a_moutnro.Play();
        // 4. Soltar la bomba
        if (bombaActual != null)
            bombaActual.transform.parent = null;

        // Destruir al perdedor
        Destroy(loser);

        // 🔥 Asignar nuevo dueño de la bomba
        if (BombManager.Instance != null)
        {
            BombManager.Instance.AssignRandomOwner();
        }
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