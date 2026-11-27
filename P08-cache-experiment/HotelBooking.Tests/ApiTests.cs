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

    // The web app with the in-memory store, as the wiring built it before Step 8.
    private static async Task<HttpClient> NewClient()
    {
        var stats = new Stats();
        var service = new BookingService(Rooms, new CountingReservationStore(new InMemoryReservationStore(), stats), new EmailNotificationSender());
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        HotelApi.Map(app, Rooms, work => work(service), stats); // a list in memory has nothing to save at the end of a request
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
}
