using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Inventory/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Input (Ingredience)")]
    public List<Ingredient> ingredients;

    [Header("Output (Výsledek)")]
    public ItemData resultItem;
    public int resultAmount = 1;

    [Header("Settings")]
    public bool isFurnaceRecipe = false; // Je to pro pec nebo pro stùl?
    public float cookTime = 10f;         // Jen pro pec
}

[System.Serializable]
public class Ingredient
{
    public ItemData item;
    public int amount = 1;
}