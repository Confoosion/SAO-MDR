using UnityEngine;

public class CharacterAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public void ChangeAnimation(Movement movement)
    {
        switch(movement)
        {
            case Movement.Standing:
                {
                    animator.Play("Idle");
                    break;
                }
            case Movement.Moving:
                {
                    animator.Play("Run");
                    break;
                }
            case Movement.Jumping:
                {
                    animator.Play("Jump");
                    break;
                }
            case Movement.Guarding:
                {
                    animator.Play("Guard");
                    break;
                }
            
        }
    }
}
