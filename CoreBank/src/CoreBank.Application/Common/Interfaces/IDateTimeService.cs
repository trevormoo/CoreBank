namespace CoreBank.Application.Common.Interfaces;

public interface IDateTimeService
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
