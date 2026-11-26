namespace HotelBooking;

// What BookingService needs from storage. Today a list in memory; from P07 a database.
public interface IReservationStore
{
    int Count { get; }
    bool HasOverlap(Room room, DateRange stay);
    void Save(Reservation reservation);
    Reservation? Find(string id);
    int CountConfirmed(Room room, DateRange stay);
}
