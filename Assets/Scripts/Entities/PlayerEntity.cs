using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PlayerEntity
{
    public int SeatIndex;
    public bool IsHuman;
    public int CurrentHp;
    public int MaxHp;

    public List<HandCard> Hand = new List<HandCard>();

    public PlayerBuffController Buffs = new PlayerBuffController();

    public bool SpadeBonusNextStrike;
    public int TempShieldCharges;
    public bool DoubleNextStrike;

    public bool IsEliminated => CurrentHp <= 0;

    public string HandSummary
    {
        get
        {
            if (Hand.Count == 0)
            {
                return "empty";
            }

            var sb = new StringBuilder();
            for (int i = 0; i < Hand.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(Hand[i].ShortLabel());
            }

            return sb.ToString();
        }
    }

    public void ApplyDamage(int amount, bool isChipDamage = false)
    {
        if (amount <= 0)
        {
            return;
        }

        if (isChipDamage && Buffs.HeartImmuneChip)
        {
            return;
        }

        int next = CurrentHp - amount;
        if (next <= 0 && Buffs.HeartDeathNegationAvailable)
        {
            Buffs.HeartDeathNegationAvailable = false;
            CurrentHp = 1;
            return;
        }

        CurrentHp = Mathf.Max(0, next);
    }

    public void ApplyHeal(int amount)
    {
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + Mathf.Max(0, amount));
    }
}
