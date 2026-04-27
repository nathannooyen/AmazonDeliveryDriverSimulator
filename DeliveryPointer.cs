using UnityEngine;

public class DeliveryPointer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the main Player Car object here")]
    public Transform player;

    [Tooltip("Drag the Sprite Renderer of THIS arrow object here")]
    public SpriteRenderer arrowSprite;

    private House[] allHouses;

    void Start()
    {
        // Find all the houses in the game automatically
        allHouses = FindObjectsByType<House>(FindObjectsSortMode.None);

        if (arrowSprite == null)
            arrowSprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        House closestHouse = GetClosestActiveHouse();

        if (closestHouse != null)
        {
            // If there is an active delivery, show the arrow
            arrowSprite.enabled = true;

            // Figure out the math direction pointing from the player to the house
            Vector2 direction = (closestHouse.transform.position - player.position).normalized;

            // Convert that direction into a rotation angle
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Apply the rotation. 
            // NOTE: We subtract 90 degrees because Unity 2D sprites face UP by default. 
            // If your arrow points perfectly sideways, change - 90f to 0f.
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // Keep the arrow locked near the player (optional, prevents it from swinging wildly if the car spins)
            transform.position = player.position + (Vector3)direction * 1.5f; // Change 1.5f to move the arrow closer/further from the car
        }
        else
        {
            // Hide the arrow if no deliveries are currently active
            arrowSprite.enabled = false;
        }
    }

    private House GetClosestActiveHouse()
    {
        House closest = null;
        float minDistance = Mathf.Infinity;

        foreach (House h in allHouses)
        {
            // Only look at houses that currently want a delivery and aren't waiting on a cooldown
            if (h.WantsDelivery && !h.IsDeliveryComplete())
            {
                float dist = Vector2.Distance(player.position, h.transform.position);

                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = h;
                }
            }
        }

        return closest;
    }
}