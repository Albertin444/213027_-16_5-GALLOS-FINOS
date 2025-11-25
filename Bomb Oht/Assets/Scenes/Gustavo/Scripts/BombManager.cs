using UnityEngine;
using System.Collections.Generic;

public class BombManager : MonoBehaviour
{
    public static BombManager Instance;

    [Header("Referencias")]
    public BombScript bomb;
    public PlayerScript player;
    public List<NPCScript> npcList;

    [Header("Estado de la bomba")]
    public GameObject currentOwner;   // dueño actual
    public GameObject lastOwner;      // último dueño registrado

    public float pickupRadius = 2f;
    private float pickupCooldown = 0f;


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (player == null)
            Debug.LogError("[BombManager] PlayerScript no asignado.");

        if (bomb != null)
            bomb.onBombHit += OnBombHit;
        else
            Debug.LogError("[BombManager] Bomba no asignada.");

        AssignRandomOwner();
    }

    void Update()
    {
        if (pickupCooldown > 0f)
            pickupCooldown -= Time.deltaTime;

        if (currentOwner == null && pickupCooldown <= 0f)
        {
            if (player != null && player.gameObject != null)
                CheckForPickup(player.gameObject);

            npcList.RemoveAll(npc => npc == null || npc.gameObject == null);

            foreach (var npc in npcList)
            {
                if (npc != null && npc.gameObject != null)
                    CheckForPickup(npc.gameObject);
            }
        }
    }

    public void AssignRandomOwner()
    {
        List<GameObject> options = new();

        if (player != null) options.Add(player.gameObject);
        foreach (var npc in npcList)
        {
            if (npc != null && npc.gameObject != null)
                options.Add(npc.gameObject);
        }

        if (options.Count == 0)
        {
            Debug.LogWarning("[BombManager] No hay entidades disponibles.");
            return;
        }

        GameObject chosen = options[Random.Range(0, options.Count)];
        SetBombOwner(chosen);
        Debug.Log($"🔥 Bomba asignada a: {chosen.name}");
       
    }

    public void SetBombOwner(GameObject newOwner)
    {
        if (newOwner == null || newOwner.gameObject == null) return;
        if (newOwner == currentOwner) return;

        currentOwner = newOwner;
        lastOwner = newOwner;

        Transform bombPoint = newOwner.transform.Find("BombPoint");
        if (bombPoint != null && bomb != null)
            bomb.Adhere(bombPoint);

        if (newOwner.CompareTag("NPC"))
        {
            NPCScript npc = newOwner.GetComponent<NPCScript>();
            if (npc != null)
            {
                npc.HasBomb(true);
                npc.ReceiveBomb(bomb);
            }
        }
        else if (newOwner.CompareTag("Player") && player != null)
        {
            player.ReceiveBomb(bomb);
        }

        foreach (var npc in npcList)
        {
            if (npc == null) continue;
            if (npc.gameObject == newOwner)
                npc.SetBehavior(NPCScript.NPCBehavior.Attack);
            else
                npc.SetBehavior(NPCScript.NPCBehavior.Flee);
        }
    }

    public void OnBombThrown()
    {
        if (bomb != null)
            bomb.currentCondition = BombScript.Condition.OnFly;

        currentOwner = null;
        pickupCooldown = 1.5f;
    }

    public void ClearOwner()
    {
        if (currentOwner != null)
        {
            if (currentOwner.CompareTag("NPC"))
            {
                NPCScript npc = currentOwner.GetComponent<NPCScript>();
                if (npc != null) npc.HasBomb(false);
            }
            else if (currentOwner.CompareTag("Player"))
            {
                PlayerScript p = currentOwner.GetComponent<PlayerScript>();
                if (p != null) p.ResetBomb();
            }
        }

        currentOwner = null;
        if (bomb != null)
        {
            bomb.currentCondition = BombScript.Condition.OnFloor;
            bomb.transform.SetParent(null);
        }
    }

    // ✅ Ahora la bomba se pega siempre que toque un oponente
    void OnBombHit(GameObject hit)
    {
        if (hit == null || bomb == null) return;

        if (hit.CompareTag("Player") || hit.CompareTag("NPC"))
        {
            SetBombOwner(hit);
        }
        else
        {
            bomb.currentCondition = BombScript.Condition.OnFloor;
        }
    }

    void CheckForPickup(GameObject entity)
    {
        if (entity == null || bomb == null) return;

        float distance = Vector3.Distance(entity.transform.position, bomb.transform.position);
        if (distance <= pickupRadius)
        {
            SetBombOwner(entity);
        }
    }
}