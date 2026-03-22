using UnityEngine;
using UnityEngine.UI;

public class DragonController : MonoBehaviour
{
    public int currentStage = 1; // 1: Young, 2: Teen, 3: Adult
    public Animator dragonAnimator;
    public GameObject fishermanPrefab;
    public Transform[] spawnPoints;
    public GameObject endScreen;
    public GameObject villagerAnimation; // Visual for "freeing villagers"
    public Transform startingPoint;
    public int[] requirements = { 2, 5, 10 }; // Fish needed for Stage 1, 2, 3
    private int fishFed = 0;

    public void UpgradeDragon()
    {
        // 1. Play Growing/Eating Animation
        fishFed = 0;
        dragonAnimator.Play("Grow_2");
        SoundController.Instance.PlaySFX(SoundType.Upgrade);

        // 2. Run action: Freeing villagers
        if (villagerAnimation != null) villagerAnimation.SetActive(true);

        // 3. Increment stage and spawn more fishermen
        if (currentStage < 2)
        {
            SoundController.Instance.PlaySFX(SoundType.FireBreath);
            //SpawnFishermen(spawnPoints.Length); // Spawns more as dragon grows
            currentStage++;
        }
        else
        {
            FinalWin(); // End screen on last stage
        }
    }

    public void FeedFishOneByOne(int fishAmount)
    {
        if (fishAmount <= 0) return;
        fishFed += fishAmount;
        // 🔥 Play animation per fish
        SoundController.Instance.PlaySFX(SoundType.Feed);
        if (fishFed >= requirements[currentStage - 1])
        {
            UpgradeDragon();
        }

    }

    public void FeedAnimation() => dragonAnimator.SetTrigger("Feed");

    public void FeedFish(int fishAmount)
    {
        if (fishAmount <= 0) return;

        fishFed += fishAmount;
        SoundController.Instance.PlaySFX(SoundType.Feed);
    }

    void SpawnFishermen(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // Instantiate(fishermanPrefab, spawnPoints[i].position, Quaternion.identity);
            spawnPoints[i].gameObject.SetActive(true); // Activate pre-placed fishermen at spawn points
            spawnPoints[i].GetComponent<Fisherman>().MoveToLocation(startingPoint.position);
        }
    }

    void FinalWin()
    {
        if(endScreen != null) endScreen.SetActive(true);
        
        // Stop all inputs
        Fisherman[] allFishermen = FindObjectsOfType<Fisherman>();
        foreach (var f in allFishermen) f.StopFishing();
        
        Debug.Log("Congratulations! You've saved the city!");
        SoundController.Instance.PlaySFX(SoundType.Win);
    }
}