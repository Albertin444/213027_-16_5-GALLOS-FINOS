using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelector : MonoBehaviour
{
    [Header("Referencias UI")]
    public Image characterDisplay;          // Imagen que muestra el personaje
    public Text characterNameText;          // Texto del nombre del personaje

    [Header("Personajes disponibles")]
    public Sprite[] characterSprites;       // Sprites de los personajes
    public string[] characterNames;         // Nombres de los personajes

    private int currentIndex = 0;

    void Start()
    {
        // Mostrar el primer personaje al iniciar
        ShowCharacter(currentIndex);
    }

    public void NextCharacter()
    {
        currentIndex++;
        if (currentIndex >= characterSprites.Length)
            currentIndex = 0;

        ShowCharacter(currentIndex);
    }

    public void PreviousCharacter()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = characterSprites.Length - 1;

        ShowCharacter(currentIndex);
    }

    void ShowCharacter(int index)
    {
        characterDisplay.sprite = characterSprites[index];
        characterNameText.text = characterNames[index];
    }

    public void SelectCharacter()
    {
        // Guarda la selección para la siguiente escena
        PlayerPrefs.SetInt("SelectedCharacter", currentIndex);
        PlayerPrefs.Save();

        // Cargar la escena del juego (asegúrate de agregarla en Build Settings)
        SceneManager.LoadScene("GameScene");
    }
}