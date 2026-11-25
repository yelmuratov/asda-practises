# Evidence index

Labels: proposed = a planned check, not run; simulated = a simplified model on paper; observed = something actually done and seen.

No executable booking application or tested concurrency lab is included in this baseline.

## 1. Sold out (proposed)

- Requirement: capacity rule and our sold-out example (Requirements.md).
- Actions: start with Saturday at 20 confirmed bookings out of 20; Sara taps Book; look at the screen and the saved count.
- Expected outcome: "Sold out" is shown; no new booking is saved; the count stays 20.
- Actual outcome: not run; there is no app yet.
- Environment: not applicable yet.
- Limitation: this is a plan. It cannot prove that an app works.

## 2. Repeat booking (proposed)

- Requirement: duplicate rule (Requirements.md).
- Actions: start with T18 saved for Ali and 18 confirmed bookings; Ali taps Book again; look at the screen and the saved count.
- Expected outcome: "You already have booking T18" is shown; no booking is added; the count stays 18.
- Actual outcome: not run; there is no app yet.
- Environment: not applicable yet.
- Limitation: the check depends on how we know it is the same customer, which is still open (Scope question 1).

## 3. Last ticket without protection (simulated)

- Requirement: last-ticket rule (Requirements.md): exactly one booking is confirmed and the final count is 20.
- Actions: a paper model of two requests that each read the count, decide, then save.

| Step | Ali's request | Sara's request | Saved count |
|---|---|---|---|
| 1 | reads the count: 19 | | 19 |
| 2 | | reads the count: 19 | 19 |
| 3 | 19 < 20, so saves a booking | | 20 |
| 4 | | 19 < 20, so saves a booking | 21 |

- Expected outcome (from the rule): one Confirmed, one Sold out, count 20.
- Simulated outcome: both are Confirmed and the count is 21, which breaks the capacity rule.
- Environment: paper model only.
- Limitation: a simplified model, not an observation from a live database. It shows why the rule needs protection; we have not chosen or tested the protection yet.

## 4. Tool-access record corrected (observed)

- Requirement: the Tool-access record must describe what is really installed.
- Actions: I reviewed the first Tool-access draft and asked for the .NET check to be redone.
- Changed sentence. Before: "dotnet is installed, but the only SDK is 6.0.400." After: "SDKs 10.0.401 and 8.0.425 are installed in ~/.dotnet, and an old 6.0.400 in /usr/share/dotnet."
- Reason: the first check only asked which `dotnet` runs first on PATH, and it missed the user install in ~/.dotnet.
- Environment: my laptop, Kali GNU/Linux Rolling 2026.3, zsh, 2026-11-25.
- Limitation: a review of a document by me, not by a second person, and not a booking-system test.

## 5. Peer review (not done yet)

- Next action: ask a classmate or the teacher to review one part (for example the sold-out rule), then record here who reviewed it, the changed sentence and the reason.
