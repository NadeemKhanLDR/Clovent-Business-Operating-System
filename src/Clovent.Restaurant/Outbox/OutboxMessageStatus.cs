namespace Clovent.Restaurant.Outbox;

/// <summary>Lifecycle status of a transactional outbox message.</summary>
public enum OutboxMessageStatus
{
    /// <summary>Created and awaiting pickup by background worker.</summary>
    Pending = 0,

    /// <summary>Claimed by worker and currently being processed.</summary>
    Processing = 1,

    /// <summary>Successfully processed by destination handler.</summary>
    Completed = 2,

    /// <summary>Failed an attempt but scheduled for exponential backoff retry.</summary>
    RetryScheduled = 3,

    /// <summary>Failed permanently without further automatic retries.</summary>
    Failed = 4,

    /// <summary>Exceeded max retries and moved to dead-letter queue for review.</summary>
    DeadLetter = 5
}
