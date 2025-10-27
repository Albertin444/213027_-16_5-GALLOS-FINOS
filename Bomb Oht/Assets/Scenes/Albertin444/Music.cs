using UnityEngine;

public class Music : MonoBehaviour
{
    [Header("Reference to the Game_Time script")]
    public Game_Time gameTime; // Reference to access the Count_Players variable

    [Header("Audio clips for different player counts")]
    public AudioClip music1Player;   // Music when there is 1 player
    public AudioClip music2Players;  // Music when there are 2 players
    public AudioClip music3Players;  // Music when there are 3 players
    public AudioClip music4Players;  // Music when there are 4 players

    private AudioSource audioSource; // The AudioSource component that plays the music
    private int lastPlayerCount = -1; // Stores the last known player count to detect changes

    void Start()
    {
        // Get the AudioSource component attached to this GameObject
        audioSource = GetComponent<AudioSource>();

        // Automatically find the Game_Time script in the scene if not manually assigned
        if (gameTime == null)
            gameTime = FindObjectOfType<Game_Time>();

        // Play the correct music at the start based on the initial player count
        UpdateMusic();
    }

    void Update()
    {
        // Check if the number of players has changed since the last frame
        if (gameTime != null && gameTime.Count_Players != lastPlayerCount)
        {
            // Update the background music when player count changes
            UpdateMusic();
        }
    }

    void UpdateMusic()
    {
        // Safety check: make sure the Game_Time reference exists
        if (gameTime == null) return;

        // Get the current number of players
        int playerCount = gameTime.Count_Players;

        // Update the last known count
        lastPlayerCount = playerCount;

        // Variable to hold the correct music clip
        AudioClip newClip = null;

        // Choose the music clip based on player count
        switch (playerCount)
        {
            case 1:
                newClip = music1Player;
                break;
            case 2:
                newClip = music2Players;
                break;
            case 3:
                newClip = music3Players;
                break;
            case 4:
                newClip = music4Players;
                break;
            default:
                Debug.LogWarning("Unexpected player count: " + playerCount);
                break;
        }

        // If a valid clip is found and it's not already playing
        if (newClip != null && audioSource.clip != newClip)
        {
            // Change the audio clip and play it
            audioSource.clip = newClip;
            audioSource.Play();
        }
    }
}
