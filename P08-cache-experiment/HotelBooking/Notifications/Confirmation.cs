namespace HotelBooking.Notifications;

// Notifications' own message: the five fields of ReservationConfirmed from P05 (contexts.md, "Between contexts").
// Because of it, Notifications never needs the Reservation class.
public record Confirmation(string ReservationId, string GuestEmail, string RoomNumber, DateOnly CheckIn, DateOnly CheckOut);
