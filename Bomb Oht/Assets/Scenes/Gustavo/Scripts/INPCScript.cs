using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class NPCScript : MonoBehaviour
{
    public enum NPCBehavior { Idle, Attack, Flee }
    public enum NPCState { Idle, Attack_HasBomb, Attack_Throwing, Fleeing }

    [Header("Referencias")]
    public Transform handPoint;
    public Transform preferredTarget;
    public BombManager bombManager;

    [Header("Movimiento")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 7f;
    public float gravity = -9.81f;
    public float rotationSpeed = 8f;

    [Header("Decisiones y rangos")]
    public float detectTargetRadius = 20f;
    public float closeThrowDistance = 6f;
    public float pickupDistance = 1.6f;

    [Header("Probabilidades y tiempos")]
    [Range(0f, 1f)] public float nearThrowProbability = 0.75f;
    [Range(0f, 1f)] public float farThrowProbability = 0.18f;
    public float autoThrowMinDelay = 1.0f;
    public float autoThrowMaxDelay = 4.0f;
    public float throwForce = 10f;

    [Header("Sprint")]
    public float sprintDuration = 1.0f;
    public float sprintCooldown = 3.0f;
    [Range(0f, 1f)] public float sprintChanceWhenClose = 0.6f;
    [Range(0f, 1f)] public float sprintChanceWhenFar = 0.25f;

    [Header("Lanzamiento")]
    public float throwDistance = 5f;

    // --- Estados internos ---
    private CharacterController controller;
    private Rigidbody rbd; // <-- Referencia al Rigidbody para frenado
    private Transform currentTarget;
    private Vector3 velocity;
    private BombScript bomb;

    // ** 🎯 ANIMATOR INTEGRATION **
    private Animator anim;
    private int moveHash; // Hash para el parámetro bool "move"
    private bool isMovingThisFrame = false; // Flag para controlar si se llamó a MoveTowards()
    // *************************

    [HideInInspector] public bool hasBomb = false; // Hacemos público para acceso por PlayerScript
    private bool localOwnerFlag = false;
    private bool isSprinting = false;
    private bool canSprint = true;
    private bool isThrowing = false;
    private bool canThrow = true;
    private bool isStunned = false; // <-- Para evitar el bug de "pegado"
    private float throwCooldown = 0f;

    private NPCBehavior requestedBehavior = NPCBehavior.Idle;
    private NPCState currentState;


    void Start()
    {
        controller = GetComponent<CharacterController>();
        rbd = GetComponent<Rigidbody>(); // Inicializar Rigidbody

        // ** 🎯 NUEVO: Buscar Animator en el objeto HIJO **
        anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            moveHash = Animator.StringToHash("move");
        }
        else
        {
            Debug.LogWarning("[NPCScript] Animator no encontrado en los hijos. La animación de movimiento estará deshabilitada.");
        }
        // **********************************

        if (preferredTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) preferredTarget = p.transform;
        }

        StartCoroutine(AutoThrowLoop());

        if (bombManager == null)
            bombManager = FindFirstObjectByType<BombManager>();
    }

    void Update()
    {
        // Reiniciar flag de movimiento al inicio del frame
        isMovingThisFrame = false;

        // 🛑 BLOQUEAR MOVIMIENTO SI ESTÁ EN PAUSA (después del contacto) 🛑
        if (isStunned)
        {
            // Detener animación si está aturdido
            if (anim != null) anim.SetBool(moveHash, false);

            // Solo aplicar gravedad, sin movimiento horizontal
            if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
            return; // Saltar toda la lógica de IA
        }

        UpdateCurrentTarget();

        GameObject bombOwner = BombManager.Instance?.currentOwner;
        BombScript currentBomb = BombManager.Instance?.bomb;

        // --- LÓGICA DE ESTADOS Y PRIORIDADES ---

        // 1. 🛑 PRIORIDAD MÁXIMA: HUIR (La bomba está ADHERIDA a un oponente)
        if (bombOwner != null && bombOwner != gameObject)
        {
            DefensiveBehaviour();
            currentState = NPCState.Fleeing;
        }

        // 2. 🟠 PRIORIDAD MEDIA: RECOGER O MANTENER LA POSESIÓN 
        else
        {
            // 2a. Si la bomba está libre (Nadie la tiene adherida en ese momento):
            if (bombOwner == null && currentBomb != null)
            {
                // Si la bomba NO es mía y está cerca (riesgo activo), debo huir.
                if (!hasBomb && currentBomb.lastOwner != gameObject && currentTarget != null)
                {
                    float distToBomb = Vector3.Distance(transform.position, currentBomb.transform.position);

                    if (distToBomb < detectTargetRadius)
                    {
                        // HUIR DE LA POSICIÓN DE LA BOMBA
                        Vector3 dirAway = (transform.position - currentBomb.transform.position);
                        dirAway.y = 0f;
                        Vector3 moveDir = dirAway.normalized;

                        // Lógica de Sprint para huir
                        if (!isSprinting && canSprint && distToBomb < closeThrowDistance + 3f)
                        {
                            if (Random.value < sprintChanceWhenClose) StartCoroutine(SprintRoutine());
                        }

                        float moveSpeed = isSprinting ? sprintSpeed : walkSpeed;
                        MoveTowards(moveDir, moveSpeed);

                        currentState = NPCState.Fleeing;
                        goto ApplyMovementAndGravity; // Mantiene la estructura de salto
                    }
                }

                // Si la bomba es MÍA (fallé el lanzamiento) o está en el suelo (neutra), la recojo.
                if (currentBomb.lastOwner == gameObject || currentBomb.currentCondition == BombScript.Condition.OnFloor)
                {
                    CollectBombBehaviour();
                    currentState = NPCState.Idle;
                    goto ApplyMovementAndGravity; // Mantiene la estructura de salto
                }
            }

            // 2b. Comportamiento Ofensivo/Idle:
            OffensiveBehaviour();
            currentState = hasBomb ? NPCState.Attack_HasBomb : NPCState.Idle;
        }

    ApplyMovementAndGravity:
        // ** 🎯 CONTROL DE ANIMACIÓN EN BASE AL MOVIMIENTO **
        if (anim != null)
        {
            // Si MoveTowards fue llamado, isMovingThisFrame es TRUE, sino es FALSE (Idle)
            anim.SetBool(moveHash, isMovingThisFrame);
        }
        // *************************************************

        // ➡️ LÓGICA DE ESTADOS INTERNOS Y GRAVEDAD
        // (La velocidad horizontal se setea en MoveTowards, si aplica)
        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Control de cooldown de lanzamiento
        if (throwCooldown > 0f)
            throwCooldown -= Time.deltaTime;
    }

    // ----------------------------
    // Touch Adhesion (Adherencia por contacto)
    // ----------------------------
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // ... (Tu lógica original sin cambios)
        if (!hasBomb || isThrowing || isStunned) return;
        GameObject target = hit.gameObject;

        if (target.CompareTag("Player") || target.CompareTag("NPC"))
        {
            bool targetHasBomb = false;
            if (target.CompareTag("Player"))
            {
                PlayerScript targetScript = target.GetComponent<PlayerScript>();
                if (targetScript != null) targetHasBomb = targetScript.hasBomb;
            }
            else
            {
                NPCScript targetScript = target.GetComponent<NPCScript>();
                if (targetScript != null) targetHasBomb = targetScript.hasBomb;
            }

            if (!targetHasBomb)
            {
                target.SendMessage("ReceiveBomb", bomb, SendMessageOptions.DontRequireReceiver);
                hasBomb = false;
                bomb = null;
                // ... (Lógica de frenado)
                velocity = Vector3.zero;
                isSprinting = false;
                StartCoroutine(TransferStunRoutine(0.2f));
                Debug.Log($"Bomba adherida por contacto a: {target.name}");
            }
        }
    }

    // ----------------------------
    // Comportamiento ofensivo (Buscar objetivo SOLO si tiene la bomba)
    // ----------------------------
    void OffensiveBehaviour() // Restaurado a void
    {
        float moveSpeed = walkSpeed;
        Vector3 moveDir = Vector3.zero;

        // Lógica de persecución y lanzamiento (SOLO si tiene la bomba)
        if (hasBomb && currentTarget != null)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.position);
            moveDir = (currentTarget.position - transform.position).normalized;
            moveDir.y = 0f;

            // Lógica de Sprint...
            if (!isSprinting && canSprint)
            {
                float chance = (dist <= closeThrowDistance) ? sprintChanceWhenClose : sprintChanceWhenFar;
                if (Random.value < chance) StartCoroutine(SprintRoutine());
            }

            moveSpeed = isSprinting ? sprintSpeed : walkSpeed;
        }

        // Aplicar movimiento
        if (moveDir != Vector3.zero)
        {
            MoveTowards(moveDir, moveSpeed);
        }
        // Si moveDir es cero, MoveTowards no se llama y la animación se desactiva en Update.
    }

    // ----------------------------
    // Comportamiento de recogida (Ir hacia la bomba en el suelo/aire)
    // ----------------------------
    void CollectBombBehaviour()
    {
        if (BombManager.Instance == null || BombManager.Instance.bomb == null) return;

        float moveSpeed = walkSpeed;
        BombScript bombObj = BombManager.Instance.bomb;

        // ➡️ Calcular dirección hacia la bomba
        float distToBomb = Vector3.Distance(transform.position, bombObj.transform.position);
        Vector3 moveDir = (bombObj.transform.position - transform.position).normalized;
        moveDir.y = 0f;

        // 🟢 LÓGICA DE SPRINT AQUÍ
        if (!isSprinting && canSprint && distToBomb > pickupDistance)
        {
            float chance = sprintChanceWhenFar;
            if (Random.value < chance) StartCoroutine(SprintRoutine());
        }

        moveSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // Aplicar movimiento
        if (moveDir != Vector3.zero)
        {
            MoveTowards(moveDir, moveSpeed);
        }
    }

    // ----------------------------
    // Comportamiento defensivo (Huir del dueño adherido)
    // ----------------------------
    void DefensiveBehaviour()
    {
        GameObject owner = (BombManager.Instance != null) ? BombManager.Instance.currentOwner : null;
        if (owner != null && owner.gameObject != null)
        {
            Vector3 dirAway = (transform.position - owner.transform.position);
            dirAway.y = 0f;
            Vector3 moveDir = dirAway.normalized;

            float distToOwner = Vector3.Distance(transform.position, owner.transform.position);

            // 🟢 LÓGICA DE SPRINT AQUÍ (Para huir)
            if (!isSprinting && canSprint && distToOwner < closeThrowDistance + 3f)
            {
                if (Random.value < sprintChanceWhenClose) StartCoroutine(SprintRoutine());
            }

            float moveSpeed = isSprinting ? sprintSpeed : walkSpeed;
            MoveTowards(moveDir, moveSpeed);
        }
    }

    // ----------------------------
    // API pública (para BombManager, etc.)
    // ----------------------------

    // CORRECCIÓN: Método ahora es público (para BombManager)
    public void ReceiveBomb(BombScript newBomb)
    {
        bomb = newBomb;
        hasBomb = true;

        if (handPoint != null && bomb != null)
        {
            newBomb.transform.position = handPoint.position;
            newBomb.transform.rotation = handPoint.rotation;

            bomb.Adhere(handPoint);
        }

        currentState = NPCState.Attack_HasBomb;
        SetBehavior(NPCBehavior.Attack);

        Debug.Log("[NPCScript] NPC recibió la bomba.");
    }

    // CORRECCIÓN: Método ahora es público (para BombManager)
    public void HasBomb(bool value)
    {
        hasBomb = value;
        localOwnerFlag = value;
    }

    // CORRECCIÓN: Método ahora es público (para BombManager)
    public void SetBehavior(NPCBehavior behavior)
    {
        requestedBehavior = behavior;
    }

    // ----------------------------
    // Lanzamiento (Sin cambios)
    // ----------------------------
    private void DoThrow(Vector3 direction)
    {
        // ... (Tu lógica original sin cambios)
        if (bomb == null || handPoint == null) return;
        bomb.transform.position = handPoint.position + direction * 0.5f;
        bomb.transform.SetParent(null);
        bomb.Launch(direction, throwForce, gameObject);
        hasBomb = false;
        localOwnerFlag = false;
        if (bombManager != null) bombManager.OnBombThrown();
        currentState = NPCState.Attack_Throwing;
        throwCooldown = 2f;
        Debug.Log("[NPCScript] Bomba lanzada.");
    }

    // ----------------------------
    // Movimiento helper (Punto clave de animación)
    // ----------------------------
    void MoveTowards(Vector3 dir, float speed)
    {
        if (dir.sqrMagnitude < 0.001f || controller == null || gameObject == null)
        {
            // Si la lógica llama con un vector de dirección nulo, no se mueve.
            return;
        }

        // ** 🎯 ANIMATOR: Activar la bool 'move' **
        if (anim != null)
        {
            anim.SetBool(moveHash, true); // Aseguramos que se activa
        }
        isMovingThisFrame = true; // Establecemos el flag para el control en Update()

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

        // Establecer la velocidad horizontal (X, Z) que será aplicada en Update()
        Vector3 forwardDir = transform.forward * speed;
        velocity.x = forwardDir.x;
        velocity.z = forwardDir.z;
    }

    // ----------------------------
    // IA y Corrutinas (Sin cambios)
    // ----------------------------
    private IEnumerator TransferStunRoutine(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    IEnumerator AutoThrowLoop()
    {
        // ... (Tu lógica original sin cambios)
        while (true)
        {
            float wait = Random.Range(autoThrowMinDelay, autoThrowMaxDelay);
            yield return new WaitForSeconds(wait);
            if (hasBomb && canThrow && !isThrowing && gameObject != null && !isStunned)
                AttemptThrow();
        }
    }

    void AttemptThrow()
    {
        // ... (Tu lógica original sin cambios)
        float prob = 0f;
        if (currentTarget != null && currentTarget.gameObject != null)
        {
            float d = Vector3.Distance(transform.position, currentTarget.position);
            prob = (d <= closeThrowDistance) ? nearThrowProbability : farThrowProbability;
        }
        else
        {
            prob = farThrowProbability * 0.5f;
        }
        if (Random.value <= prob) StartCoroutine(ThrowRoutine());
    }

    IEnumerator ThrowRoutine()
    {
        // ... (Tu lógica original sin cambios)
        isThrowing = true;
        canThrow = false;
        float aimDelay = Random.Range(0.2f, 0.6f);
        yield return new WaitForSeconds(aimDelay);
        if (hasBomb && BombManager.Instance != null && handPoint != null)
        {
            Vector3 dir = (currentTarget != null && currentTarget.gameObject != null)
                ? (currentTarget.position - handPoint.position).normalized
                : transform.forward;
            dir += new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(0f, 0.06f), Random.Range(-0.12f, -0.12f));
            dir.Normalize();
            DoThrow(dir);
        }
        yield return new WaitForSeconds(1.0f);
        isThrowing = false;
        yield return new WaitForSeconds(0.5f);
        canThrow = true;
    }

    // ----------------------------
    // Actualizar target (Sin cambios)
    // ----------------------------
    private void UpdateCurrentTarget()
    {
        // ... (Tu lógica original sin cambios)
        if (preferredTarget != null && preferredTarget.gameObject != null)
        {
            float d = Vector3.Distance(transform.position, preferredTarget.position);
            if (d <= detectTargetRadius)
            {
                currentTarget = preferredTarget;
                return;
            }
        }
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            float d = Vector3.Distance(transform.position, playerObj.transform.position);
            if (d <= detectTargetRadius)
            {
                currentTarget = playerObj.transform;
                return;
            }
        }
        if (currentTarget != null)
        {
            float d = Vector3.Distance(transform.position, currentTarget.position);
            if (d > detectTargetRadius) currentTarget = null;
        }
    }

    // ----------------------------
    // Sprint (Sin cambios)
    // ----------------------------
    private IEnumerator SprintRoutine()
    {
        isSprinting = true;
        canSprint = false;
        yield return new WaitForSeconds(sprintDuration);
        isSprinting = false;
        yield return new WaitForSeconds(sprintCooldown);
        canSprint = true;
    }
}