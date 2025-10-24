using UnityEngine;

public class Game_Time : MonoBehaviour
{
    public float GameTimeCompleted = 30f; // Tiempo total
    public float GameTime = 30f;          // Tiempo restante

    private float timer = 0f;

    private void Start()
    {
        GameTime=GameTimeCompleted;
    }
    void Update()
    {
        if (GameTime > 0f)
        {
            timer += Time.deltaTime;

            // Cada 0.1 segundos restamos 0.1
            if (timer >= 0.1f)
            {
                GameTime -= 0.1f;
                timer = 0f;
            }
        }
        else
        {
            GameTime = 0f; // evita números negativos
        }
    }
}
