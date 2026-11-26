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
