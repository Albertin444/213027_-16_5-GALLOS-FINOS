using UnityEngine;

public class Efect_Bomba : MonoBehaviour
{
    [Header("Referencia del tiempo del juego")]
    public Game_Time gameTimeScript;

    [Header("Escala del objeto")]
    public Vector3 escalaInicial = Vector3.one;
    public Vector3 escalaFinal = new Vector3(3f, 3f, 3f);

    [Header("Referencias de objetos con materiales")]
    public Renderer objetoConMaterial1;
    public Renderer objetoConMaterial2;
    public Renderer objetoConMaterial3;

    [Header("Emisión de materiales")]
    [SerializeField, Range(0f, 100f)] float emisionInicial = 0f;
    [SerializeField, Range(0f, 100f)] float emisionFinal = 5f;
    [SerializeField] Color colorEmision = Color.white;

    private Material material1;
    private Material material2;
    private Material material3;

    void Start()
    {
        if (objetoConMaterial1 != null)
        {
            material1 = objetoConMaterial1.material;
            material1.EnableKeyword("_EMISSION");
            material1.SetColor("_EmissionColor", colorEmision * emisionInicial);
        }

        if (objetoConMaterial2 != null)
        {
            material2 = objetoConMaterial2.material;
            material2.EnableKeyword("_EMISSION");
            material2.SetColor("_EmissionColor", colorEmision * emisionInicial);
        }

        if (objetoConMaterial3 != null)
        {
            material3 = objetoConMaterial3.material;
            material3.EnableKeyword("_EMISSION");
            material3.SetColor("_EmissionColor", colorEmision * emisionInicial);
        }
    }

    void Update()
    {
        if (gameTimeScript == null)
            return;

        float progreso = 1f - (gameTimeScript.GameTime / gameTimeScript.GameTimeCompleted);
        progreso = Mathf.Clamp01(progreso);

        // Escala del objeto
        transform.localScale = Vector3.Lerp(escalaInicial, escalaFinal, progreso);

        // Emisión compartida
        float intensidadActual = Mathf.Lerp(emisionInicial, emisionFinal, progreso);
        Color colorFinal = colorEmision * intensidadActual;

        if (material1 != null)
            material1.SetColor("_EmissionColor", colorFinal);
        if (material2 != null)
            material2.SetColor("_EmissionColor", colorFinal);
        if (material3 != null)
            material3.SetColor("_EmissionColor", colorFinal);
    }
}
