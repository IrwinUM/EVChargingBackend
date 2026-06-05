using EVChargingBackend.Domain.Enums;

namespace EVChargingBackend.Domain.Entities;

public class ChargingSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal EnergyKwh { get; set; }
    public decimal TariffRatePerKwh { get; set; }
    public ChargingSessionStatus Status { get; set; }
}