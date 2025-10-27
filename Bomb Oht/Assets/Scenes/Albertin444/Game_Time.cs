using UnityEngine;

public class Game_Time : MonoBehaviour
{
    [Header("⏱️ Timer Settings")]
    public float GameTimeCompleted = 30f; // Total time
    public float GameTime = 30f;          // Remaining time

    private float timer = 0f;

    [Header("⏸️ Pause Control")]
    public bool isPaused = false;         // Controls whether the timer is paused

    public int Count_Players =4;
    private void Start()
    {
        // Initialize timer with the total time
        GameTime = GameTimeCompleted;
    }

    private void Update()
    {
        // Only count down if not paused and time is greater than zero
        if (!isPaused && GameTime > 0f)
        {
            timer += Time.deltaTime;

            // Decrease GameTime by 0.1 every 0.1 seconds
            if (timer >= 0.1f)
            {
                GameTime -= 0.1f;
                timer = 0f;
            }
        }
        else if (GameTime <= 0f)
        {
            GameTime = 0f; // Prevent negative numbers
        }
    }

    /// <summary>
    /// Pauses the timer.
    /// </summary>
    public void PauseTimer()
    {
        isPaused = true;
    }

    /// <summary>
    /// Resumes the timer.
    /// </summary>
    public void ResumeTimer()
    {
        isPaused = false;
    }

    /// <summary>
    /// Toggles between pause and unpause.
    /// </summary>
    public void TogglePause()
    {
        isPaused = !isPaused;
    }
}
