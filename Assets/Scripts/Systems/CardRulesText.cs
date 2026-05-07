using UnityEngine;

public static class CardRulesText
{
    public static string ZoomTitle(in HandCard h)
    {
        if (h.instanceId == 0 && h.dealUid == 0)
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(h.displayName))
        {
            return h.displayName;
        }

        if (h.IsSpecial)
        {
            return h.special.ToString();
        }

        if (h.isJoker)
        {
            return "Joker";
        }

        if (h.IsSuitedNumber)
        {
            return $"{h.suit} {h.UiRankLabel}";
        }

        return h.UiRankLabel;
    }

    public static string ZoomBody(in HandCard h)
    {
        if (h.instanceId == 0 && h.dealUid == 0)
        {
            return string.Empty;
        }

        if (h.IsSpecial)
        {
            return h.special switch
            {
                SpecialEffectType.AoEAttack => "Attack all players. Does not consume your main attack. Removed after use.",
                SpecialEffectType.DoubleDamage => "Double damage on this strike. Returns to pile.",
                SpecialEffectType.DamagePerRound => "10 damage to everyone each round for 2 rounds. Returns to pile.",
                SpecialEffectType.ForcedAllIn => "Duel: you and the target immediately play your entire hands. Removed after use.",
                SpecialEffectType.Heal30 => "Heal 30 HP. Returns to pile.",
                SpecialEffectType.Reveal4Cards => "All players reveal 4 cards. Removed after use.",
                SpecialEffectType.VoidCard => "Once per game: ban one special type and two ranks. Removed after use.",
                _ => "",
            };
        }

        if (h.isJoker)
        {
            return "Roll for abilities. Max 2 joker effects. Cannot earn permanent suit buffs.";
        }

        return "Strike: pick cards that share one rank or one suit (number cards only). Buff: select two same-suit number cards and tap BUFF.";
    }
}
