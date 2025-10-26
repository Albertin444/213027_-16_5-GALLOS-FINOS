using UnityEngine;
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

    //Call the activatetimer() method to start the timer from the beginning
    private void Start()
    {
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
}
