using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DragonController : MonoBehaviour
{
    public int currentStage = 1; // 1: Young, 2: Teen, 3: Adult
    public Animator dragonAnimator;
    public GameObject fishermanPrefab;
    public Transform[] spawnPoints;
    public GameObject endScreen, DragonLevel2;
    public GameObject villagerAnimation; // Visual for "freeing villagers"
    public Transform startingPoint, head;
    public TextMeshProUGUI fishCountText;
    public Image fishBGFill;
    public GameObject flameThrowEffect;
    public GameObject[] currentDragonObjects;
    public Transform[] snowLevel2ObjectToLowerLeft, snowLevel2ObjectToLowerRight, snowLevel3ObjectToLowerLeft, snowLevel3ObjectToLowerRight;
    public Transform[] grassLevel2ObjectToLowerLeft, grassLevel2ObjectToLowerRight, grassLevel3ObjectToLowerLeft, grassLevel3ObjectToLowerRight;
    public Vector3 lowerPoint;
    public GameObject boatObject, endVillageViewObject;
    public Transform level2GrassPatchParent, level3GrassPatchParent, level2SnowPatchParent, level3SnowPathParent; 
    public int[] requirements = { 2, 5, 10 }; // Fish needed for Stage 1, 2, 3
    private int fishFed = 0;

    public void UpgradeDragon()
    {
        // 1. Play Growing/Eating Animation
        fishFed = 0;
        CurrencyHandler.Instance.AddGoldFromFish(4);
        // dragonAnimator.Play("Grow_2");
        SoundController.Instance.PlaySFX(SoundType.Upgrade);
        flameThrowEffect.SetActive(true); // Show flame effect during upgrade
        DOVirtual.DelayedCall(2f, () => flameThrowEffect.SetActive(false)); // Hide effect after 1.5 seconds
        DOVirtual.DelayedCall(3.75f, () => flameThrowEffect.SetActive(true)); // Hide effect after 1.5 seconds
        DOVirtual.DelayedCall(5.5f, () => flameThrowEffect.SetActive(false)); // Hide effect after 1.5 seconds
        // 2. Run action: Freeing villagers
        if (villagerAnimation != null) villagerAnimation.SetActive(true);
        MoveSnowDown();
        //EnableGrassObjectAndDisableSnowObject();
        // 3. Increment stage and spawn more fishermen
        if (currentStage < 2)
        {
            SoundController.Instance.PlaySFX(SoundType.FireBreath);
            DragonLevel2.SetActive(true); // Show new dragon visuals
            foreach (var obj in currentDragonObjects) obj.SetActive(false); // Hide old dragon visuals
            dragonAnimator = DragonLevel2.GetComponent<Animator>(); // Switch to new animator
            dragonAnimator.SetTrigger("Firebreath_L"); // Play upgrade animation
            //SpawnFishermen(spawnPoints.Length); // Spawns more as dragon grows
            currentStage++;
            fishCountText.text = Math.Max(0,requirements[currentStage - 1] - fishFed).ToString(); // Update UI with remaining fish needed
        }
        else
        {
            FinalWin(); // End screen on last stage
        }
    }

    [ContextMenu("Level up")]
    public void MoveSnowDown()
    {
        if(currentStage == 2)
        {
            level2GrassPatchParent.DOMove(lowerPoint + level2GrassPatchParent.position, 1f);
            DOVirtual.DelayedCall(1f, () => level2SnowPatchParent.gameObject.SetActive(false));
            boatObject.gameObject.SetActive(true);
        }
        if(currentStage == 1)
        {
            level3GrassPatchParent.DOMove(lowerPoint + level3GrassPatchParent.position, 1f);
            DOVirtual.DelayedCall(1f, () => level3SnowPathParent.gameObject.SetActive(false));
        }
    }

    private void EnableGrassObjectAndDisableSnowObject()
    {
        if(currentStage == 1)
        {
            foreach(Transform grassObject in grassLevel2ObjectToLowerLeft)
            {
                grassObject.gameObject.SetActive(true);
            }
            foreach(Transform snowObject in snowLevel2ObjectToLowerLeft)
            {
                snowObject.DOMove(snowObject.position + lowerPoint, 2f);
                DOVirtual.DelayedCall(1f, () =>snowObject.gameObject.SetActive(false));
            }
            DOVirtual.DelayedCall(3.75f, () => 
            {
                foreach(Transform grassObject in grassLevel2ObjectToLowerRight)
                {
                    grassObject.gameObject.SetActive(true);
                }
                foreach(Transform snowObject in snowLevel2ObjectToLowerRight)
                {
                    snowObject.DOMove(snowObject.position + lowerPoint, 2f);
                    DOVirtual.DelayedCall(1f, () =>snowObject.gameObject.SetActive(false));
                }
            });
        }
        if(currentStage == 2)
        {
            foreach(Transform grassObject in grassLevel3ObjectToLowerLeft)
            {
                grassObject.gameObject.SetActive(true);
            }
            foreach(Transform snowObject in snowLevel3ObjectToLowerLeft)
            {
                snowObject.DOMove(snowObject.position + lowerPoint, 2f);
                DOVirtual.DelayedCall(1f, () =>snowObject.gameObject.SetActive(false));
            }
            DOVirtual.DelayedCall(3.75f, () => 
            {
                foreach(Transform grassObject in grassLevel3ObjectToLowerRight)
                {
                    grassObject.gameObject.SetActive(true);
                }
                foreach(Transform snowObject in snowLevel3ObjectToLowerRight)
                {
                    snowObject.DOMove(snowObject.position + lowerPoint, 2f);
                    DOVirtual.DelayedCall(1f, () =>snowObject.gameObject.SetActive(false));
                }
            });
        }
    }

    public bool canAddCoins = true;
    public void FeedFishOneByOne(int fishAmount)
    {
        if (fishAmount <= 0) return;
        fishFed += fishAmount;
        fishBGFill.fillAmount = 1f - (float)fishFed / (float)requirements[currentStage - 1] ;
        fishCountText.text = Math.Max(0,requirements[currentStage - 1] - fishFed).ToString(); // Update UI with remaining fish needed
        // 🔥 Play animation per fish
        SoundController.Instance.PlaySFX(SoundType.Feed);
        if(canAddCoins) 
        {
            canAddCoins = false;
            CurrencyHandler.Instance.AddGoldFromFish(1);
        }
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
        //if(endScreen != null) endScreen.SetActive(true);
        
        // Stop all inputs
        Fisherman[] allFishermen = FindObjectsOfType<Fisherman>();
        foreach (var f in allFishermen) f.StopFishing();
        
        Debug.Log("Congratulations! You've saved the city!");
        SoundController.Instance.PlaySFX(SoundType.Win);
        endVillageViewObject.gameObject.SetActive(true);
        DOVirtual.DelayedCall(4.1f, () => endVillageViewObject.gameObject.SetActive(false));
        DOVirtual.DelayedCall(4.1f, () => endScreen.gameObject.SetActive(true));
    }
}