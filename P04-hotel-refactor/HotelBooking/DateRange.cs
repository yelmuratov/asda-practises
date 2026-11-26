namespace HotelBooking;

// A stay. The check-in night counts, the check-out day does not (half-open range).
public readonly record struct DateRange(DateOnly CheckIn, DateOnly CheckOut)
{
    public bool IsValid() => CheckOut > CheckIn;

    // The only place where the "share a night" rule is written.
    public bool Overlaps(DateRange other) =>
        CheckIn < other.CheckOut && other.CheckIn < CheckOut;
}
