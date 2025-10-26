using UnityEngine;
using UnityEngine.SceneManagement;

public class initialmenu : MonoBehaviour
//A function called "Play" is created, and within the function, the scene is changed.
{
    public void Jugar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void Salir()
    {
        Debug.Log("Salir...");
        Application.Quit();
    }
}
