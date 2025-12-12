using UnityEngine;
using System.Collections.Generic;

public class RecipeManager : MonoBehaviour
{
    public static RecipeManager instance;
    public List<CraftingRecipe> allRecipes;

    void Awake()
    {
        instance = this;
    }

    // Pro Pec
    public CraftingRecipe GetFurnaceRecipe(ItemData input)
    {
        foreach (var recipe in allRecipes)
        {
            if (recipe.isFurnaceRecipe && recipe.ingredients.Count > 0 && recipe.ingredients[0].item == input)
                return recipe;
        }
        return null;
    }

    // --- LOGIKA PRO CRAFTING TABLE ---

    // 1. Najít recepty podle ingredience (Search Slot)
    public List<CraftingRecipe> GetRecipesByIngredient(ItemData ingredient)
    {
        List<CraftingRecipe> matching = new List<CraftingRecipe>();
        foreach (var recipe in allRecipes)
        {
            if (recipe.isFurnaceRecipe) continue;

            foreach (var ing in recipe.ingredients)
            {
                if (ing.item == ingredient)
                {
                    matching.Add(recipe);
                    break;
                }
            }
        }
        return matching;
    }

    // 2. Najít recepty, které jdou vyrobit z toho, co máš v inventáøi
    public List<CraftingRecipe> GetCraftableRecipes()
    {
        List<CraftingRecipe> craftable = new List<CraftingRecipe>();
        if (InventoryManager.instance == null) return craftable;

        foreach (var recipe in allRecipes)
        {
            if (recipe.isFurnaceRecipe) continue;
            // Zobrazíme recept, pokud máme suroviny alespoò na 1 kus
            if (CanCraft(recipe, 1))
            {
                craftable.Add(recipe);
            }
        }
        return craftable;
    }

    // 3. Kolikrát to mùžu maximálnì vyrobit? (Pro omezení InputFieldu)
    public int GetMaxCraftableAmount(CraftingRecipe recipe)
    {
        int maxAmount = int.MaxValue;

        foreach (var ing in recipe.ingredients)
        {
            int playerHas = InventoryManager.instance.GetItemCount(ing.item);
            if (ing.amount == 0) continue;
            int canMake = playerHas / ing.amount;
            if (canMake < maxAmount) maxAmount = canMake;
        }

        if (maxAmount == int.MaxValue) return 0;
        return maxAmount;
    }

    // 4. Mám dost surovin na X kusù?
    public bool CanCraft(CraftingRecipe recipe, int multiplier)
    {
        foreach (var ing in recipe.ingredients)
        {
            int required = ing.amount * multiplier;
            int current = InventoryManager.instance.GetItemCount(ing.item);
            if (current < required) return false;
        }
        return true;
    }

    // 5. Odebrat suroviny
    public void ConsumeIngredients(CraftingRecipe recipe, int multiplier)
    {
        foreach (var ing in recipe.ingredients)
        {
            InventoryManager.instance.RemoveItemByName(ing.item, ing.amount * multiplier);
        }
    }
}