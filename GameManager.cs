// ============================================================
//  HOW TO ADD THIS SCRIPT IN UNITY
// ============================================================
//  1. Right-click your Scripts folder in the Project window
//     -> Create -> C# Script. Name it exactly "GameManager".
//
//  2. In the Hierarchy: GameObject -> Create Empty -> name it "GameManager".
//
//  3. Drag this script onto that empty GameObject, OR click
//     Add Component in the Inspector and search "GameManager".
//
//  4. Only ONE GameManager should ever exist in the scene —
//     the Singleton pattern below enforces this automatically.
//
//  5. No extra wiring needed. House.cs calls
//     GameManager.Instance.OnDeliveryComplete() automatically.
// ============================================================

using UnityEngine;

public class GameManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    private int score;
    private int money;

    [Header("Delivery Rewards")]
    [Tooltip("Minimum coins awarded per delivery.")]
    public int minMoneyReward = 5;

    [Tooltip("Maximum coins awarded per delivery.")]
    public int maxMoneyReward = 20;

    [Tooltip("Score points added per delivery.")]
    public int scorePerDelivery = 100;

    public int Score => score;
    public int Money => money;
    
    // ── Debug / Testing ─────────────────────────────────────────────────
    [ContextMenu("Debug: Add 100 Money")]
    void Debug_AddMoney() { money += 100; OnStatsChanged?.Invoke(score, money); }

    [ContextMenu("Debug: Add 1000 Score")]
    void Debug_AddScore() { score += 1000; OnStatsChanged?.Invoke(score, money); }

    [ContextMenu("Debug: Reset Stats")]
    void Debug_Reset() { ResetStats(); }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); // survives scene loads (e.g. opening the shop)
    }

    /// <summary>Called by House.cs when a delivery is completed.</summary>
    public void OnDeliveryComplete()
    {
        score += scorePerDelivery;

        int reward = Random.Range(minMoneyReward, maxMoneyReward + 1);
        money += reward;

        Debug.Log($"Delivery complete! +{scorePerDelivery} score | +{reward} coins | " +
                  $"Total — Score: {score}  Money: {money}");

        OnStatsChanged?.Invoke(score, money);
    }

    /// <summary>
    /// Call this from your shop to purchase an upgrade.
    /// Returns false if the player can't afford it.
    /// </summary>
    public bool SpendMoney(int amount)
    {
        if (amount > money)
        {
            Debug.Log($"Not enough money. Have {money}, need {amount}.");
            return false;
        }
        money -= amount;
        OnStatsChanged?.Invoke(score, money);
        return true;
    }

    /// <summary>Wipe score and money on game restart.</summary>
    public void ResetStats()
    {
        score = 0;
        money = 0;
        OnStatsChanged?.Invoke(score, money);
    }
    /// <summary>Called by TrafficCollision when the player hits a traffic car.</summary>
    public void ApplyCollisionPenalty(int scoreLoss, int moneyLoss)
    {
        score = Mathf.Max(0, score - scoreLoss);   // clamp at 0, no negative score
        money = Mathf.Max(0, money - moneyLoss);

        Debug.Log($"Collision penalty! -{scoreLoss} score | -{moneyLoss} coins | " +
                $"Total — Score: {score}  Money: {money}");

        OnStatsChanged?.Invoke(score, money);
    }

    // ── Event for UI ───────────────────────────────────────────────────────
    // Subscribe from any HUD/shop script like this:
    //   GameManager.Instance.OnStatsChanged += (s, m) => UpdateHUD(s, m);
    public event System.Action<int, int> OnStatsChanged;
}