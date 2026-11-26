# Requirements: hotel booking app, version 1

The hotel has two rooms: R7 (standard) and R8 (deluxe). Guests book on the website; a receptionist books for guests who phone or walk in. Both go into one shared list.

Dates count nights, not days. A stay of 12–14 November uses the nights of the 12th and the 13th. The 14th is the check-out day, so the next guest can check in that day (a half-open date range: check-in counts, check-out does not).

Not in version 1: prices, payments, invoices, logins, housekeeping.

## R1 · Search
A guest or receptionist can list the rooms that are free for given dates.
Check: empty hotel, 12–14 November → R7, R8.
Proof: Free rooms 12-14 Nov: R7, R8

## R2 · Book
A guest or receptionist can book a free room. The app answers with a reservation id.
Check: Maya books R7 for 12–14 November → Accepted R101.
Proof: Maya books R7 12-14 Nov: Accepted R101

## R3 · No clash
A room never has two Confirmed bookings on the same night. Check-out must be after check-in.
Check: Omar asks for R7 on 13–15 November → Unavailable. 15–15 November → InvalidDates.
Proof: Omar books R7 13-15 Nov: Unavailable; Omar books R8 15-15 Nov: InvalidDates; Nora books R7 14-16 Nov: Accepted R102
Not proven for two requests at the same moment: the race run gave R7 two Confirmed reservations for 12–14 November (design-notes.md, "The race").

More examples:
- Maya R7 12–14 → Confirmed (already booked: nights of the 12th and 13th)
- Omar R7 13–15 → Unavailable (night of the 13th is Maya's)
- Nora R7 14–16 → Accepted (no shared night: the 14th is Maya's check-out day)

My examples for R8, which is already booked for 20–22 November (nights of the 20th and 21st):
- R8 22–24 → Accepted (no shared night: the 22nd is the check-out day of the first guest)
- R8 19–21 → Unavailable (night of the 20th is shared)

## R4 · Cancel
A booking can be cancelled. It is kept as Cancelled and no longer blocks the room.
Check: Maya cancels R101 → R7 is free again for 12–14 November.
Proof: Maya cancels R101: True, then Free rooms 12-14 Nov: R7, R8

## R5 · View
A booking can be looked up by its id.
Check: R101 → Maya, R7, 12–14 November, Confirmed.
Proof (indirect): Maya cancels R101: True. Cancel finds R101 by its id with ReservationStore.Find("R101") before cancelling it. The demo does not print the booking's details, so the full view line is not shown yet.
