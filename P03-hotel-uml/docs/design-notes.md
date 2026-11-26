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
