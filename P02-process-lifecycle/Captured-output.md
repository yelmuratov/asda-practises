# Illustrative captured output

This is a teaching sample with invented process IDs. It is **not** a record of a student run and must not be submitted as evidence.

```text
PARENT: starting child with this Python runtime: /usr/bin/python3
PARENT: child PID is 42190; waiting for it to exit.
CHILD: started
CHILD: PID=42190 PPID=42188
CHILD: platform=Linux Python=3.12.3
CHILD: CPU phase for 0.75 seconds...
CHILD: CPU phase complete (5269812 loop iterations).
CHILD: press Enter to continue and write synthetic lab output:
CHILD: wrote synthetic data to output/child-result.txt
CHILD: exiting with code 7.
PARENT: wait() returned; child exit code was 7.
PARENT: non-zero is intentional in this lab; the parent reports it and exits normally.
```

Interpretation: after the child starts it performs a short CPU calculation. When it is waiting for terminal input, it is blocked waiting for input. The parent is waiting in `wait()`. The exact visible order of terminal messages is useful evidence of what appeared on this terminal, but it does **not** prove the kernel’s exact scheduling order.


## Illustrative inspection samples for comparison

All values below are invented classroom examples, not measurements. A real student's counts, states, names and memory values can differ. Keep the displayed column labels and units; do not rank operating systems using these values.

| Platform and tool | PID / PPID | Process and state | CPU display | Resident memory | Virtual size | Threads / resource example |
| --- | --- | --- | --- | --- | --- | --- |
| Windows PowerShell Get-Process + CIM | 42190 / 42188 | python; no universal ready/blocked column | CPUSeconds 0.80 s (accumulated) | WorkingSetBytes 18,874,368 bytes | VirtualMemorySize64 47,185,920 bytes | ThreadCount 1; Handles 90 |
| Linux ps | 42190 / 42188 | python3; STAT S (interruptible sleep snapshot) | %CPU 0.2 (sample/tool-defined average) | RSS 18,432 KiB | VSZ 46,080 KiB | NLWP 1; /proc/PID/fd may list terminal descriptors |
| macOS ps + Activity Monitor | 42190 / 42188 | Python; STATE S (sleep snapshot) | %CPU 0.2 (tool-defined average) | RSS 18,432 KiB | VSZ 46,080 KiB | Threads 1 in GUI; lsof may list terminal resources |

A live input prompt plus the code supports the inference that input is missing. A desktop status column alone does not prove every state transition. CPU seconds are cumulative; they are not the same measure as %CPU. Memory accounting differs across platforms.

## Illustrative explicit POSIX fork/exec/wait trace

```text
FORK-DEMO: parent PID=42188 is about to fork.
FORK-DEMO: parent created PID=42190; waiting for it.
CHILD: started
CHILD: PID=42190 PPID=42188
[CPU phase, then input prompt; the user presses Enter]
CHILD: wrote synthetic data to output/child-result.txt
CHILD: exiting with code 7.
FORK-DEMO: waited for PID=42190; exit code=7
```

The supplied code explicitly calls fork, then execv in the child, then waitpid in the parent. Child PID 42190 after exec matches the created PID: exec replaces the program in that process. Earlier printed lines can interleave differently. On Windows, the portable subprocess route uses Windows creation rather than os.fork; Windows students may reason from this supplied trace without claiming a local POSIX run.
