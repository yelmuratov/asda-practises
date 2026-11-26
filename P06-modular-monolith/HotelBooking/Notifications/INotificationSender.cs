namespace HotelBooking.Notifications;

// Notifications' public contract: "give me a Confirmation and I will tell the guest".
// How the message travels is Notifications' own business.
public interface INotificationSender
{
    void SendConfirmation(Confirmation confirmation);
}
