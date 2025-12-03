using System.Collections.Generic;
using UnityEngine;

public class ControladorJuego : MonoBehaviour
{
    [Header("References")]
    public List<GameObject> personajes = new List<GameObject>(); // Todos los jugadores
    public GameObject personajeSeleccionado; // El infectado actual
    public countergame crnometro;             // Referencia al contador (tu cronómetro)
    public GameObject puntoCreacionBomba;     // Objeto destino donde irá la bomba

    void Start()
    {
        // Buscar el punto de creación de bomba por nombre si no está asignado
        if (puntoCreacionBomba == null)
        {
            puntoCreacionBomba = GameObject.Find("Creacion de Bomba");
            if (puntoCreacionBomba == null)
                Debug.LogWarning("⚠️ No se encontró el objeto 'Creacion de Bomba' en la escena.");
        }

        // Buscar jugadores y NPCs
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC2");

        personajes.AddRange(players);
        personajes.AddRange(npcs);

        if (personajes.Count > 0)
        {
            int index = Random.Range(0, personajes.Count);
            personajeSeleccionado = personajes[index];
            Debug.Log("🎯 Infectado inicial: " + personajeSeleccionado.name);
        }
        else
        {
            Debug.LogWarning("⚠️ No hay personajes en escena.");
        }
    }

    /// <summary>
    /// Elimina el infectado actual, libera la bomba y asigna otro nuevo.
    /// </summary>
    public void EliminarYAsignarNuevoInfectado()
    {
        if (personajes.Count <= 1)
        {
            Debug.Log("⚠️ Solo queda un jugador, no se eliminará a nadie.");
            return;
        }

        if (personajeSeleccionado != null)
        {
            // Buscar la bomba (por tag)
            GameObject bomba = GameObject.FindGameObjectWithTag("BOM");
            if (bomba != null)
            {
                // Liberar la bomba antes de destruir al infectado
                bomba.transform.SetParent(null);

                // Si hay punto de creación de bomba, mover hacia allá
                if (puntoCreacionBomba != null)
                {
                    // Detener la física para que se pueda mover de forma suave
                    Rigidbody bombaRb = bomba.GetComponent<Rigidbody>();
                    if (bombaRb != null)
                    {
                        bombaRb.isKinematic = false;
                        bombaRb.useGravity = true;
                        bombaRb.linearVelocity = Vector3.zero;
                        bombaRb.angularVelocity = Vector3.zero;

                        // Opcional: aplicar fuerza suave hacia el punto
                        Vector3 dir = (puntoCreacionBomba.transform.position - bomba.transform.position).normalized;
                        bombaRb.AddForce(dir * 200f, ForceMode.Impulse);
                    }
                    else
                    {
                        // Si no tiene Rigidbody, simplemente la movemos
                        bomba.transform.position = puntoCreacionBomba.transform.position;
                    }
                }
            }

            // Reiniciar cronómetro
            if (crnometro != null)
                crnometro.Resetcontador();

            Debug.Log($"💀 Eliminando infectado: {personajeSeleccionado.name}");

            // Eliminar el infectado del registro y de la escena
            personajes.Remove(personajeSeleccionado);
            Destroy(personajeSeleccionado);
        }

        // Asignar nuevo infectado
        if (personajes.Count > 0)
        {
            int index = Random.Range(0, personajes.Count);
            personajeSeleccionado = personajes[index];
            Debug.Log($"🧠 Nuevo infectado: {personajeSeleccionado.name}");
        }
        else
        {
            personajeSeleccionado = null;
            Debug.LogWarning("⚠️ No hay personajes restantes para asignar.");
        }
    }
    public void EliminarDeLista(GameObject personaje)
    {
        if (personajes.Contains(personaje))
        {
            personajes.Remove(personaje);
            Debug.Log("🗑️ Eliminado de la lista: " + personaje.name);
        }
        else
        {
            Debug.Log("❌ El personaje no estaba en la lista: " + personaje.name);
        }
    }

}
