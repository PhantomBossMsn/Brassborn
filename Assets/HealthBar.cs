using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    public Slider instantBar;   // updates immediately
    public Slider delayedBar;   // lags behind
    public TMP_Text healthText; // optional, shows numbers

    [Header("Settings")]
    public float easeSpeed = 2f; // how fast delayed bar eases down

    private float currentHealth;
    private float maxHealth;

    public void Initialize(float maxHP)
    {
        maxHealth = maxHP;
        currentHealth = maxHP;

        instantBar.maxValue = maxHP;
        delayedBar.maxValue = maxHP;

        instantBar.value = maxHP;
        delayedBar.value = maxHP;

        if (healthText != null)
            healthText.text = $"{currentHealth}/{maxHealth}";
    }

    public void UpdateHealth(float newHealth)
    {
        // Clamp so it never goes below 0 or above max
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);

        // Update the instant bar immediately
        instantBar.value = currentHealth;

        // Update the health text
        if (healthText != null)
            healthText.text = $"{currentHealth}/{maxHealth}";
    }

    void Update()
    {
        // Smoothly move delayed bar towards current health
        if (delayedBar.value > currentHealth)
        {
            delayedBar.value = Mathf.Lerp(delayedBar.value, currentHealth, Time.deltaTime * easeSpeed);

            // To prevent it from never quite reaching the value
            if (Mathf.Abs(delayedBar.value - currentHealth) < 0.01f)
                delayedBar.value = currentHealth;
        }
    }
}
