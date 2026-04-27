// ============================================================
//  GameManager.cs
// ============================================================
//  HOW TO ADD THIS SCRIPT IN UNITY
// ============================================================
//  1. Right-click your Scripts folder → Create → C# Script.
//     Name it exactly "GameManager".
//
//  2. Create an empty GameObject in the Hierarchy.
//     Name it "GameManager".
//
//  3. Drag the script onto that GameObject.
//
//  4. In the Inspector, set:
//     - Min Money Reward  : minimum coins earned per delivery
//     - Max Money Reward  : maximum coins earned per delivery
//     - Score Per Delivery : score points earned per delivery
//
//  5. IMPORTANT NOTES:
//     - This is a singleton. Only ONE GameManager should exist.
//     - It uses DontDestroyOnLoad, so it persists across scenes.
//     - Money is saved/loaded via SaveManager automatically.
//     - Score resets each run; money carries over between runs.
//
//  6. OTHER SCRIPTS THAT DEPEND ON THIS:
//     - HUDDisplay        (subscribes to OnStatsChanged)
//     - BuildingCollision  (calls ApplyCollisionPenalty)
//     - TrafficCollision   (calls ApplyCollisionPenalty)
//     - FollowerEnemy      (calls ApplyCollisionPenalty)
//     - House              (calls OnDeliveryComplete)
//     - UpgradeMenuUI      (reads Score/Money, calls SpendMoney)
//     - GasTank            (calls OnGasEmpty)
//
//  7. DEBUG TOOLS: Right-click the component header in the
//     Inspector to access "Debug: Add 100 Money", etc.
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private int score;
    private int money;

    [Header("Delivery Rewards")]
    public int minMoneyReward = 20;
    public int maxMoneyReward = 2000;
    public int scorePerDelivery = 100;
    public float moneyMultiplier = 1.0f;

    //variables for timer
    [Header("Timer & Win Screen")]
    public GameObject winPanel;
    public TMP_Text winTimeText;
    private float gameTimer = 0f;
    private bool gameWon = false;

    public int Score => score;
    public int Money => money;

    [ContextMenu("Debug: Add 100 Money")]
    void Debug_AddMoney() { money += 100; SaveManager.SaveMoney(money); OnStatsChanged?.Invoke(score, money); }
    [ContextMenu("Debug: Add 1000 Score")]
    void Debug_AddScore() { score += 1000; OnStatsChanged?.Invoke(score, money); }
    [ContextMenu("Debug: Reset Stats")]
    void Debug_Reset() { ResetStats(); }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load persisted money so it survives scene reloads and sessions
        money = SaveManager.LoadMoney();
        Debug.Log($"GameManager: Loaded money = {money}");
    }

    public void OnDeliveryComplete()
    {
        score += scorePerDelivery;

        // We calculate the reward using the ranges you set in the Inspector
        int baseReward = Random.Range(minMoneyReward, maxMoneyReward + 1);

        // We multiply that base by your car's specific multiplier
        int finalReward = Mathf.RoundToInt(baseReward * moneyMultiplier);

        money += finalReward;

        SaveManager.SaveMoney(money);

        // This tells the HUD to update the screen
        OnStatsChanged?.Invoke(score, money);

        Debug.Log($"Delivery Complete! Base: {baseReward} | Mult: {moneyMultiplier}x | Total: {finalReward}");
    }

    void Update()
    {
        // Count up the timer every frame as long as the game isn't won yet
        if (!gameWon)
        {
            gameTimer += Time.deltaTime;
        }
    }
    public bool SpendMoney(int amount)
    {
        if (amount > money) { Debug.Log($"Not enough money. Have {money}, need {amount}."); return false; }
        money -= amount;
        SaveManager.SaveMoney(money);
        OnStatsChanged?.Invoke(score, money);
        return true;
    }

    /// <summary>Resets SCORE only. Money is kept between runs.</summary>
    public void ResetStats()
    {
        score = 0;
        // money intentionally NOT reset — preserved across restarts.
        OnStatsChanged?.Invoke(score, money);
    }

    /// <summary>Wipes both score AND money. Also clears all save data.</summary>
    public void ResetAll()
    {
        score = 0;
        money = 0;
        SaveManager.DeleteAll();
        OnStatsChanged?.Invoke(score, money);
    }

    /// <summary>Called by collision scripts. Deducts score+money and fires the HUD flash event.</summary>
    public void ApplyCollisionPenalty(int scoreLoss, int moneyLoss)
    {
        score = Mathf.Max(0, score - scoreLoss);
        money = Mathf.Max(0, money - moneyLoss);
        SaveManager.SaveMoney(money);
        Debug.Log($"Collision penalty! -{scoreLoss} score | -{moneyLoss} coins | Total — Score: {score}  Money: {money}");
        OnStatsChanged?.Invoke(score, money);
        OnCollisionPenalty?.Invoke(scoreLoss, moneyLoss);
    }

    public void OnGasEmpty()
    {
        //Debug.Log("Gas empty — opening car shop.");
        
        //// Open the new Car Shop UI instead of the old upgrade menu
        //if (CarShopUI.Instance != null)
        //{
        //    // If the menu isn't already open, open it
        //    if (!CarShopUI.Instance.shopPanel.activeSelf)
        //    {
        //        CarShopUI.Instance.ToggleMenu();
        //    }
        //}
        //else
        //{
        //    Debug.LogWarning("GameManager: No CarShopUI found in the scene.");
        //}
    }

    public event System.Action<int, int> OnStatsChanged;

    /// <summary>Fires on crash. Args = amounts LOST (not new totals). Use to flash HUD labels.</summary>
    public event System.Action<int, int> OnCollisionPenalty;

    public void ResetGame()
    {
        Debug.Log("Resetting Game...");

        // 1. Wipe all saved data (money, owned cars, etc.)
        // If you don't want to lose saved money, delete this line!
        PlayerPrefs.DeleteAll();

        // 2. Unpause the game (just in case they clicked reset while the shop was open)
        Time.timeScale = 1f;

        // 3. Reload the current active scene to start fresh
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void TriggerWinScreen()
    {
        gameWon = true;
        Time.timeScale = 0f; // Pause the game

        if (winPanel != null) winPanel.SetActive(true);

        // Format the raw seconds into MM:SS.XX
        int minutes = Mathf.FloorToInt(gameTimer / 60f);
        int seconds = Mathf.FloorToInt(gameTimer % 60f);
        int hundredths = Mathf.FloorToInt((gameTimer * 100f) % 100f);
        string formattedTime = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, hundredths);

        if (winTimeText != null)
        {
            winTimeText.text = "Congratulations!\nYour time was " + formattedTime;
        }
    }

    public void ContinueFreePlay()
    {
        // Hide the victory screen panel
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        // Unpause the game so the car can drive again
        Time.timeScale = 1f;

        Debug.Log("Continuing in Free Play mode!");
    }
}