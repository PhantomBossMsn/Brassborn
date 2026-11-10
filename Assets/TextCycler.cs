using UnityEngine;
using TMPro;

public class TextCycler : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI textField;

    [Header("Texts To Display")]
    [TextArea]
    public string[] messages;

    [Header("Settings")]
    public float cycleTime = 2f; // time between messages
    public bool loop = true;     // keep looping or stop at end

    private int currentIndex = 0;
    private float timer = 0f;

    void Start()
    {
        if (messages.Length > 0 && textField != null)
        {
            textField.text = messages[0]; // start with first text
        }
    }

    void Update()
    {
        if (messages.Length == 0 || textField == null) return;

        timer += Time.unscaledDeltaTime; // use unscaled if you want it to work even when paused
        if (timer >= cycleTime)
        {
            timer = 0f;
            currentIndex++;

            if (currentIndex >= messages.Length)
            {
                if (loop)
                    currentIndex = 0;
                else
                    return; // stop cycling
            }

            textField.text = messages[currentIndex];
        }
    }
}
