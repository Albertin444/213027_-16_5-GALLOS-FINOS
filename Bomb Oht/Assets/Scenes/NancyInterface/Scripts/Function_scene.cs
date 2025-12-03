using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Function_scene : MonoBehaviour
{
    public string sceneName;


    public void GoToScene()//Function that takes you to a scene
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("❌ No se asignó ninguna escena en el inspector.");
        }
    }

    public void ResetGame()
    {
        // Reloads the current active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
