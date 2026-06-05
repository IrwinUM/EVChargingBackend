namespace EVChargingBackend.Domain.Entities;

public class WalletTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }   // for idempotency
    public decimal Amount { get; set; }    // negative = debit, positive = credit
    public TransactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }
}
