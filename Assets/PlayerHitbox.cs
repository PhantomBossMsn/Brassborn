using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    public CombatInputHandler player;
    public CombatBotHandler bot;
    public CombatInputHandler.HitPart hitPart;
    public CombatBotHandler.HitPart hitPartbot;
    public bool isnpc=false;
    private void Awake()
    {
        // Find the CombatInputHandler in parent hierarchy automatically
        player = GetComponentInParent<CombatInputHandler>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isnpc)
        {
            if (other.CompareTag("EnemyAttack") && player != null)
            {
                player.ApplyHit(hitPart, false);
            }
            if (other.CompareTag("SweepEnemyAttack") && player != null)
            {
                player.ApplyHit(hitPart, true);
            }
        }
        else
        {
            if (other.CompareTag("Attack") && bot != null)
            {
                bot.ApplyHit(hitPartbot, false);
                Debug.Log("YESATTACk");
            }
            if (other.CompareTag("SweepPlayerAttack") && bot != null)
            {
                bot.ApplyHit(hitPartbot, true);
                Debug.Log("SWEAPYESATTACk");

            }
        }
    }
}
