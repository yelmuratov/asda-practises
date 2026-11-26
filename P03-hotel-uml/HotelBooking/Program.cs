// P03 hotel booking, version 1. One file; all reservations live in memory.
//   dotnet run --project HotelBooking            -> demo of the requirement examples
//   dotnet run --project HotelBooking -- --race  -> Maya and Omar book R7 at the same moment

var r7 = new Room("R7", "Standard");
var r8 = new Room("R8", "Deluxe");
var maya = new Guest("Maya", "maya@example.com");
var omar = new Guest("Omar", "omar@example.com");
var nora = new Guest("Nora", "nora@example.com");

if (args.Contains("--race")) RunRace(); else RunDemo();

void RunDemo()
{
    var store = new ReservationStore();
    var email = new EmailSender();
    var service = new BookingService([r7, r8], store, email);

    Console.WriteLine($"Free rooms 12-14 Nov: {Names(service.FindFreeRooms(Nov(12, 14)))}");
    Console.WriteLine($"Maya books R7 12-14 Nov: {service.Reserve(maya, r7, Nov(12, 14))}");
    Console.WriteLine($"Omar books R7 13-15 Nov: {service.Reserve(omar, r7, Nov(13, 15))}");
    Console.WriteLine($"Nora books R7 14-16 Nov: {service.Reserve(nora, r7, Nov(14, 16))}");
    Console.WriteLine($"Omar books R8 15-15 Nov: {service.Reserve(omar, r8, Nov(15, 15))}");
    Console.WriteLine($"Free rooms 12-14 Nov: {Names(service.FindFreeRooms(Nov(12, 14)))}");
    Console.WriteLine($"Maya cancels R101: {service.Cancel("R101")}");
    Console.WriteLine($"Free rooms 12-14 Nov: {Names(service.FindFreeRooms(Nov(12, 14)))}");
    Console.WriteLine($"Confirmation emails sent: {email.Sent}");
}

void RunRace()
{
    var store = new ReservationStore();
    // Every request now waits half a second between the check and the save.
    var service = new BookingService([r7, r8], store, new EmailSender(), slowStepMs: 500);
    var stay = Nov(12, 14);
    BookingResult? mayaResult = null, omarResult = null;

    Console.WriteLine("Maya and Omar both book R7 for 12-14 Nov at the same moment...");
    var mayaRequest = new Thread(() => mayaResult = service.Reserve(maya, r7, stay));
    var omarRequest = new Thread(() => omarResult = service.Reserve(omar, r7, stay));
    mayaRequest.Start();
    Thread.Sleep(100); // Omar presses Book 0.1 s after Maya
    omarRequest.Start();
    mayaRequest.Join();
    omarRequest.Join();

    Console.WriteLine($"Maya: {mayaResult}");
    Console.WriteLine($"Omar: {omarResult}");
    var confirmed = store.CountConfirmed(r7, stay);
    Console.WriteLine($"Confirmed reservations for R7 on 12-14 Nov: {confirmed}");
    Console.WriteLine(confirmed > 1
        ? "Double booking! Both requests checked before either one saved."
        : "Only one guest has R7.");
}

DateRange Nov(int checkIn, int checkOut) =>
    new(new DateOnly(2026, 11, checkIn), new DateOnly(2026, 11, checkOut));

string Names(IEnumerable<Room> rooms) => string.Join(", ", rooms.Select(room => room.Number));

// A stay. The check-in night counts, the check-out day does not (half-open range).
readonly record struct DateRange(DateOnly CheckIn, DateOnly CheckOut)
{
    public bool IsValid() => CheckOut > CheckIn;

    // The only place where the "share a night" rule is written.
    public bool Overlaps(DateRange other) =>
        CheckIn < other.CheckOut && other.CheckIn < CheckOut;
}

record Guest(string Name, string Email);

record Room(string Number, string Type);

enum ReservationStatus { Confirmed, Cancelled }

class Reservation
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

// Keeps every reservation in a list in memory. A later practice swaps this for a database.
class ReservationStore
{
    private readonly List<Reservation> _rows = [];

    public int Count => _rows.Count;

    public bool HasOverlap(Room room, DateRange stay) => _rows.Any(row => row.Blocks(room, stay));

    public void Save(Reservation reservation) => _rows.Add(reservation);

    public Reservation? Find(string id) => _rows.FirstOrDefault(row => row.Id == id);

    public int CountConfirmed(Room room, DateRange stay) => _rows.Count(row => row.Blocks(room, stay));
}

// Simulated: it only counts confirmations, so no email account or network is needed.
class EmailSender
{
    public int Sent { get; private set; }

    public void SendConfirmation(Reservation reservation) => Sent++;
}

record BookingResult(string Outcome, string? ReservationId = null)
{
    public override string ToString() => ReservationId is null ? Outcome : $"{Outcome} {ReservationId}";
}

// Runs each request in the order of the sequence diagram: check dates, check overlap, save, email, answer.
class BookingService(IReadOnlyList<Room> rooms, ReservationStore store, EmailSender email, int slowStepMs = 0)
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
        email.SendConfirmation(reservation);
        return new("Accepted", reservation.Id);
    }

    public bool Cancel(string reservationId) => store.Find(reservationId)?.Cancel() ?? false;
}
