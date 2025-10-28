using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerScript : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 4f;
    public float sprintMultiplier = 1.8f;
    public float gravity = -9.81f;
    public float rotationSpeed = 10f;

    [Header("Sprint")]
    public float sprintDuration = 1.0f;
    public float sprintCooldown = 3.0f;

    [Header("Lanzamiento")]
    public Transform launchPoint;
    public float throwForce = 10f;
    public float pickupRadius = 2f;

    [Header("Referencias")]
    public BombManager bombManager;
    public LayerMask bombMask;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isSprinting = false;
    private bool canSprint = true;
    private bool hasBomb = false;
    private float originalSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (bombManager == null)
            bombManager = FindFirstObjectByType<BombManager>();
    }

    private void Start()
    {
        originalSpeed = moveSpeed;
    }

    private void Update()
    {
        HandleMovement();
        HandleThrowInput();
        HandlePickup();
    }

    private void HandleMovement()
    {
        float horizontal = Keyboard.current.aKey.isPressed ? -1 :
                           Keyboard.current.dKey.isPressed ? 1 : 0;
        float vertical = Keyboard.current.sKey.isPressed ? -1 :
                         Keyboard.current.wKey.isPressed ? 1 : 0;

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        if (inputDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Sprint input
        if (Keyboard.current.spaceKey.wasPressedThisFrame && canSprint && !isSprinting)
        {
            StartCoroutine(SprintRoutine());
        }

        float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
        Vector3 moveDirection = transform.forward * inputDirection.magnitude;
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;
        else
            velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }

    private IEnumerator SprintRoutine()
    {
        isSprinting = true;
        canSprint = false;

        Debug.Log("[Player] Sprint iniciado.");
        yield return new WaitForSeconds(sprintDuration);

        isSprinting = false;
        Debug.Log("[Player] Sprint terminado. Cooldown iniciado.");
        yield return new WaitForSeconds(sprintCooldown);

        canSprint = true;
        Debug.Log("[Player] Sprint listo nuevamente.");
    }

    private void HandleThrowInput()
    {
        if (hasBomb && Keyboard.current.leftShiftKey.wasPressedThisFrame)
        {
            LaunchBomb();
        }
    }

    private void LaunchBomb()
    {
        BombScript bomb = bombManager.bomb;
        if (bomb == null || launchPoint == null)
        {
            Debug.LogWarning("[PlayerScript] No se puede lanzar: bomba o punto de lanzamiento nulo.");
            return;
        }

        bomb.transform.position = launchPoint.position;
        Vector3 direction = (transform.forward + Vector3.up * 0.2f).normalized;
        bomb.Launch(direction, throwForce, gameObject);

        hasBomb = false;
        bombManager.OnBombThrown();
        Debug.Log("[PlayerScript] Bomba lanzada.");
    }

    private void HandlePickup()
    {
        if (hasBomb || bombManager == null || bombManager.bomb == null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius, bombMask);
        foreach (var hit in hits)
        {
            if (hit == null || hit.gameObject == null) continue;

            BombScript nearbyBomb = hit.GetComponent<BombScript>();
            if (nearbyBomb != null && nearbyBomb.currentCondition == BombScript.Condition.OnFloor)
            {
                bombManager.bomb = nearbyBomb;
                nearbyBomb.Adhere(transform);
                hasBomb = true;
                bombManager.SetBombOwner(gameObject);
                break;
            }
        }
    }

    public void ReceiveBomb(BombScript newBomb)
    {
        bombManager.bomb = newBomb;
        newBomb.Adhere(transform);
        hasBomb = true;
    }

    public bool HasBomb() => hasBomb;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = hasBomb ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}

