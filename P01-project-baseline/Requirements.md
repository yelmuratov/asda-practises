# Ticket requirements and rules

Based on Scope.md. Classroom assumptions: 20 tickets per concert; no numbered seats; one ticket per customer per concert; no payment in version one. These are proposed requirements, not tested results.

Example data: the Saturday concert has 20 tickets. Ali and Sara are fictional customers; T18 is an invented booking number.

## Our three actions

1. See available tickets
   - Start: Saturday has 17 confirmed bookings out of 20.
   - Customer action: Sara opens the Saturday concert.
   - Visible result: "3 tickets available".
   - Saved data: no change; still 17. The number only shows the current state; it does not hold a ticket for Sara.
2. Book a ticket
   - Start: Saturday has 17 confirmed bookings and Ali has no Saturday booking.
   - Customer action: Ali taps Book.
   - Visible result: "Confirmed, booking T18".
   - Saved data: one Ali/Saturday booking is added; the total becomes 18.
3. Check my booking status
   - Start: booking T18 exists for Ali.
   - Customer action: Ali opens "My booking" for Saturday.
   - Visible result: "Confirmed, booking T18, Saturday concert". A customer with no booking sees "You have no booking for this concert".
   - Saved data: no change; the total stays 18.

## Rules

Capacity rule: confirmed bookings must never exceed capacity for that concert.

Duplicate rule: a customer has at most one confirmed booking per concert. Return the existing booking for a repeat.
- Example: Ali already has T18 and taps Book again. Show "You already have booking T18"; add no booking; the total stays 18.
- How we know it is the same customer is still open (Scope question 1).

Our sold-out example:
- Start: Saturday has 20 confirmed bookings out of 20.
- Action: Sara taps Book.
- Message: "Sold out".
- Saved count: no new booking is saved; the count stays 20.

Last ticket: Saturday has 19 out of 20, and Ali and Sara tap Book at the same time. Exactly one of them gets Confirmed, the other gets Sold out, and the final count is 20. We have only described the result; we have not built or tested a way to protect it.

Lost reply: T18 was saved, but Ali's phone lost the connection before the reply arrived. Checking status shows T18. Tapping Book again returns T18 (duplicate rule) instead of making a second booking. A missing reply does not mean there is no booking.

## Three quality concerns

1. Clear result on a phone. Condition: a customer books on a small phone screen. Outcome: the result (Confirmed with the booking number, or Sold out) is shown in plain words without scrolling. Proposed check: later open the booking page on a phone-sized screen, tap Book and record whether the result is visible straight away.
2. Speed when booking opens. Condition: an agreed number of customers tap Book at the same time. Outcome: each customer gets a result within an agreed time, and confirmed bookings never go over 20. Proposed check: later send that many booking requests at once in a test setup, measure the time to each result and check the saved count.
3. Privacy. Condition: someone other than the customer, for example another customer or support, looks up a booking. Outcome: a customer can see only their own booking; support sees only the details allowed by an agreed access policy. Proposed check: later try to open another customer's booking, then log in as support, and record which details are shown. The access policy is still open (Scope question 3).

## Unknown numeric targets

To agree; not measured today:
1. How many customers may tap Book at the same time (quality concern 2). (Teacher and organiser)
2. The maximum time before a booking result is shown (quality concern 2). (Teacher and organiser)
