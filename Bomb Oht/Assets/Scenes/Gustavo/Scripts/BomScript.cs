using UnityEngine;
using System;

public class BombScript : MonoBehaviour
{
    public enum Condition { OnFloor, OnPlayer, OnFly }
    public Condition currentCondition = Condition.OnFloor;

    public float detectionRadius = 1.5f;
    public LayerMask detectionMask;

    public Rigidbody rbd;
    public Collider col;

    public Action<GameObject> onBombHit;

    private GameObject lastOwner;

    void Awake()
    {
        if (rbd == null) rbd = GetComponent<Rigidbody>();
        if (col == null) col = GetComponent<Collider>();
    }

    private void Update()
    {
        if (currentCondition == Condition.OnFly)
            DetectNearbyTargets();
    }

    public void Launch(Vector3 direction, float force, GameObject owner)
    {
        lastOwner = owner;
        transform.SetParent(null);
        gameObject.SetActive(true);

        rbd.isKinematic = false;
        rbd.useGravity = true;
        col.enabled = true;
        currentCondition = Condition.OnFly;

        rbd.linearVelocity = direction * force;
    }

    public void Adhere(Transform target)
    {
        rbd.isKinematic = true;
        rbd.useGravity = false;
        col.enabled = false;

        transform.SetParent(target);

        if (target.CompareTag("Player"))
            transform.localPosition = new Vector3(0, 0.2f, 0.6f); // ✅ para el jugador
        else if (target.CompareTag("NPC"))
            transform.localPosition = new Vector3(0, 1.0f, 0.3f); // ✅ más cerca del torso del NPC

        transform.localRotation = Quaternion.identity;
        currentCondition = Condition.OnPlayer;
    }

    private void DetectNearbyTargets()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, detectionMask);
        foreach (var hit in hits)
        {
            if (hit == null || hit.gameObject == null) continue;
            if (hit.gameObject == lastOwner) continue;

            if (hit.CompareTag("Player") || hit.CompareTag("NPC"))
            {
                onBombHit?.Invoke(hit.gameObject);
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
