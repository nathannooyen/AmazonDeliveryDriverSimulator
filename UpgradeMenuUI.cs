// ============================================================
//  UpgradeMenuUI.cs  (v3 — persistent upgrades + cost on buttons)
// ============================================================
//
//  HOW TO SET UP IN UNITY
//  ──────────────────────
//  1. CREATE THE CANVAS
//     GameObject → UI → Canvas.
//     Set Render Mode → Screen Space – Overlay.
//
//  2. CREATE THE PANEL
//     Right-click Canvas → UI → Panel.  Name it "UpgradeMenuPanel".
//     This is your full-screen overlay (hidden until gas runs out).
//
//  3. ADD A STATS LABEL
//     Right-click UpgradeMenuPanel → UI → Text – TextMeshPro.
//     Name it "StatsLabel".  Position it near the top.
//
//  4. ADD UPGRADE BUTTONS  (one per upgrade — default script has 3)
//     Right-click UpgradeMenuPanel → UI → Button – TextMeshPro.
//     Name them "UpgradeBtn_0", "UpgradeBtn_1", "UpgradeBtn_2".
//     Each button NEEDS two child TMP_Text objects:
//       • "Label"    — the upgrade name + buy count (top line)
//       • "CostText" — cost display                (bottom line)
//     The script finds these by name, so spelling matters.
//
//     TIP: Duplicate the first button for the others — they share
//     the same child structure.
//
//  5. ADD A RESTART BUTTON
//     Right-click UpgradeMenuPanel → UI → Button – TextMeshPro.
//     Name it "RestartButton".
//
//  6. ADD A RESET SAVE BUTTON  (optional but recommended for testing)
//     Right-click UpgradeMenuPanel → UI → Button – TextMeshPro.
//     Name it "ResetSaveButton".  Wire it to the "ResetSaveData"
//     public method in the Inspector's OnClick list.
//
//  7. ATTACH THE SCRIPT
//     Create an empty GameObject, name it "UpgradeMenuUI",
//     add this script as a component.
//
//  8. WIRE UP THE INSPECTOR FIELDS
//     • Upgrade Panel        → drag UpgradeMenuPanel
//     • Stats Label          → drag StatsLabel
//     • Restart Button       → drag RestartButton
//     • Reset Save Button    → drag ResetSaveButton (optional)
//     • Upgrade Buttons      → drag UpgradeBtn_0, _1, _2  (in order)
//     • Base Costs           → e.g. 20, 15, 25
//     • Cost Multiplier      → e.g. 1.6
//     • Upgrade Descriptions → e.g. "Bigger Tank", "Eco Engine", "Turbo"
//
//  HOW PERSISTENCE WORKS
//  ─────────────────────
//  Purchase counts are saved to PlayerPrefs automatically every time
//  you buy an upgrade.  They survive scene reloads and Play-mode
//  restarts.  On startup the stat effects are re-applied so the
//  player car always matches the saved level.
//  Call ResetSaveData() (or the button) to wipe all saves.
//
//  HOW COSTS SCALE
//  ───────────────
//  nextCost = Mathf.RoundToInt(baseCost × multiplier ^ purchaseCount)
//  With baseCost=20 and multiplier=1.6:
//    Buy 1 → $20,  Buy 2 → $32,  Buy 3 → $51,  Buy 4 → $82 …
//
// ============================================================

// ============================================================
//  UpgradeMenuUI.cs  (v4 — delegates persistence to SaveManager)
// ============================================================
// Setup instructions unchanged from v3 — see original comments.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UpgradeMenuUI : MonoBehaviour
{
    public static UpgradeMenuUI Instance { get; private set; }

    [Header("Panel & Labels")]
    public GameObject upgradePanel;
    public TMP_Text statsLabel;

    [Header("Restart")]
    public Button restartButton;

    [Header("Reset Save (optional)")]
    public Button resetSaveButton;

    [Header("Upgrade Buttons")]
    public List<Button> upgradeButtons;

    [Header("Upgrade Configuration")]
    public List<int> baseCosts;

    [Range(1f, 3f)]
    public float costMultiplier = 1.6f;

    public List<string> upgradeDescriptions;

    [Header("Tank Upgrade — Bigger Tank (index 0)")]
    public float tankCapacityPerUpgrade = 30f;

    [Header("Tank Upgrade — Eco Engine (index 1)")]
    public float drainRateReductionPerUpgrade = 1f;

    [Header("Car Upgrade — Turbo Engine (index 2)")]
    public float speedIncreasePerUpgrade = 2f;
    public float accelIncreasePerUpgrade  = 3f;

    private int[] purchaseCounts;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        purchaseCounts = new int[upgradeButtons.Count];
        LoadSaveData();

        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }

    private void Start()
    {
        if (restartButton  != null) restartButton.onClick.AddListener(RestartGame);
        if (resetSaveButton != null) resetSaveButton.onClick.AddListener(ResetSaveData);

        for (int i = 0; i < upgradeButtons.Count; i++)
        {
            int index = i;
            if (upgradeButtons[i] != null)
                upgradeButtons[i].onClick.AddListener(() => OnUpgradeClicked(index));
        }

        ReapplyAllUpgrades();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void ShowUpgradeMenu()
    {
        if (upgradePanel != null) upgradePanel.SetActive(true);
        Time.timeScale = 0f;
        RefreshUI();
        Debug.Log("Upgrade menu opened.");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null) GameManager.Instance.ResetStats();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Wipes all upgrade and save data. Now delegates to SaveManager.DeleteAll()
    /// so money, difficulty, and upgrades are all cleared in one call.
    /// </summary>
    public void ResetSaveData()
    {
        for (int i = 0; i < purchaseCounts.Length; i++)
            purchaseCounts[i] = 0;

        SaveManager.DeleteAll();   // clears money, difficulty, and all upgrade keys
        Debug.Log("UpgradeMenuUI: Save data wiped via SaveManager.");
        RefreshUI();
    }

    // ── Save / Load — now via SaveManager ─────────────────────────────────

    private void LoadSaveData()
    {
        for (int i = 0; i < purchaseCounts.Length; i++)
            purchaseCounts[i] = SaveManager.LoadUpgrade(i);

        Debug.Log("UpgradeMenuUI: Upgrade data loaded via SaveManager.");
    }

    private void SaveSingleUpgrade(int index)
    {
        SaveManager.SaveUpgrade(index, purchaseCounts[index]);
    }

    // ── Re-apply on scene start ────────────────────────────────────────────

    private void ReapplyAllUpgrades()
    {
        GasTank       tank = FindFirstObjectByType<GasTank>();
        CarController car  = FindFirstObjectByType<CarController>();

        for (int i = 0; i < purchaseCounts.Length; i++)
        {
            int count = purchaseCounts[i];
            if (count == 0) continue;

            switch (i)
            {
                case 0:
                    if (tank != null)
                        tank.UpgradeCapacity(tank.maxGas + tankCapacityPerUpgrade * count, true);
                    break;
                case 1:
                    if (tank != null)
                        tank.drainRate = Mathf.Max(0.5f, tank.drainRate - drainRateReductionPerUpgrade * count);
                    break;
                case 2:
                    if (car != null)
                    {
                        CarStats s = car.stats;
                        s.maxSpeed          += speedIncreasePerUpgrade * count;
                        s.accelerationForce += accelIncreasePerUpgrade * count;
                        car.ApplyUpgrade(s);
                    }
                    break;
            }
        }
    }

    // ── Cost helpers ───────────────────────────────────────────────────────

    private int CurrentCost(int index)
    {
        int baseCost = index < baseCosts.Count ? baseCosts[index] : 10;
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, purchaseCounts[index]));
    }

    // ── UI refresh ─────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (statsLabel != null && GameManager.Instance != null)
            statsLabel.text = $"Score: {GameManager.Instance.Score}\nMoney: ${GameManager.Instance.Money}";

        for (int i = 0; i < upgradeButtons.Count; i++)
        {
            if (upgradeButtons[i] == null) continue;

            int    cost  = CurrentCost(i);
            int    count = purchaseCounts[i];
            string desc  = i < upgradeDescriptions.Count ? upgradeDescriptions[i] : $"Upgrade {i + 1}";

            TMP_Text nameLabel = FindChildText(upgradeButtons[i], "Label")
                              ?? upgradeButtons[i].GetComponentInChildren<TMP_Text>();
            if (nameLabel != null)
                nameLabel.text = count > 0 ? $"{desc}  [×{count}]" : desc;

            TMP_Text costLabel = FindChildText(upgradeButtons[i], "CostText");
            if (costLabel != null)
            {
                bool canAfford = GameManager.Instance != null && GameManager.Instance.Money >= cost;
                costLabel.text = canAfford ? $"${cost}" : $"Need ${cost}";
            }

            upgradeButtons[i].interactable = GameManager.Instance != null &&
                                             GameManager.Instance.Money >= cost;
        }
    }

    private TMP_Text FindChildText(Button btn, string childName)
    {
        Transform found = btn.transform.Find(childName);
        if (found != null) return found.GetComponent<TMP_Text>();
        foreach (Transform child in btn.transform)
        {
            Transform deep = child.Find(childName);
            if (deep != null) return deep.GetComponent<TMP_Text>();
        }
        return null;
    }

    // ── Upgrade logic ──────────────────────────────────────────────────────

    private void OnUpgradeClicked(int index)
    {
        int cost = CurrentCost(index);
        if (GameManager.Instance == null || !GameManager.Instance.SpendMoney(cost))
        {
            Debug.Log($"Cannot afford upgrade {index}. Need ${cost}.");
            return;
        }

        purchaseCounts[index]++;
        SaveSingleUpgrade(index);
        ApplyUpgradeEffect(index);
        RefreshUI();
    }

    private void ApplyUpgradeEffect(int index)
    {
        GasTank       tank = FindFirstObjectByType<GasTank>();
        CarController car  = FindFirstObjectByType<CarController>();

        switch (index)
        {
            case 0:
                if (tank != null)
                {
                    tank.UpgradeCapacity(tank.maxGas + tankCapacityPerUpgrade, true);
                    Debug.Log($"Tank capacity → {tank.maxGas}  (purchase #{purchaseCounts[0]})");
                }
                else Debug.LogWarning("UpgradeMenuUI: No GasTank found in scene.");
                break;

            case 1:
                if (tank != null)
                {
                    tank.drainRate = Mathf.Max(0.5f, tank.drainRate - drainRateReductionPerUpgrade);
                    Debug.Log($"Drain rate → {tank.drainRate:F1}  (purchase #{purchaseCounts[1]})");
                }
                else Debug.LogWarning("UpgradeMenuUI: No GasTank found in scene.");
                break;

            case 2:
                if (car != null)
                {
                    CarStats s = car.stats;
                    s.maxSpeed          += speedIncreasePerUpgrade;
                    s.accelerationForce += accelIncreasePerUpgrade;
                    car.ApplyUpgrade(s);
                    Debug.Log($"Speed → {s.maxSpeed}  Accel → {s.accelerationForce}  (purchase #{purchaseCounts[2]})");
                }
                else Debug.LogWarning("UpgradeMenuUI: No CarController found in scene.");
                break;

            default:
                Debug.Log($"Upgrade {index} has no effect defined yet.");
                break;
        }
    }
}