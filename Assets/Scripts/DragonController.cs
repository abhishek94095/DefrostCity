using UnityEngine;

public class DragonController : MonoBehaviour
{
    public int currentStage = 1; // 1: Young, 2: Teen, 3: Adult
    public Animator dragonAnimator;
    public GameObject fishermanPrefab;
    public Transform[] spawnPoints;
    public GameObject endScreen;
    public GameObject villagerAnimation; // Visual for "freeing villagers"

    public void UpgradeDragon()
    {
        // 1. Play Growing/Eating Animation
        dragonAnimator.SetTrigger("Grow");

        // 2. Run action: Freeing villagers
        if (villagerAnimation != null) villagerAnimation.SetActive(true);

        // 3. Increment stage and spawn more fishermen
        if (currentStage < 3)
        {
            SpawnFishermen(currentStage); // Spawns more as dragon grows
            currentStage++;
        }
        else
        {
            FinalWin(); // End screen on last stage
        }
    }

    void SpawnFishermen(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Instantiate(fishermanPrefab, spawnPoints[i].position, Quaternion.identity);
        }
    }

    void FinalWin()
    {
        if(endScreen != null) endScreen.SetActive(true);
        
        // Stop all inputs
        Fisherman[] allFishermen = FindObjectsOfType<Fisherman>();
        foreach (var f in allFishermen) f.StopFishing();
        
        Debug.Log("Congratulations! You've saved the city!");
    }
}