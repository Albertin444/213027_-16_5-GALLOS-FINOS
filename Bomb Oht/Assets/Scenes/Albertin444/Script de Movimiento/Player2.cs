using UnityEngine;
using UnityEngine.UIElements;

public class Player2 : MonoBehaviour
{
    [SerializeField] private bool infectado = false;   // If true, NPC is infected (logic to be added later)
    public bool infectado2;
    [Header("References")]
    public CharacterController controller;  // Reference to the Character Controller component

    [Header("Movement Settings")]
    public float moveSpeed = 5f;            // Normal movement speed
    public float acceleration = 5f;         // How fast the player accelerates
    public float deceleration = 5f;         // How fast the player slows down
  public float posicionYinmovible=-50;//position to not move the characters

    [Header("Sprint Settings")]
    public float sprintDistance = 100f;     // Total distance to move during sprint
    public float sprintSpeed = 20f;         // How fast the player sprints
    private bool isSprinting = false;       // Is the player currently sprinting?
    private float sprintProgress = 0f;      // How far has the sprint progressed (0 to sprintDistance)
    private Vector3 sprintDirection;        // Direction of the sprint

    private Vector3 currentVelocity = Vector3.zero; // Current velocity of the player

    [Header("References")]
    public GameObject empty_global;            // Reference to the selected GameObject by the game controller

    void Start()
    {
        // Automatically assign the CharacterController if not set
        if (controller == null)
            controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        infectado2 = infectado;
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


        // If player is sprinting, continue sprint movement
        if (isSprinting)
        {
            ContinueSprint();
            return; // Skip normal movement while sprinting
        }

        // Handle normal movement
        HandleMovement();

        // If Space is pressed, start sprint
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Sprint();
        }
     
    }

    void HandleMovement()
    {
        // --- 1. Read input axes ---
        float horizontal = Input.GetAxisRaw("Horizontal"); // A (-1) and D (+1)
        float vertical = Input.GetAxisRaw("Vertical");     // S (-1) and W (+1)

        // --- 2. Determine target direction ---
        Vector3 targetDirection = new Vector3(horizontal, 0f, vertical).normalized;

        // --- 3. Rotate towards movement direction ---
        if (targetDirection.magnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        // --- 4. Smooth acceleration / deceleration ---
        Vector3 targetVelocity = targetDirection * moveSpeed;

        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity,
            (targetVelocity.magnitude > 0f ? acceleration : deceleration) * Time.deltaTime);
        currentVelocity.y = posicionYinmovible;
        // --- 5. Move the character ---
        controller.Move(currentVelocity * Time.deltaTime);
    }

    // --- Sprint Function ---
    void Sprint()
    {
        // Initialize sprint state
        isSprinting = true;
        sprintProgress = 0f;

        // Sprint direction is the player's current forward direction
        sprintDirection = transform.forward.normalized;
    }

    // --- Handles sprint movement over time ---
    void ContinueSprint()
    {
        // Move forward progressively based on sprint speed
        float distanceThisFrame = sprintSpeed * Time.deltaTime;

        // If adding this distance exceeds total sprint distance, clamp it
        if (sprintProgress + distanceThisFrame >= sprintDistance)
        {
            distanceThisFrame = sprintDistance - sprintProgress;
            isSprinting = false; // Sprint finished
        }

        // Apply movement
        controller.Move(sprintDirection * distanceThisFrame);

        // Update sprint progress
        sprintProgress += distanceThisFrame;
    }
}
