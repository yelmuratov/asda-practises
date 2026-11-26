namespace HotelBooking;

// Added in P04 Step 7 without changing BookingService. Simulated, like the email sender.
public class SmsNotificationSender : INotificationSender
{
    public int Sent { get; private set; }

    public void SendConfirmation(Reservation reservation) => Sent++;
}
