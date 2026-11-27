namespace HotelBooking.Payments.Internal;

// Simulated: no card and no network. It works out the deposit (30% of the stay's price, e.g. 72 of 240)
// and treats a positive deposit as Paid; anything else is Declined.
public class SimulatedPaymentService : IPaymentService
{
    private const decimal DepositShare = 0.30m;

    public PaymentStatus TakeDeposit(string reservationId, decimal stayPrice)
    {
        var deposit = Math.Round(stayPrice * DepositShare, 2);
        return deposit > 0 ? PaymentStatus.Paid : PaymentStatus.Declined;
    }
}
