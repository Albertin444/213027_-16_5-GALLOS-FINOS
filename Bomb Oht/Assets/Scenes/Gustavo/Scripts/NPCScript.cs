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
        // 🛑 BLOQUEAR MOVIMIENTO SI ESTÁ EN PAUSA (después del contacto) 🛑
        if (isStunned)
        {
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
            // La bomba está EN POSESIÓN del oponente (es una amenaza).
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
                        return; // Detener la ejecución para huir
                    }
                }

                // Si la bomba es MÍA (fallé el lanzamiento) o está en el suelo (neutra), la recojo.
                if (currentBomb.lastOwner == gameObject || currentBomb.currentCondition == BombScript.Condition.OnFloor)
                {
                    CollectBombBehaviour();
                    currentState = NPCState.Idle;
                    return;
                }
            }

            // 2b. Comportamiento Ofensivo/Idle:
            OffensiveBehaviour();
            currentState = hasBomb ? NPCState.Attack_HasBomb : NPCState.Idle;
        }

        // ➡️ LÓGICA DE ESTADOS INTERNOS Y GRAVEDAD
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
        // Solo proceder si: yo tengo la bomba y no la estoy lanzando activamente.
        if (!hasBomb || isThrowing || isStunned) return;

        GameObject target = hit.gameObject;

        // Verificar si el objetivo es un oponente válido (Player o NPC)
        if (target.CompareTag("Player") || target.CompareTag("NPC"))
        {
            bool targetHasBomb = false;

            // Intenta obtener el script del oponente
            if (target.CompareTag("Player"))
            {
                PlayerScript targetScript = target.GetComponent<PlayerScript>();
                if (targetScript != null)
                    targetHasBomb = targetScript.hasBomb;
            }
            else // NPC
            {
                NPCScript targetScript = target.GetComponent<NPCScript>();
                if (targetScript != null)
                    targetHasBomb = targetScript.hasBomb;
            }

            // 2. Si el oponente no tiene la bomba, la transferimos.
            if (!targetHasBomb)
            {
                // 🛑 TRANSFERENCIA CLAVE 🛑

                // Llamar al método de recepción del oponente
                target.SendMessage("ReceiveBomb", bomb, SendMessageOptions.DontRequireReceiver);

                // Limpiar mi estado de posesión.
                hasBomb = false;
                bomb = null; // CRÍTICO: Eliminar la referencia local.

                // Frenar inmediatamente para evitar el pegado
                if (rbd != null)
                {
                    rbd.linearVelocity = Vector3.zero;
                    rbd.angularVelocity = Vector3.zero;
                }

                velocity = Vector3.zero; // Detener el CharacterController
                isSprinting = false;

                // Iniciar la pausa para que la IA se reevalúe
                StartCoroutine(TransferStunRoutine(0.2f));

                Debug.Log($"Bomba adherida por contacto a: {target.name}");
            }
        }
    }

    // ----------------------------
    // Comportamiento ofensivo (Buscar objetivo SOLO si tiene la bomba)
    // ----------------------------
    void OffensiveBehaviour()
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
    // API pública
    // ----------------------------
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

    public void HasBomb(bool value)
    {
        hasBomb = value;
        localOwnerFlag = value;
    }

    public void SetBehavior(NPCBehavior behavior)
    {
        requestedBehavior = behavior;
    }

    // ----------------------------
    // Lanzamiento
    // ----------------------------
    private void DoThrow(Vector3 direction)
    {
        if (bomb == null || handPoint == null) return;

        bomb.transform.position = handPoint.position + direction * 0.5f;
        bomb.transform.SetParent(null);

        bomb.Launch(direction, throwForce, gameObject);

        hasBomb = false;
        localOwnerFlag = false;

        if (bombManager != null)
            bombManager.OnBombThrown();

        currentState = NPCState.Attack_Throwing;
        throwCooldown = 2f;

        Debug.Log("[NPCScript] Bomba lanzada.");
    }

    // ----------------------------
    // Movimiento helper 
    // ----------------------------
    void MoveTowards(Vector3 dir, float speed)
    {
        if (dir.sqrMagnitude < 0.001f || controller == null || gameObject == null) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

        Vector3 move = transform.forward * speed * Time.deltaTime;
        controller.Move(move);
    }

    // ----------------------------
    // IA y Corrutinas
    // ----------------------------

    private IEnumerator TransferStunRoutine(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    IEnumerator AutoThrowLoop()
    {
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
        if (!hasBomb || !canThrow || isThrowing || gameObject == null || isStunned) return;

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

        if (Random.value <= prob)
            StartCoroutine(ThrowRoutine());
    }

    IEnumerator ThrowRoutine()
    {
        isThrowing = true;
        canThrow = false;

        float aimDelay = Random.Range(0.2f, 0.6f);
        yield return new WaitForSeconds(aimDelay);

        if (hasBomb && BombManager.Instance != null && handPoint != null)
        {
            Vector3 dir = (currentTarget != null && currentTarget.gameObject != null)
                ? (currentTarget.position - handPoint.position).normalized
                : transform.forward;

            dir += new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(0f, 0.06f), Random.Range(-0.12f, 0.12f));
            dir.Normalize();

            DoThrow(dir);
        }

        yield return new WaitForSeconds(1.0f);
        isThrowing = false;
        yield return new WaitForSeconds(0.5f);
        canThrow = true;
    }

    // ----------------------------
    // Actualizar target 
    // ----------------------------
    private void UpdateCurrentTarget()
    {
        // Si hay un preferredTarget válido, usarlo
        if (preferredTarget != null && preferredTarget.gameObject != null)
        {
            float d = Vector3.Distance(transform.position, preferredTarget.position);
            if (d <= detectTargetRadius)
            {
                currentTarget = preferredTarget;
                return;
            }
        }

        // Si no hay preferredTarget o está fuera de rango, intentar buscar al Player por tag
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

        // Si nada cumple, mantener el target actual (o null si no existía)
        if (currentTarget != null)
        {
            float d = Vector3.Distance(transform.position, currentTarget.position);
            if (d > detectTargetRadius) currentTarget = null;
        }
    }

    // ----------------------------
    // Sprint 
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