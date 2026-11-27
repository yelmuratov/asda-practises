# Quality scenarios

A quality scenario turns a wish ("fast", "reliable") into a situation that can be tested, with a number. Every scenario below has the same seven lines. S3 and S5 are already proven by the P04 tests; the others are targets that later practices measure.

## Stakeholders

| Who | What they care about |
|---|---|
| Guest | Finds a free room quickly; never loses a booking |
| Receptionist | Never promises one room twice; can cancel at the desk |
| Hotel manager (domain expert) | Knows the rules; wants deposits and SMS soon |
| Development team | Can change one area without breaking the others |
| Email/SMS and payment providers | Outside systems that can be slow or down |

## S1 · Performance: free-room search under load

| Part | S1 |
|---|---|
| Source | 200 guests |
| Stimulus | They search free rooms for 12–14 November |
| Environment | All 200 searches within one minute, during normal operation |
| Artifact | The free-room search (`BookingService.FindFreeRooms`) |
| Response | Every guest gets the correct list of free rooms |
| Measure | p95 response ≤ 1 second (190 of the 200 searches answer within 1 second); 0 wrong lists |
| Checked in | P08 (cache) and P23 (load test) |

p95 instead of the average: 199 fast searches and one 30-second search still average under 1 second, so the average hides the slow guest.

## S2 · Data integrity: no double booking

| Part | S2 |
|---|---|
| Source | 100 pairs of guests |
| Stimulus | Both guests in each pair book the same room for the same nights |
| Environment | The two requests of a pair arrive at the same moment |
| Artifact | `BookingService.Reserve` and the reservation store |
| Response | One guest of each pair is Confirmed; the other gets Unavailable |
| Measure | Exactly 1 Confirmed reservation per pair (100 in total); 0 double bookings |
| Checked in | P09. Not met today: the P03/P04 race demo gave 2 Confirmed reservations for one pair |

## S3 · Availability: a failed message keeps the booking

| Part | S3 |
|---|---|
| Source | The email provider |
| Stimulus | It stops answering |
| Environment | For 10 minutes, during normal bookings |
| Artifact | `BookingService.Reserve` and the notification sender |
| Response | Every valid booking is still Accepted and saved; the failed message is reported |
| Measure | 0 lost bookings; every failed message is counted |
| Checked in | P04 (R6 test `A_failed_confirmation_keeps_the_booking`, done) and P10 |

## S4 · Security: a guest sees only their own reservations

| Part | S4 |
|---|---|
| Source | A guest who is signed in as themselves |
| Stimulus | They try 100 times to read another guest's reservation, for example by guessing ids R101, R102 … |
| Environment | Normal operation, with the app behind its HTTP API |
| Artifact | The "view a booking" request (R5) |
| Response | Every attempt is refused; only the guest's own reservations are shown |
| Measure | 0 of the 100 attempts show a reservation to the wrong guest |
| Checked in | P21. Not testable today: version 1 has no logins, so anyone who knows an id can look it up |

## S5 · Modifiability: SMS confirmations

| Part | S5 |
|---|---|
| Source | The hotel manager |
| Stimulus | Asks for SMS confirmations as well as email |
| Environment | During development |
| Artifact | The notification part of the code |
| Response | SMS is added as a new sender class; `BookingService` is not edited |
| Measure | `BookingService.cs` changed 0 times; at most 2 files change |
| Checked in | P04, done: the SMS step added `SmsNotificationSender.cs` and one test in `BookingServiceTests.cs` (2 files), and the hash of `BookingService.cs` did not change |

## S6 · Sustainability: energy per search

| Part | S6 |
|---|---|
| Source | The development team, measuring a change (for example the P08 cache) |
| Stimulus | The same 1,000 searches are run before and after the change |
| Environment | Same server, same data, same load |
| Artifact | The free-room search and the server it runs on |
| Response | The same correct answers, using less energy |
| Measure | At least 20% fewer watt-hours per 1,000 completed searches |
| Checked in | P08 and P24 |

Energy is measured per completed search, not per hour, so a change that answers fewer searches cannot look "greener" just because it did less work.
