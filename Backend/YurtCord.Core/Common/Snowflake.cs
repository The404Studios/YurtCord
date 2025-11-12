namespace YurtCord.Core.Common;

/// <summary>
/// Twitter Snowflake-based distributed ID generator
/// Format: 64-bit long (timestamp + worker ID + sequence)
/// </summary>
public readonly struct Snowflake : IEquatable<Snowflake>, IComparable<Snowflake>
{
    private const long Epoch = 1609459200000L; // January 1, 2021 00:00:00 UTC
    private const int TimestampBits = 42;
    private const int WorkerIdBits = 10;
    private const int SequenceBits = 12;

    private const long MaxWorkerId = (1L << WorkerIdBits) - 1;
    private const long MaxSequence = (1L << SequenceBits) - 1;

    private const int WorkerIdShift = SequenceBits;
    private const int TimestampShift = SequenceBits + WorkerIdBits;

    public long Value { get; }

    public Snowflake(long value)
    {
        Value = value;
    }

    public DateTime Timestamp
    {
        get
        {
            var milliseconds = (Value >> TimestampShift) + Epoch;
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
        }
    }

    public int WorkerId => (int)((Value >> WorkerIdShift) & MaxWorkerId);
    public int Sequence => (int)(Value & MaxSequence);

    public static Snowflake Parse(string value)
    {
        if (long.TryParse(value, out var id))
            return new Snowflake(id);
        throw new FormatException($"Invalid Snowflake format: {value}");
    }

    public static bool TryParse(string value, out Snowflake result)
    {
        if (long.TryParse(value, out var id))
        {
            result = new Snowflake(id);
            return true;
        }
        result = default;
        return false;
    }

    public override string ToString() => Value.ToString();
    public override int GetHashCode() => Value.GetHashCode();
    public override bool Equals(object? obj) => obj is Snowflake other && Equals(other);
    public bool Equals(Snowflake other) => Value == other.Value;
    public int CompareTo(Snowflake other) => Value.CompareTo(other.Value);

    public static bool operator ==(Snowflake left, Snowflake right) => left.Equals(right);
    public static bool operator !=(Snowflake left, Snowflake right) => !left.Equals(right);
    public static bool operator <(Snowflake left, Snowflake right) => left.Value < right.Value;
    public static bool operator >(Snowflake left, Snowflake right) => left.Value > right.Value;
    public static bool operator <=(Snowflake left, Snowflake right) => left.Value <= right.Value;
    public static bool operator >=(Snowflake left, Snowflake right) => left.Value >= right.Value;

    public static implicit operator long(Snowflake snowflake) => snowflake.Value;
    public static implicit operator Snowflake(long value) => new(value);
}

/// <summary>
/// Generator for Snowflake IDs with thread safety
/// </summary>
public class SnowflakeGenerator
{
    private readonly long _workerId;
    private long _sequence;
    private long _lastTimestamp = -1L;
    private readonly object _lock = new();

    private const long Epoch = 1609459200000L;
    private const int WorkerIdBits = 10;
    private const int SequenceBits = 12;
    private const long MaxSequence = (1L << SequenceBits) - 1;
    private const int WorkerIdShift = SequenceBits;
    private const int TimestampShift = SequenceBits + WorkerIdBits;

    public SnowflakeGenerator(int workerId = 0)
    {
        if (workerId < 0 || workerId > ((1 << WorkerIdBits) - 1))
            throw new ArgumentException($"Worker ID must be between 0 and {(1 << WorkerIdBits) - 1}");

        _workerId = workerId;
    }

    public Snowflake Generate()
    {
        lock (_lock)
        {
            var timestamp = GetCurrentTimestamp();

            if (timestamp < _lastTimestamp)
                throw new InvalidOperationException("Clock moved backwards. Refusing to generate ID.");

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & MaxSequence;
                if (_sequence == 0)
                {
                    // Sequence overflow, wait for next millisecond
                    timestamp = WaitNextMillis(_lastTimestamp);
                }
            }
            else
            {
                _sequence = 0;
            }

            _lastTimestamp = timestamp;

            var id = ((timestamp - Epoch) << TimestampShift) |
                     (_workerId << WorkerIdShift) |
                     _sequence;

            return new Snowflake(id);
        }
    }

    private static long GetCurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private static long WaitNextMillis(long lastTimestamp)
    {
        var timestamp = GetCurrentTimestamp();
        while (timestamp <= lastTimestamp)
        {
            timestamp = GetCurrentTimestamp();
        }
        return timestamp;
    }
}
