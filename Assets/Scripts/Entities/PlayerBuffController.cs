using UnityEngine;

public class PlayerBuffController
{
    public bool HasSpadeBuff;
    public bool HasHeartBuff;
    public bool HasDiamondBuff;
    public bool HasClubBuff;

    public int OngoingFlatDefense;
    public int ClubBonusDamage;
    public bool HeartImmuneChip;
    public bool HeartDeathNegationAvailable;
    public bool SpadeChipEveryoneOnAttack;
    public int SpadeStrikeBonus;

    public bool VoidCardUsedThisMatch;
    public int VoidBannedRank1 = -1;
    public int VoidBannedRank2 = -1;

    public bool DiamondShieldUsedThisMatch;
    public int DiamondShieldHp;
    public int DiamondShieldRoundsRemaining;

    public void ResetBuffs()
    {
        HasSpadeBuff = false;
        HasHeartBuff = false;
        HasDiamondBuff = false;
        HasClubBuff = false;
        OngoingFlatDefense = 0;
        ClubBonusDamage = 0;
        HeartImmuneChip = false;
        HeartDeathNegationAvailable = false;
        SpadeChipEveryoneOnAttack = false;
        SpadeStrikeBonus = 0;
        VoidCardUsedThisMatch = false;
        VoidBannedRank1 = -1;
        VoidBannedRank2 = -1;
        DiamondShieldUsedThisMatch = false;
        DiamondShieldHp = 0;
        DiamondShieldRoundsRemaining = 0;
    }

    public bool IsRankVoidBanned(int rank)
    {
        return rank == VoidBannedRank1 || rank == VoidBannedRank2;
    }
}
