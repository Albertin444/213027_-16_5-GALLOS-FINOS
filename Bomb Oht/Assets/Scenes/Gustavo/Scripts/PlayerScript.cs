using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerScript : MonoBehaviour
{
    [Header("Referencias")]
    public Transform handPoint;
    public BombManager bombManager;

    [Header("Movimiento")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float rotationSpeed = 18f; // Velocidad de rotación suave
    public float gravity = -9.81f;

    [Header("Sprint")]
    public float sprintDuration = 1.5f;
    public float sprintCooldown = 2.5f;

    [Header("Lanzamiento")]
    public float throwForce = 12f;
    public float throwSpawnOffset = 1.0f;

    // --- Variables internas ---
    private CharacterController controller;
    private Vector3 velocity; // Almacena velocidad horizontal (X, Z) y vertical (Y)
    private BombScript bomb;
    private Rigidbody rbd; // Referencia al Rigidbody para frenado en contacto

    // ** 🎯 ANIMATOR INTEGRATION **
    private Animator anim;
    private int moveHash; // Para guardar el hash del parámetro "move"
    // *************************

    [HideInInspector] public bool hasBomb = false;
    private bool isThrowing = false;
    private bool isSprinting = false;
    private bool canSprint = true;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        rbd = GetComponent<Rigidbody>();

        // ** 🎯 CORRECCIÓN CLAVE: Buscar Animator en el objeto HIJO **
        // Asume que el objeto hijo (la malla) tiene el componente Animator.
        anim = GetComponentInChildren<Animator>();

        if (anim != null)
        {
            moveHash = Animator.StringToHash("move");
        }
        else
        {
            // Esto se disparará si el Animator no está en el objeto padre NI en ninguno de sus hijos.
            Debug.LogWarning("[PlayerScript] Animator no encontrado en los hijos. La animación de movimiento estará deshabilitada.");
        }
        // **********************************

        if (bombManager == null)
            bombManager = FindFirstObjectByType<BombManager>();

        if (handPoint == null)
            Debug.LogWarning("[PlayerScript] BombPoint no asignado.");
    }

    void Update()
    {
        HandleMovement(); // Calcula velocity.x y velocity.z

        // Lanzar bomba con Shift
        if (hasBomb && Input.GetKeyDown(KeyCode.LeftShift))
            ThrowBomb();

        // 1. Gravedad y reset de Y al tocar el suelo
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        // 2. Aplicar el movimiento: Combinar X, Y, Z de la variable 'velocity'
        Vector3 finalMove = new Vector3(velocity.x, velocity.y, velocity.z);

        // CRÍTICO: Mover una sola vez por frame
        controller.Move(finalMove * Time.deltaTime);
    }

    // ----------------------------
    // Movimiento + Sprint (Space)
    // ----------------------------
    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 1. Crear el vector de input
        Vector3 inputDir = new Vector3(h, 0f, v);

        // 2. Normalizar la dirección para la rotación y el cálculo de velocidad uniforme
        Vector3 direction = inputDir.normalized;
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // Determinar si el jugador tiene entrada de movimiento
        bool isMoving = inputDir.sqrMagnitude > 0.01f;

        // ** 🎯 ANIMATOR: Actualizar la bool 'move' **
        if (anim != null)
        {
            // Establece la bool "move" en el Animator (true si se está moviendo, false si está quieto)
            anim.SetBool(moveHash, isMoving);
        }
        // ******************************************

        // 3. 🎯 ASIGNAR AL VECTOR DE CLASE (velocity)
        if (isMoving) // Si hay alguna entrada (movimiento)
        {
            // Rotación SUAVE
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                rotationSpeed * Time.deltaTime
            );

            // Asignar velocidad horizontal (X, Z)
            velocity.x = direction.x * currentSpeed;
            velocity.z = direction.z * currentSpeed;
        }
        else
        {
            // Si no hay input, detener la velocidad horizontal
            velocity.x = 0f;
            velocity.z = 0f;
        }

        // Sprint con Space
        if (Input.GetKeyDown(KeyCode.Space) && canSprint && isMoving)
            StartCoroutine(SprintRoutine());
    }

    IEnumerator SprintRoutine()
    {
        isSprinting = true;
        canSprint = false;

        yield return new WaitForSeconds(sprintDuration);

        isSprinting = false;

        yield return new WaitForSeconds(sprintCooldown);

        canSprint = true;
    }

    // ----------------------------
    // Adherencia por contacto (Touch-Adhesion)
    // ----------------------------
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!hasBomb || isThrowing) return;

        GameObject target = hit.gameObject;

        if (target.CompareTag("Player") || target.CompareTag("NPC"))
        {
            bool targetHasBomb = false;

            if (target.CompareTag("Player"))
            {
                PlayerScript targetScript = target.GetComponent<PlayerScript>();
                if (targetScript != null) targetHasBomb = targetScript.hasBomb;
            }
            // ... (Lógica de NPC omitida para brevedad)

            if (!targetHasBomb)
            {
                target.SendMessage("ReceiveBomb", bomb, SendMessageOptions.DontRequireReceiver);

                hasBomb = false;
                bomb = null;

                // ... (Lógica de Rigidbody y frenado omitida para brevedad)

                velocity = Vector3.zero;
                isSprinting = false;

                Debug.Log($"Bomba adherida por contacto a: {target.name}");
            }
        }
    }

    // ----------------------------
    // Recibir bomba
    // ----------------------------
    public void ReceiveBomb(BombScript newBomb)
    {
        bomb = newBomb;
        hasBomb = true;

        if (handPoint != null && bomb != null)
        {
            newBomb.transform.rotation = handPoint.rotation;
            newBomb.transform.position = handPoint.position;

            bomb.Adhere(handPoint);
        }

        Debug.Log("[PlayerScript] Player recibió la bomba en handPoint.");
    }

    // ----------------------------
    // Lanzar bomba (Shift)
    // ----------------------------
    void ThrowBomb()
    {
        if (!hasBomb || bomb == null || handPoint == null) return;

        isThrowing = true;

        Vector3 direction = transform.forward;
        bomb.transform.position = handPoint.position + direction * throwSpawnOffset;

        bomb.Launch(direction, throwForce, gameObject);

        hasBomb = false;
        bomb = null;
        isThrowing = false;

        if (bombManager != null)
            bombManager.OnBombThrown();

        Debug.Log("[PlayerScript] Player lanzó la bomba.");
    }

    // ----------------------------
    // Métodos auxiliares
    // ----------------------------
    // ... (El resto de métodos auxiliares no tienen cambios relevantes)
    public void TryLaunchBomb(float force)
    {
        if (bomb != null && handPoint != null)
        {
            bomb.Launch(handPoint.forward, force, gameObject);
            hasBomb = false;
            bomb = null;
            Debug.Log("[PlayerScript] Player lanzó la bomba desde handPoint.");
        }
    }

    public void ResetBomb()
    {
        if (bomb != null)
        {
            bomb.transform.SetParent(null);
        }
        bomb = null;
        hasBomb = false;
        Debug.Log("[PlayerScript] Player soltó la bomba.");
    }
}