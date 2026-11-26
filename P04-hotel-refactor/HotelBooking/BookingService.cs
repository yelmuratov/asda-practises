namespace HotelBooking;

// Runs each request in the order of the sequence diagram: check dates, check overlap, save, notify, answer.
// Depends only on the two interfaces; Program decides which store and sender are plugged in.
public class BookingService(IReadOnlyList<Room> rooms, IReservationStore store, INotificationSender notifications, int slowStepMs = 0)
{
    public IReadOnlyList<Room> FindFreeRooms(DateRange stay) =>
        rooms.Where(room => !store.HasOverlap(room, stay)).ToList();

    public BookingResult Reserve(Guest guest, Room room, DateRange stay)
    {
        if (!stay.IsValid()) return new("InvalidDates");
        if (store.HasOverlap(room, stay)) return new("Unavailable");

        Thread.Sleep(slowStepMs); // 0 in the demo; the race makes the gap between check and save visible

        var reservation = new Reservation($"R{101 + store.Count}", guest, room, stay);
        store.Save(reservation);
        try
        {
            notifications.SendConfirmation(reservation);
        }
        catch (Exception)
        {
            // R6: the booking is already saved, so a failed message must not undo or hide it.
            return new("Accepted", reservation.Id, NotificationFailed: true);
        }
        return new("Accepted", reservation.Id);
    }

    public bool Cancel(string reservationId) => store.Find(reservationId)?.Cancel() ?? false;
}
