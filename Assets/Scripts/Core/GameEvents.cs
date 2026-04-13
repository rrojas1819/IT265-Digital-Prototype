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

    public static void RaiseGameStarted(int playerCount) => OnGameStarted?.Invoke(playerCount);
    public static void RaiseTurnChanged(int playerId) => OnTurnChanged?.Invoke(playerId);
    public static void RaisePlayerDamaged(int playerId, int amount) => OnPlayerDamaged?.Invoke(playerId, amount);
    public static void RaisePlayerHealed(int playerId, int amount) => OnPlayerHealed?.Invoke(playerId, amount);
    public static void RaisePlayerEliminated(int playerId) => OnPlayerEliminated?.Invoke(playerId);
    public static void RaiseRoundChanged(int round) => OnRoundChanged?.Invoke(round);
    public static void RaiseCardDrawn(int playerId) => OnCardDrawn?.Invoke(playerId);
    public static void RaiseCardDiscarded(int playerId) => OnCardDiscarded?.Invoke(playerId);
    public static void RaiseGameEnded(int winnerId) => OnGameEnded?.Invoke(winnerId);

    public static void ClearAllListeners()
    {
        OnGameStarted = null;
        OnTurnChanged = null;
        OnPlayerDamaged = null;
        OnPlayerHealed = null;
        OnPlayerEliminated = null;
        OnRoundChanged = null;
        OnCardDrawn = null;
        OnCardDiscarded = null;
        OnGameEnded = null;
    }
}
