using UnityEngine;

public class CambioDeObjetivo : MonoBehaviour
{
    [Header("References")]
    public GameObject empty_global;              // Reference to the GameController (with ControladorJuego script)
    private ControladorJuego controladorJuego;   // Cached script reference
    private Rigidbody bombaRb;                   // Rigidbody de la bomba

    [Header("Explosion Effect")]
    public GameObject prefabExplosion;           // Prefab to spawn when hitting a new target
    public float explosionDuration = 2f;         // Time before the explosion is destroyed

    [Header("Last Target")]
    public GameObject ultimoObjetivoTocado;      // Last object that the bomb collided with

    void Start()
    {
        // Get reference to the ControladorJuego script
        if (empty_global != null)
            controladorJuego = empty_global.GetComponent<ControladorJuego>();

        // Cache Rigidbody
        bombaRb = GetComponent<Rigidbody>();

        if (controladorJuego == null)
            Debug.LogWarning("⚠️ ControladorJuego not found! Please assign it to 'empty_global'.");
    }

    void OnCollisionEnter(Collision collision)
    {
        // React only to Player or NPC2
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("NPC2"))
        {
            // --- Si el nuevo objetivo NO es el mismo que el padre actual ---
            if (transform.parent != null && collision.gameObject != transform.parent.gameObject)
            {
                // 🚫 Deja de ser hijo del anterior padre
                transform.SetParent(null);

                // 🔧 Activa el Rigidbody (física otra vez)
                if (bombaRb != null)
                {
                    bombaRb.isKinematic = false;
                    bombaRb.useGravity = true;
                }

                Debug.Log("💣 La bomba fue liberada del padre anterior.");
            }

            // --- Si es un nuevo objetivo diferente al último tocado ---
            if (collision.gameObject != ultimoObjetivoTocado)
            {
                ultimoObjetivoTocado = collision.gameObject;

                // Actualiza el objetivo global
                if (controladorJuego != null)
                {
                    controladorJuego.personajeSeleccionado = ultimoObjetivoTocado;
                    Debug.Log("💥 Nuevo objetivo seleccionado: " + ultimoObjetivoTocado.name);
                }

                // Crea efecto de explosión
                if (prefabExplosion != null)
                {
                    GameObject explosion = Instantiate(prefabExplosion, transform.position, Quaternion.identity);
                    Destroy(explosion, explosionDuration);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (ultimoObjetivoTocado != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, ultimoObjetivoTocado.transform.position);
        }
    }
}
