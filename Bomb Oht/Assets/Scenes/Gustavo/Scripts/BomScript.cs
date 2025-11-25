using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BombScript : MonoBehaviour
{
    public enum Condition { OnPlayer, OnNPC, OnFly, OnFloor }

    [Header("Estado")]
    public Condition currentCondition = Condition.OnFloor;

    [Header("Eventos")]
    // Usar System.Action<GameObject>
    public Action<GameObject> onBombHit;

    private Rigidbody rbd;
    private Collider col;
    private GameObject currentOwner;
    public GameObject lastOwner; // Necesario para evitar auto-impacto
    public AudioSource nuevo_objetivo;
    [Header("UI Effector")]
    public GameObject uiEffector;   // arrastra aquí el objeto del Canvas


    void UpdateUIEffector()
    {
        if (lastOwner == null) return;

        if (lastOwner.CompareTag("Player"))
            uiEffector.SetActive(true);     // activar UI
        else
            uiEffector.SetActive(false);    // desactivar UI
    }
    void Awake()
    {
        UpdateUIEffector();
        rbd = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        // Modo de detección de colisión para mejor precisión de impacto
        rbd.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    // --- Adherir bomba a un punto ---
    // BombScript.cs (Método Adhere corregido con limpieza extrema)

    public void Adhere(Transform parent)
    {
        if (rbd != null)
        {
            // 1. ✅ LIMPIAR VELOCIDADES PRIMERO (mientras NO es cinemático)
            rbd.linearVelocity = Vector3.zero; // Línea 45 (Asegúrate de que el número de línea coincida)
            rbd.angularVelocity = Vector3.zero; // Línea 46
            rbd.Sleep(); // Opcional, pero bueno para detener la física

            // 2. 🛑 HACERLO CINEMÁTICO (para la adhesión)
            rbd.isKinematic = true;
            rbd.detectCollisions = false;
        }

        transform.SetParent(parent);
        // 3. Ignorar Colisiones (usando tu lógica que funcionó antes)
        Collider[] ownerColliders = parent.GetComponentsInParent<Collider>(true);
        if (col != null)
        {
            foreach (Collider ownerCol in ownerColliders)
            {
                if (ownerCol != null)
                    Physics.IgnoreCollision(col, ownerCol, true);
            }
        }

        // 4. Establecer Jerarquía y Transform
        transform.SetParent(parent);

        // ✅ CRÍTICO: Reseteo de escala local para evitar deformación por herencia
        transform.localScale = Vector3.one;
        transform.localPosition = Vector3.zero;

        // 5. Reactivar el Collider y la Corrutina
        if (col != null)
        {
            col.enabled = true; // Reactivar el Collider
        }

        if (ownerColliders.Length > 0 && col != null)
        {
            StartCoroutine(RestoreCollisions(ownerColliders, 0.5f));
        }
    }

    // --- Lanzar bomba ---
    public void Launch(Vector3 direction, float force, GameObject owner)
    {
        // 1. Reseteamos escala antes de desanclar
        transform.localScale = Vector3.one;

        transform.SetParent(null); // Desanclar

        // 2. Activar física
        if (rbd != null)
        {
            rbd.isKinematic = false;
            rbd.detectCollisions = true;

            rbd.linearVelocity = Vector3.zero;
            rbd.angularVelocity = Vector3.zero;

            rbd.AddForce(direction * force, ForceMode.VelocityChange);
        }

        currentOwner = owner;
        lastOwner = owner;
        currentCondition = Condition.OnFly;

        // 3. Ignorar colisión con el dueño que lanza (para evitar auto-impacto)
        Collider throwerCol = owner.GetComponent<Collider>();
        if (throwerCol != null && col != null)
        {
            // Ignoramos solo el collider principal del lanzador.
            Physics.IgnoreCollision(col, throwerCol, true);
            // Restauramos la colisión después de un breve delay
            StartCoroutine(RestoreCollisions(new Collider[] { throwerCol }, 0.3f));
        }
    }

    // --- Corrutina para restaurar colisiones (Maneja un array) ---
    private IEnumerator RestoreCollisions(Collider[] ownerCols, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (col != null)
        {
            foreach (Collider c in ownerCols)
            {
                // Si el collider no es nulo, restaurar la colisión
                if (c != null)
                    Physics.IgnoreCollision(col, c, false);
            }
        }
    }

    // --- Colisión ---
    void OnCollisionEnter(Collision collision)
    {
        GameObject hit = collision.gameObject;

        // ⚠️ CRÍTICO: Evitar el auto-impacto o impacto prematuro del lanzador
        if (hit == lastOwner && currentCondition == Condition.OnFly)
        {
            return;
        }

        // Llamar al evento para que el Player/NPC maneje la lógica de ReceiveBomb
        if (hit.CompareTag("Player") || hit.CompareTag("NPC"))
        {
            currentCondition = hit.CompareTag("Player") ? Condition.OnPlayer : Condition.OnNPC;
            nuevo_objetivo.Play();
            UpdateUIEffector();
            onBombHit?.Invoke(hit);
            // ⚠️ La llamada a Adhere se realiza ahora en el script del Player/NPC
        }
        else
        {
            currentCondition = Condition.OnFloor;
        }
    }
}