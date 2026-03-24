using UnityEngine;

public class InteractionZone : MonoBehaviour
{
    public InteractionType type;
}
public enum InteractionType
{
    None,
    Dragon,
    Fisherman,
    FishingArea
}