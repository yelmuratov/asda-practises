namespace HotelBooking;

// What BookingService needs: "tell the guest". How the message travels is not its job.
public interface INotificationSender
{
    void SendConfirmation(Reservation reservation);
}
