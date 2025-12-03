using UnityEngine;

public class EnergyWave : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        LaserShooter player = collision.GetComponent<LaserShooter>();

        if (player != null)
        {
            player.PlayerDie();

            Debug.Log("HITTTTTTT");
        }
    }
}