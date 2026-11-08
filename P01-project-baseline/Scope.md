# Ticket-app project scope

Fictional brief: a concert organiser needs online reservations instead of booking through messages.

Classroom assumptions: 20 tickets per concert; no numbered seats; one ticket per customer per concert; no payment in version one.

## Problem description

Currently the organiser takes bookings through SMS, so they get many messages and the process is chaotic. In this chaos they might accept more than 20 bookings or book the same person twice, or tickets might stay unsold because a message was missed. It is also difficult for customers to check their own booking.

Our app fixes the chaotic booking process with one organised list of bookings: customers get a clear answer straight away (Confirmed or Sold out), and the organiser never accepts more than 20.

## First three actions

1. See available tickets
2. Book a ticket
3. Check my booking status

## Later decisions (not in version one)

1. Payment: version one only reserves a ticket. Payment would add a payment provider, failed payments and refunds.
2. Cancellation: the rules are not decided yet, for example when a cancelled ticket goes back on sale. The organiser has to decide this.
3. Waiting list: it only matters once cancellations exist, and the order of the list is not decided.

## People and needs

1. Customer: needs to see availability, book a ticket and get a clear answer right away (Confirmed or Sold out), and later check their ticket status to be sure the booking exists.
2. Organiser: with SMS booking they could not identify who has a ticket. With our app they need an accurate list of confirmed customers for each concert, never more than 20, so they can check people at the entrance.
3. Support: needs to look up a customer's booking and see its status, so they can help a customer who is not sure the booking worked.

## Unanswered questions and who can answer

1. How do we know it is the same customer: by phone number, email or an account? (Organiser and teacher)
2. If cancellations are added later, when does the ticket go back on sale? (Organiser)
3. What is support allowed to see about a customer (name, phone, email)? (Organiser and teacher)
