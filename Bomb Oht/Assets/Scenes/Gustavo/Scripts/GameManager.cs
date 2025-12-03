using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

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

    public countergame cOuntergame;
    public AudioSource Conversión_mounter;
    public ControladorJuego controladorJuego;


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
 

        timer = roundDuration;
        roundRunning = true;

        // Primero restablece el tiempo global
        gameTime.GameTime = gameTime.GameTimeCompleted;
        gameTime.ResumeTimer();

        // Ahora sí reinicia el contador visual
        cOuntergame.restart();
        Conversión_mounter.Play();  

        if (BombManager.Instance != null)
            BombManager.Instance.AssignRandomOwner();

        UpdateTimerUI();


    }


    void SpawnExplosionAndMonster(GameObject loser)
    {
        if (loser == null) return;

        Vector3 pos = loser.transform.position;

        Instantiate(explosionPrefab, pos, Quaternion.identity);
        Instantiate(monstruoPrefab, pos, Quaternion.identity);

        // obtener referencia segura a la bomba actual
        GameObject bombaActual = BombManager.Instance?.bomb?.gameObject;
        if (bombaActual != null)
        {
            // 1) asegurarnos que la bomba deje de ser hija del perdedor AHORA
            bombaActual.transform.SetParent(null, true);

            // 2) moverla a una posición segura (evita que esté dentro del cuerpo)
            Vector3 safePos = loser.transform.position + Vector3.up * 2f;
            bombaActual.transform.position = safePos;

            // 3) marcar bomba como en el suelo / libre
            var bombScript = BombManager.Instance.bomb;
            if (bombScript != null)
                bombScript.currentCondition = BombScript.Condition.OnFloor;
        }

        // 4) quitar dueño actual en el manager (ya no hay owner)
        BombManager.Instance.ClearOwner();

        // 5) arrancar la corrutina que reasigna y destruye (pasándole la referencia a la bomba)
        StartCoroutine(DestroyAndReassign(loser, bombaActual));
    }

    IEnumerator DestroyAndReassign(GameObject loser, GameObject bombaActual)
    {
        // espera un frame para garantizar que Unity procesó SetParent(null)
        yield return null;

        // reasegurar que la bomba no sea hija; (protección extra)
        if (bombaActual != null)
        {
            bombaActual.transform.SetParent(null, true);

            // si tienes Collider, podrías temporalmente ignorar colisión con el loser
            Collider bombCol = bombaActual.GetComponent<Collider>();
            Collider loserCol = loser.GetComponent<Collider>();
            if (bombCol != null && loserCol != null)
                Physics.IgnoreCollision(bombCol, loserCol, true);
        }

        // 1) reasignar bomba a un nuevo dueño que NO sea el perdedor
        BombManager.Instance.AssignRandomOwner(loser);

        // 2) desactivar el CharacterController del perdedor para evitar que siga recogiendo o colisionando
        CharacterController cc = loser.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        // 3) opcional: desactivar también colliders del perdedor (si quieres máxima seguridad)
        Collider[] cols = loser.GetComponentsInChildren<Collider>();
        foreach (var c in cols)
            c.enabled = false;

        // 4) si antes ignoramos la colisión, reactívala (después de un frame adicional)
        yield return null;
        if (bombaActual != null)
        {
            Collider bombCol2 = bombaActual.GetComponent<Collider>();
            Collider loserCol2 = loser.GetComponent<Collider>();
            if (bombCol2 != null && loserCol2 != null)
                Physics.IgnoreCollision(bombCol2, loserCol2, false);
        }

        // 5) finalmente destruir al perdedor
controladorJuego.EliminarDeLista(loser);
        Destroy(loser);
        

        // 6) reiniciar la ronda
        RestartRound();
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