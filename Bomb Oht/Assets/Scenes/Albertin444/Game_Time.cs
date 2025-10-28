using UnityEngine;

public class Game_Time : MonoBehaviour
{
    [Header("⏱️ Timer Settings")]
    public float GameTimeCompleted = 30f;
    public float GameTime = 30f;
    private float timer = 0f;

    [Header("🧩 References")]
    public ControladorJuego controladorJuego;

    [Header("⏸️ Pause Control")]
    public bool isPaused = false;

    public int Count_Players = 0;

    private void Start()
    {
        GameTime = GameTimeCompleted;

        // Buscar controlador si no se asignó
        if (controladorJuego == null)
        {
            controladorJuego = FindObjectOfType<ControladorJuego>();
        }
    }

    private void Update()
    {
        // Actualiza el conteo de jugadores
        if (controladorJuego != null)
            Count_Players = controladorJuego.personajes.Count;

        // Contador principal
        if (!isPaused && GameTime > 0f)
        {
            timer += Time.deltaTime;

            if (timer >= 0.1f)
            {
                GameTime -= 0.1f;
                timer = 0f;
            }
        }
        else if (GameTime <= 0f)
        {
            GameTime = 0f;
            if (controladorJuego != null)
            {
                // Solo aplicar destrucción si hay más de un jugador
                if (controladorJuego.personajes.Count > 1)
                {
                    controladorJuego.EliminarYAsignarNuevoInfectado();
                }
                else
                {
                    Debug.Log("🏁 Solo queda un jugador, fin del juego.");
                }
            }

            // Reiniciar el tiempo
            GameTime = GameTimeCompleted;
        }
    }

    public void PauseTimer() => isPaused = true;
    public void ResumeTimer() => isPaused = false;
    public void TogglePause() => isPaused = !isPaused;
}
