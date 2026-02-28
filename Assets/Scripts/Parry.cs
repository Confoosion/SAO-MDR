using UnityEngine;

public class Parry : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.transform.CompareTag("EnemyAttack"))
            Debug.Log("PARRIED");
    }
}
