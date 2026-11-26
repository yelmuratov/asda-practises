using HotelBooking.Reservations;
using NetArchTest.Rules;

namespace HotelBooking.Tests;

// The three borders between the modules, from the P05 context map. NetArchTest reads the compiled
// app (every type and the code inside its methods) and lists the types that break a rule.
public class ArchitectureTests
{
    private static readonly Types App = Types.InAssembly(typeof(BookingService).Assembly);

    // Rule 1: BookingService may use the Notifications contract, never the email sender itself.
    [Fact]
    public void Reservations_uses_only_the_public_contracts_of_other_modules()
    {
        var result = App.That().ResideInNamespace("HotelBooking.Reservations")
            .ShouldNot().HaveDependencyOnAny("HotelBooking.Rooms.Internal", "HotelBooking.Payments.Internal", "HotelBooking.Notifications.Internal")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    // Rule 2: Rooms is the base; it knows nothing about reservations, money or messages.
    [Fact]
    public void Rooms_does_not_depend_on_any_other_module()
    {
        var result = App.That().ResideInNamespace("HotelBooking.Rooms")
            .ShouldNot().HaveDependencyOnAny("HotelBooking.Reservations", "HotelBooking.Payments", "HotelBooking.Notifications")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    // Rule 3: the arrows point one way, out of Reservations, so there is never a dependency circle.
    [Fact]
    public void Payments_and_Notifications_do_not_depend_on_Reservations()
    {
        var result = App.That().ResideInNamespace("HotelBooking.Payments").Or().ResideInNamespace("HotelBooking.Notifications")
            .ShouldNot().HaveDependencyOn("HotelBooking.Reservations")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}
