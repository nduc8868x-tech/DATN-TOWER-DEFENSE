public readonly struct SessionSnapshot
{
    public GameState State { get; }
    public int Gold { get; }
    public int Lives { get; }
    public int CurrentWave { get; }
    public int TotalWaves { get; }
    public int AliveEnemies { get; }

    public SessionSnapshot(GameState state, int gold, int lives, int currentWave, int totalWaves, int aliveEnemies)
    {
        State = state;
        Gold = gold;
        Lives = lives;
        CurrentWave = currentWave;
        TotalWaves = totalWaves;
        AliveEnemies = aliveEnemies;
    }
}
