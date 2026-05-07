#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ChaosDeckBootstrap
{
    const string CardsRoot = "Assets/Resources/Cards";

    [MenuItem("ChaosPoker/Generate Deck Card Assets")]
    public static void GenerateDeckCardAssets()
    {
        EnsureFolder(CardsRoot);

        CardSuit[] suits = { CardSuit.Spade, CardSuit.Heart, CardSuit.Diamond, CardSuit.Club };
        foreach (CardSuit suit in suits)
        {
            for (int rank = 4; rank <= 10; rank++)
            {
                string stem = $"Num_{suit}_{rank}";
                string label = $"{suit} {rank}";
                CreateNumber(stem, label, suit, rank, false);
            }
        }

        CreateNumber("Num_Joker", "Joker", CardSuit.None, 14, true);

        CreateSpecial("Spec_AoE", "Volcano (AoE)", SpecialEffectType.AoEAttack, true, false, 10);
        CreateSpecial("Spec_DD", "Double Damage", SpecialEffectType.DoubleDamage, false, false, 10);
        CreateSpecial("Spec_DPR", "Damage Per Round", SpecialEffectType.DamagePerRound, false, false, 10);
        CreateSpecial("Spec_FAI", "Forced All-In", SpecialEffectType.ForcedAllIn, true, false, 10);
        CreateSpecial("Spec_Heal", "Heal 30", SpecialEffectType.Heal30, false, false, 10);
        CreateSpecial("Spec_Reveal", "Reveal 4", SpecialEffectType.Reveal4Cards, true, false, 10);
        CreateSpecial("Spec_Void", "Void", SpecialEffectType.VoidCard, true, true, 10);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ChaosPoker] Deck card assets generated under Resources/Cards.");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(parent))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        string leaf = path.Substring(parent.Length + 1);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static void CreateNumber(string fileStem, string display, CardSuit suit, int rank, bool joker)
    {
        string path = $"{CardsRoot}/{fileStem}.asset";
        if (AssetDatabase.LoadAssetAtPath<NumberCardData>(path) != null)
        {
            return;
        }

        var d = ScriptableObject.CreateInstance<NumberCardData>();
        d.cardId = fileStem.ToLowerInvariant();
        d.displayName = display;
        d.suit = suit;
        d.rank = rank;
        d.isJoker = joker;
        AssetDatabase.CreateAsset(d, path);
    }

    static void CreateSpecial(string fileStem, string display, SpecialEffectType fx, bool removeAfter, bool once, int rankScore)
    {
        string path = $"{CardsRoot}/{fileStem}.asset";
        if (AssetDatabase.LoadAssetAtPath<SpecialCardData>(path) != null)
        {
            return;
        }

        var d = ScriptableObject.CreateInstance<SpecialCardData>();
        d.cardId = fileStem.ToLowerInvariant();
        d.displayName = display;
        d.suit = CardSuit.None;
        d.effectType = fx;
        d.removeAfterUse = removeAfter;
        d.oncePerGame = once;
        d.rankForScoring = rankScore;
        AssetDatabase.CreateAsset(d, path);
    }
}
#endif
