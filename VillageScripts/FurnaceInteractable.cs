using UnityEngine;

public class FurnaceInteractable : MonoBehaviour
{
    [Header("Settings")]
    public float interactionRange = 3f;
    public string furnaceName = "Smelter";

    [Header("State")]
    public ItemData inputItem;
    public int inputAmount;

    public ItemData fuelItem;
    public int fuelAmount;
    public float currentBurnTime;
    public float maxBurnTime;

    public ItemData outputItem;
    public int outputAmount;

    public float cookTimer;
    public bool isCooking;

    private FurnaceUI ui;

    void Start()
    {
        if (FurnaceUI.instance != null) ui = FurnaceUI.instance;
        else ui = FindFirstObjectByType<FurnaceUI>(FindObjectsInactive.Include);
    }

    void Update()
    {
        // 1. Auto-Close
        if (ui != null && ui.gameObject.activeSelf && ui.currentFurnace == this)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && Vector2.Distance(transform.position, player.transform.position) > interactionRange)
            {
                ui.CloseFurnace();
            }
        }

        // 2. Logika Peèení
        if (isCooking)
        {
            CraftingRecipe recipe = null;
            if (inputItem != null) recipe = RecipeManager.instance.GetFurnaceRecipe(inputItem);

            // ZMÌNA: Kontrola množství (inputAmount < recipe cost)
            int cost = (recipe != null && recipe.ingredients.Count > 0) ? recipe.ingredients[0].amount : 1;

            if (inputItem == null || recipe == null || !CanOutputResult(recipe) || inputAmount < cost)
            {
                cookTimer = 0;
                isCooking = false; // Zastavit, pokud došly suroviny (nebo jich je málo)
                return;
            }

            // Spotøeba paliva
            if (currentBurnTime > 0)
            {
                currentBurnTime -= Time.deltaTime;
            }
            else
            {
                if (fuelItem != null && fuelAmount > 0)
                {
                    fuelAmount--;
                    currentBurnTime = fuelItem.burnDuration;
                    maxBurnTime = fuelItem.burnDuration;
                }
                else
                {
                    isCooking = false;
                    return;
                }
            }

            // Peèení
            cookTimer += Time.deltaTime;
            if (cookTimer >= recipe.cookTime)
            {
                SmeltItem(recipe);
            }
        }

        if (ui != null && ui.currentFurnace == this && ui.gameObject.activeSelf)
        {
            ui.UpdateVisuals();
        }
    }

    bool CanOutputResult(CraftingRecipe recipe)
    {
        if (outputItem == null) return true;
        if (outputItem != recipe.resultItem) return false;
        if (outputAmount + recipe.resultAmount > outputItem.maxStackSize) return false;
        return true;
    }

    void SmeltItem(CraftingRecipe recipe)
    {
        // ZMÌNA: Odeèítáme podle receptu!
        int cost = 1;
        if (recipe.ingredients.Count > 0) cost = recipe.ingredients[0].amount;

        inputAmount -= cost; // Odeèíst napø. 2

        if (inputAmount <= 0)
        {
            inputItem = null;
            inputAmount = 0;
        }

        if (outputItem == null) outputItem = recipe.resultItem;
        outputAmount += recipe.resultAmount; // Pøièíst napø. 1

        cookTimer = 0;

        CheckIfCanCook();
    }

    public void Interact()
    {
        if (ui != null) ui.OpenFurnace(this);
    }

    void CheckIfCanCook()
    {
        // 1. Máme input?
        if (inputItem == null)
        {
            isCooking = false;
            return;
        }

        // 2. Máme recept?
        CraftingRecipe recipe = RecipeManager.instance.GetFurnaceRecipe(inputItem);
        if (recipe == null) return;

        // 3. ZMÌNA: Máme DOST inputu? (Napø. máme 1, ale recept chce 2)
        int cost = (recipe.ingredients.Count > 0) ? recipe.ingredients[0].amount : 1;
        if (inputAmount < cost)
        {
            isCooking = false;
            return;
        }

        // 4. Máme oheò?
        if (currentBurnTime > 0 || (fuelItem != null && fuelAmount > 0))
        {
            if (CanOutputResult(recipe))
            {
                isCooking = true;
            }
        }
    }

    public int TryAddInput(ItemData item, int amountToAdd)
    {
        if (RecipeManager.instance.GetFurnaceRecipe(item) == null) return 0;

        if (inputItem == null)
        {
            inputItem = item;
            inputAmount = amountToAdd;
            CheckIfCanCook();
            return amountToAdd;
        }
        else if (inputItem == item)
        {
            inputAmount += amountToAdd;
            CheckIfCanCook();
            return amountToAdd;
        }
        return 0;
    }

    public int TryAddFuel(ItemData item, int amountToAdd)
    {
        if (item.burnDuration <= 0) return 0;

        if (fuelItem == null)
        {
            fuelItem = item;
            fuelAmount = amountToAdd;
            CheckIfCanCook();
            return amountToAdd;
        }
        else if (fuelItem == item)
        {
            fuelAmount += amountToAdd;
            CheckIfCanCook();
            return amountToAdd;
        }
        return 0;
    }

    public void CollectOutput()
    {
        if (outputItem != null)
        {
            if (InventoryManager.instance.AddItem(outputItem, outputAmount))
            {
                outputItem = null;
                outputAmount = 0;
                ui.UpdateVisuals();
            }
        }
    }
}