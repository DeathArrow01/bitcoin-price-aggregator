namespace BitcoinPriceAggregator.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when an invalid date range is provided
    /// </summary>
    public class InvalidDateRangeException : DomainException
    {
        public InvalidDateRangeException(DateTimeOffset startTime, DateTimeOffset endTime) 
            : base($"Invalid date range: start time {startTime:yyyy-MM-dd HH:mm:ss} is after end time {endTime:yyyy-MM-dd HH:mm:ss}")
        {
            StartTime = startTime;
            EndTime = endTime;
        }

        public DateTimeOffset StartTime { get; }
        public DateTimeOffset EndTime { get; }
    }
} 