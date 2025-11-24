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

    [HideInInspector] public bool hasBomb = false;
    private bool isThrowing = false;
    private bool isSprinting = false;
    private bool canSprint = true;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        rbd = GetComponent<Rigidbody>(); // Inicializar Rigidbody

        // Si el Player no tiene Rigidbody, rbd será null y la lógica de frenado lo ignorará.

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
        // Usar GetAxis para suavizar la aceleración/frenado (opcional, pero mejora la sensación)
        float h = Input.GetAxisRaw("Horizontal"); // Usamos Raw si queremos respuesta inmediata
        float v = Input.GetAxisRaw("Vertical");

        // 1. Crear el vector de input
        Vector3 inputDir = new Vector3(h, 0f, v);

        // 2. Normalizar la dirección para la rotación y el cálculo de velocidad uniforme
        Vector3 direction = inputDir.normalized;
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // 3. 🎯 ASIGNAR AL VECTOR DE CLASE (velocity)
        // Esto define la velocidad horizontal que se usará en Update()
        velocity.x = direction.x * currentSpeed;
        velocity.z = direction.z * currentSpeed;

        // 4. Aplicar ROTACIÓN SUAVE (basada en la dirección de input)
        if (inputDir.sqrMagnitude > 0.0001f) // Si hay alguna entrada
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction), // Usamos 'direction' normalizada
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            // Si no hay input, el personaje se detiene (velocity.x/z ya son cero)
            velocity.x = 0f;
            velocity.z = 0f;
        }

        // Sprint con Space
        if (Input.GetKeyDown(KeyCode.Space) && canSprint && inputDir.sqrMagnitude > 0.01f)
            StartCoroutine(SprintRoutine());
    }

    IEnumerator SprintRoutine()
    {
        isSprinting = true;
        canSprint = false;

        yield return new WaitForSeconds(sprintDuration); // Usar WaitForSeconds, no Realtime

        isSprinting = false;

        yield return new WaitForSeconds(sprintCooldown);

        canSprint = true;
    }

    // ----------------------------
    // Adherencia por contacto (Touch-Adhesion)
    // ----------------------------
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Solo proceder si: yo tengo la bomba y no la estoy lanzando activamente.
        if (!hasBomb || isThrowing) return;

        GameObject target = hit.gameObject;

        if (target.CompareTag("Player") || target.CompareTag("NPC"))
        {
            bool targetHasBomb = false;

            // Comprobar si el objetivo ya tiene la bomba (el código de la IA necesita la variable)
            if (target.CompareTag("Player"))
            {
                PlayerScript targetScript = target.GetComponent<PlayerScript>();
                if (targetScript != null) targetHasBomb = targetScript.hasBomb;
            }
            else // NPC
            {
                NPCScript targetScript = target.GetComponent<NPCScript>();
                if (targetScript != null) targetHasBomb = targetScript.hasBomb;
            }

            if (!targetHasBomb)
            {
                // 🛑 TRANSFERENCIA CLAVE 🛑
                target.SendMessage("ReceiveBomb", bomb, SendMessageOptions.DontRequireReceiver);

                // 2. Limpiar mi estado y frenar.
                hasBomb = false;
                bomb = null;

                if (rbd != null)
                {
                    rbd.linearVelocity = Vector3.zero;
                    rbd.angularVelocity = Vector3.zero;
                }

                velocity = Vector3.zero; // Detener tu variable de control (horizontal y vertical)
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

        isThrowing = true; // Flag para bloquear Touch-Adhesion

        Vector3 direction = transform.forward;
        bomb.transform.position = handPoint.position + direction * throwSpawnOffset;

        bomb.Launch(direction, throwForce, gameObject);

        hasBomb = false;
        bomb = null;
        isThrowing = false; // Resetear flag

        if (bombManager != null)
            bombManager.OnBombThrown();

        Debug.Log("[PlayerScript] Player lanzó la bomba.");
    }

    // ----------------------------
    // Métodos auxiliares
    // ----------------------------

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
            // Asumiendo que BombScript tiene este estado:
            // bomb.currentCondition = BombScript.Condition.OnFloor; 
        }

        bomb = null;
        hasBomb = false;

        Debug.Log("[PlayerScript] Player soltó la bomba.");
    }
}