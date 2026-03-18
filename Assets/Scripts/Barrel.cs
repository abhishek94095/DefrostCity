using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Barrel : MonoBehaviour
{
    public int currentFish = 0;
    public int[] requirements = { 10, 30, 60 }; // Fish needed for Stage 1, 2, 3
    public TextMeshProUGUI countText;
    public Button feedButton;
    public DragonController dragon;

    void Start()
    {
        if(feedButton != null) feedButton.gameObject.SetActive(false);
        UpdateUI();
    }

    public void AddFish(int amount)
    {
        currentFish += amount;
        UpdateUI();

        // Show button if requirement for current dragon stage is met
        if (currentFish >= requirements[dragon.currentStage - 1])
        {
            if(feedButton != null) feedButton.gameObject.SetActive(true);
        }
    }

    void UpdateUI()
    {
        if(countText != null) countText.text = currentFish.ToString();
    }

    public void OnFeedClick()
    {
        if(feedButton != null) feedButton.gameObject.SetActive(false);
        dragon.UpgradeDragon(); // Trigger upgrade action
        currentFish = 0;
        UpdateUI();
    }
}