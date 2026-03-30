using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PurchaseFishman : MonoBehaviour
{
    public Button purchaseButton;
    public TextMeshProUGUI costText;
    [SerializeField] private Fisherman[] fishermans;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform[] finalPositions;
    [SerializeField] private Fisherman firstFisherman; // Reference to the first fisherman in the scene
    public int cost = 40; // Cost to purchase a fisherman
    private int fishermenPurchased = 0;
    private List<Fisherman> spawnedFisherMan = new List<Fisherman>();
    public void OnEnable()
    {
        CurrencyHandler.Instance.OnGoldChanged += UpdatePurchaseButton;
        costText.text = $"x{cost}";
        UpdatePurchaseButton(CurrencyHandler.Instance.currentGold);
    }

    void Start()
    {
        spawnedFisherMan.Add(firstFisherman);
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
        if(CurrencyHandler.Instance.currentGold < cost) return;
        
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

        if(fishermenPurchased == 1)
        {
            cost = 10;
            costText.text = $"x{cost}";
        }

        // Fisherman newFisherman = Instantiate(fishermanPrefab, spawnPoint.position, Quaternion.identity);
        Fisherman newFisherman = fishermans[fishermenPurchased - 1];
        newFisherman.gameObject.SetActive(true);
        newFisherman.transform.position = spawnPoint.position;
        newFisherman.MoveToPosition(finalPositions[fishermenPurchased - 1]);
        newFisherman.SetFirstFisherman(firstFisherman, fishermenPurchased); // Set reference to the first fisherman
        if(fishermenPurchased == finalPositions.Length) gameObject.SetActive(false);
        spawnedFisherMan.Add(newFisherman);
        OnPurchaseButtonClick();
        //firstFisherman.goToBarrel += newFisherman.MoveToBarrel;
        //firstFisherman.goToFishingSpot += newFisherman.ReturnToFishingSite;
    }
}
