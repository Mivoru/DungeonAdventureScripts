using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class FurnaceUI : MonoBehaviour
{
    public static FurnaceUI instance;

    [Header("UI References")]
    public GameObject panel;
    public Image inputIcon;
    public TMP_Text inputText;
    public Image fuelIcon;
    public TMP_Text fuelText;
    public Image outputIcon;
    public TMP_Text outputText;
    public Image fireFill;
    public Image progressFill;

    [Header("Layout Settings")]
    public Vector2 inventoryOffset = new Vector2(400, 0);

    [HideInInspector] public FurnaceInteractable currentFurnace;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public void OpenFurnace(FurnaceInteractable furnace)
    {
        if (panel.activeSelf && currentFurnace == furnace)
        {
            CloseFurnace();
            return;
        }

        currentFurnace = furnace;
        panel.SetActive(true);
        UpdateVisuals();

        if (InventoryManager.instance != null)
            InventoryManager.instance.OpenInventoryForTrading(inventoryOffset);
        if (TimeUI.instance != null) TimeUI.instance.ShowClock(false); // Schovat
    }

    public void CloseFurnace()
    {
        panel.SetActive(false);
        currentFurnace = null;

        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.ResetInventoryPosition();
            // --- PØIDÁNO: Zrušit drag ---
            InventoryManager.instance.CancelActiveDrag();
        }
        if (TimeUI.instance != null) TimeUI.instance.ShowClock(true); // Zobrazit
    }

    public void UpdateVisuals()
    {
        if (currentFurnace == null) return;

        UpdateSlot(inputIcon, inputText, currentFurnace.inputItem, currentFurnace.inputAmount);
        UpdateSlot(fuelIcon, fuelText, currentFurnace.fuelItem, currentFurnace.fuelAmount);
        UpdateSlot(outputIcon, outputText, currentFurnace.outputItem, currentFurnace.outputAmount);

        if (currentFurnace.maxBurnTime > 0)
            fireFill.fillAmount = currentFurnace.currentBurnTime / currentFurnace.maxBurnTime;
        else
            fireFill.fillAmount = 0;

        float maxCookTime = 10f;
        if (currentFurnace.inputItem != null)
        {
            var recipe = RecipeManager.instance.GetFurnaceRecipe(currentFurnace.inputItem);
            if (recipe != null) maxCookTime = recipe.cookTime;
        }
        progressFill.fillAmount = currentFurnace.cookTimer / maxCookTime;
    }

    void UpdateSlot(Image icon, TMP_Text text, ItemData item, int amount)
    {
        if (item != null && amount > 0)
        {
            icon.sprite = item.icon;
            icon.color = Color.white;
            icon.enabled = true;
            text.text = amount.ToString();
        }
        else
        {
            icon.sprite = null;
            icon.color = Color.clear;
            icon.enabled = false;
            text.text = "";
        }
    }

    public void OnOutputClick()
    {
        if (currentFurnace != null) { currentFurnace.CollectOutput(); UpdateVisuals(); }
    }

    public void HandleItemDrop(ItemData item, int slotIndex, int amount, string slotType)
    {
        if (currentFurnace == null) return;

        int added = 0;
        if (slotType == "Fuel") added = currentFurnace.TryAddFuel(item, amount);
        else if (slotType == "Input") added = currentFurnace.TryAddInput(item, amount);

        if (added > 0)
        {
            InventoryManager.instance.RemoveItem(slotIndex, added);
            UpdateVisuals();
        }
    }

    public void TryMoveItemToFurnace(ItemData item, int slotIndex)
    {
        if (currentFurnace == null) return;

        int added = 0;
        if (item.burnDuration > 0) added = currentFurnace.TryAddFuel(item, 1);

        if (added == 0 && RecipeManager.instance.GetFurnaceRecipe(item) != null)
            added = currentFurnace.TryAddInput(item, 1);

        if (added > 0)
        {
            InventoryManager.instance.RemoveItem(slotIndex, 1);
            UpdateVisuals();
        }
    }
    
}