# LiveSplit.AverageTime

A LiveSplit Component that queries speedrun.com and shows the Average Time

Based on [LiveSplit/LiveSplit.WorldRecord@bb6c710](https://github.com/LiveSplit/LiveSplit.WorldRecord/commit/bb6c710c3e32e79c3f06c593dd211e82e6727483)

I mainly did this for personnal use, with minimal changes, so some references (like versionning) to LiveSplit.WorldRecord are still left in. If someone really wants it, I can clean up and make this its own proper component.

Excludes the last 5% and rounds down to the nearest second when miliseconds are not included.

Run `git submodule update --init --recursive` to download LiveSplit and its submodules as a submodule so it can be built