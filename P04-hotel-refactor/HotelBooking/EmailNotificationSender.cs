namespace HotelBooking;

// Simulated: counts the emails it would send, so no email account or network is needed.
public class EmailNotificationSender : INotificationSender
{
    public int Sent { get; private set; }

    public void SendConfirmation(Reservation reservation) => Sent++;
}
