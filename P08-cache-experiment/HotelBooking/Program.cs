// P07 hotel booking: the P06 modular monolith behind an HTTP API on http://localhost:5080,
// with its reservations in PostgreSQL (first: docker compose up -d).
// This file is the wiring: the only app file that names the Internal classes of the modules.
//   dotnet run --project HotelBooking            -> the API on http://localhost:5080
//   dotnet run --project HotelBooking -- --demo  -> demo of the requirement examples (data in lists)
//   dotnet run --project HotelBooking -- --race  -> Maya and Omar book R7 at the same moment
using HotelBooking.Api;
using HotelBooking.Notifications.Internal;
using HotelBooking.Reservations;
using HotelBooking.Reservations.Internal;
using HotelBooking.Rooms;
using HotelBooking.Rooms.Internal;
using Microsoft.EntityFrameworkCore;

var r7 = new Room("R7", "Standard");
var r8 = new Room("R8", "Deluxe");
var maya = new Guest("Maya", "maya@example.com");
var omar = new Guest("Omar", "omar@example.com");
var nora = new Guest("Nora", "nora@example.com");

// Rooms keeps the list of rooms; BookingService only receives the list, never the catalog class.
var catalog = new InMemoryRoomCatalog([r7, r8]);

if (args.Contains("--race")) RunRace();
else if (args.Contains("--demo")) RunDemo();
else RunApi();

void RunApi()
{
    // The wiring for the web app: PostgreSQL from docker-compose.yml, published on 15432 on this laptop
    // (HOTEL_DB_PORT overrides it, e.g. 5432 where that port is free).
    var builder = WebApplication.CreateBuilder(args);
    var connection = $"Host=localhost;Port={Environment.GetEnvironmentVariable("HOTEL_DB_PORT") ?? "15432"};Database=hotel;Username=hotel;Password=hotel";
    builder.Services.AddDbContextFactory<RoomsDb>(options => options.UseNpgsql(connection));
    builder.Services.AddDbContextFactory<ReservationsDb>(options => options.UseNpgsql(connection));
    var app = builder.Build();

    // The rooms are read once, when the app starts; the reservations are read and written per request.
    var rooms = new PostgresRoomCatalog(app.Services.GetRequiredService<IDbContextFactory<RoomsDb>>()).All();
    var databases = app.Services.GetRequiredService<IDbContextFactory<ReservationsDb>>();
    var email = new EmailNotificationSender();
    var stats = new Stats();

    // One request: a fresh store over its own DbContext, counted for GET /stats; save what changed at the end.
    IResult UnitOfWork(Func<BookingService, IResult> work)
    {
        using var db = databases.CreateDbContext();
        var store = new PostgresReservationStore(db, rooms);
        var result = work(new BookingService(rooms, new CountingReservationStore(store, stats), email));
        store.SaveChanges();
        return result;
    }

    HotelApi.Map(app, rooms, UnitOfWork, stats);
    app.Run("http://localhost:5080");
}

void RunDemo()
{
    // The one place that chooses the real store and sender (the composition root).
    var store = new InMemoryReservationStore();
    var email = new EmailNotificationSender();
    var service = new BookingService(catalog.All(), store, email);

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
    var store = new InMemoryReservationStore();
    // Every request now waits half a second between the check and the save.
    var service = new BookingService(catalog.All(), store, new EmailNotificationSender(), slowStepMs: 500);
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
