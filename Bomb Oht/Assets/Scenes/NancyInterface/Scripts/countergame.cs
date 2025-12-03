using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//A class called CounterGame is created. It inherits from MonoBehaviour.
public class countergame : MonoBehaviour
{
    //maximumtime defines the total time the timer lasts.
    //slider refers to the UI Slider component that will display the progress of the time.
    //currenttime stores the current time as it counts down.
    //activatedtime indicates whether the timer is active (true) or paused (false).
    [SerializeField] private float maximumtime;
    [SerializeField] private Slider slider; 
    private float currenttime;
    private bool activatedtime = false;

    [Header("Referencia del tiempo del juego")]
    public Game_Time gameTimeScript;

    //Call the activatetimer() method to start the timer from the beginning
    private void Start()
    {
        maximumtime=gameTimeScript.GameTimeCompleted;
        activatetimer();
    }
    public void Resetcontador()
    {
        maximumtime = gameTimeScript.GameTimeCompleted;
        activatetimer();
    }

    //Only update the timer if activatedtime == true, preventing it from continuing to run when stopped.
    private void Update()
    {
        if (activatedtime)
        {
            changecounter();
        }
    }

    private void changecounter()
    {
        //Subtract from the current time the number of seconds elapsed since the last frame (so that the count is real and constant).
        currenttime -= Time.deltaTime;

        //While time remains, update the Slider value to visually display the remaining time.
        if (currenttime >= 0)
        {
            slider.value = currenttime;
        }

        //When it reaches 0, it displays "Defeat" on the console and stops the timer.
        if (currenttime <= 0)
        {
            Debug.Log("Defeat");
            activatedtime = false;
        }
    }

    //Changes the timer state (active or stopped) based on the state parameter
    private void changetimer(bool estado)
    {
        activatedtime = estado;
    }

    //Reset the timer
    public void activatetimer()
    {
        currenttime = maximumtime;
        slider.maxValue = maximumtime;
        changetimer(true);
    }

    //Stops the counter, without resetting it
    public void deactivatetimer()
    {
        changetimer(false);

    }

    //Public method that pauses the game (can be called from a button).
    public void pause()
    {
        //Stops the game time (everything freezes).
        Time.timeScale = 0f;
    }

    //Method that resumes the game.
    public void resume()
    {
        //Reactivates the normal flow of time.
        Time.timeScale = 1f;

    }
    //Method to reset the character selection panel (does not reload the scene).
    public void restart()
    {
        Debug.Log("? COUNTERGAME RESTART ejecutado | maxTime = ");
        //Resets time.
        Time.timeScale = 1f;
        // Restart the timer.

        activatetimer();
    }

}
