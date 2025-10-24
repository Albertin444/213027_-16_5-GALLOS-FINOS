using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Efect_Audio : MonoBehaviour
{
    [Header("Referencia del tiempo del juego")]
    public Game_Time gameTimeScript;

    [Header("Configuración del audio")]
    public float pitchInicial = 1f;
    public float pitchFinal = 2f;
    public float volumenInicial = 1f;
    public float volumenFinal = 1f;
    public bool reiniciarAlFinal = false;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (!audioSource.isPlaying)
            audioSource.Play();

        audioSource.pitch = pitchInicial;
        audioSource.volume = volumenInicial;
    }

    void Update()
    {
        if (gameTimeScript == null)
            return;

        // Progreso de 0 a 1
        float progreso = 1f - (gameTimeScript.GameTime / gameTimeScript.GameTimeCompleted);
        progreso = Mathf.Clamp01(progreso);

        // Aumenta velocidad (pitch)
        audioSource.pitch = Mathf.Lerp(pitchInicial, pitchFinal, progreso);

        // Opcional: cambia volumen con el tiempo
        audioSource.volume = Mathf.Lerp(volumenInicial, volumenFinal, progreso);

        // Reinicio
        if (reiniciarAlFinal && gameTimeScript.GameTime <= 0f)
        {
            gameTimeScript.GameTime = gameTimeScript.GameTimeCompleted;
            audioSource.pitch = pitchInicial;
            audioSource.volume = volumenInicial;
            audioSource.Play();
        }
    }
}
