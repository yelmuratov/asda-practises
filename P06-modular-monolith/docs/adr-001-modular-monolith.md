# ADR-001: Build the hotel booking app as a modular monolith

Status: Accepted, 26 November 2026 (written down in P06, when the decision became code)

## Context

One developer (me today, a small team later) builds, tests and releases the whole app. In P05 the hotel split into four business areas: Rooms, Reservations, Payments and Notifications. The scenarios that matter most are S2 (no double booking) and S3 (a failed message keeps the booking). No area has to be released or scaled on its own yet: the app has two rooms and runs as one program.

## Decision

The app stays one program, built and run once, with four modules: `HotelBooking.Rooms`, `HotelBooking.Reservations`, `HotelBooking.Payments` and `HotelBooking.Notifications`. Each module has a public contract and an `Internal` namespace. Reservations owns the booking rule and talks to the other modules only through their contracts. `Program.cs` is the only app file that creates internal classes. Three architecture tests (NetArchTest) keep the borders.

## Alternatives considered

- Four separate services (or only Payments and Notifications as services): each could be released and scaled on its own, but every call would cross the network, which adds timeouts, retries, partial failures, more deployments and more monitoring. Rejected for now: nothing needs it yet, and S2 is easier to keep in one process and one store.
- One program with no modules (the P04 code): simplest, but nothing stops one area from reaching into another's code, and the P04 sender already made Reservations and Notifications depend on each other. Rejected.

## Consequences

- Good: one build, one deployment and plain method calls; S2 is checked in one place; a failed message is caught in one method (S3).
- Good: a module's internal code can change without touching the others (the P07 PostgreSQL store is one new class plus one line in `Program.cs`), and the module borders are where services could be cut later.
- Bad: all modules are released together, and one crash stops all of them.
- Bad: the borders hold only while the architecture tests run (today by hand with `dotnet test`; in CI from P25).

## Revisit when

A module needs its own release schedule or its own scaling, shown by measurements rather than guesses, for example Notifications sending far more messages than bookings happen. L07/P13 makes that decision with real reasons.
