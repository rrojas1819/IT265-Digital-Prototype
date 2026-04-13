#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RulesetAssetBootstrap
{
    private const string AssetFolder = "Assets/Resources/Rules";
    private const string AssetPath = AssetFolder + "/DefaultRulesetData.asset";

    [InitializeOnLoadMethod]
    private static void EnsureDefaultRulesetAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<RulesetData>(AssetPath) != null)
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Rules");
        }

        RulesetData ruleset = ScriptableObject.CreateInstance<RulesetData>();
        AssetDatabase.CreateAsset(ruleset, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created missing default rules asset at Assets/Resources/Rules/DefaultRulesetData.asset");
    }
}
#endif
