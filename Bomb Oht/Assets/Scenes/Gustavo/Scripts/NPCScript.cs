using UnityEngine;
using System.Collections;

/// <summary>
/// NPCScript
/// - Si tiene la bomba: actúa ofensivamente (persigue, lanza, recoge si lanzó y falló)
/// - Si no tiene la bomba: huye del actual poseedor y NO recoge la bomba
/// - Lanzamientos ocasionales incluso desde lejos (comportamiento impredecible)
/// - Sprint dinámico
/// - Compatible con BombManager / BombScript / PlayerScript entregados antes
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class NPCScript : MonoBehaviour
{
    // --- Inspector / tuning ---
    [Header("Referencias")]
    public Transform launcherPoint;                // punto desde donde lanza la bomba
    public Transform preferredTarget;              // opcional: asigna el Player o deja null para auto-detectar

    [Header("Movimiento")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 7f;
    public float gravity = -9.81f;
    public float rotationSpeed = 8f;

    [Header("Decisiones y rangos")]
    public float detectTargetRadius = 20f;         // radio para encontrar oponentes
    public float closeThrowDistance = 6f;          // distancia "cerca" para lanzar con más probabilidad
    public float pickupDistance = 1.6f;            // distancia para "recoger" la bomba cuando es dueño

    [Header("Probabilidades y tiempos")]
    [Range(0f, 1f)] public float nearThrowProbability = 0.75f;    // si target está cerca
    [Range(0f, 1f)] public float farThrowProbability = 0.18f;    // si está lejos
    public float autoThrowMinDelay = 1.0f;         // tiempo mínimo entre intentos automáticos
    public float autoThrowMaxDelay = 4.0f;         // tiempo máximo entre intentos automáticos
    public float throwForce = 10f;                 // fuerza base de lanzamiento

    [Header("Sprint")]
    public float sprintDuration = 1.0f;
    public float sprintCooldown = 3.0f;
    [Range(0f, 1f)] public float sprintChanceWhenClose = 0.6f; // prob. de sprint cuando target se acerca
    [Range(0f, 1f)] public float sprintChanceWhenFar = 0.25f;  // prob. de sprint para alcanzar objetivo

    // --- Estados internos ---
    private CharacterController controller;
    private Transform currentTarget;               // objetivo actual (player u otro NPC)
    private Vector3 velocity;
    private bool hasBomb = false;                  // si en este momento el NPC tiene la bomba
    private bool localOwnerFlag = false;           // si este NPC se considera dueño incluso tras lanzar (importante)
    private bool isSprinting = false;
    private bool canSprint = true;
    private bool isThrowing = false;
    private bool canThrow = true;


    // Enum para compatibilidad con BombManager que pueda pedir SetBehavior
    public enum NPCBehavior { Idle, Attack, Flee, Hunt }
    private NPCBehavior requestedBehavior = NPCBehavior.Idle;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Si no asignaste preferredTarget desde el inspector, busca el player por tag
        if (preferredTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) preferredTarget = p.transform;
        }

        // Arrancar la corrutina que decide lanzar de forma intermitente si tiene la bomba
        StartCoroutine(AutoThrowLoop());
    }

    void Update()
    {
        // actualizar target dinámicamente: objetivo es el jugador u otro personaje sin bomba más cercano
        UpdateCurrentTarget();
        GameObject[] allNPCs = GameObject.FindGameObjectsWithTag("NPC");
        foreach (var n in allNPCs)
        {
            if (n == null) continue;
            if (BombManager.Instance != null && BombManager.Instance.currentOwner == n) continue;

            // lógica segura
        }
        // Lógica principal según posesión
        if (hasBomb)
        {
            OffensiveBehaviour();
        }
        else
        {
            DefensiveBehaviour();
        }

        // Aplicar gravedad
        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // ----------------------------
    // Core behaviours
    // ----------------------------
    void OffensiveBehaviour()
    {
        // Si tengo bomba → debo perseguir a un objetivo para tocarlo o lanzarle la bomba
        if (currentTarget == null)
        {
            // si no hay objetivo detectable, puedo patrullar (aquí simplemente quedo idle para simplicidad)
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        // Si estoy lo suficientemente cerca: intentar tocar (mover hacia el objetivo)
        Vector3 dirToTarget = (currentTarget.position - transform.position);
        dirToTarget.y = 0f;
        Vector3 moveDir = dirToTarget.normalized;

        // Posible sprint para alcanzar
        if (!isSprinting && canSprint)
        {
            float chance = (dist <= closeThrowDistance) ? sprintChanceWhenClose : sprintChanceWhenFar;
            if (Random.value < chance) StartCoroutine(SprintRoutine());
        }

        // movimiento hacia objetivo (si no estoy lanzando)
        float moveSpeed = isSprinting ? sprintSpeed : walkSpeed;
        MoveTowards(moveDir, moveSpeed);

        // Si lancé en el pasado y la bomba quedó fuera (en vuelo o en suelo), debo ir a recogerla
        // Nota: la bandera localOwnerFlag se activa en HasBomb(true) y permanece true incluso tras Launch()
        if (localOwnerFlag && BombManager.Instance != null)
        {
            var bomb = BombManager.Instance.bomb;
            if (bomb != null)
            {
                // Si la bomba no está adherida a alguien (es OnFly o OnFloor) y yo soy el dueño local,
                // priorizo recogerla: ir directo hacia la bomba hasta pickupDistance
                if (bomb.currentCondition == BombScript.Condition.OnFly || bomb.currentCondition == BombScript.Condition.OnFloor)
                {
                    float distToBomb = Vector3.Distance(transform.position, bomb.transform.position);
                    // mover más directo hacia la bomba para recogerla
                    Vector3 dirToBomb = (bomb.transform.position - transform.position);
                    dirToBomb.y = 0f;
                    MoveTowards(dirToBomb.normalized, isSprinting ? sprintSpeed : walkSpeed);

                    if (distToBomb <= pickupDistance)
                    {
                        // recoger: pedir al BombManager que nos asigne la bomba
                        BombManager.Instance.SetBombOwner(this.gameObject);
                        // tras recoger, seguimos ofensivos como dueño (HasBomb(true) se llamará dentro de SetBombOwner)
                    }
                    return; // priorizamos recoger sobre perseguir
                }
            }
        }

        // Si estoy en rango de "tocar" físicamente al objetivo, el contacto debería ser manejado por BombScript (OverlapSphere o colisiones)
        // Adicionalmente, podemos decidir lanzar con cierta probabilidad (auto-throw coroutine también maneja esto)
    }

    void DefensiveBehaviour()
    {
        // Si NO tengo la bomba → huir del actual poseedor (si existe)
        GameObject owner = (BombManager.Instance != null) ? BombManager.Instance.currentOwner : null;
        if (owner != null && owner.gameObject != null)
        {
            Vector3 dirAway = (transform.position - owner.transform.position);
            dirAway.y = 0f;
            Vector3 moveDir = dirAway.normalized;

            float distToOwner = Vector3.Distance(transform.position, owner.transform.position);
            if (!isSprinting && canSprint && distToOwner < closeThrowDistance)
            {
                if (Random.value < sprintChanceWhenClose) StartCoroutine(SprintRoutine());
            }

            float moveSpeed = isSprinting ? sprintSpeed : walkSpeed;
            MoveTowards(moveDir, moveSpeed);
        }

        else
        {
            // Si no hay owner asignado (caso raro), mantener comportamiento neutro (no intentar recoger salvo si se quiere)
            // según tus reglas: solo el dueño puede recoger, así que aquí no hacemos pickup
            // Podrías agregar patrulla si quieres
        }
    }

    // ----------------------------
    // Movement helper
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
    // Auto-throw loop: intenta lanzar ocasionalmente mientras tenga la bomba
    // ----------------------------
    IEnumerator AutoThrowLoop()
    {
        while (true)
        {
            float wait = Random.Range(autoThrowMinDelay, autoThrowMaxDelay);
            yield return new WaitForSeconds(wait);

            if (hasBomb && canThrow && !isThrowing && gameObject != null)
            {
                AttemptThrow();
            }
        }
    }


    // Decide si lanzar y ejecuta la acción (corrutina ThrowRoutine maneja el delay real)

    void AttemptThrow()
    {
        if (!hasBomb || !canThrow || isThrowing || gameObject == null) return;

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
        {
            StartCoroutine(ThrowRoutine());
        }
    }


    // Corrutina que ejecuta el lanzamiento con pequeña demora (simule "apuntar")

    IEnumerator ThrowRoutine()
    {
        isThrowing = true;
        canThrow = false;

        float aimDelay = Random.Range(0.2f, 0.6f);
        yield return new WaitForSeconds(aimDelay);

        if (hasBomb && BombManager.Instance != null && launcherPoint != null)
        {
            Vector3 dir;
            if (currentTarget != null && currentTarget.gameObject != null)
                dir = (currentTarget.position - launcherPoint.position).normalized;
            else
                dir = transform.forward;

            dir += new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(0f, 0.06f), Random.Range(-0.12f, 0.12f));
            dir.Normalize();

            var bomb = BombManager.Instance.bomb;
            if (bomb != null && bomb.gameObject != null)
            {
                bomb.transform.position = launcherPoint.position;
                localOwnerFlag = true;

                // ✅ Corrección aquí: se pasa el GameObject como dueño
                bomb.Launch(dir, throwForce, gameObject);

                try { BombManager.Instance.OnBombThrown(); } catch { }
            }
        }

        yield return new WaitForSeconds(1.0f);
        isThrowing = false;
        yield return new WaitForSeconds(0.5f);
        canThrow = true;
    }


    // ----------------------------
    // Sprint routine
    // ----------------------------
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
    // Helper: actualizar target (elige el objetivo más cercano que no tenga la bomba)
    // ----------------------------
    void UpdateCurrentTarget()
    {
        float bestDist = float.MaxValue;
        Transform best = null;

        if (preferredTarget != null && preferredTarget.gameObject != null)
        {
            GameObject pgo = preferredTarget.gameObject;
            bool pHas = (BombManager.Instance != null && BombManager.Instance.currentOwner == pgo);
            if (!pHas)
            {
                float d = Vector3.Distance(transform.position, preferredTarget.position);
                if (d < bestDist) { bestDist = d; best = preferredTarget; }
            }
        }

        GameObject[] allNPCs = GameObject.FindGameObjectsWithTag("NPC");
        foreach (var n in allNPCs)
        {
            if (n == null || n == this.gameObject || n.gameObject == null) continue;
            bool nHas = (BombManager.Instance != null && BombManager.Instance.currentOwner == n);
            if (nHas) continue;

            float d = Vector3.Distance(transform.position, n.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = n.transform;
            }
        }

        currentTarget = best;
    }


    // ----------------------------
    // API pública para que BombManager llame cuando la posesión cambia
    // ----------------------------
    public void HasBomb(bool value)
    {
        hasBomb = value;
        // Si pasamos a ser dueños, también marcamos la bandera local
        if (value)
        {
            localOwnerFlag = true;
        }
        else
        {
            localOwnerFlag = false;
        }
    }

    // Compatibilidad con BombManager.SetBehavior(...)
    public void SetBehavior(NPCBehavior behavior)
    {
        requestedBehavior = behavior;
        // la implementación actual ignora requestedBehavior en favor de la lógica principal,
        // pero lo mantenemos para compatibilidad y extensibilidad futura.
    }

    // ----------------------------
    // Visual debugging
    // ----------------------------
    void OnDrawGizmosSelected()
    {
        Gizmos.color = hasBomb ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, detectTargetRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, closeThrowDistance);
    }
}

