using System;

namespace TheSocialMediaV2.Domain.Utilities;

public static class AgeCalculator
{
    public static int Calculate(DateTime birthDate)
    {
        if (birthDate > DateTime.Today)
            throw new ArgumentException("Doğum tarihi gelecekte olamaz.");

        int age = DateTime.Today.Year - birthDate.Year;

        if (birthDate.Date > DateTime.Today.AddYears(-age))
            age--;

        return age;
    }
}