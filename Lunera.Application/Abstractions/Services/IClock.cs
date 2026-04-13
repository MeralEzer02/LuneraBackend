using System;

namespace Lunera.Application.Abstractions.Services
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}