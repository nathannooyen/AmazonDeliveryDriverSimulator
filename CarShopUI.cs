using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro; // <-- REQUIRED for UI Text

public class CarShopUI : MonoBehaviour
{
    public static CarShopUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject shopPanel;

    [Header("Button Text Labels")]
    public TMP_Text crapCarText;
    public TMP_Text porscheText;
    public TMP_Text densmobileText;

    [Header("Car Reference")]
    public SpriteRenderer carSpriteRenderer;
    public CarController playerCar;

    [Header("Car Sprites")]
    public Sprite porscheSprite;
    public Sprite crapCarSprite;
    public Sprite densmobileSprite;

    private List<string> ownedCars = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        ownedCars.Add("CrapCar");
    }

    private void Start()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        UpdateButtonText(); // Set the initial text when the game starts
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        if (shopPanel == null) return;
        bool isMenuOpen = !shopPanel.activeSelf;
        shopPanel.SetActive(isMenuOpen);
        Time.timeScale = isMenuOpen ? 0f : 1f;

        // Refresh the text every time we open the menu just to be safe
        if (isMenuOpen) UpdateButtonText();
    }

    // --- BUTTON FUNCTIONS ---

    public void BuyCrapCar()
    {
        TryPurchaseOrSwitch("CrapCar", 0, crapCarSprite, 8f, 6f, 1f, 80f);
    }

    public void BuyPorsche()
    {
        TryPurchaseOrSwitch("Porsche", 500, porscheSprite, 14f, 12f, 2f, 120f);
    }

    public void BuyDensmobile()
    {
        TryPurchaseOrSwitch("Densmobile", 5000, densmobileSprite, 20f, 16f, 4f, 150f);
    }

    // --- CORE LOGIC ---

    private void TryPurchaseOrSwitch(string carName, int cost, Sprite carGraphic, float accel, float topSpeed, float incomeMult, float turnSpeed)
    {
        if (ownedCars.Contains(carName))
        {
            ApplyCarChange(carGraphic, accel, topSpeed, incomeMult, turnSpeed);
            ToggleMenu();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.SpendMoney(cost))
        {
            ownedCars.Add(carName);
            UpdateButtonText(); // Refresh the labels so it says "Purchased"
            ApplyCarChange(carGraphic, accel, topSpeed, incomeMult, turnSpeed);
            ToggleMenu();

            // --- THE FIX: THIS TRIGGERS THE WIN SCREEN WHEN YOU BUY THE $5000 CAR ---
            if (cost >= 5000)
            {
                FindAnyObjectByType<GameManager>().TriggerWinScreen();
            }
            // ------------------------------------------------------------------------
        }
        else
        {
            Debug.Log("Cannot afford this car!");
        }
    }

    private void ApplyCarChange(Sprite graphic, float accel, float topSpeed, float incomeMult, float turnSpeed)
    {
        if (carSpriteRenderer != null && graphic != null)
            carSpriteRenderer.sprite = graphic;

        if (playerCar != null)
        {
            playerCar.stats.accelerationForce = accel;
            playerCar.stats.maxSpeed = topSpeed;
            playerCar.stats.steeringSpeed = turnSpeed;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.moneyMultiplier = incomeMult;
        }
    }

    // --- NEW: TEXT UPDATE LOGIC ---
    private void UpdateButtonText()
    {
        // The starter car is always owned
        if (crapCarText != null) crapCarText.text = "Purchased";

        // Check if the list contains the Porsche. If yes, say Purchased. If no, show price.
        if (porscheText != null)
            porscheText.text = ownedCars.Contains("Porsche") ? "Purchased" : "$500";

        if (densmobileText != null)
            densmobileText.text = ownedCars.Contains("Densmobile") ? "Purchased" : "$5000";
    }

    public void QuitToDesktop()
    {
        // This prints to the console so you know the button works while testing
        Debug.Log("Quitting Game...");

        // This actually closes the built game
        Application.Quit();
    }
}