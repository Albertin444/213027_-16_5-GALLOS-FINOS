using UnityEngine;

public class WallRepeller : MonoBehaviour
{
    [Header("Ajuste Opcional")]
    // Si deseas un impulso EXTRA que SUME fuerza a la velocidad de salida, ajústalo aquí.
    // De lo contrario, déjalo en 0.0f. El control de velocidad principal ya está integrado.
    public float extraImpulseForce = 0.0f;

    // Este método se llama solo una vez cuando la bomba impacta la pared
    void OnCollisionEnter(Collision collision)
    {
        // Intentamos obtener el Rigidbody del objeto que nos golpeó
        Rigidbody rbd = collision.collider.GetComponent<Rigidbody>();

        // 1. Verificar si es la bomba lanzada (Rigidbody y no Kinematic)
        if (rbd != null && !rbd.isKinematic)
        {
            // 2. Obtener la normal de la pared (dirección de salida)
            Vector3 wallNormal = collision.contacts[0].normal;

            // 3. Obtener la magnitud (rapidez) de la velocidad de entrada.
            float speedOut = rbd.linearVelocity.magnitude;

            // 4. 🔥 SOBREESCRITURA DE VELOCIDAD (La Solución Robusta) 🔥
            // Asignamos la velocidad de salida (rapidez de entrada) en la dirección de la normal de la pared.
            // Esto anula completamente la velocidad antigua y establece la nueva dirección de forma instantánea.
            rbd.linearVelocity = wallNormal * speedOut;

            // 5. Impulso Adicional (OPCIONAL)
            if (extraImpulseForce > 0.0f)
            {
                rbd.AddForce(wallNormal * extraImpulseForce, ForceMode.Impulse);
            }
        }
    }
}