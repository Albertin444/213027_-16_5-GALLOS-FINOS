using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LanzamientoNPC : MonoBehaviour
{
    [Header("References")]
    public GameObject bomba;                      // Reference to the bomb (tag "BOM")
    private Rigidbody bombaRb;                    // Rigidbody reference (cached)
    public GameObject collider_recoleccion;       // Position reference for bomb holding
    public GameObject empty_global;

    [Header("States")]
    public bool recogida_bomba = false;           // True if NPC currently holds the bomb
    public bool infectado = false;                // Is this NPC the infected one?
    public bool lanzamiento = false;              // Public flag: when true, will call LanzarBomba()
    public bool recoger = true;
    private bool corriendoAtaque = false;         // Evita múltiples corrutinas

    [Header("Attack Settings")]
    public float patronDeAtaque = 5f;             // Time interval between automatic launches
    public float fuerzaDeLanzamiento = 1000f;     // Force applied when throwing (Impulse)
    public float cooldownLanzamiento = 1f;        // Cooldown after launching

    [Header("Movement")]
    public float moveSpeed = 5f;                  // Movement speed when chasing
    public GameObject objetivoSeleccionado;       // The target to pursue (from ControladorJuego)

    [Header("Objective Change")]
    public float tiempoCambioObjetivo = 3f;       // Every X seconds, change target
    private float temporizadorCambio = 0f;

    void Start()
    {
        // Find bomb in scene if not assigned
        if (bomba == null)
            bomba = GameObject.FindGameObjectWithTag("BOM");

        if (bomba != null)
            bombaRb = bomba.GetComponent<Rigidbody>();

        objetivoSeleccionado = SeleccionarObjetivo();
    }

    void Update()
    {
        // Sin controlador global, el NPC no hace nada
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

        if (!infectado) return; // Solo el infectado actúa

        // --- Lanzamiento manual (desde inspector u otro script) ---
        if (lanzamiento)
        {
            recogida_bomba = false;
            lanzamiento = false;
            LanzarBomba();
            return;
        }

        // --- Cambio automático de objetivo cada X segundos ---
        temporizadorCambio += Time.deltaTime;
        if (temporizadorCambio >= tiempoCambioObjetivo)
        {
            objetivoSeleccionado = SeleccionarObjetivo();
            temporizadorCambio = 0f;
            Debug.Log($"{name} cambió su objetivo a: {(objetivoSeleccionado != null ? objetivoSeleccionado.name : "Ninguno")}");
        }

        // --- Lógica principal ---
        if (recogida_bomba)
        {
            // Mantener la bomba en la mano
            if (bomba != null && collider_recoleccion != null)
            {
                if (bombaRb == null)
                    bombaRb = bomba.GetComponent<Rigidbody>();

                if (bomba.transform.parent != transform)
                    bomba.transform.SetParent(transform);

                bomba.transform.position = collider_recoleccion.transform.position;
                bomba.transform.rotation = collider_recoleccion.transform.rotation;

                if (bombaRb != null)
                {
                    bombaRb.isKinematic = true;
                    bombaRb.useGravity = false;
                    bombaRb.linearVelocity = Vector3.zero;
                    bombaRb.angularVelocity = Vector3.zero;
                }
            }

            // Mover hacia el objetivo
            PerseguirObjetivoSeleccionado();

            // Lanzar automáticamente cada cierto tiempo
            if (!corriendoAtaque)
                StartCoroutine(TemporizadorDeAtaque());
        }
        else
        {
            if (!lanzamiento)
                PerseguirBomba();
        }
    }

    // Mover hacia la bomba
    void PerseguirBomba()
    {
        if (bomba == null) return;

        Vector3 dir = (bomba.transform.position - transform.position);
        if (dir.sqrMagnitude < 0.001f) return;

        dir.Normalize();
        transform.position += dir * moveSpeed * Time.deltaTime;
        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * 5f);
    }

    // Mover hacia el objetivo actual
    void PerseguirObjetivoSeleccionado()
    {
        if (objetivoSeleccionado == null) return;

        Vector3 dir = (objetivoSeleccionado.transform.position - transform.position);
        if (dir.sqrMagnitude < 0.001f) return;

        dir.Normalize();
        transform.position += dir * moveSpeed * Time.deltaTime;
        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * 5f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!infectado) return;

        if (other.CompareTag("BOM") && !recogida_bomba)
        {
            RecogerBomba(other.gameObject);
        }
    }

    void RecogerBomba(GameObject bombObject)
    {
        if (!recoger) return;

        bomba = bombObject;
        bombaRb = bomba.GetComponent<Rigidbody>();

        if (collider_recoleccion != null)
        {
            bomba.transform.position = collider_recoleccion.transform.position;
            bomba.transform.rotation = collider_recoleccion.transform.rotation;
        }

        bomba.transform.SetParent(transform);
        if (bombaRb != null)
        {
            bombaRb.isKinematic = true;
            bombaRb.useGravity = false;
            bombaRb.linearVelocity = Vector3.zero;
            bombaRb.angularVelocity = Vector3.zero;
        }

        recogida_bomba = true;
        Debug.Log($"{name} recogió la bomba.");
    }

    // Corrutina de ataque automático
    IEnumerator TemporizadorDeAtaque()
    {
        corriendoAtaque = true;
        yield return new WaitForSeconds(patronDeAtaque);
        recoger = false;
        lanzamiento = true;
        yield return new WaitForSeconds(cooldownLanzamiento);
        recoger = true;
        corriendoAtaque = false;
    }

    void LanzarBomba()
    {
        if (bomba == null)
        {
            Debug.LogWarning($"{name} intentó lanzar pero no tiene bomba.");
            lanzamiento = false;
            return;
        }

        if (bombaRb == null)
            bombaRb = bomba.GetComponent<Rigidbody>();

        if (bombaRb == null)
        {
            Debug.LogWarning($"{name} intentó lanzar pero no tiene Rigidbody.");
            lanzamiento = false;
            return;
        }

        bomba.transform.SetParent(null);
        bombaRb.isKinematic = false;
        bombaRb.useGravity = true;

        Vector3 dir = transform.forward;
        bombaRb.AddForce(dir * fuerzaDeLanzamiento, ForceMode.Impulse);

        recogida_bomba = false;
        Debug.Log($"{name} lanzó la bomba con fuerza {fuerzaDeLanzamiento}.");
    }

    // Selecciona un objetivo aleatorio que no sea este NPC
    GameObject SeleccionarObjetivo()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC2");

        List<GameObject> posibles = new List<GameObject>();
        posibles.AddRange(players);
        posibles.AddRange(npcs);
        posibles.Remove(this.gameObject);

        if (posibles.Count == 0) return null;

        int index = Random.Range(0, posibles.Count);
        return posibles[index];
    }
}
