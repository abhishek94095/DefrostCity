using UnityEngine;

public class FollowFishmen : MonoBehaviour
{
    public Transform target; // The fisherman to follow
    public Vector3 offset; // Offset from the fisherman
    public void LateUpdate()
    {
        transform.position = target.position + offset;
    }
}
