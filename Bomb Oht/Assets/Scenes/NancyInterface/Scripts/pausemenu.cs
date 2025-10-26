using UnityEngine;
using UnityEngine.SceneManagement;

//Create a class called pausemenu that inherits from MonoBehaviour
public class pausemenu : MonoBehaviour
{
    //Declare three GameObject objects, visible in the Inspector:
    //pausebutton: The button that pauses the game.
    //menupause: The pause menu panel.
    //CharacterSelect: The panel where the player selects a character.
    [SerializeField] private GameObject pausebutton;
    [SerializeField] private GameObject menupause;
    [SerializeField] private GameObject CharacterSelect;
    [SerializeField] private countergame timer;

    //Public method that pauses the game (can be called from a button).
    public void pause()
    {
        //Stops the game time (everything freezes).
        Time.timeScale = 0f;
        //Hides the pause button and shows the pause menu.
        pausebutton.SetActive(false);
        menupause.SetActive(true);
    }

    //Method that resumes the game.
    public void resume()
    {
        //Reactivates the normal flow of time.
        Time.timeScale = 1f;
        //Shows the pause button again and hides the pause menu.
        pausebutton.SetActive(true);
        menupause.SetActive(false);
    }

    //Method to reset the character selection panel (does not reload the scene).
    public void restart()
    {
        //Resets time.
        //Turns the character selection panel on and off to reset it.
        //Hides the pause menu.
        //Re - displays the pause button.
        Time.timeScale = 1f;
        CharacterSelect.SetActive(false);
        CharacterSelect.SetActive(true);
        menupause.SetActive(false);
        pausebutton.SetActive(true);

        // Restart the timer.
        if (timer != null)
        {
            timer.activatetimer();
        }
    }

    //Exit method
    public void exit()
    {
        //Resets normal time and reloads the current scene from scratch.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
