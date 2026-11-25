# Individual reflection

My contribution: I described the problem: the organiser takes bookings by SMS, and it gets chaotic. I checked every part before it was committed and asked for changes, for example turning the questions table in Scope into a numbered list. I also found that the tool check had missed my .NET 10 SDK. I chose to work alone, to leave payment, cancellation and email out of version one, and to use C# with .NET 10.

One booking rule explained in my own words: the capacity rule. A concert has 20 tickets, so the app must never confirm more than 20 bookings. If 20 are already booked and someone taps Book, the app shows "Sold out", saves nothing, and the count stays 20. This matters because the organiser checks people at the entrance with the confirmed list. If 21 people have a confirmed booking, one person with a valid booking cannot get in.

One uncertainty: I do not know yet how to stop two people taking the last ticket at the same time. On paper, both requests read 19, both save, and the count becomes 21. This matters because the capacity rule would break exactly when the concert is selling out, which is the busiest moment.

One learning goal and next action: I want to learn how two requests can run at the same time (P02), and how a database can stop the 19 → 21 problem. Before starting P02, I will fix the .NET PATH in ~/.zshrc so that `dotnet --version` shows 10.0.401.
