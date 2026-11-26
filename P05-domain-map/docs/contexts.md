# Hotel contexts

A context (bounded context) is one business area with its own words and rules. Inside one context every word has exactly one meaning. P06 turns each context below into a module.

## Words

The hotel's words, used the same way in conversation, in these documents and in the code (the ubiquitous language).

| Word | Meaning here | Name in our code |
|---|---|---|
| Stay | Check-in to check-out, half-open: 12–14 Nov = the nights of the 12th and 13th | `DateRange` |
| Reservation | Guest + room + stay + status, with an id such as R101 | `Reservation` |
| Confirmed | The room is promised to this guest for these nights | `ReservationStatus.Confirmed` |
| Guest | The person who stays, with a name and an email, e.g. Maya | `Guest` |
| Room | A room the hotel sells, e.g. R8: number, type, price, on sale or not | `Room` |
| Room type | Standard or Deluxe; it sets the nightly price | `Room.Type` |
| Nightly price | What one night in a room costs, e.g. Deluxe more than Standard | not in code yet |
| Out of service | The room is taken off sale, e.g. R8 is being repaired next week | not in code yet |
| Cancelled | The promise is withdrawn; the reservation is kept, but its nights are free again | `ReservationStatus.Cancelled` |
| Book | Ask for a room for some nights; the answer is Accepted, Unavailable or InvalidDates | `BookingService.Reserve(guest, room, stay)` |
| Cancel | Withdraw a reservation by its id, e.g. Maya cancels R101 | `BookingService.Cancel(id)` |
| Free rooms | The rooms with no Confirmed reservation sharing a night with the stay | `BookingService.FindFreeRooms(stay)` |
| Deposit | 30% of the stay's price, taken when the guest books (from P17) | not in code yet |
| Paid | The deposit was taken | not in code yet |
| Declined | The deposit could not be taken (e.g. the card was refused); no money moved | not in code yet |
| Confirmation message | The email or SMS that tells the guest "R101 is confirmed: room R7, 12–14 November" | `INotificationSender.SendConfirmation(reservation)` |
| Sent | The provider accepted the message | not in code yet as a message status (`EmailNotificationSender.Sent` only counts messages) |
| Failed | The message could not be sent; it is recorded and never changes the reservation | not in code yet as a message status (`BookingResult.NotificationFailed` only reports it to the caller) |

## Rooms

Job: knows which rooms the hotel sells.

| Word | Meaning here |
|---|---|
| Room | A room the hotel sells: number, type, nightly price, on sale or out of service |
| Room type | Standard or Deluxe; it sets the nightly price |
| Nightly price | What one night in this room costs |
| Out of service | Taken off sale for some nights, e.g. for repairs; not offered to guests |

Rules this context owns:
- A room that is out of service is not offered to guests (card 4).
- The nightly price depends on the room type (card 5).

Needs from other contexts: nothing.
Tells other contexts: nothing. It only answers Reservations' question "which rooms exist?".

## Reservations

Job: decides whether a guest gets a room for some nights, and keeps every reservation. This is the core domain: correct bookings are the reason the hotel wants the app.

| Word | Meaning here |
|---|---|
| Stay | Check-in to check-out, half-open: 12–14 Nov = the nights of the 12th and 13th |
| Reservation | Guest + room number + stay + status, with an id such as R101 |
| Confirmed | The room is promised to this guest for these nights |
| Cancelled | The promise is withdrawn; the nights are free again |
| Guest | The person the reservation is for: name and email |
| Book | Ask for a room for some nights: Accepted, Unavailable or InvalidDates |
| Cancel | Withdraw a reservation; it stays in the list as Cancelled |
| Free rooms | Rooms with no Confirmed reservation sharing a night with the stay |

Rules this context owns:
- A room never has two Confirmed reservations that share a night (card 1).
- Check-out must be after check-in (card 2).
- Cancelling a reservation frees its nights for new bookings (card 3).

Needs from other contexts: the list of rooms (Rooms); from P17, Paid or Declined for a deposit (Payments).
Tells other contexts: ReservationConfirmed (Notifications).

## Payments

Job: takes deposits and records whether they were paid.

| Word | Meaning here |
|---|---|
| Deposit | 30% of the stay's price, taken when the guest books, e.g. 72 of a 240 stay |
| Paid | The deposit was taken |
| Declined | The deposit could not be taken; no money moved |

Rules this context owns:
- The deposit is 30% of the stay's price (card 6).
- Only a Paid deposit can be refunded (card 7).

Needs from other contexts: nothing it asks for itself. It receives Reservations' request "take a deposit" with the reservation id and the stay's price.
Tells other contexts: nothing on its own. It answers that request with Paid or Declined.

## Notifications

Job: tells guests what happened, by email or SMS.

| Word | Meaning here |
|---|---|
| Confirmation message | The email or SMS that tells the guest their reservation is confirmed |
| Sent | The provider accepted the message |
| Failed | The message could not be sent; recorded, and the reservation does not change |

Rules this context owns:
- A failed message is recorded as Failed and never changes the reservation (card 8, rule R6).

Needs from other contexts: nothing it asks for. It reacts to the ReservationConfirmed event, which carries the five fields it needs.
Tells other contexts: nothing.

## Same word, different meaning

| Word | Rooms | Reservations | Payments | Notifications |
|---|---|---|---|---|
| Room | R8 itself: Deluxe, its nightly price, on sale or out of service | Only the room number, R8, on a reservation | Not used | Text in the message: "Room R8" |
| Status | In service / out of service | Confirmed / Cancelled | Paid / Declined | Sent / Failed |
| Cancel | Take a room off sale | Withdraw a reservation; its nights become free | Refund a deposit | Not used |

If Notifications could set a reservation's status to Failed, one failed SMS would turn Maya's Confirmed R101 into Failed: R7 would look free for 12–14 November and could be sold to someone else, so Maya loses her room because of a message, which is exactly what rule R6 forbids. That is why each context keeps its own status.

## Rule owners

The owner of a rule is the context that keeps all the data the rule reads.

| Card | Rule | Owner | Why this owner |
|---|---|---|---|
| 1 | A room never has two Confirmed reservations that share a night. | Reservations | It keeps every reservation of the room, so it can check them all together. |
| 2 | Check-out must be after check-in. | Reservations | The stay is its word, and it refuses wrong dates before checking anything else. |
| 3 | Cancelling a reservation frees its nights for new bookings. | Reservations | It keeps the status, and only Confirmed reservations block a room. |
| 4 | A room that is out of service is not offered to guests. | Rooms | It knows which rooms are on sale and which are not. |
| 5 | The nightly price depends on the room type. | Rooms | It keeps each room's type and price. |
| 6 | The deposit is 30% of the stay's price. | Payments | Money rules belong to Payments. It applies the 30% to the stay's price, which arrives with the deposit request. |
| 7 | Only a Paid deposit can be refunded. | Payments | It keeps the payment status. |
| 8 | A failed message is recorded as Failed and never changes the reservation. | Notifications | It keeps the message status; the reservation's status belongs to Reservations (rule R6). |

My decision on card 6: the price reaches Payments inside Reservations' "take a deposit" request (Reservations gets the nightly price from Rooms), instead of Payments asking Rooms itself. That keeps the context map at three arrows, all starting at Reservations, and Payments never depends on Rooms. Trade-off: Reservations must know the stay's price before it asks for the deposit.

## Between contexts

### Reservations → Rooms: which rooms exist? (question)

Sent when: a guest searches free rooms or books a room.

What Reservations sends: only the question. Later it can send the stay as well, so that Rooms leaves out rooms that are out of service on those nights.

The answer and its fields:
- for each room on sale: number (R7) and type (Standard);
- from the pricing practice: also the nightly price.

Reservations waits for this answer before it searches or books.

Never crosses: reservations. Rooms never learns who booked which room. Today the list of rooms is handed to `BookingService` in its constructor (`IReadOnlyList<Room> rooms`); in P06 it comes from Rooms' public contract.

### Reservations → Payments: take a deposit (question, from P17)

Sent when: a booking needs a deposit, after the room is held (from P17).

What Reservations sends:
- reservationId: R101
- stayPrice: 240 (Payments takes 30% of it: 72)

The answer and its fields:
- reservationId: R101
- result: Paid or Declined

Reservations waits for the answer and decides what Declined means for the booking.

Never crosses: card details never reach Reservations, and Payments never changes a reservation's status itself.

### Reservations → Notifications: ReservationConfirmed (event)

Sent when: a reservation is saved as Confirmed.

Fields:
- reservationId: R101
- guestEmail: maya@example.com
- roomNumber: R7
- checkIn: 2026-11-12
- checkOut: 2026-11-14

Reservations does not wait for an answer. A failed message never changes R101 (rule R6).

Never crosses: Reservations' list of reservations. Notifications never reads it. Today the P04 code still passes the whole `Reservation` object to `SendConfirmation`; P06 replaces it with Notifications' own five-field Confirmation message.
