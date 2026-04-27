using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public partial class CarShopUI : MonoBehaviour
{
    public static CarShopUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject shopPanel;

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

        // Start with the CrapCar owned
        ownedCars.Add("CrapCar");
    }

    private void Start()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
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

        // Pause/Unpause the world
        Time.timeScale = isMenuOpen ? 0f : 1f;
    }

    // --- BUTTON FUNCTIONS ---
    // These match the 4 buttons on your UI.

    // Acceleration: Lower numbers make the car feel "heavy" and slow to start.
    // Top Speed: Lower numbers cap the max speed so you don't lose control.

    public void BuyCrapCar()
    {
        // Very sluggish — good for learning the map
        TryPurchaseOrSwitch("CrapCar", 0, crapCarSprite, 5f, 4f, 1f);
    }

    public void BuyPorsche()
    {
        // Noticeable upgrade, but still very safe
        TryPurchaseOrSwitch("Porsche", 500, porscheSprite, 8f, 7f, 2f);
    }

    public void BuyDensmobile()
    {
        // The "fast" car, now capped at a reasonable cruising speed
        TryPurchaseOrSwitch("Densmobile", 1000, densmobileSprite, 12f, 10f, 4f);
    }
    // --- CORE LOGIC ---

    private void TryPurchaseOrSwitch(string carName, int cost, Sprite carGraphic, float accel, float topSpeed, float incomeMult)
    {
        // 1. If we already own it, just switch for free
        if (ownedCars.Contains(carName))
        {
            ApplyCarChange(carGraphic, accel, topSpeed, incomeMult);
            ToggleMenu();
            return;
        }

        // 2. If not owned, check money in GameManager
        if (GameManager.Instance != null && GameManager.Instance.SpendMoney(cost))
        {
            ownedCars.Add(carName);
            ApplyCarChange(carGraphic, accel, topSpeed, incomeMult);
            ToggleMenu();
        }
        else
        {
            Debug.Log("Cannot afford this car!");
        }
    }

    private void ApplyCarChange(Sprite graphic, float accel, float topSpeed, float incomeMult)
    {
        // Change the Visuals
        if (carSpriteRenderer != null && graphic != null)
            carSpriteRenderer.sprite = graphic;

        // Change the Driving Stats
        if (playerCar != null)
        {
            playerCar.stats.accelerationForce = accel;
            playerCar.stats.maxSpeed = topSpeed;
        }

        // Change the Money Multiplier
        if (GameManager.Instance != null)
        {
            GameManager.Instance.moneyMultiplier = incomeMult;
        }
    }
}