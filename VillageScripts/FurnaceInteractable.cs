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
        // Najdeme UI (bezpeènìji pøes Singleton, pokud existuje, nebo Find)
        if (FurnaceUI.instance != null) ui = FurnaceUI.instance;
        else ui = FindFirstObjectByType<FurnaceUI>(FindObjectsInactive.Include);
    }

    void Update()
    {
        // 1. Auto-Close
        if (ui != null && ui.gameObject.activeSelf && ui.currentFurnace == this)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (Vector2.Distance(transform.position, player.transform.position) > interactionRange)
                {
                    ui.CloseFurnace();
                }
            }
        }

        // 2. Logika Peèení
        if (isCooking && inputItem != null)
        {
            // --- KONTROLA VÝSTUPU (NOVÉ) ---
            // Zjistíme, co má vzniknout
            CraftingRecipe recipe = RecipeManager.instance.GetFurnaceRecipe(inputItem);

            // Pokud recept neexistuje NEBO je výstup plný/jiný -> STOP
            if (recipe == null || !CanOutputResult(recipe))
            {
                cookTimer = 0; // Reset progressu (nebo ho nech, ale nepeè)
                // isCooking = false; // Mùžeme vypnout, nebo nechat hoøet palivo naprázdno (tvá volba)
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
                    isCooking = false; // Došlo palivo -> zhasnout
                    return;
                }
            }

            // Peèení (když máme palivo i místo na výstupu)
            cookTimer += Time.deltaTime;

            if (cookTimer >= recipe.cookTime)
            {
                SmeltItem(recipe);
            }
        }
        else
        {
            // Když se nepeèe, resetujeme timer
            cookTimer = 0;
            // Palivo mùže dohoøívat
            if (currentBurnTime > 0) currentBurnTime -= Time.deltaTime;
        }

        // Aktualizace UI
        if (ui != null && ui.currentFurnace == this && ui.gameObject.activeSelf)
        {
            ui.UpdateVisuals();
        }
    }

    // --- NOVÁ METODA PRO KONTROLU VÝSTUPU ---
    bool CanOutputResult(CraftingRecipe recipe)
    {
        if (outputItem == null) return true; // Je tam prázdno -> OK
        if (outputItem != recipe.resultItem) return false; // Je tam nìco jiného -> STOP
        if (outputAmount + recipe.resultAmount > outputItem.maxStackSize) return false; // Je plno -> STOP

        return true;
    }

    void SmeltItem(CraftingRecipe recipe)
    {
        inputAmount -= 1; // Vždy bereme 1 várku (podle receptu by to mìlo být recipe.ingredients[0].amount)
                          // Pro jednoduchost teï bereme 1 input item = 1 cyklus

        if (inputAmount <= 0) inputItem = null;

        outputItem = recipe.resultItem;
        outputAmount += recipe.resultAmount;

        cookTimer = 0;

        if (inputItem == null) isCooking = false;
    }

    public void Interact()
    {
        if (ui != null) ui.OpenFurnace(this);
    }

    // --- UPRAVENÉ METODY PRO VKLÁDÁNÍ ---
    // Nyní vrací int = kolik se skuteènì vložilo (zbytek zùstane v ruce/inventáøi)

    public int TryAddInput(ItemData item, int amountToAdd)
    {
        if (RecipeManager.instance.GetFurnaceRecipe(item) == null) return 0; // Není to tavitelná vìc

        if (inputItem == null)
        {
            inputItem = item;
            inputAmount = amountToAdd;
            isCooking = true;
            return amountToAdd; // Vzali jsme vše
        }
        else if (inputItem == item)
        {
            // Tady by se dala øešit kapacita (max stack), zatím bereme vše
            inputAmount += amountToAdd;
            isCooking = true;
            return amountToAdd;
        }
        return 0; // Jiný item
    }

    public int TryAddFuel(ItemData item, int amountToAdd)
    {
        if (item.burnDuration <= 0) return 0;

        if (fuelItem == null)
        {
            fuelItem = item;
            fuelAmount = amountToAdd;
            return amountToAdd;
        }
        else if (fuelItem == item)
        {
            fuelAmount += amountToAdd;
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
            }
        }
    }
}