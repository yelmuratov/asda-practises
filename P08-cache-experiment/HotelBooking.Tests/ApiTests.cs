using System.Net;
using System.Net.Http.Json;
using HotelBooking.Api;
using HotelBooking.Notifications.Internal;
using HotelBooking.Reservations;
using HotelBooking.Reservations.Internal;
using HotelBooking.Rooms;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace HotelBooking.Tests;

// The four API tests: HTTP in, HTTP out, with the reservations in a list. ASP.NET Core's test server
// runs the web app inside the test, so they need no running server, no network and no database.
public class ApiTests
{
    private static readonly Room[] Rooms = [new("R7", "Standard"), new("R8", "Deluxe")];
    private const string Search = "/rooms/availability?checkIn=2026-11-12&checkOut=2026-11-14";
    private static readonly object Maya = new { guestName = "Maya", guestEmail = "maya@example.com", room = "R7", checkIn = "2026-11-12", checkOut = "2026-11-14" };
    private static readonly object Omar = new { guestName = "Omar", guestEmail = "omar@example.com", room = "R7", checkIn = "2026-11-13", checkOut = "2026-11-15" };
    private const string BothFree = """{"checkIn":"2026-11-12","checkOut":"2026-11-14","rooms":[{"number":"R7","type":"Standard"},{"number":"R8","type":"Deluxe"}],"next":null}""";
    private const string OnlyR8 = """{"checkIn":"2026-11-12","checkOut":"2026-11-14","rooms":[{"number":"R8","type":"Deluxe"}],"next":null}""";

    // Redis's three commands on a dictionary, so the tests need no Redis. The tests take far less than
    // 30 seconds, so the TTL can be ignored.
    private class InMemoryKeyValueStore : IKeyValueStore
    {
        private readonly Dictionary<string, string> _values = [];
        public string? Get(string key) => _values.GetValueOrDefault(key);
        public void Set(string key, string value, TimeSpan ttl) => _values[key] = value;
        public void Increment(string key) => _values[key] = (int.Parse(_values.GetValueOrDefault(key, "0")) + 1).ToString();
    }

    // The web app with the in-memory store, counted for GET /stats, and the cache in a dictionary.
    private static async Task<HttpClient> NewClient()
    {
        var stats = new Stats();
        var service = new BookingService(Rooms, new CountingReservationStore(new InMemoryReservationStore(), stats), new EmailNotificationSender());
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        HotelApi.Map(app, Rooms, work => work(service), stats, new AvailabilityCache(new InMemoryKeyValueStore())); // a list in memory has nothing to save at the end of a request
        await app.StartAsync();
        return app.GetTestClient();
    }

    private static async Task<string?> Title(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<Dictionary<string, object>>())?["title"].ToString();

    [Fact]
    public async Task Search_answers_200_with_the_free_rooms()
    {
        var response = await (await NewClient()).GetAsync(Search);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"checkIn":"2026-11-12","checkOut":"2026-11-14","rooms":[{"number":"R7","type":"Standard"},{"number":"R8","type":"Deluxe"}],"next":null}""",
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Booking_answers_201_then_409_for_a_shared_night()
    {
        var client = await NewClient();
        var first = await client.PostAsJsonAsync("/reservations", Maya);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal("""{"id":"R101","status":"Confirmed","notificationFailed":false}""", await first.Content.ReadAsStringAsync());
        var second = await client.PostAsJsonAsync("/reservations", Omar);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("Unavailable", await Title(second));
    }

    [Fact]
    public async Task Wrong_dates_answer_400_with_problem_details()
    {
        var response = await (await NewClient()).PostAsJsonAsync("/reservations",
            new { guestName = "Maya", guestEmail = "maya@example.com", room = "R7", checkIn = "2026-11-12", checkOut = "2026-11-12" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("""{"title":"InvalidDates","status":400,"detail":"checkOut must be after checkIn."}""", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cancel_answers_204_then_404()
    {
        var client = await NewClient();
        await client.PostAsJsonAsync("/reservations", Maya);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/reservations/R101")).StatusCode);
        var second = await client.DeleteAsync("/reservations/R101");
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Equal("NotFound", await Title(second));
    }

    // P08: the second search for the same stay is answered from the copy, without the database.
    [Fact]
    public async Task The_second_search_is_answered_by_the_cache()
    {
        var client = await NewClient();
        var first = await client.GetStringAsync(Search);
        Assert.Equal(first, await client.GetStringAsync(Search));
        Assert.Equal("""{"searches":2,"databaseQueries":2,"cacheHits":1,"cacheMisses":1}""", await client.GetStringAsync("/stats"));
    }

    // P08: a booking and a cancel raise the version, so the next search asks the store again.
    [Fact]
    public async Task A_booking_and_a_cancel_make_the_next_search_fresh()
    {
        var client = await NewClient();
        Assert.Equal(BothFree, await client.GetStringAsync(Search));
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/reservations", Maya)).StatusCode);
        Assert.Equal(OnlyR8, await client.GetStringAsync(Search));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/reservations/R101")).StatusCode);
        Assert.Equal(BothFree, await client.GetStringAsync(Search));
    }
}
