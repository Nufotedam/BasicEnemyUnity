using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeZone : MonoBehaviour
{
    PlayerMovement player;

    private void Start()
    {
        player = FindObjectOfType<PlayerMovement>();
    }

    private void OnTriggerEnter(Collider other)
    {
        /*
         * If the player is in the safe zone the nemmy cant go in and
         * cant continue chasing the player
         * */
        if (other.CompareTag("Player"))
        {
            player.safeZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        /*
         * If the player leaver the safe zone the enemy can chase the player again
         * */
        if (other.CompareTag("Player"))
        {
            player.safeZone = false;
        }
    }
}
