# Business glossary

These terms have the same meanings in Scope.md and Requirements.md.

1. Capacity: the maximum number of confirmed bookings allowed for one concert. In our classroom example it is 20 for each concert. Available tickets = capacity minus confirmed bookings (20 - 17 = 3 in our example).
2. Concert: one event on a stated date and time, for example the Saturday concert. Each concert has its own capacity and its own bookings; a booking for Saturday gives no entry to another concert.
3. Confirmed booking: a saved record giving one customer entry to one concert, with a booking number such as T18. It exists once it is saved, even if the customer did not receive the reply (see the lost reply rule). A customer has at most one confirmed booking per concert. It does not mean the ticket is paid; there is no payment in version one.
4. Sold out: confirmed bookings for a concert have reached its capacity (20 out of 20). No new booking may be saved, and the customer sees "Sold out".
5. Cancellation: a possible future action that removes a confirmed booking. It is not in version one. Unresolved until agreed: when a cancelled ticket goes back on sale (Scope question 2) and who is allowed to cancel. The organiser decides this.
