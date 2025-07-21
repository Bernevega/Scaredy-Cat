using UnityEngine;

public class PlayerLookAtNPC : MonoBehaviour
{
    public Transform model;
    public float rotationSpeed = 5f;
    private Transform currentTarget;

    void Update()
    {
        if (currentTarget == null) return;

        Vector3 direction = (currentTarget.position - transform.position);
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            model.rotation = Quaternion.Slerp(model.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    public void LookAt(Transform target)
    {
        currentTarget = target;
    }

    public void ClearTarget()
    {
        currentTarget = null;
    }
}
