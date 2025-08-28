namespace AlertaBoletaService.Models
{
    public enum BoletaStatus
    {
        PendingApproval,
        PendingReApproval
    }

    public static class BoletaStatusExtensions
    {
        public static string ToDisplayString(this BoletaStatus status)
        {
            return status switch
            {
                BoletaStatus.PendingApproval => "Pending Approval",
                BoletaStatus.PendingReApproval => "Pending Re-Approval",
                _ => throw new ArgumentOutOfRangeException(nameof(status))
            };
        }

        public static string ToEmailSection(this BoletaStatus status)
        {
            return status switch
            {
                BoletaStatus.PendingApproval => "approval",
                BoletaStatus.PendingReApproval => "re-approval",
                _ => throw new ArgumentOutOfRangeException(nameof(status))
            };
        }
    }
}
