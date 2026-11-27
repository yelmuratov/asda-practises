# API check: seven requests against PostgreSQL

Checked on 2026-11-27 at 15:03 on Linux, so the command is `curl` (on Windows it is `curl.exe`, with the same options). `-s` hides the progress bar and `-w "\n%{http_code}\n"` prints the status code on its own line after the body.

Start from an empty database, then start the app (in a second terminal):

```text
docker compose down -v
docker compose up -d
dotnet run --project HotelBooking
```

Run each request from the hotel-booking folder. ✓ means the answer was exactly the expected one.

## 1. Search 12–14 Nov: R7 and R8 are free ✓

```text
curl -s -w "\n%{http_code}\n" "http://localhost:5080/rooms/availability?checkIn=2026-11-12&checkOut=2026-11-14"

{"checkIn":"2026-11-12","checkOut":"2026-11-14","rooms":[{"number":"R7","type":"Standard"},{"number":"R8","type":"Deluxe"}],"next":null}
200
```

## 2. Maya books R7 for 12–14 Nov ✓

```text
curl -s -w "\n%{http_code}\n" -X POST -H "Content-Type: application/json" --data "@docs/requests/maya-r7.json" http://localhost:5080/reservations

{"id":"R101","status":"Confirmed","notificationFailed":false}
201
```

## 3. Omar asks for R7 for 13–15 Nov: the night of the 13th is Maya's ✓

```text
curl -s -w "\n%{http_code}\n" -X POST -H "Content-Type: application/json" --data "@docs/requests/omar-r7.json" http://localhost:5080/reservations

{"title":"Unavailable","status":409,"detail":"The room is booked for at least one night of this stay."}
409
```

## 4. Omar asks for R8 for 15–15 Nov: no night at all ✓

```text
curl -s -w "\n%{http_code}\n" -X POST -H "Content-Type: application/json" --data "@docs/requests/omar-r8.json" http://localhost:5080/reservations

{"title":"InvalidDates","status":400,"detail":"checkOut must be after checkIn."}
400
```

## 5. Search 12–14 Nov again: only R8 is free ✓

```text
curl -s -w "\n%{http_code}\n" "http://localhost:5080/rooms/availability?checkIn=2026-11-12&checkOut=2026-11-14"

{"checkIn":"2026-11-12","checkOut":"2026-11-14","rooms":[{"number":"R8","type":"Deluxe"}],"next":null}
200
```

## 6. Cancel R101 ✓

```text
curl -s -w "\n%{http_code}\n" -X DELETE http://localhost:5080/reservations/R101

(empty line: a 204 has no body)
204
```

## 7. Cancel R101 a second time ✓

```text
curl -s -w "\n%{http_code}\n" -X DELETE http://localhost:5080/reservations/R101

{"title":"NotFound","status":404,"detail":"There is no confirmed reservation R101."}
404
```

## Restart check: the booking survives ✓

8. Maya books R7 for 12–14 Nov again (the same command as request 2):

```text
{"id":"R102","status":"Confirmed","notificationFailed":false}
201
```

9. Stop the app (Ctrl+C) and start it again with `dotnet run --project HotelBooking`.

10. Search 12–14 Nov (the same command as request 1): R7 is still booked.

```text
{"checkIn":"2026-11-12","checkOut":"2026-11-14","rooms":[{"number":"R8","type":"Deluxe"}],"next":null}
200
```

The reservations table after the restart check:

```text
docker compose exec db psql -U hotel -d hotel -c "SELECT id, guest_name, room, check_in, check_out, status FROM reservations ORDER BY id;"

  id  | guest_name | room |  check_in  | check_out  |  status
------+------------+------+------------+------------+-----------
 R101 | Maya       | R7   | 2026-11-12 | 2026-11-14 | Cancelled
 R102 | Maya       | R7   | 2026-11-12 | 2026-11-14 | Confirmed
(2 rows)
```

R102 outlives the app because it is a row in PostgreSQL, which keeps running in Docker while the app stops and starts. The app itself keeps nothing between requests (a stateless instance, L04). R101 is kept as Cancelled, not deleted.
