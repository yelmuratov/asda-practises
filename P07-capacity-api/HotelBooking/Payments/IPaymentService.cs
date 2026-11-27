namespace HotelBooking.Payments;

// Payments' public contract: "take the deposit for this reservation", answered with Paid or Declined.
// It receives the stay's price, not the deposit, because the 30% rule belongs to Payments (P05, card 6).
// Booking does not call it yet: the booking saga uses it in P17.
public interface IPaymentService
{
    PaymentStatus TakeDeposit(string reservationId, decimal stayPrice);
}
