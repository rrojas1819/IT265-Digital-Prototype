using System;
using System.Collections.Generic;
using UnityEngine;

public class RNGService
{
    private static RNGService _instance = new RNGService();

    public static RNGService Instance => _instance;

    private System.Random _random;
    public int CurrentSeed { get; private set; }

    private RNGService()
    {
        SetSeed(Environment.TickCount);
    }

    public void SetSeed(int seed)
    {
        CurrentSeed = seed;
        _random = new System.Random(seed);
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        return _random.Next(minInclusive, maxExclusive);
    }

    public float Value()
    {
        return (float)_random.NextDouble();
    }

    public void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = _random.Next(0, i + 1);
            T temp = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = temp;
        }
    }
}
