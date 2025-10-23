using UnityEngine;

public class GameManager1 : MonoBehaviour
{
    [Header("Prefabs de los personajes")]
    public GameObject[] characterPrefabs;

    void Start()
    {
        int selectedIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
        Vector3 spawnPosition = new Vector3(0, 0, 0);

        // Instancia el personaje seleccionado
        Instantiate(characterPrefabs[selectedIndex], spawnPosition, Quaternion.identity);
    }
}