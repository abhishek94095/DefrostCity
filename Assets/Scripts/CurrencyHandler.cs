using System;
using UnityEngine;
using TMPro;

public class CurrencyHandler : MonoBehaviour
{
    public static CurrencyHandler Instance;

    [SerializeField] private int currentGold = 0;
    [SerializeField] private int goldPerFish = 10; // 🔥 configurable
    [SerializeField] private TextMeshProUGUI goldText;

    public Action<int> OnGoldChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateGoldUI();
    }

    // 🔹 MAIN ENTRY: give fish → get gold
    public void AddGoldFromFish(int fishAmount)
    {
        if (fishAmount <= 0) return;

        int goldToAdd = fishAmount * goldPerFish;
        currentGold += goldToAdd;

        OnGoldChanged?.Invoke(currentGold);
        UpdateGoldUI();

        Debug.Log($"Fish: {fishAmount} → Gold: {goldToAdd}");
    }

    // 🔹 Spend gold
    public bool SpendGold(int amount)
    {
        if (currentGold < amount) return false;

        currentGold -= amount;

        OnGoldChanged?.Invoke(currentGold);
        UpdateGoldUI();

        return true;
    }

    // 🔹 Check gold
    public bool HasEnoughGold(int amount)
    {
        return currentGold >= amount;
    }

    private void UpdateGoldUI()
    {
        if (goldText != null)
        {
            goldText.text = currentGold.ToString();
        }
    }
}