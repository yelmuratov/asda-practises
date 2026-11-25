# System context

Our own context sketch for version one: the people from Scope.md and the messages between them and the ticket app.

```
      Customer                    Organiser
       │    ▲                      │    ▲
   (1) │    │ (2)              (3) │    │ (4)
       ▼    │                      ▼    │
  ┌──────────────────────────────────────────┐
  │                Ticket app                │
  └──────────────────────────────────────────┘
                    ▲    │
                (5) │    │ (6)
                    │    ▼
                   Support
```

## Labelled messages

1. Customer -> Ticket app: see available tickets / book a ticket / check my booking
2. Ticket app -> Customer: "3 tickets available" / "Confirmed, booking T18" / "Sold out" / "You already have booking T18" / "You have no booking for this concert"
3. Organiser -> Ticket app: request the confirmed list for the Saturday concert
4. Ticket app -> Organiser: confirmed customers for Saturday, never more than 20, to check people at the entrance
5. Support -> Ticket app: look up a customer's booking, for example T18
6. Ticket app -> Support: the saved booking and its status, showing only the details the access policy allows (Scope question 3, still open)

## Not in this sketch

Payment provider, confirmation email or SMS, and cancellations are outside version one (see Scope.md, Later decisions).

These arrows describe communication only. They do not choose a programming framework, prove safe access or show a working application.
