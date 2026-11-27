using HotelBooking.Rooms;

namespace HotelBooking.Reservations;

public class Reservation
{
    public Reservation(string id, Guest guest, Room room, DateRange stay)
    {
        Id = id;
        Guest = guest;
        Room = room;
        Stay = stay;
    }

    public string Id { get; }
    public Guest Guest { get; }
    public Room Room { get; }
    public DateRange Stay { get; }
    public ReservationStatus Status { get; private set; } = ReservationStatus.Confirmed;

    // Only a Confirmed reservation for the same room that shares a night blocks a request.
    public bool Blocks(Room room, DateRange stay) =>
        Status == ReservationStatus.Confirmed && Room == room && Stay.Overlaps(stay);

    // Cancelling changes the status; the reservation stays in the store as history.
    public bool Cancel()
    {
        if (Status == ReservationStatus.Cancelled) return false;
        Status = ReservationStatus.Cancelled;
        return true;
    }
}
