namespace Clovent.Restaurant.Application.OrderHealth;

/// <summary>An open order's wait-time health, mirroring the Running Orders screen's traffic-light colors.</summary>
public enum OrderHealthStatus
{
    /// <summary>Waited less than <see cref="OrderHealthThresholds.GreenMinutes"/> minutes.</summary>
    Green,

    /// <summary>Waited between the green and orange thresholds.</summary>
    Orange,

    /// <summary>Waited <see cref="OrderHealthThresholds.OrangeMinutes"/> minutes or more.</summary>
    Red
}
