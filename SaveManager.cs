// ============================================================
//  SaveManager.cs  — static persistence utility
// ============================================================
//  HOW TO ADD IN UNITY
//  ─────────────────────
//  1. Right-click Scripts folder → Create → C# Script.
//     Name it exactly "SaveManager".
//  2. Paste this code, replacing defaults.
//  3. Do NOT attach to any GameObject — it is a static class.
//     Call it from anywhere: SaveManager.SaveMoney(100);
//
//  WHAT IS SAVED
//  ─────────────
//  • Money          (persists across sessions)
//  • Difficulty     (traffic difficulty level)
//  • Upgrade counts (per-slot purchase history)
// ============================================================

using UnityEngine;

public static class SaveManager
{
    private const string KEY_MONEY      = "Save_Money";
    private const string KEY_DIFFICULTY = "Save_Difficulty";
    private const string KEY_UPGRADE    = "Upgrade_Count_";

    // ── Money ──────────────────────────────────────────────────────────────
    public static void SaveMoney(int amount) => Write(KEY_MONEY, amount);
    public static int  LoadMoney()           => Read(KEY_MONEY, 0);

    // ── Difficulty ─────────────────────────────────────────────────────────
    public static void SaveDifficulty(int d) => Write(KEY_DIFFICULTY, d);
    public static int  LoadDifficulty()      => Read(KEY_DIFFICULTY, 1);

    // ── Upgrades ───────────────────────────────────────────────────────────
    public static void SaveUpgrade(int index, int count) => Write(KEY_UPGRADE + index, count);
    public static int  LoadUpgrade(int index)            => Read(KEY_UPGRADE + index, 0);

    // ── Full wipe ──────────────────────────────────────────────────────────
    /// <summary>Deletes ALL save data. Called by UpgradeMenuUI.ResetSaveData().</summary>
    public static void DeleteAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("SaveManager: All save data wiped.");
    }

    // ── Internals ──────────────────────────────────────────────────────────
    private static void Write(string key, int value) { PlayerPrefs.SetInt(key, value); PlayerPrefs.Save(); }
    private static int  Read(string key, int def)    => PlayerPrefs.GetInt(key, def);
}