using UnityEngine;

public class NPC2 : MonoBehaviour
{
    [Header("NPC State")]
    [SerializeField] private bool infectado = false;   // If true, NPC is infected
    [SerializeField] private bool aDistancia = false;  // Reserved for future use
    private bool zonaDeRiesgo = false;                 // True if within bomb danger range

    [Header("References")]
    public GameObject empty_global;            // Reference to the selected GameObject by the game controller

    [Header("Movement Settings")]
    public float moveSpeed = 5f;                       // Movement speed
    public float distanciaBomba = 500f;                // Distance at which NPC detects danger
    public Vector3 posicionObjetiva;                   // Current target position

    [Header("Position Range Settings")]
    public float xMin = -50f;
    public float xMax = 50f;
    public float yMin = 0f;
    public float yMax = 0f;                            // Usually fixed for flat terrain
    public float zMin = -50f;
    public float zMax = 50f;

    [Header("Escape Timing")]
    [Tooltip("Minimum time (s) between escape calls while inside danger zone")]
    public float tiempo_de_llamado = 2f;
    private float _ultimoLlamadoEscape = -999f;

    [Header("Map Range Reference")]
    public GameObject rangoDeMapa;                     // Defines map boundaries

    private GameObject bomba;                          // Reference to the object with tag "BOM"

    void Start()
    {
        // Find the object with tag "BOM" in the scene
        bomba = GameObject.FindGameObjectWithTag("BOM");

        // Assign an initial random objective
        AsignarNuevoObjetivo();
    }

    void Update()
    {
        if (empty_global != null)
        {
            ControladorJuego ctrl = empty_global.GetComponent<ControladorJuego>();
            if (ctrl != null)
                infectado = (ctrl.personajeSeleccionado == this.gameObject);
        }
        else
        {
            infectado = false;
        }


        // If infected, skip movement or custom logic
        if (infectado) return;

        // Bomb detection logic
        if (bomba != null)
        {
            float distancia = Vector3.Distance(transform.position, bomba.transform.position);
            zonaDeRiesgo = distancia < distanciaBomba;
        }
        else
        {
            zonaDeRiesgo = false;
        }

        // If NPC goes out of map bounds, assign new random target
        if (rangoDeMapa != null && !EstaDentroDelRangoDeMapa())
        {
            AsignarNuevoObjetivo();
        }

        // If inside danger zone, try to escape (timed)
        if (zonaDeRiesgo)
        {
            if (Time.time - _ultimoLlamadoEscape >= tiempo_de_llamado)
            {
                EscaparInversaRespectoPromedio();
                _ultimoLlamadoEscape = Time.time;
            }
        }

        // Move toward target position
        MoverHaciaObjetivo();

        // If close to target, choose a new one (only if not escaping)
        if (!zonaDeRiesgo && Vector3.Distance(transform.position, posicionObjetiva) < 2f)
        {
            AsignarNuevoObjetivo();
        }
    }

    bool EstaDentroDelRangoDeMapa()
    {
        if (rangoDeMapa == null) return true;

        Collider rango = rangoDeMapa.GetComponent<Collider>();
        if (rango == null) return true;

        return rango.bounds.Contains(transform.position);
    }

    void AsignarNuevoObjetivo()
    {
        float x = Random.Range(xMin, xMax);
        float y = Random.Range(yMin, yMax);
        float z = Random.Range(zMin, zMax);
        posicionObjetiva = new Vector3(x, y, z);
    }

    void MoverHaciaObjetivo()
    {
        Vector3 direccion = (posicionObjetiva - transform.position);
        Vector3 direccionNormalizada = direccion.normalized;

        transform.position += direccionNormalizada * moveSpeed * Time.deltaTime;

        // Rotate smoothly toward movement direction
        if (direccionNormalizada.magnitude > 0f)
        {
            Quaternion rotacion = Quaternion.LookRotation(direccionNormalizada);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotacion, Time.deltaTime * 5f);
        }
    }

    void EscaparInversaRespectoPromedio()
    {
        float midX = (xMax + xMin) / 2f;
        float midZ = (zMax + zMin) / 2f;

        float posX = transform.position.x;
        float posZ = transform.position.z;

        float targetX = 2f * midX - posX;
        float targetZ = 2f * midZ - posZ;

        targetX = Mathf.Clamp(targetX, xMin, xMax);
        float targetY = Mathf.Clamp(transform.position.y, yMin, yMax);
        targetZ = Mathf.Clamp(targetZ, zMin, zMax);

        posicionObjetiva = new Vector3(targetX, targetY, targetZ);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position - transform.forward * 2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(posicionObjetiva, 0.5f);
        Gizmos.DrawLine(transform.position, posicionObjetiva);

        Gizmos.color = Color.cyan;
        Vector3 mid = new Vector3((xMin + xMax) / 2f, transform.position.y, (zMin + zMax) / 2f);
        Gizmos.DrawWireCube(mid, new Vector3(Mathf.Abs(xMax - xMin), 0.1f, Mathf.Abs(zMax - zMin)));
    }
}
