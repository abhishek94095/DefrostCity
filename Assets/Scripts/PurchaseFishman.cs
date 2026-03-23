using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PurchaseFishman : MonoBehaviour
{
    public Button purchaseButton;
    public TextMeshProUGUI costText;
    [SerializeField] private Fisherman fishermanPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform[] finalPositions;
    public int cost = 40; // Cost to purchase a fisherman
    private int fishermenPurchased = 0;
    public void OnEnable()
    {
        CurrencyHandler.Instance.OnGoldChanged += UpdatePurchaseButton;
        costText.text = $"x{cost}";
        UpdatePurchaseButton(CurrencyHandler.Instance.currentGold);
    }

    public void OnDisable()
    {
        CurrencyHandler.Instance.OnGoldChanged -= UpdatePurchaseButton;
    }

    private void UpdatePurchaseButton(int coins)
    {
        purchaseButton.interactable = coins >= cost;
    }

    public void OnPurchaseButtonClick()
    {
        CurrencyHandler.Instance.SpendGold(cost);
        fishermenPurchased++;
        SpawnFisherman();
    }

    private void SpawnFisherman()
    {
        if (fishermenPurchased > finalPositions.Length)
        {
            Debug.LogWarning("Max fishermen purchased!");
            return;
        }

        Fisherman newFisherman = Instantiate(fishermanPrefab, spawnPoint.position, Quaternion.identity);
        newFisherman.MoveToPosition(finalPositions[fishermenPurchased - 1]);
    }
}
