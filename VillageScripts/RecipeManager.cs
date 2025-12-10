using UnityEngine;
using System.Collections.Generic;

public class RecipeManager : MonoBehaviour
{
    public static RecipeManager instance;

    public List<CraftingRecipe> allRecipes; // Sem pøetáhneš všechny vytvoøené recepty

    void Awake()
    {
        instance = this;
    }

    public CraftingRecipe GetFurnaceRecipe(ItemData input)
    {
        foreach (var recipe in allRecipes)
        {
            if (recipe.isFurnaceRecipe && recipe.ingredients.Count > 0 && recipe.ingredients[0].item == input)
            {
                return recipe;
            }
        }
        return null;
    }

    public List<CraftingRecipe> GetCraftingStationRecipes()
    {
        List<CraftingRecipe> list = new List<CraftingRecipe>();
        foreach (var r in allRecipes)
        {
            if (!r.isFurnaceRecipe) list.Add(r);
        }
        return list;
    }
}