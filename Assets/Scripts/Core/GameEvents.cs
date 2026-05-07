using System;

public static class GameEvents
{
    public static Action<int> OnGameStarted;
    public static Action<int> OnTurnChanged;
    public static Action<int, int> OnPlayerDamaged;
    public static Action<int, int> OnPlayerHealed;
    public static Action<int> OnPlayerEliminated;
    public static Action<int> OnRoundChanged;
    public static Action<int> OnCardDrawn;
    public static Action<int> OnCardDiscarded;
    public static Action<int> OnGameEnded;

    public static void RaiseGameStarted(int playerCount)
    {
        if (OnGameStarted != null) OnGameStarted(playerCount);
    }

    public static void RaiseTurnChanged(int playerId)
    {
        if (OnTurnChanged != null) OnTurnChanged(playerId);
    }

    public static void RaisePlayerDamaged(int playerId, int amount)
    {
        if (OnPlayerDamaged != null) OnPlayerDamaged(playerId, amount);
    }

    public static void RaisePlayerHealed(int playerId, int amount)
    {
        if (OnPlayerHealed != null) OnPlayerHealed(playerId, amount);
    }

    public static void RaisePlayerEliminated(int playerId)
    {
        if (OnPlayerEliminated != null) OnPlayerEliminated(playerId);
    }

    public static void RaiseRoundChanged(int round)
    {
        if (OnRoundChanged != null) OnRoundChanged(round);
    }

    public static void RaiseCardDrawn(int playerId)
    {
        if (OnCardDrawn != null) OnCardDrawn(playerId);
    }

    public static void RaiseCardDiscarded(int playerId)
    {
        if (OnCardDiscarded != null) OnCardDiscarded(playerId);
    }

    public static void RaiseGameEnded(int winnerId)
    {
        if (OnGameEnded != null) OnGameEnded(winnerId);
    }
}
