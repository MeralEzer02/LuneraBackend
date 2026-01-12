using System;
using TheSocialMediaV2.Enums; // Enum'ları buradan çekecek

namespace TheSocialMediaV2.Utilities
{
    public static class ZodiacCalculator
    {
        // Tuple yapısı ile gün ve burç eşleşmesi
        private static readonly (int StartDay, ZodiacSign Sign)[] ZodiacTable =
        {
            (356, ZodiacSign.Capricorn),   // 22 Aralık
            (20,  ZodiacSign.Aquarius),    // 21 Ocak
            (50,  ZodiacSign.Pisces),      // 19 Şubat
            (80,  ZodiacSign.Aries),       // 21 Mart
            (110, ZodiacSign.Taurus),      // 21 Nisan
            (141, ZodiacSign.Gemini),      // 21 Mayıs
            (173, ZodiacSign.Cancer),      // 22 Haziran
            (205, ZodiacSign.Leo),         // 23 Temmuz
            (236, ZodiacSign.Virgo),       // 23 Ağustos
            (266, ZodiacSign.Libra),       // 23 Eylül
            (296, ZodiacSign.Scorpio),     // 23 Ekim
            (326, ZodiacSign.Sagittarius)  // 22 Kasım
        };

        public static ZodiacSign Calculate(DateTime birthDate)
        {
            int dayOfYear = birthDate.DayOfYear;

            // Tersten kontrol ederek doğru aralığı buluyoruz
            for (int i = ZodiacTable.Length - 1; i >= 0; i--)
            {
                if (dayOfYear >= ZodiacTable[i].StartDay)
                    return ZodiacTable[i].Sign;
            }

            // Hiçbiri değilse Oğlak (Yıl başı döngüsü)
            return ZodiacSign.Capricorn;
        }
    }
}