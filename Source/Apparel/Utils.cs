#nullable disable
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Genesis.Apparel
{
    internal static class Utils
    {
        private static readonly HashSet<ThingCategoryDef> modifiedCategories = new HashSet<ThingCategoryDef>();
        public static readonly HashSet<ThingDef> thingDefRecipesToRegenerate = new HashSet<ThingDef>();
        public static void ApplyCategory(ThingDef thingDef, string Category)
        {
            ThingCategoryDef apparelCategory = DefDatabase<ThingCategoryDef>.GetNamed(Category);
            if (thingDef.thingCategories != null)
            {
                List<ThingCategoryDef> oldCategories = new List<ThingCategoryDef>(thingDef.thingCategories);
                foreach (ThingCategoryDef oldCat in oldCategories)
                {
                    oldCat.childThingDefs?.Remove(thingDef);
                    thingDef.thingCategories.Remove(oldCat);
                    modifiedCategories.Add(oldCat);
                }
            }
            else
            {
                thingDef.thingCategories = new List<ThingCategoryDef>();
            }

            thingDef.thingCategories.Add(apparelCategory);
            apparelCategory.childThingDefs.Add(thingDef);
            modifiedCategories.Add(apparelCategory);
        }

        public static void RefreshModifiedCategories()
        {
            foreach (ThingCategoryDef category in modifiedCategories)
            {
                category.ClearCachedData();
                category.ResolveReferences();
            }
            modifiedCategories.Clear();
        }

        public static void RefreshAllModifiedRecipes()
        {
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe.products != null)
                {
                    ThingDef matchingThingDef = recipe.products.FirstOrDefault(p => thingDefRecipesToRegenerate.Contains(p.thingDef))?.thingDef;

                    if (matchingThingDef != null)
                    {
                        if (recipe.ingredients != null && recipe.ingredients.Count > 0)
                        {
                            recipe.ingredients.RemoveAll(ingredient => ingredient.IsFixedIngredient);

                            foreach (IngredientCount ingredient in recipe.ingredients)
                            {
                                ingredient.SetBaseCount(matchingThingDef.costStuffCount);
                            }
                        }
                        recipe.ClearCachedData();
                        recipe.ResolveReferences();
                    }
                }
            }
            thingDefRecipesToRegenerate.Clear();
        }
    }



}