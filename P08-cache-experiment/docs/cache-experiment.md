# Cache experiment: the free-room search

PostgreSQL is the source of truth. Redis keeps copies of search answers for 30 seconds.

## Measurements

1,000 searches for 12–14 Nov, one after another over one connection, with `dotnet run tools/measure.cs`. The app runs on this laptop with `dotnet run` (Debug build), PostgreSQL and Redis in Docker. Times from this laptop only; another laptop gives other times, but the same counts.

| Run | Searches | Database queries | Cache hits | Average | p95 |
|---|---|---|---|---|---|
| No cache | 1,000 | 2,000 | 0 | 2.7 ms | 7.4 ms |
| Cache-aside, TTL 30 s | 1,000 | 2 | 999 | 0.5 ms | 1.2 ms |

- Both runs meet S1 (p95 of 1 second or less) by far, but only with two rooms and one user sending one search at a time. The times say little about 200 guests at once (that is P23's load test).
- The counts are the real result: the cache removed 1,998 of the 2,000 database queries. Only the first search missed (2 queries, one per room); the other 999 came within 30 seconds and used the copy. With 40 rooms it would remove 39,960 of 40,000.
- Both runs started right after the app did, with one or two searches by hand first, so the first-request warm-up is in both.

## Stale result (Step 5, before invalidation)

Started from an empty database and an empty cache (`docker compose down -v`, `up -d`); the four requests were sent within the same second:

1. Search 12–14 Nov: R7 and R8, 200. A miss: the copy `"R7,R8"` is kept for 30 s under `availability:v0:2026-11-12:2026-11-14`.
2. Maya books R7 for 12–14 Nov: 201 R101. Saved in PostgreSQL; nothing touches the copy.
3. Search 12–14 Nov again: R7 and R8, 200. A hit, and STALE: R7 is Maya's.
4. Omar asks for R7 for 13–15 Nov: 409 Unavailable. `BookingService` asks PostgreSQL, not the cache, so it finds Maya's R101 and refuses: nobody is double-booked.

GET /stats afterwards: 2 searches, 1 hit, 1 miss and 6 database queries (2 for the miss, 3 for Maya's booking: overlap, count, save; 1 for Omar's overlap check).

The copy can mislead a guest who is looking (for up to 30 s, R7 looks free), but it never decides a booking.

## After invalidation (Step 6)

A successful booking (201) or cancel (204) now runs `INCR availability:version` after the unit of work has saved it in PostgreSQL. The same four requests from an empty database and cache:

1. Search 12–14 Nov: R7 and R8, 200 (a miss, the copy is kept under version 0).
2. Maya books R7: 201 R101, and the version becomes 1.
3. Search again: R8 only, 200. The key is now `availability:v1:2026-11-12:2026-11-14`, which has no copy yet, so the search asked PostgreSQL: fresh.
4. Omar asks for R7: 409 Unavailable. A 409 changed nothing, so the version stays 1.

`/stats`: 2 searches, 0 hits, 2 misses, 8 database queries. Redis afterwards held the v0 and the v1 copy and `availability:version` = 1; the v0 copy is never read again and Redis deletes it after its 30 seconds.

The cache now has both protections: invalidation makes the search right at once after a change, and the TTL limits a copy whose invalidation was ever missed to 30 seconds.

## Hot key and the next bottleneck (Step 7)

1,000 searches for New Year (31 Dec–2 Jan): 2 database queries, 999 hits, 1 miss (average 0.4 ms, p95 0.5 ms). All of them used one key, `availability:v1:2026-12-31:2027-01-02`: a hot key. In a real December most guests search the same stay, so one Redis key (on one Redis server, even if Redis ever runs on several) does most of the work.

Then Nora booked R8 for New Year (`docs/requests/nora-r8-new-year.json`): 201 R102, version 2. One more search answered R7 only and made a new copy, `availability:v2:2026-12-31:2027-01-02`, next to the old v1 copy (TTL 29 s each); 28 seconds later only the v2 copy was left. So in a busy New Year sale every booking throws the hot copy away, and the next search goes to PostgreSQL again: the hit rate falls exactly when the hotel is busiest.

The bottleneck has moved. Before the cache, searches sent 2,000 questions to PostgreSQL per 1,000 searches; now they send 2. What is left for PostgreSQL is mostly `POST /reservations`, 3 questions per booking (overlap, count, save), and those must be exact. Two bookings for the same room at the same moment can still both pass the overlap check (the P03 race, still in `--race`): that is P09.

## When Redis stops

With the app running, `docker compose stop cache`:
- A search answered 500 after 5.1 s (StackExchange.Redis waits 5 s for a connection, then throws `RedisConnectionException`).
- Omar booked R8 for 20–22 Nov: 500 after 5.2 s, but PostgreSQL had saved R103: the booking worked, only raising the version failed. When he tried again he got 409 Unavailable, because R8 was already his.
- `docker compose start cache`: the same app reconnected by itself; the next searches answered 200 without a restart.

So while Redis is down, the search does not work and bookings work but report an error. There is no fallback to PostgreSQL yet (ADR-002).
