using UnityEngine;
using System.Collections.Generic;

public class Mounters : MonoBehaviour
{
    [Header("Chase behavior settings")]
    public float moveSpeed = 3f;               // Movement speed of the monster
    public float change_of_objective_Max = 5f;     // Time interval (seconds) to change target
    public float change_of_objective_Min = 5f;
    public float change_of_objective = 5f;

    private Transform currentTarget;           // Current target (Player or NPC)
    private float timer;                       // Timer for switching target
    private bool isCollidingWithTarget = false; // Flag to check if currently colliding with target

    private GameObject player;                 // Reference to the Player
    private List<GameObject> npcs = new List<GameObject>(); // List of active NPCs

    void Start()
    {

        // Find the Player using its tag
        player = GameObject.FindGameObjectWithTag("Player");

        // Find all NPCs by tag and store them in a list
        GameObject[] foundNPCs = GameObject.FindGameObjectsWithTag("NPC2");
        npcs.AddRange(foundNPCs);

        // Choose an initial target when the game starts
        ChooseNewTarget();
    }
    public int GetRandomObjectiveTime()
    {
        int randomValue = Random.Range(Mathf.RoundToInt(change_of_objective_Min), Mathf.RoundToInt(change_of_objective_Max) + 1);
        return randomValue;
    }

    void Update()
    {
        // Update timer to track target change time
        timer += Time.deltaTime;

        // If the timer exceeds the limit, pick a new target
        if (timer >= change_of_objective)
        {
            ChooseNewTarget();
            timer = 0f; // Reset the timer
        }

        // If there is a target and the monster is NOT colliding, move toward the target
        if (currentTarget != null && !isCollidingWithTarget)
        {
            MoveTowardsTarget();
        }
    }

    void ChooseNewTarget()
    {
        change_of_objective = GetRandomObjectiveTime();
        // Create a list of possible targets (Player + active NPCs)
        List<Transform> possibleTargets = new List<Transform>();

        if (player != null)
            possibleTargets.Add(player.transform);

        foreach (GameObject npc in npcs)
        {
            // Add only active NPCs
            if (npc != null && npc.activeInHierarchy)
                possibleTargets.Add(npc.transform);
        }

        // If there are no valid targets, stop chasing
        if (possibleTargets.Count == 0)
        {
            currentTarget = null;
            return;
        }

        // Randomly pick a target from the list
        int randomIndex = Random.Range(0, possibleTargets.Count);
        currentTarget = possibleTargets[randomIndex];

        Debug.Log($"{gameObject.name} switched target to: {currentTarget.name}");
    }

    void MoveTowardsTarget()
    {
        // Calculate the normalized direction vector to the target
        Vector3 direction = (currentTarget.position - transform.position).normalized;

        // Move the monster toward the target
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Rotate the monster to face the target
        transform.LookAt(currentTarget);
    }

    // When two monsters (tag: Mounter) collide, both change their target
    void OnCollisionEnter(Collision collision)
    {
        // If colliding with another monster
        if (collision.gameObject.CompareTag("Mounter"))
        {
            Debug.Log($"{gameObject.name} collided with another Mounter. Changing target...");
            ChooseNewTarget();
            timer = 0f; // Reset timer to avoid multiple quick changes
        }

        // If colliding with the current target (Player or NPC)
        if (currentTarget != null && collision.gameObject == currentTarget.gameObject)
        {
            Debug.Log($"{gameObject.name} collided with its target {currentTarget.name}. Stopping movement.");
            isCollidingWithTarget = true; // Stop moving
        }
    }

    // When the collision with another object ends
    void OnCollisionExit(Collision collision)
    {
        // If the monster was colliding with its target and the collision ends
        if (currentTarget != null && collision.gameObject == currentTarget.gameObject)
        {
            Debug.Log($"{gameObject.name} stopped colliding with {currentTarget.name}. Resuming chase.");
            isCollidingWithTarget = false; // Resume chasing
        }
    }

    // Optional: trigger-based version if you use "Is Trigger" colliders instead of physics collisions
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Mounter"))
        {
            Debug.Log($"{gameObject.name} triggered with another Mounter. Changing target...");
            ChooseNewTarget();
            timer = 0f;
        }

        // Stop movement if colliding with the target
        if (currentTarget != null && other.gameObject == currentTarget.gameObject)
        {
            Debug.Log($"{gameObject.name} triggered its target {currentTarget.name}. Stopping movement.");
            isCollidingWithTarget = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Resume chasing after leaving the target
        if (currentTarget != null && other.gameObject == currentTarget.gameObject)
        {
            Debug.Log($"{gameObject.name} stopped triggering {currentTarget.name}. Resuming chase.");
            isCollidingWithTarget = false;
        }
    }
}
