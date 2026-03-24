using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Barrel : MonoBehaviour
{
    public static Barrel Instance;
    public int currentFish = 0;
    public int[] requirements = { 2, 5, 10 }; // Fish needed for Stage 1, 2, 3
    public TextMeshProUGUI countText;
    public Button feedButton;
    public DragonController dragon;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if(feedButton != null) feedButton.gameObject.SetActive(false);
        UpdateUI();
    }

    public void AddFish(int amount)
    {
        currentFish += amount;
        //UpdateUI();
        dragon.FeedFish(amount);

        // Show button if requirement for current dragon stage is met
        if (currentFish >= requirements[dragon.currentStage - 1])
        {
            if(feedButton != null) feedButton.gameObject.SetActive(true);
            //FTUEManager.Instance.ShowFeedFTUE();
        }
    }

    void UpdateUI()
    {
        if(countText != null) countText.text = currentFish + " / " + requirements[dragon.currentStage - 1];
    }

    public void OnFeedClick()
    {
        if(feedButton != null) feedButton.gameObject.SetActive(false);
        dragon.UpgradeDragon(); // Trigger upgrade action
        currentFish = 0;
        UpdateUI();
        //FTUEManager.Instance.StopFeedFTUE();
    }
}