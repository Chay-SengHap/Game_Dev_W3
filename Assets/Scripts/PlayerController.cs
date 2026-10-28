using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// PlayerController:
/// 1. Solid Physics Movement: Moves via Rigidbody so the player NEVER walks through walls.
/// 2. Coin Collection: Automatically finds, triggers, and collects all coins/orbs.
/// 3. Floor Safety: Prevents falling through the floor.
/// 4. Camera Follow: Keeps camera aimed cleanly at the player.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 7f;

    [Header("Lab Wiring")]
    [SerializeField] private GameObject orbPrefab;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 9f, -7f);

    private Rigidbody rb;
    private Vector3 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Configure Rigidbody for solid physical collisions against walls:
        // - Freeze X and Z rotation so the player never topples over
        // - Continuous collision detection stops tunneling through walls
        // - Interpolate makes physics movement butter-smooth
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Ensure player has a collider
        if (GetComponent<Collider>() == null)
        {
            CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
            col.radius = 0.5f;
            col.height = 1.8f;
            col.center = new Vector3(0f, 0.9f, 0f);
        }
    }

    void Start()
    {
        Debug.Log("[PlayerController] Ready! Walk with WASD or Arrow Keys. Collect all coins to win.");

        // Automatically setup coins in the scene and remove duplicates
        AutoSetupCoins();

        // Ensure a GameManager exists for UI score display
        EnsureGameManager();

        // Refresh coin count in GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateTotalCoins();
        }
    }

    void Update()
    {
        ReadInput();
    }

    void FixedUpdate()
    {
        // Physics movement in FixedUpdate ensures we NEVER go through walls!
        ApplyMovement();

        // Safety floor check: if player ever slips below floor level, snap back up immediately
        if (transform.position.y < 0.1f)
        {
            Vector3 safePos = transform.position;
            safePos.y = 1f;
            transform.position = safePos;
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            }
        }
    }

    void LateUpdate()
    {
        // Smooth camera follow if cameraTransform is assigned here
        if (cameraTransform != null)
        {
            Vector3 targetCamPos = transform.position + cameraOffset;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetCamPos, 6f * Time.deltaTime);
            cameraTransform.LookAt(transform.position + Vector3.up * 0.8f);
        }
    }

    private void ReadInput()
    {
        float h = 0f;
        float v = 0f;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (h == 0f && v == 0f)
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
        }
#elif !ENABLE_INPUT_SYSTEM
        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");
#endif

        moveInput = new Vector3(h, 0f, v).normalized;
    }

    private void ApplyMovement()
    {
        if (rb == null) return;

        // Preserve gravity (Y velocity) while moving on X and Z
        Vector3 currentVel = rb.linearVelocity;
        rb.linearVelocity = new Vector3(moveInput.x * moveSpeed, currentVel.y, moveInput.z * moveSpeed);
    }

    // Trigger detection (when coin has 'Is Trigger' checked)
    private void OnTriggerEnter(Collider other)
    {
        TryCollectCoin(other.gameObject);
    }

    // Solid collision detection (walls, or coins if collider is not a trigger)
    private void OnCollisionEnter(Collision collision)
    {
        if (TryCollectCoin(collision.gameObject))
        {
            return;
        }

        Debug.Log($"[PlayerController] Bumped into: {collision.gameObject.name}");
    }

    private bool TryCollectCoin(GameObject targetObj)
    {
        if (targetObj == null) return false;

        // Find root coin object
        Transform coinTransform = targetObj.transform;
        while (coinTransform.parent != null && coinTransform.parent.name.ToLower().Contains("coin"))
        {
            coinTransform = coinTransform.parent;
        }

        GameObject coinRoot = coinTransform.gameObject;

        bool isCoin = coinRoot.GetComponent<Orb>() != null || 
                      coinRoot.name.ToLower().Contains("coin");

        if (isCoin)
        {
            Debug.Log($"[PlayerController] Coin Collected: {coinRoot.name}!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.Collect();
            }

            Destroy(coinRoot);
            return true;
        }

        return false;
    }

    // Automatically configure all coins in the scene and remove duplicates
    private void AutoSetupCoins()
    {
        HashSet<GameObject> uniqueCoins = new HashSet<GameObject>();

        // Find all root coin objects
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj == gameObject || obj.transform.IsChildOf(transform)) continue;

            if (obj.name.ToLower().Contains("coin") || obj.GetComponent<Orb>() != null)
            {
                Transform root = obj.transform;
                while (root.parent != null && root.parent.name.ToLower().Contains("coin"))
                {
                    root = root.parent;
                }
                uniqueCoins.Add(root.gameObject);
            }
        }

        // Configure each unique coin
        foreach (GameObject coinRoot in uniqueCoins)
        {
            // Remove duplicate Orb components on the root object
            Orb[] rootOrbs = coinRoot.GetComponents<Orb>();
            for (int i = 1; i < rootOrbs.Length; i++)
            {
                Destroy(rootOrbs[i]);
            }

            // Remove Orb components from child mesh objects
            foreach (Transform child in coinRoot.transform)
            {
                Orb[] childOrbs = child.GetComponentsInChildren<Orb>(true);
                foreach (var co in childOrbs)
                {
                    Destroy(co);
                }
            }

            // Ensure the root object has exactly ONE Orb component
            if (coinRoot.GetComponent<Orb>() == null)
            {
                coinRoot.AddComponent<Orb>();
            }

            // Ensure trigger collider exists on the coin
            Collider col = coinRoot.GetComponent<Collider>();
            if (col == null)
            {
                SphereCollider sc = coinRoot.AddComponent<SphereCollider>();
                sc.radius = 0.6f;
                sc.isTrigger = true;
            }
            else
            {
                col.isTrigger = true;
            }
        }
    }

    private void EnsureGameManager()
    {
        if (GameManager.Instance == null)
        {
            GameManager existing = FindFirstObjectByType<GameManager>();
            if (existing == null)
            {
                GameObject gm = new GameObject("GameManager");
                gm.AddComponent<GameManager>();
            }
        }
    }

    private void OnDestroy()
    {
        if (orbPrefab != null)
        {
            Instantiate(orbPrefab, transform.position, Quaternion.identity);
            Debug.Log("[PlayerController] Bonus orb spawned on destroy!");
        }
    }
}
