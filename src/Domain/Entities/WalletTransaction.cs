using EVChargingBackend.Domain.Enums;

namespace EVChargingBackend.Domain.Entities;

public class WalletTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }
}