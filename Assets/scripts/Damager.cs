using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damager : MonoBehaviour
{
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 8f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerPlatformerController player = other.GetComponent<PlayerPlatformerController>();
        if (player != null)
        {
            Vector2 direction = (player.transform.position - transform.position).normalized;
            player.TakeDamage(damageAmount, direction, knockbackForce);
        }
    }
}
