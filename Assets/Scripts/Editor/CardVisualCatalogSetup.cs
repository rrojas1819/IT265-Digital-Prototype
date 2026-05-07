#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class CardVisualCatalogSetup
{
    const string CatalogPath = "Assets/Resources/Visuals/DefaultCardVisualCatalog.asset";
    const string SpritesFolder = "Assets/Sprites";

    [MenuItem("ChaosPoker/Repair Card Visual Catalog (reload all sprites)")]
    public static void RepairCatalog()
    {
        var cat = AssetDatabase.LoadAssetAtPath<CardVisualCatalog>(CatalogPath);
        if (cat == null)
        {
            Debug.LogError($"[ChaosPoker] Missing catalog at {CatalogPath}");
            return;
        }

        int ok = 0;
        int missing = 0;

        Sprite LogBind(string label, Sprite resolved, string sourcePath)
        {
            if (resolved != null)
            {
                ok++;
                Debug.Log($"[ChaosPoker] {label} -> {resolved.name} ({sourcePath})");
            }
            else
            {
                missing++;
                Debug.LogError($"[ChaosPoker] {label} -> NULL (expected at {sourcePath})");
            }

            return resolved;
        }

        if (cat.clubRankSprites == null || cat.clubRankSprites.Length != 7)
        {
            cat.clubRankSprites = new Sprite[7];
        }

        for (int rank = 4; rank <= 10; rank++)
        {
            int idx = rank - 4;
            string path = $"{SpritesFolder}/{idx}.png";
            cat.clubRankSprites[idx] = LogBind($"clubRankSprites[{idx}] (rank {rank})", LoadSingleSpriteAtPath(path), path);
        }

        const int maxSliceForSuit = 6;
        BindRankSheet(LogBind, ref cat.diamondRankSprites, SuitSheetPath("Full-7"), "Full-7", maxSliceForSuit);
        BindRankSheet(LogBind, ref cat.heartRankSprites, SuitSheetPath("Full-19"), "Full-19", maxSliceForSuit);
        BindRankSheet(LogBind, ref cat.spadeRankSprites, SuitSheetPath("Full-29"), "Full-29", maxSliceForSuit);

        cat.diamondShared = LogBind(
            nameof(cat.diamondShared),
            LoadNamedSubSprite(SuitSheetPath("Full-7"), "Full-7_0"),
            $"{SuitSheetPath("Full-7")}#Full-7_0");
        cat.heartShared = LogBind(
            nameof(cat.heartShared),
            LoadNamedSubSprite(SuitSheetPath("Full-19"), "Full-19_0"),
            $"{SuitSheetPath("Full-19")}#Full-19_0");
        cat.spadeShared = LogBind(
            nameof(cat.spadeShared),
            LoadNamedSubSprite(SuitSheetPath("Full-29"), "Full-29_0"),
            $"{SuitSheetPath("Full-29")}#Full-29_0");

        cat.jokerSprite = LogBind(
            nameof(cat.jokerSprite),
            LoadNamedSubSprite(SuitSheetPath("Full-23"), "Full-23_0"),
            $"{SuitSheetPath("Full-23")}#Full-23_0");

        cat.aoeVolcano = LogBind(
            nameof(cat.aoeVolcano),
            LoadNamedSubSprite(SuitSheetPath("Full-1"), "Full-1_0"),
            $"{SuitSheetPath("Full-1")}#Full-1_0");
        cat.doubleDamage = LogBind(
            nameof(cat.doubleDamage),
            LoadNamedSubSprite(SuitSheetPath("Full-11"), "Full-11_0"),
            $"{SuitSheetPath("Full-11")}#Full-11_0");
        cat.damagePerRound = LogBind(
            nameof(cat.damagePerRound),
            LoadNamedSubSprite(SuitSheetPath("Full-13"), "Full-13_0"),
            $"{SuitSheetPath("Full-13")}#Full-13_0");
        cat.forcedAllIn = LogBind(
            nameof(cat.forcedAllIn),
            LoadNamedSubSprite(SuitSheetPath("Full-15"), "Full-15_0"),
            $"{SuitSheetPath("Full-15")}#Full-15_0");
        cat.heal30 = LogBind(
            nameof(cat.heal30),
            LoadNamedSubSprite(SuitSheetPath("Full-17"), "Full-17_0"),
            $"{SuitSheetPath("Full-17")}#Full-17_0");
        cat.reveal4 = LogBind(
            nameof(cat.reveal4),
            LoadNamedSubSprite(SuitSheetPath("Full-24"), "Full-24_0"),
            $"{SuitSheetPath("Full-24")}#Full-24_0");
        cat.voidCard = LogBind(
            nameof(cat.voidCard),
            LoadNamedSubSprite(SuitSheetPath("Full-30"), "Full-30_0"),
            $"{SuitSheetPath("Full-30")}#Full-30_0");

        EditorUtility.SetDirty(cat);
        AssetDatabase.SaveAssets();

        int total = ok + missing;
        Debug.Log($"[ChaosPoker] Repair complete. Bound {ok}/{total} slots ({missing} missing). Catalog saved.");
    }

    static string SuitSheetPath(string sheetName) => $"{SpritesFolder}/{sheetName}.png";

    static Sprite LoadSingleSpriteAtPath(string assetPath)
    {
        Sprite primary = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (primary != null)
        {
            return primary;
        }

        foreach (UnityEngine.Object o in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (o is Sprite s)
            {
                return s;
            }
        }

        return null;
    }

    static Sprite LoadNamedSubSprite(string assetPath, string spriteName)
    {
        foreach (UnityEngine.Object o in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (o is Sprite s && s.name == spriteName)
            {
                return s;
            }
        }

        return null;
    }

    static void BindRankSheet(
        Func<string, Sprite, string, Sprite> logBind,
        ref Sprite[] slots,
        string assetPath,
        string slicePrefix,
        int maxSliceIndex)
    {
        if (slots == null || slots.Length != 7)
        {
            slots = new Sprite[7];
        }

        for (int rank = 4; rank <= 10; rank++)
        {
            int slice = Mathf.Clamp(rank - 4, 0, maxSliceIndex);
            string sliceName = $"{slicePrefix}_{slice}";
            string label = $"{slicePrefix} rank {rank} -> {sliceName}";
            string refPath = $"{assetPath}#{sliceName}";
            Sprite resolved = LoadNamedSubSprite(assetPath, sliceName);
            slots[rank - 4] = logBind(label, resolved, refPath);
        }
    }
}
#endif
