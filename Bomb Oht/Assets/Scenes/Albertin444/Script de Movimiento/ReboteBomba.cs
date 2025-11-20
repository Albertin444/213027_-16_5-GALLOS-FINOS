using UnityEngine;

public class ReboteBomba : MonoBehaviour
{
    [Header("Rebound Settings")]
    public float fuerzaDeRebote = 10f;  // Strength of the rebound force

    private Rigidbody rb;

    void Start()
    {
        // Get the Rigidbody component of the bomb
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("No Rigidbody found on the bomb!");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the bomb hit an enemy (object with tag "Monstruo")
        if (collision.gameObject.CompareTag("Mounter"))
        {
            // Get the monster's forward direction
            Vector3 reboundDirection = collision.transform.forward;

            // Clear any current velocity before applying the new force
            rb.linearVelocity = Vector3.zero;

            // Apply rebound force in the monster's forward direction
            rb.AddForce(reboundDirection * fuerzaDeRebote, ForceMode.Impulse);

            Debug.Log("?? Bomb bounced off the monster!");
        }
    }
}
