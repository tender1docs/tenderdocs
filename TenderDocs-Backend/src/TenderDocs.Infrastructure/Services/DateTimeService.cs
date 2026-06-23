using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Infrastructure.Services;

public class DateTimeService : IDateTime
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
