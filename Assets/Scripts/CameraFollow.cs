using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CameraFollow: Smoothly follows the player and keeps the player centered in view.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Framing")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 9f, -7f);
    [SerializeField] private float followSharpness = 6f;

    void Awake()
    {
        // Fix any distorted scale on the camera
        transform.localScale = Vector3.one;
    }

    void Start()
    {
        FindPlayerTarget();
        SnapToTarget();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindPlayerTarget();
            if (target == null) return;
        }

        // 1. Calculate desired camera position
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSharpness * Time.deltaTime);

        // 2. ALWAYS aim the camera directly at the player's center so it never stares into the floor
        Vector3 lookTarget = target.position + Vector3.up * 0.8f;
        transform.LookAt(lookTarget);
    }

    private void SnapToTarget()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(target.position + Vector3.up * 0.8f);
        }
    }

    private void FindPlayerTarget()
    {
        if (target != null) return;

        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            target = pc.transform;
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null) target = player.transform;
    }
}
