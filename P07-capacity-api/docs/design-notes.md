# P03 design notes

## Why design before code

Low-level design decides which classes the app has, what each class keeps and does, and which class calls which, before any code is typed. For example, the "share a night" rule gets one home, `DateRange.Overlaps()`, so the website and the reception desk call the same method instead of each keeping its own copy of the check. A diagram is cheap to change; code written twice is not.

## Where the methods live

- `Cancel()` lives in `Reservation`, because cancelling changes only that reservation's own status (Confirmed → Cancelled). Nothing is deleted, so the reservation stays in the store as history.
- `HasOverlap(room, stay)` lives in `ReservationStore`, because only the store can see all the reservations. It asks each one `Blocks(room, stay)`, and each reservation answers with its own status, room and dates.

## Activity traces

R7 already has Maya's Confirmed booking for 12–14 November.

- Omar, R7, 13–15 November. Diamond 1 "Check-out after check-in?": Yes (15 is after 13). Diamond 2 "A Confirmed booking shares a night?": Yes, the night of the 13th. The request ends at "Unavailable · save nothing".
- Nora, R7, 14–16 November. Diamond 1: Yes (16 is after 14). Diamond 2: No (Maya's last night is the 13th; the 14th is her check-out day). Then "Save the reservation as Confirmed", "Send the confirmation email", and the request ends at "Accepted + reservation id".
- Omar, R8, 15–15 November. Diamond 1: No (check-out is the same day as check-in, so the stay has zero nights). The request ends at "InvalidDates · save nothing"; diamond 2 is never asked.

## Prediction

Before running, my question was whether Nora should be accepted at all, because Maya may still be in R7 on the morning of the 14th. R3 counts nights, not days: Maya's nights are the 12th and the 13th, and check-out time on the 14th comes before check-in time, so Nora shares no night with her. Expected line: `Nora books R7 14-16 Nov: Accepted R102` (R102, because Omar's refused request saved nothing).

Result (`dotnet run --project HotelBooking`, 2026-11-26 12:06): `Nora books R7 14-16 Nov: Accepted R102`, and all nine lines matched the expected output.

## The race

My prediction: R7 ends with 1 Confirmed reservation (Maya Accepted, Omar Unavailable).

Result (`dotnet run --project HotelBooking -- --race`, 2026-11-26 12:06):

```text
Maya: Accepted R101
Omar: Accepted R102
Confirmed reservations for R7 on 12-14 Nov: 2
Double booking! Both requests checked before either one saved.
```

My prediction was wrong. Both requests called `store.HasOverlap(room, stay)` before either one called `store.Save(reservation)`, so both checks saw an empty store, and each request then saved its own reservation: R7 has two Confirmed guests for the same nights and R3 is broken. The problem sits in `Reserve()` between those two calls: the answer of the check is already out of date when the save happens (check, then act: a race condition).

I expected Omar's check to see Maya's booking, but at 0.1 s Maya's request was still in the 0.5 s slow step and had saved nothing. It is the same problem as the last ticket in P01 (19 read twice, 21 saved). Testing one request at a time could not find it, because each request on its own is correct.

Not fixed in P03: a lock in the code would only work while the app runs as one program on one computer. P09 fixes it in the database. Limitation: one run on one laptop; the 0.5 s pause makes the gap large on purpose, so without it the double booking would be rarer, but still possible.

## Check and hand-over (Step 9)

Checked on 2026-11-26 against the P03 checklist:

- [x] `requirements.md`: R1–R5 each have a Check line and a Proof line (R5 only indirect); R3 has five examples.
- [x] `design-notes.md`: Why design before code, Where the methods live, Activity traces, Prediction, The race.
- [x] `diagrams.drawio` · Use cases: 2 actors outside the box, 4 goals inside, 8 lines.
- [x] `diagrams.drawio` · Classes: 8 boxes and 7 links.
- [x] `diagrams.drawio` · Sequence - book and Sequence - cancel: 4 lifelines and 6 numbered messages each.
- [x] `diagrams.drawio` · Activity - book: start, two diamonds with Yes/No, the actions and the end.
- [x] Program: runs, prints the nine demo lines, and `--race` shows the double booking.

I work alone, so there was no laptop swap with another pair and no partner to copy the folder to; the folder is pushed to GitHub instead. P04 starts from this folder.

# P04 design notes

P04 starts from a copy of the finished P03 folder (`P03-hotel-uml` stays as it was handed in).

## Refactoring

A refactoring changes how the code is organised (names, files, which class depends on which) but not what the app does for the guest. A change in behaviour is a bug fix or a new feature, and it goes in a separate step.

Must stay the same:
- the 6 tests from Step 2 pass after every move;
- the demo prints the same nine lines as in P03;
- the race demo still double-books (refactoring does not fix bugs).

## What our code breaks

- `BookingService` asks for exactly `ReservationStore` (D: dependency inversion). In P07 we would have to edit `BookingService` just to use a database.
- `BookingService` asks for exactly `EmailSender` (D and O: open/closed). Adding SMS confirmations would mean editing its constructor and `Reserve()`, and a test cannot make the email fail, so nothing can prove that a failed email keeps the booking.
- All nine types live in one `Program.cs` (S: single responsibility). One file changes for every reason, so every change touches the same file.
- Noted, not fixed: the demo pause (`Thread.Sleep(slowStepMs)`) inside `Reserve()` stays on purpose, so the race can still be shown until P09.

## Characterization tests (Step 2)

Before moving anything, I made every type `public` (the test project is a separate program and can only see public types) and wrote 6 xUnit tests in `HotelBooking.Tests`: free room, clash, adjacent stay, invalid dates, cancel, one confirmation per accepted booking. On the unchanged P03 code: 6 passed. They record what the app does today; the race is not tested because it depends on timing.

## Refactoring moves (Step 5)

| Move | What changed | Tests after the move |
| --- | --- | --- |
| 1 | New `INotificationSender`; `EmailSender` renamed `EmailNotificationSender` and implements it | 6 passed |
| 2 | New `IReservationStore`; `ReservationStore` renamed `InMemoryReservationStore` and implements it | 6 passed |
| 3 | `BookingService` in its own file; its constructor asks for `IReservationStore` and `INotificationSender`; `BookingResult` in its own file | 6 passed |
| 4–5 | `DateRange`, `Guest`, `Room`, `ReservationStatus`, `Reservation` one file each; `Program.cs` keeps only the entry point, the one place that picks the real store and sender | 6 passed (one run after both moves) |

No test body changed during the refactoring; only the `NewService()` helper and the `using` line did.

## Proof

Run on 2026-11-26 at 12:11, after the last move:
- Tests: `dotnet test HotelBooking.Tests` → 6 passed, 0 failed.
- Demo: same. `diff` between the P03 and P04 demo output is empty (all nine lines identical).
- Race: still double-booked, same output as P03 (`Confirmed reservations for R7 on 12-14 Nov: 2`).

## The benefit (Step 7)

- Fake sender: the tests now plug in `FakeNotificationSender`, which records the ids it would confirm. Test 6 checks the exact ids, R101 and R102: 6 passed.
- R6, test first. `A_failed_confirmation_keeps_the_booking` failed (red) with `System.InvalidOperationException : The mail server is down.`: the error escaped `Reserve()` after R101 had already been saved, so the guest would see a failure for a booking that exists. I changed `Reserve()` to catch the error after the save and answer Accepted with `NotificationFailed = true`: 7 passed. R6 changes behaviour, so it came after the refactoring proof, not during it.
- SMS: `SmsNotificationSender` and one test: 8 passed. The SHA-256 of `BookingService.cs` was the same before and after the SMS step (`c2d94134…`), so `BookingService` was not edited (O: open for extension, closed for modification).
- After Step 7 the demo is still identical to P03 and the race still double-books.

Trade-off in R6: `catch (Exception)` catches every error from the sender, including a programming mistake in it. I accept that, because the failure is not hidden: `NotificationFailed` tells the caller, so the hotel can resend. Limitation: there is no resend yet, and both senders are simulated.

## Check and hand-over (Step 8)

Checked on 2026-11-26 against the P04 checklist:

- [x] Tests: 8 pass (the 6 from Step 2, plus R6 and SMS).
- [x] App code: `BookingService`'s constructor takes `IReservationStore` and `INotificationSender`; one type per file.
- [x] Demo and race: the same nine demo lines as P03; the race still double-books.
- [x] `requirements.md`: R6 added, with a Proof line.
- [x] `design-notes.md`: Refactoring, What our code breaks, Proof (plus the moves and Step 7).
- [x] `diagrams.drawio` · Classes - after: 2 interfaces, 5 classes, 2 "uses" arrows, 4 "implements" lines.

I work alone, so there is no partner laptop to copy the folder to; it is pushed to GitHub instead. P05 starts from this folder.

# P06 design notes

P06 is again a refactoring: the P04 app moves into four modules, one for each P05 context, and must do exactly what it did before. "Before" and "After" are the proof.

## Before

Run on 2026-11-26 at 14:31, on the unchanged copy of the P05 folder:
- Tests: `dotnet test HotelBooking.Tests` → 8 passed, 0 failed.
- Demo: the same nine lines as P04 (`diff` with the P04 output is empty), ending with `Confirmation emails sent: 2`.
- Race: `Confirmed reservations for R7 on 12-14 Nov: 2` and "Double booking!" (P09 fixes it).

## Modules

Each P05 context is now a module: a folder and a namespace, `HotelBooking.<Module>`, with its internal code in `HotelBooking.<Module>.Internal`. The public contract is what the module promises the others; the internal code is how it keeps that promise, and it can change without anyone else noticing.

| Module | Public contract (other modules may use it) | Internal code (only the module itself, `Program.cs` and the tests) |
| --- | --- | --- |
| Rooms | `Room`, `IRoomCatalog` (which rooms the hotel sells) | `InMemoryRoomCatalog` |
| Reservations | `BookingService`, `BookingResult`, `Reservation`, `DateRange`, `Guest`, `ReservationStatus`, `IReservationStore` | `InMemoryReservationStore` |
| Payments | `PaymentStatus`, `IPaymentService` (take a deposit) | `SimulatedPaymentService` |
| Notifications | `Confirmation`, `INotificationSender` (tell the guest) | `EmailNotificationSender`, `SmsNotificationSender` |

The `Internal` namespace is only a sign: C# itself would still let Reservations use `EmailNotificationSender`. The architecture tests are what keep the border.

## Moves (Steps 3–5)

Order: Rooms and Notifications first, because they need no other module; Reservations last, because it uses both. After every move the code compiled and every arrow between modules pointed one way.

| Move | What changed | Tests after the move |
| --- | --- | --- |
| 3.1 Rooms | `Room` into `HotelBooking.Rooms`; new `IRoomCatalog` and `Rooms.Internal.InMemoryRoomCatalog`; a `using HotelBooking.Rooms;` line in each file that uses `Room` | 8 passed |
| 3.2 Notifications | New `Confirmation` record and `INotificationSender.SendConfirmation(Confirmation)`; the email and SMS senders into `Notifications.Internal`; `Reserve()` builds a `Confirmation`; the fake sender records `confirmation.ReservationId` | 8 passed |
| 4.1 Reservations | `BookingService`, `BookingResult`, `Reservation`, `DateRange`, `Guest`, `ReservationStatus`, `IReservationStore` into `HotelBooking.Reservations`; `InMemoryReservationStore` into `Reservations.Internal` | 8 passed |
| 4.2 Payments | New `PaymentStatus`, `IPaymentService` and `Payments.Internal.SimulatedPaymentService`; nothing calls it yet (the booking saga does in P17) | 8 passed |
| 5 Wiring | `Program.cs` creates `InMemoryRoomCatalog([r7, r8])` and passes `catalog.All()` into `BookingService` | 8 passed, demo unchanged |

No test method changed: `BookingService` keeps its P04 constructor, so only the `using` lines and the fake sender changed in the test file.

Why Notifications got its own `Confirmation`: in P04, `SendConfirmation(Reservation)` made Notifications need the `Reservation` class, while Reservations needed the sender, so the two depended on each other (a circle). `Confirmation` is exactly the five fields of ReservationConfirmed from `contexts.md` ("Between contexts"). Now only Reservations depends on Notifications.

My decision on Payments: `TakeDeposit(reservationId, stayPrice)` receives the stay's price, not the deposit, and Payments works out the 30% itself. This keeps my P05 decision on card 6 (the 30% rule belongs to Payments, and the price travels in the request). Trade-off: every class that implements `IPaymentService` must apply the 30% rule; if a second one appears, the rule moves into one shared class inside Payments.

Search check (Step 5): only `Program.cs` and `BookingServiceTests.cs` name another module's `Internal` code. Every `Internal` class names only its own module.

## Architecture rules

Three rules from the P05 context map, checked in `HotelBooking.Tests/ArchitectureTests.cs` with NetArchTest.Rules 1.3.2, which reads the compiled app, including the code inside methods:

1. Reservations uses only the public contracts of other modules. `BookingService` may use `INotificationSender`, never `EmailNotificationSender`, so Notifications can change how it sends without an edit in Reservations.
2. Rooms does not depend on any other module. It is the base and knows nothing about reservations, money or messages.
3. Payments and Notifications do not depend on Reservations. Every arrow points out of Reservations, so there is no dependency circle.

Result: `dotnet test HotelBooking.Tests` → 11 passed (the 8 behaviour tests and the 3 architecture checks). A behaviour test checks what the app does; an architecture test checks how the code is connected. Both run with the same command.

## Broken on purpose

Prediction: only rule 1 fails, its message names `BookingService`, and the 8 booking tests stay green.

Result: with `using HotelBooking.Notifications.Internal;` and `private readonly EmailNotificationSender _shortcut = new();` added to `BookingService.cs`, the app still built, and only `Reservations_uses_only_the_public_contracts_of_other_modules` failed, with the message `HotelBooking.Reservations.BookingService` (1 failed, 10 passed). The prediction was right. The message names the class that broke the rule; the test's name and its list of forbidden namespaces (`HotelBooking.Notifications.Internal`) say what it reached into. After removing the two lines: 11 passed.

Rules 2 and 3 can fail too: a temporary class in Rooms and one in Notifications, each naming `Reservation`, made exactly those two checks fail (naming `HotelBooking.Rooms.TempRoomsBreak` and `HotelBooking.Notifications.TempNotificationsBreak`). After deleting both: 11 passed. A check that has never failed might be checking nothing, for example because of a misspelt namespace.

## After

Run on 2026-11-26 at 14:36, after the last move:
- Tests: 11 passed, 0 failed (the 8 from Before, plus the 3 architecture checks).
- Demo: the same nine lines as Before (`diff` is empty).
- Race: the same output as Before, 2 Confirmed reservations and "Double booking!".

They match Before. The modules changed where the code lives and what it may use, not what the app does.

## Check and hand-over (Step 8)

Checked on 2026-11-26 against the P06 checklist:

- [x] Code: every class lives in one of the four modules; `Program.cs` is the only app file that names Internal code (the tests may too).
- [x] Checks: 11 green, the 8 tests from P04 and the 3 architecture checks.
- [x] Demo and race: the same nine demo lines; the race still double-books (P09 fixes it).
- [x] `design-notes.md`: Before, Modules, Architecture rules, Broken on purpose, After.
- [x] `adr-001-modular-monolith.md`: ADR-001 is there.
- [x] `diagrams.drawio` · Modules: 5 boxes (Reservations green, the internal sender purple), 4 dashed arrows and the red Forbidden arrow.

`contexts.md` now gives the P06 names in code (Confirmation, IRoomCatalog, IPaymentService). P07 starts from this folder: the PostgreSQL store will be one new class in `Reservations.Internal` and one changed line in `Program.cs`.

# P07 design notes

P07 puts the P06 app behind an HTTP API on http://localhost:5080 and moves its reservations into PostgreSQL. `BookingService`'s rule does not change: the new code sits at the edges (an `Api` folder, two PostgreSQL stores in the modules' internal code, and the wiring).

## Before

Run on 2026-11-27 at 14:58, on the unchanged copy of the P06 folder: 11 passed (8 behaviour tests, 3 architecture checks); the demo prints the same nine lines as P06; the race still gives 2 Confirmed reservations for R7 ("Double booking!").

## Capacity (Step 1)

`docs/capacity.md`: at the peak, 3.33 searches a second (200 a minute), which is exactly the load of S1, and 133 database queries a second, because a search asks one question per room. Bookings are only about 0.002 a second. The search, not the booking, is what P08 has to make cheaper.

## Database (Step 2)

`docker-compose.yml` runs PostgreSQL 18 and `db/init.sql` creates one table per module (`rooms` for Rooms, `reservations` for Reservations) with no foreign key between them. Checked with psql: R7 Standard and R8 Deluxe; a second R101 is refused (`duplicate key value violates unique constraint "reservations_pkey"`), and so is a 15–15 November stay (`violates check constraint "reservations_check"`).

My decisions for this laptop:
- Port 15432 instead of 5432: another project's PostgreSQL already uses 5432 here. `docker-compose.yml` publishes 15432 and the wiring uses 15432 unless `HOTEL_DB_PORT` says otherwise, so the two files agree without setting anything.
- Published on 127.0.0.1 only, because the password `hotel` is only good enough for a database no other machine can reach.
- `postgres:18-alpine` instead of `postgres:18`: the same PostgreSQL 18, and the image was already on this laptop, so nothing had to be downloaded.
- `name: hotel-booking` in `docker-compose.yml`, so the P08 folder uses the same Docker volume and starts from this data.

## API decisions (Step 3)

`docs/openapi.yaml` is written before the code: three operations (search, book, cancel), the answers they give and five error titles. Checked locally: the YAML parses, all 9 `$ref`s resolve, and it has the 3 operations and the 5 titles.

- Order of the free rooms: by room number compared as text, so R10 comes before R7; `after` is compared the same way. It is written in the contract, so every client pages the same way.
- What clients compare: the `title` of an error (Unavailable, InvalidDates, UnknownRoom, InvalidRequest, NotFound). `detail` is a sentence for people, so it may be reworded without breaking a client.
- Room types are not a fixed list in the API: they are data from the `rooms` table, so a new type such as Suite needs no new API version.

## From console to web app (Steps 4–5)

`HotelBooking.csproj` now uses the Web SDK and Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3, and the build copies `docs/openapi.yaml` next to the program. Without an argument the program starts the API on http://localhost:5080; `--demo` and `--race` still run the P06 console programs on the lists, with the same output.

`Api/HotelApi.cs` lives outside the four modules and uses only their public contracts, so the three border checks needed no change. Each endpoint reads the request, checks only what `BookingService` cannot know (the shape of the request, whether the room exists), calls `BookingService` once, and turns the answer into HTTP: Accepted → 201, Unavailable → 409, InvalidDates → 400, and a failed cancel → 404. The overlap and date-order rules stay in `BookingService`, so the console demo, the API and the tests all use the same rule.

Checked with curl on the list store: search 200 with R7 and R8; Maya twice → 201 R101, then 409 Unavailable; cancel twice → 204, then 404 NotFound; `limit=1` → only R7 and `"next":"R7"`, and `after=R7` → R8 with `"next":null`; R99 → 400 UnknownRoom; a body without guestEmail → 400 InvalidRequest; `limit=0` → 400 InvalidRequest.

## API tests (Step 6)

`HotelBooking.Tests/ApiTests.cs` runs the web app inside the test on ASP.NET Core's test server (Microsoft.AspNetCore.Mvc.Testing 10.0.14), with the list store: no server, no network, no database.

| Test | Request | Expected answer |
| --- | --- | --- |
| `Search_answers_200_with_the_free_rooms` | GET /rooms/availability for 12–14 Nov | 200 and the exact body with R7 and R8, `"next":null` |
| `Booking_answers_201_then_409_for_a_shared_night` | POST Maya R7 12–14, then POST Omar R7 13–15 | 201 `{"id":"R101",…}`, then 409 Unavailable |
| `Wrong_dates_answer_400_with_problem_details` | POST with checkOut equal to checkIn | 400 InvalidDates as `application/problem+json` |
| `Cancel_answers_204_then_404` | POST Maya, then DELETE /reservations/R101 twice | 204, then 404 NotFound |

Result: 15 passed (8 behaviour, 3 architecture, 4 API). Changing the expected 409 to 201 in the second test made exactly that test fail (`Expected: Created, Actual: Conflict`); after changing it back, 15 passed.

## PostgreSQL stores (Step 7)

`PostgresRoomCatalog` (Rooms) and `PostgresReservationStore` (Reservations) live in the modules' `Internal` code, each with a row class and its own `DbContext`, one per table. `Reservation` stays free of database code: the store copies between the row and the domain object. `BookingService` did not change.

The store is a unit of work: the wiring creates a fresh store for every request, `Save` inserts at once (so the primary key refuses a second R101 right there), `Find` remembers the reservations it hands out, and at the end of the request `SaveChanges` writes the changed statuses. So a cancel is one SELECT, the status change in memory, and one UPDATE at the end; a search writes nothing.

Checked in the app's log after the restart: the rooms table is read once at start-up, and one search runs one `SELECT EXISTS (…)` per room, 2 queries for R7 and R8. With 40 rooms that is the 40 queries per search of `capacity.md`.

## The proof (Step 8)

The wiring now creates both PostgreSQL stores and the unit of work. `docs/api-check.md`, from an empty database: all seven requests gave exactly the expected answer, Maya's R102 was still Confirmed after the app restarted, and psql shows R101 Cancelled and R102 Confirmed.

Mistake found on the way: my first run of the seven requests answered R102 to Maya's first booking, and psql showed no rows. An app I had started for the Step 5 curl checks was still running on port 5080 with the list store, so the new PostgreSQL app could not bind the port ("address already in use") and every request went to the old one. After stopping it, the repeated run from an empty database gave the answers above. Lesson: check which process owns the port before trusting a result.

## After

Run on 2026-11-27 at 15:03:
- Checks: 15 passed, 0 failed (8 behaviour, 3 architecture, 4 API).
- The seven requests: all exactly as in the contract, against PostgreSQL (`docs/api-check.md`).
- Restart check: Maya's R102 survived the restart; only R8 is free for 12–14 November.
- `--demo` prints the same nine lines as Before, and `--race` still double-books (P09 fixes it in PostgreSQL).

## Check and hand-over

Checked on 2026-11-27 against the P07 checklist:

- [x] `docs/capacity.md`: five assumptions, five results, two things to measure in P08.
- [x] `docker-compose.yml`, `db/init.sql`: `docker compose up -d` starts PostgreSQL with R7 and R8.
- [x] `docs/openapi.yaml`: three operations, five error titles; checked locally (parses, every `$ref` resolves).
- [x] The Api file: three endpoints outside the four modules; `GET /openapi.yaml` serves the contract.
- [x] Checks: 15 green (8 behaviour, 3 architecture, 4 API).
- [x] The PostgreSQL stores: in Rooms' and Reservations' internal code; only the wiring (and no test) names them.
- [x] `docs/api-check.md`: seven ✓, and the booking survived the restart.
- [x] `design-notes.md`: API decisions, API tests, After.

Not in this folder: `docs/l04-notes.md` (L04 lecture answers). Instead, I compared the peak in `capacity.md` with S1: both say 200 searches a minute (3.33 a second).

PostgreSQL is stopped with `docker compose down` (without `-v`), so P08 starts from this data in the `hotel-booking` volume. I work alone, so the folder is pushed to GitHub instead of copied to a partner's laptop.
