using UnityEngine;

public class LanzamientoPlayer : MonoBehaviour
{
    [Header("References")]
    public GameObject bomba;                  // Reference to the bomb object
    private Rigidbody bombaRb;                // Rigidbody of the bomb
    private Player2 playerScript;             // Reference to the player script
    public GameObject collider_recoleccion;   // Collider used for pickup positioning

    [Header("Throw Settings")]
    public bool bomba_recogida = false;       // True if the player is holding the bomb
    public float throwForce = 10f;            // Throwing force
    public float pickupCooldown = 1f;         // Time before the bomb can be picked up again

    private bool canPickup = true;            // Can the player pick up the bomb?

    void Start()
    {
        playerScript = GetComponent<Player2>();

        if (bomba != null)
            bombaRb = bomba.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (playerScript == null || !playerScript.enabled)
            return;

        // Access private field "infectado" from Player2 script using reflection
        var field = playerScript.GetType().GetField("infectado",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field == null) return;

        bool infectado = (bool)field.GetValue(playerScript);

        // Only allow throwing if infected and currently holding the bomb
        if (infectado && bomba_recogida && Input.GetKeyDown(KeyCode.LeftShift))
        {
            ThrowBomb();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!canPickup || bomba_recogida) return;

        // Check if collided object is a bomb
        if (other.CompareTag("BOM"))
        {
            // Check if player is infected before allowing pickup
            var field = playerScript.GetType().GetField("infectado",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                bool infectado = (bool)field.GetValue(playerScript);

                if (infectado)
                {
                    RecogerBomba(other.gameObject);
                }
                else
                {
                    Debug.Log("🚫 Player is not infected — cannot pick up the bomb.");
                }
            }
        }
    }

    /// <summary>
    /// Pick up the bomb and attach it to the player using collider position reference.
    /// </summary>
    void RecogerBomba(GameObject bombObject)
    {
        bomba = bombObject;
        bombaRb = bomba.GetComponent<Rigidbody>();

        if (collider_recoleccion != null)
        {
            // Use collider position for alignment (only once)
            Vector3 pos = collider_recoleccion.transform.position;
            bomba.transform.position = new Vector3(pos.x, pos.y, pos.z);
        }

        // Parent bomb to player (not to collider)
        bomba.transform.SetParent(transform);
        bomba.transform.localRotation = Quaternion.identity;

        // Disable physics while holding
        bombaRb.isKinematic = true;
        bombaRb.useGravity = false;

        bomba_recogida = true;
        canPickup = false;

        Debug.Log("💣 Bomb picked up (infected player).");
    }

    /// <summary>
    /// Detach and throw the bomb forward from the player.
    /// </summary>
    void ThrowBomb()
    {
        if (bomba == null || bombaRb == null)
        {
            Debug.LogWarning("⚠️ No bomb or Rigidbody assigned!");
            return;
        }

        // Detach bomb
        bomba.transform.SetParent(null);

        // Enable physics again
        bombaRb.isKinematic = false;
        bombaRb.useGravity = true;

        // Reset motion
        bombaRb.linearVelocity = Vector3.zero;
        bombaRb.angularVelocity = Vector3.zero;

        // Apply throw force
        bombaRb.AddForce(transform.forward * throwForce, ForceMode.Impulse);

        bomba_recogida = false;

        // Start cooldown before pickup
        StartCoroutine(EnablePickupAfterDelay());

        Debug.Log("💥 Bomb thrown by infected player!");
    }

    /// <summary>
    /// Waits before allowing the bomb to be picked up again.
    /// </summary>
    private System.Collections.IEnumerator EnablePickupAfterDelay()
    {
        yield return new WaitForSeconds(pickupCooldown);
        canPickup = true;
        Debug.Log("✅ Pickup re-enabled.");
    }
}
