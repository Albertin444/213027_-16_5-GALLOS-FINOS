using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Function_scene : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private SceneAsset sceneToLoad; // Arrastras la escena aquí en el editor
#endif

    private string sceneName;

    private void Awake()
    {
#if UNITY_EDITOR
        if (sceneToLoad != null)
        {
            sceneName = sceneToLoad.name; // Guarda el nombre real de la escena
        }
#endif
    }

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
