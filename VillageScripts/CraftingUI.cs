using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;


public class CraftingUI : MonoBehaviour
{
    public static CraftingUI instance;

    [Header("Main Panels")]
    public GameObject panel;
    public Transform recipesContainer; // Content ve Scroll View
    public GameObject recipeButtonPrefab; // Tlaèítko receptu (Prefab)

    [Header("Details Panel")]
    public GameObject detailsPanel;
    public Image resultIcon;
    public TMP_Text resultNameText;
    public TMP_Text ingredientsListText;

    [Header("Controls")]
    public TMP_InputField amountInput;
    public Button craftButton;
    public Slider progressBar; // Slider jako èasovaè

    [Header("Search Slot")]
    public Image searchSlotIcon;
    public ItemData currentSearchItem; // Podle èeho hledáme

    [Header("Layout")]
    public Vector2 inventoryOffset = new Vector2(500, 0); // Posun inventáøe

    [HideInInspector] public CraftingTableInteractable currentTable;
    private CraftingRecipe selectedRecipe;
    private bool isCrafting = false;

    void Awake()
    {
        instance = this;
        if (panel) panel.SetActive(false);
    }

    public void OpenCrafting(CraftingTableInteractable table)
    {
        if (panel.activeSelf && currentTable == table) { CloseCrafting(); return; }

        currentTable = table;
        panel.SetActive(true);
        detailsPanel.SetActive(false); // Nic nevybráno

        // Reset Search
        currentSearchItem = null;
        UpdateSearchVisuals();

        // Otevøít inventáø
        if (InventoryManager.instance != null)
            InventoryManager.instance.OpenInventoryForTrading(inventoryOffset);

        RefreshRecipeList();
    }

    public void CloseCrafting()
    {
        panel.SetActive(false);
        currentTable = null;
        isCrafting = false;
        StopAllCoroutines();

        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.ResetInventoryPosition();
            InventoryManager.instance.CancelActiveDrag();
        }
    }

    // --- SEZNAM RECEPTÙ ---
    public void RefreshRecipeList()
    {
        foreach (Transform child in recipesContainer) Destroy(child.gameObject);

        List<CraftingRecipe> recipesToShow;

        if (currentSearchItem != null)
        {
            // Hledáme podle ingredience
            recipesToShow = RecipeManager.instance.GetRecipesByIngredient(currentSearchItem);
        }
        else
        {
            // Ukazujeme co jde vyrobit z toho co máš
            recipesToShow = RecipeManager.instance.GetCraftableRecipes();
        }

        foreach (var recipe in recipesToShow)
        {
            GameObject btnObj = Instantiate(recipeButtonPrefab, recipesContainer);

            // Nastavení tlaèítka (Ikona + Název)
            // Pøedpokládáme, že prefab má Image "Icon" a TextMeshPro "Name"
            var txt = btnObj.GetComponentInChildren<TMP_Text>();
            if (txt) txt.text = recipe.resultItem.itemName;

            var imgTrans = btnObj.transform.Find("Icon");
            if (imgTrans) imgTrans.GetComponent<Image>().sprite = recipe.resultItem.icon;

            btnObj.GetComponent<Button>().onClick.AddListener(() => OnRecipeSelected(recipe));
        }
    }

    // --- VÝBÌR RECEPTU ---
    public void OnRecipeSelected(CraftingRecipe recipe)
    {
        selectedRecipe = recipe;
        detailsPanel.SetActive(true);

        resultIcon.sprite = recipe.resultItem.icon;
        resultNameText.text = recipe.resultItem.itemName;

        // Seznam ingrediencí
        string ingText = "Required:\n";
        foreach (var ing in recipe.ingredients)
        {
            int current = InventoryManager.instance.GetItemCount(ing.item);
            string color = (current >= ing.amount) ? "white" : "red"; // Èervenì co chybí
            ingText += $"<color={color}>{ing.item.itemName}: {current}/{ing.amount}</color>\n";
        }
        ingredientsListText.text = ingText;

        // Reset Inputu
        amountInput.text = "1";
        ValidateAmount();
    }

    // Validace poètu (volat v OnValueChanged u Inputfieldu a pøi výbìru)
    public void ValidateAmount()
    {
        if (selectedRecipe == null) return;

        int max = RecipeManager.instance.GetMaxCraftableAmount(selectedRecipe);
        if (max == 0) max = 1; // Aby tam nebyla 0

        int current;
        int.TryParse(amountInput.text, out current);

        if (current > max) current = max;
        if (current < 1) current = 1;

        amountInput.text = current.ToString();

        // Tlaèítko aktivní jen pokud máme suroviny
        bool canCraft = RecipeManager.instance.CanCraft(selectedRecipe, current);
        craftButton.interactable = canCraft && !isCrafting;
    }

    // --- CRAFTING (Tlaèítko Craft) ---
    public void OnCraftButtonClick()
    {
        if (selectedRecipe == null || isCrafting) return;

        int amount;
        int.TryParse(amountInput.text, out amount);
        if (amount <= 0) return;

        StartCoroutine(CraftRoutine(amount));
    }

    IEnumerator CraftRoutine(int amount)
    {
        isCrafting = true;
        craftButton.interactable = false;

        // 1s pro 1 kus, 2s pro více kusù
        float duration = (amount == 1) ? 1.0f : 2.0f;
        float timer = 0;

        if (progressBar) progressBar.gameObject.SetActive(true);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            if (progressBar) progressBar.value = timer / duration;
            yield return null;
        }

        // Hotovo -> Craftit
        if (RecipeManager.instance.CanCraft(selectedRecipe, amount))
        {
            RecipeManager.instance.ConsumeIngredients(selectedRecipe, amount);
            InventoryManager.instance.AddItem(selectedRecipe.resultItem, selectedRecipe.resultAmount * amount);
            Debug.Log($"Crafted: {selectedRecipe.resultItem.itemName} x{amount}");
        }

        if (progressBar) progressBar.gameObject.SetActive(false);
        isCrafting = false;

        // Refresh UI
        OnRecipeSelected(selectedRecipe);
        RefreshRecipeList();
    }

    // --- SEARCH LOGIKA ---
    public void SetSearchItem(ItemData item)
    {
        currentSearchItem = item;
        UpdateSearchVisuals();
        RefreshRecipeList();
    }

    void UpdateSearchVisuals()
    {
        if (currentSearchItem != null)
        {
            searchSlotIcon.sprite = currentSearchItem.icon;
            searchSlotIcon.color = Color.white;
            searchSlotIcon.enabled = true;
        }
        else
        {
            searchSlotIcon.sprite = null;
            searchSlotIcon.color = Color.clear;
            searchSlotIcon.enabled = false;
        }
    }
}