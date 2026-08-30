using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public float Waypoint_GetSqrMagnitude(Vector3 origin)
    {
        return (transform.position - origin).sqrMagnitude;
    }
    public Vector3 Waypoint_GetDirectionTo(Vector3 origin)
    {
        return (transform.position - origin).normalized;
    }
}
