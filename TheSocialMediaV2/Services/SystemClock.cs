using System;
using TheSocialMediaV2.Application.Abstractions.Services;

namespace TheSocialMediaV2.API.Services
{
    public class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}