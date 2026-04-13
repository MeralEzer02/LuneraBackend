using System;
using Lunera.Application.Abstractions.Services;

namespace Lunera.API.Services
{
    public class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}