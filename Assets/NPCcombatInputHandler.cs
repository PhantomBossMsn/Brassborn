using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCcombatInputHandler : MonoBehaviour
{
    public float Health = 100;
    private void OnTriggerEnter(Collider other)
    {

        // Check if the collided object has the tag "EnemyAttack"
        if (other.gameObject.CompareTag("Attack"))
        {
            TakeDamage(10f); // Example: deduct 10 health per hit
        }
    }


    // Deduct health
    private void TakeDamage(float amount)
    {
        Health -= amount;
        Health = Mathf.Clamp(Health, 0, 100); // Keep health between 0 and 100
        Debug.Log("npc Health: " + Health);

        if (Health <= 0)
        {
            Die();
        }
    }

    // Handle death
    private void Die()
    {
        Debug.Log("npc is dead!");
        // Play death animation, disable movement, etc.

    }
}
