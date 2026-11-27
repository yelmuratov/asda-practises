# Capacity estimate: hotel booking app

These numbers are estimates worked out from assumptions, not measurements. P08 measures the real app.

The estimate is for the whole hotel of 40 rooms. The demo database has only R7 and R8, so today a search asks 2 questions, not 40.

## Assumptions

| Assumption | Value | Why I chose it |
|---|---|---|
| Visitors per day | 1,440 | About one visitor a minute on the booking page today |
| Searches per visitor | 20 | A guest tries several dates before booking |
| Bookings per day | 20 | 40 rooms, and most stays are two nights |
| Peak factor | 10 | Friday evenings and holiday sales are about ten times busier than average |
| Queries per search | 40 | `FindFreeRooms` asks the store one overlap question per room |

Also used: one reservation row with its index is about 600 bytes, one search answer is about 2 KB of JSON, and the booking page shows ten room photos of 200 KB (2 MB).

## Results

| Number | Calculation | Result |
|---|---|---|
| Searches, average | 1,440 × 20 = 28,800 a day; 28,800 ÷ 86,400 s | 0.33 per second |
| Searches, peak | 0.33 × 10 | 3.33 per second = 200 a minute (scenario S1) |
| Database queries, peak | 3.33 × 40 rooms | 133 per second |
| Storage | 20 bookings × 365 days × 600 bytes | 4.38 MB a year |
| Bandwidth, peak | search answers 3.33 × 2 KB; photos 0.17 visitors/s × 2 MB | search 6.67 KB/s, photos 333 KB/s |

What the numbers say:
- The peak of 200 searches a minute is exactly the load in S1 (200 guests within one minute), so the estimate and the quality scenario agree.
- Bookings are rare: 20 a day is one booking for every 1,440 searches, about 0.002 a second even at the peak. Almost all the load is searching.
- Storage is tiny (4.38 MB a year), so one PostgreSQL database is plenty for years.
- The photos are 50 times more traffic than the search answers; they belong on static file hosting or a CDN, not in this API.
- The pressure point is the database: 133 queries a second at the peak, almost all from searches whose answers hardly change. That is why P08 caches the search and not the bookings.

## To measure later

- p95 response time of 1,000 searches for 12–14 November; target 1 second or less (S1): P08.
- Database queries per search (`GET /stats`): P08.
