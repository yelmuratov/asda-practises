# ADR-002: Cache the free-room search in Redis

Status: Accepted, 27 November 2026

## Context

Every free-room search asks PostgreSQL one question per room. Measured in P08: 1,000 searches for one stay cost 2,000 database queries with our two rooms, and `capacity.md` estimates 133 queries a second at the peak with 40 rooms. A search answer only changes when someone books or cancels, so almost all of that work repeats the same answer. Bookings are rare (about one for every 1,440 searches). The booking rule (S2, no double booking) must never depend on a copy that may be out of date.

## Decision

Cache-aside in Redis for `GET /rooms/availability` only:
- key `availability:v{version}:{checkIn}:{checkOut}`, value the free room numbers (`"R7,R8"`), TTL 30 seconds;
- on a miss the search asks PostgreSQL and keeps a copy; on a hit PostgreSQL is not asked;
- a successful booking (201) or cancel (204) raises `availability:version` after the change is saved, which makes every older copy unreachable;
- `POST /reservations` and `DELETE /reservations/{id}` never read the cache. PostgreSQL is the source of truth.

## Alternatives considered

- No cache: always fresh and the simplest, but every search costs one query per room, and the database is the first thing to run out at the peak.
- TTL only: simple, but after every booking the search can show a booked room for up to 30 seconds. I saw this myself in Step 5: R7 was still listed right after Maya booked it.
- Invalidation only, no TTL: fresh after every change, but one missed invalidation (for example Redis briefly unreachable during a booking) would keep a wrong copy until Redis restarts.
- Deleting exactly the keys a booking affects: fresh and keeps the other copies, but every stay that overlaps the booked nights would have to be found and deleted. One version number is much simpler.

## Consequences

- Good: 1,000 searches for the same stay cost 2 database queries instead of 2,000 (`cache-experiment.md`); in this run p95 went from 7.4 ms to 1.2 ms.
- Good: if an invalidation is ever missed, a copy is wrong for at most 30 seconds, and it can only mislead a search, never a booking (Step 5: Omar still got 409).
- Bad: every booking or cancel empties every cached search. In a busy sale, when the hit rate matters most, it falls (Step 7: each New Year booking throws the hot key away).
- Bad: Redis is one more program to run. While it is down, searches answer 500 after 5 seconds, and a booking or cancel is saved but answers 500 because the version cannot be raised (checked in P08). There is no fallback to PostgreSQL yet.
- Bad: two misses at the same moment both ask PostgreSQL (no protection against a stampede), and a hot key puts most of the load on one Redis key.

## Revisit when

Bookings, not searches, limit the app (P09), or the hit rate measured under real load (P23) is much lower than in `cache-experiment.md`, or the hotel needs searches to keep working while Redis is down (then add a fallback to PostgreSQL).
