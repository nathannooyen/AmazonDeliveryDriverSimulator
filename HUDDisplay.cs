// ============================================================
//  HUDDisplay.cs
// ============================================================
//  HOW TO ADD THIS SCRIPT IN UNITY
// ============================================================
//  1. Right-click your Scripts folder → Create → C# Script.
//     Name it exactly "HUDDisplay".
//
//  2. Create a Canvas (if you don't already have one):
//     GameObject → UI → Canvas. Set to Screen Space – Overlay.
//
//  3. Create two TextMeshPro labels on the Canvas:
//     Right-click Canvas → UI → Text – TextMeshPro.
//     Name them "ScoreLabel" and "MoneyLabel".
//     Position them wherever you want the HUD to appear.
//
//  4. Create an empty GameObject or use the Canvas itself.
//     Attach this script to it.
//
//  5. In the Inspector, drag:
//     - ScoreLabel → "Score Label" field
//     - MoneyLabel → "Money Label" field
//
//  6. Optionally adjust:
//     - Penalty Colour  : the flash colour on crash (default red)
//     - Flash Duration  : how long the flash lasts (default 0.4s)
//
//  7. PREREQUISITES:
//     - TextMeshPro package must be installed
//       (Window → Package Manager → TextMeshPro).
//     - GameManager must exist in the scene.
// ============================================================

using System.Collections;
using UnityEngine;
using TMPro;

public class HUDDisplay : MonoBehaviour
{
    [Header("UI Labels")]
    public TMP_Text scoreLabel;
    public TMP_Text moneyLabel;

    [Header("Crash Flash")]
    [Tooltip("Colour the labels flash to when a penalty hits.")]
    public Color penaltyColour = new Color(1f, 0.25f, 0.25f);
    [Tooltip("How long the flash lasts in seconds.")]
    public float flashDuration = 0.4f;

    private Color defaultScoreColour;
    private Color defaultMoneyColour;

    private void Start()
    {
        if (scoreLabel != null) defaultScoreColour = scoreLabel.color;
        if (moneyLabel != null) defaultMoneyColour  = moneyLabel.color;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsChanged    += UpdateHUD;
            GameManager.Instance.OnCollisionPenalty += OnCrash;
            UpdateHUD(GameManager.Instance.Score, GameManager.Instance.Money);
        }
        else
        {
            Debug.LogWarning("HUDDisplay: GameManager not found.");
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsChanged    -= UpdateHUD;
            GameManager.Instance.OnCollisionPenalty -= OnCrash;
        }
    }

    private void UpdateHUD(int score, int money)
    {
        if (scoreLabel != null) scoreLabel.text = $"Score: {score}";
        if (moneyLabel  != null) moneyLabel.text  = $"${money}";
    }

    private void OnCrash(int scoreLoss, int moneyLoss)
    {
        StopAllCoroutines();
        StartCoroutine(FlashLabels());
    }

    private IEnumerator FlashLabels()
    {
        if (scoreLabel != null) scoreLabel.color = penaltyColour;
        if (moneyLabel  != null) moneyLabel.color  = penaltyColour;

        yield return new WaitForSecondsRealtime(flashDuration);

        if (scoreLabel != null) scoreLabel.color = defaultScoreColour;
        if (moneyLabel  != null) moneyLabel.color  = defaultMoneyColour;
    }
}