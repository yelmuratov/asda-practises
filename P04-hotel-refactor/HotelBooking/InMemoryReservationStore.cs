namespace HotelBooking;

// Keeps reservations in a list in memory. P07 adds a PostgreSQL store behind the same interface.
public class InMemoryReservationStore : IReservationStore
{
    private readonly List<Reservation> _rows = [];

    public int Count => _rows.Count;

    public bool HasOverlap(Room room, DateRange stay) => _rows.Any(row => row.Blocks(room, stay));

    public void Save(Reservation reservation) => _rows.Add(reservation);

    public Reservation? Find(string id) => _rows.FirstOrDefault(row => row.Id == id);

    public int CountConfirmed(Room room, DateRange stay) => _rows.Count(row => row.Blocks(room, stay));
}
