using HotelBooking.Rooms;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Reservations.Internal;

// One row of the reservations table in db/init.sql. The store builds the domain Reservation from it,
// so Reservation itself stays free of database code.
public class ReservationRow
{
    public required string Id { get; set; }
    public required string GuestName { get; set; }
    public required string GuestEmail { get; set; }
    public required string Room { get; set; }
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public required string Status { get; set; }
}

// The table the Reservations module owns.
public class ReservationsDb(DbContextOptions<ReservationsDb> options) : DbContext(options)
{
    public DbSet<ReservationRow> Reservations => Set<ReservationRow>();

    protected override void OnModelCreating(ModelBuilder model) => model.Entity<ReservationRow>(table =>
    {
        table.ToTable("reservations");
        table.Property(row => row.Id).HasColumnName("id");
        table.Property(row => row.GuestName).HasColumnName("guest_name");
        table.Property(row => row.GuestEmail).HasColumnName("guest_email");
        table.Property(row => row.Room).HasColumnName("room");
        table.Property(row => row.CheckIn).HasColumnName("check_in");
        table.Property(row => row.CheckOut).HasColumnName("check_out");
        table.Property(row => row.Status).HasColumnName("status");
    });
}

// Keeps the store promise with PostgreSQL. Program.cs creates one per request (a unit of work):
// it remembers the reservations it hands out, and SaveChanges writes what changed at the end.
public class PostgresReservationStore(ReservationsDb db, IReadOnlyList<Room> rooms) : IReservationStore
{
    private readonly List<(ReservationRow Row, Reservation Reservation)> loaded = [];

    // SELECT count(*): the next id is R{101 + count}, as before.
    public int Count => db.Reservations.Count();

    // One question per room: a search asks it for every room.
    public bool HasOverlap(Room room, DateRange stay) => Confirmed(room, stay).Any();

    public void Save(Reservation reservation)
    {
        db.Reservations.Add(new ReservationRow
        {
            Id = reservation.Id, GuestName = reservation.Guest.Name, GuestEmail = reservation.Guest.Email,
            Room = reservation.Room.Number, CheckIn = reservation.Stay.CheckIn, CheckOut = reservation.Stay.CheckOut,
            Status = reservation.Status.ToString(),
        });
        db.SaveChanges(); // INSERT now, so a second R101 is refused here by the primary key
    }

    // SELECT ... WHERE id = ...: builds the Reservation and remembers it until the end of the request.
    public Reservation? Find(string id)
    {
        var row = db.Reservations.Find(id);
        if (row is null) return null;
        var reservation = new Reservation(row.Id, new Guest(row.GuestName, row.GuestEmail),
            rooms.First(room => room.Number == row.Room), new DateRange(row.CheckIn, row.CheckOut));
        if (row.Status == "Cancelled") reservation.Cancel();
        loaded.Add((row, reservation));
        return reservation;
    }

    public int CountConfirmed(Room room, DateRange stay) => Confirmed(room, stay).Count();

    // The end of the request: copy each loaded reservation's status to its row; EF Core runs an UPDATE
    // only for the rows that changed (a cancel), and nothing at all after a search.
    public void SaveChanges()
    {
        foreach (var (row, reservation) in loaded) row.Status = reservation.Status.ToString();
        db.SaveChanges();
    }

    // Confirmed reservations of this room that share at least one night with the stay (half-open dates).
    private IQueryable<ReservationRow> Confirmed(Room room, DateRange stay) =>
        db.Reservations.Where(row => row.Room == room.Number && row.Status == "Confirmed"
            && row.CheckIn < stay.CheckOut && stay.CheckIn < row.CheckOut);
}
