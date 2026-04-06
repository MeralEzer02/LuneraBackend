using System;

namespace TheSocialMediaV2.Application.Abstractions.Services
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}