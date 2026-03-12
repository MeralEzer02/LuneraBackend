namespace TheSocialMediaV2.Domain.Enums
{
    public enum AdminActionType
    {
        UserBan = 1,
        UserUnban = 2,
        WarningIssued = 3,

        ReportReviewed = 100, // İncelendi (Karar bekleniyor)
        ReportAccepted = 101, // Onaylandı (Ceza verilecek)
        ReportRejected = 102  // Reddedildi (Suçsuz bulundu)

        // Gelecek rezervleri için aralıklı sayı verdik.
    }
}