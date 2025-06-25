using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    public Vector3 targetDirection = new Vector3(0, 0, 1);

    public float rotationSpeed = 1080f;

    private void Update()
    {
        if (transform.forward != targetDirection)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(targetDirection), rotationSpeed * Time.deltaTime);
        }
    }
}
