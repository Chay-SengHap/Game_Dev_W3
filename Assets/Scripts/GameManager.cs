using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameManager: Keeps track of score, shows live UI in the corner,
/// and displays "YOU WIN!" when all coins are collected.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("State")]
    [SerializeField] private int totalOrbs = 0;

    private int collected = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateTotalCoins();
    }

    public void UpdateTotalCoins()
    {
        // Use a HashSet to count only UNIQUE coin GameObjects (ignoring duplicate scripts or child parts)
        HashSet<GameObject> uniqueCoins = new HashSet<GameObject>();

        // 1. Check all objects with Orb script
        foreach (Orb orb in FindObjectsByType<Orb>(FindObjectsSortMode.None))
        {
            Transform root = GetCoinRoot(orb.transform);
            uniqueCoins.Add(root.gameObject);
        }

        // 2. Check all objects with "coin" in their name
        foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.name.ToLower().Contains("coin"))
            {
                Transform root = GetCoinRoot(go.transform);
                uniqueCoins.Add(root.gameObject);
            }
        }

        totalOrbs = uniqueCoins.Count;
        Debug.Log($"[GameManager] Total unique coins detected: {totalOrbs}");
    }

    private Transform GetCoinRoot(Transform t)
    {
        // Traverse up to the top-level coin object (e.g., "coin-gold (1)")
        Transform current = t;
        while (current.parent != null && current.parent.name.ToLower().Contains("coin"))
        {
            current = current.parent;
        }
        return current;
    }

    public void Collect()
    {
        collected++;
        Debug.Log($"[GameManager] Coins Collected: {collected} / {totalOrbs}");
    }

    void OnGUI()
    {
        // Draw score in the top-left corner
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold
        };
        labelStyle.normal.textColor = Color.yellow;

        if (collected >= totalOrbs && totalOrbs > 0)
        {
            // Big centered "YOU WIN!" banner
            GUIStyle winStyle = new GUIStyle(labelStyle)
            {
                fontSize = 50,
                alignment = TextAnchor.MiddleCenter
            };
            winStyle.normal.textColor = Color.green;

            GUI.Label(new Rect(0, Screen.height / 2f - 60, Screen.width, 120), "🎉 YOU WIN! 🎉", winStyle);
        }
        else
        {
            GUI.Label(new Rect(20, 20, 450, 40), $"Coins Collected: {collected} / {totalOrbs}", labelStyle);
        }
    }
}
