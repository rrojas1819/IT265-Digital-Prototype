using UnityEngine;

public class PlayerBuffController
{
    public bool HasSpadeBuff;
    public bool HasHeartBuff;
    public bool HasDiamondBuff;
    public bool HasClubBuff;

    public void ResetBuffs()
    {
        HasSpadeBuff = false;
        HasHeartBuff = false;
        HasDiamondBuff = false;
        HasClubBuff = false;
    }
}
