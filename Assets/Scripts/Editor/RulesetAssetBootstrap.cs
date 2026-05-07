#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RulesetAssetBootstrap
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string RulesFolder = "Assets/Resources/Rules";
    private const string CardsFolder = "Assets/Resources/Cards";
    private const string RulesetPath = RulesFolder + "/DefaultRulesetData.asset";
    private const string StarterSpecialCardPath = CardsFolder + "/Starter_Special_DoubleDamage.asset";

    [InitializeOnLoadMethod]
    private static void EnsureDefaultRulesetAsset()
    {
        EnsureFolder("Assets", "Resources", ResourcesFolder);
        EnsureFolder(ResourcesFolder, "Rules", RulesFolder);
        EnsureFolder(ResourcesFolder, "Cards", CardsFolder);

        if (AssetDatabase.LoadAssetAtPath<RulesetData>(RulesetPath) == null)
        {
            RulesetData ruleset = ScriptableObject.CreateInstance<RulesetData>();
            AssetDatabase.CreateAsset(ruleset, RulesetPath);
            Debug.Log("Created missing default rules asset at Assets/Resources/Rules/DefaultRulesetData.asset");
        }

        if (AssetDatabase.LoadAssetAtPath<SpecialCardData>(StarterSpecialCardPath) == null)
        {
            SpecialCardData specialCard = ScriptableObject.CreateInstance<SpecialCardData>();
            specialCard.cardId = "starter_special_double_damage";
            specialCard.displayName = "Starter Double Damage";
            specialCard.effectType = SpecialEffectType.DoubleDamage;
            specialCard.removeAfterUse = true;
            specialCard.oncePerGame = false;
            AssetDatabase.CreateAsset(specialCard, StarterSpecialCardPath);
            Debug.Log("Created starter SpecialCardData asset at Assets/Resources/Cards/Starter_Special_DoubleDamage.asset");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolder(string parentFolder, string childFolder, string expectedPath)
    {
        if (!AssetDatabase.IsValidFolder(expectedPath))
        {
            AssetDatabase.CreateFolder(parentFolder, childFolder);
        }
    }
}
#endif
