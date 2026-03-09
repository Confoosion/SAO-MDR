using UnityEngine;
using System;

/// <summary>
/// Manages the SP charge bar. Gains SP from landing hits.
/// Fires events for the UI to react to.
/// </summary>
[Serializable]
public class SPGauge
{
    [SerializeField] private int maxSP = 100;
    [SerializeField] private int currentSP = 0;

    // SP gained per hit landed — tune per weapon type if desired
    [SerializeField] private int spPerHit = 20;

    public int CurrentSP => currentSP;
    public int MaxSP => maxSP;
    public float FillRatio => maxSP > 0 ? (float)currentSP / maxSP : 0f;

    public event Action<int, int> OnSPChanged;  // (currentSP, maxSP)
    public event Action OnGaugeFull;

    public SPGauge(int maxSP = 100, int spPerHit = 20)
    {
        this.maxSP   = maxSP;
        this.spPerHit = spPerHit;
        currentSP    = 0;
    }

    /// <summary>
    /// Call this every time the player lands a hit.
    /// </summary>
    public void OnHitLanded()
    {
        AddSP(spPerHit);
    }

    /// <summary>
    /// Adds SP and fires events. Clamps to maxSP.
    /// </summary>
    public void AddSP(int amount)
    {
        bool wasFull = currentSP >= maxSP;
        currentSP = Mathf.Clamp(currentSP + amount, 0, maxSP);
        OnSPChanged?.Invoke(currentSP, maxSP);

        if (!wasFull && currentSP >= maxSP)
            OnGaugeFull?.Invoke();
    }

    /// <summary>
    /// Attempts to spend SP for a skill. Returns false if not enough SP.
    /// </summary>
    public bool TrySpend(int cost)
    {
        if (currentSP < cost) return false;
        currentSP -= cost;
        OnSPChanged?.Invoke(currentSP, maxSP);
        return true;
    }

    public bool CanActivate(int cost) => currentSP >= cost;
}