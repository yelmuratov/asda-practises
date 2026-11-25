# P02 individual worksheet — process evidence and runtime ADR

**Name:** Salimbay  **Date:** 2026-11-25  **Operating system:** Linux (Kali GNU/Linux Rolling 2026.3, kernel 7.2.15)

**Pair partner:** none (working alone)  **First role:** Operator and Observer (working alone)  **Role swap completed:** not applicable (working alone)

Use observations from your own computer where possible. If a command/tool is unavailable, write “unavailable” and why. Do not copy the illustrative sample as your evidence.

## 1. Before running

1. In one sentence, what is the kernel responsible for in this exercise?

   The kernel creates the child process when the parent asks and gives it a PID, puts the child to sleep while it waits for terminal input, writes the output file for it, and gives the child's exit code back to the waiting parent.

2. Which program is the parent and which is the child? What is each one’s job?

   Parent: `starter/parent.py`. It asks the OS to start the child (`subprocess.Popen`), waits for it (`wait()`) and reports the child's exit code. Child: `starter/child.py`. It prints its PID and PPID, does 0.75 seconds of CPU work, waits for Enter (`input()`), writes `output/child-result.txt` and exits with code 7.

3. Prediction before the first run: What will `wait()` make the parent do?

   The parent will stop and do nothing until the child exits; then `wait()` returns the child's exit code (7) and the parent reports it and finishes normally.

### My execution-boundary diagram

Draw or write: program/runtime in user mode -> OS interface -> kernel/filesystem -> return. Add CPU, working RAM and saved file. Which calculation stays in user mode? What survives process exit?

```text
USER MODE (child.py running in Python)          KERNEL MODE
--------------------------------------          -----------
CPU: bounded loop, 8796778 iterations
     (pure calculation, no kernel needed)
RAM: child_pid, parent_pid, iterations,
     exit code (values in process memory)
        |
        | write_text(...): Python asks the OS
        v
  OS interface (system calls: open, write) ---> kernel filesystem services
                                                    |  data may wait in a buffer first
  <---------------- write returns ----------------  v
                                                 DISK: output/child-result.txt
```

The bounded CPU loop stays in user mode. After the process exits, its RAM (the variables) is gone, but the saved file `output/child-result.txt` survives and can be reopened. Saving a file hands the data to the kernel and the disk; process memory belongs only to the running process. A successful write can be buffered, so it does not prove the data would survive a power loss.

## 2. Evidence while the child waited for input

| Field | Your observation or “unavailable” |
| --- | --- |
| OS and Python versions, observation time | Kali GNU/Linux Rolling 2026.3 (kernel 7.2.15), Python 3.14.10; 2026-11-25 20:27:45 (+05) |
| Observed / supplied / simulated | Observed (my own run) |
| Command/tool used | Terminal A: `python starter/parent.py`. Terminal B: `ps -o pid,ppid,stat,%cpu,vsz,rss,nlwp,comm -p 196865` and `ls /proc/196865/fd \| wc -l` |
| Child PID | 196865 |
| Parent PID or PPID | 196864 |
| Process name | python |
| CPU value shown (with unit if shown) | %CPU 0.7 (ps: average CPU share since the process started) |
| Virtual memory value, if your tool exposes it | VSZ 21728 KiB |
| Working set/RSS (resident memory), if exposed | RSS 14832 KiB |
| Thread count, if exposed | NLWP 1 |
| Handles/open resources, if exposed | 3 open file descriptors in /proc/196865/fd (count only) |
| State/snapshot label, if exposed | STAT S+ (S = sleeping, interruptible; + = in the terminal's foreground group) |
| Why the child could not continue | It was inside `input()` waiting for a line from the terminal; no line arrived until I pressed Enter. |

Do not equate one display label with a complete lifecycle state. Write one sentence that distinguishes **what you observed** from **what you infer**:

I observed that `ps` showed STAT `S+` for PID 196865 while the "press Enter" prompt was on screen; I infer that it was blocked waiting for terminal input, because the code calls `input()` at that point, but `ps` itself does not say which event the process is waiting for.

## 3. Lifecycle for this run

Draw arrows or complete this line with evidence labels:

```text
new --Popen--> running (CPU phase, 0.75 s) --input()--> waiting for terminal input (STAT S+) --Enter--> running (writes file) --return EXIT_CODE 7--> exited --wait()--> reaped by parent
```

- Creation evidence (`Popen` or `fork`): after `subprocess.Popen`, the parent printed "child PID is 196865"; the child printed PID=196865 PPID=196864.
- Event that made the child wait: `input()` needed a line from the terminal.
- Event that let the child continue: I pressed Enter in Terminal A (the output file was written at 20:28:12).
- Exit result reported by parent: "PARENT: wait() returned; child exit code was 7." Code 7 is non-zero on purpose in this lab, so the parent reported it and then finished normally itself.
- What “reaped by parent” means here: the parent's `wait()` collected the child's exit code, so the kernel could remove the finished child completely. At 20:29:18, `ps -p 196865,196864` showed neither process.

### Windows and POSIX comparison

Name what fork creates, what exec replaces, and what wait observes. Explain how Windows CreateProcess differs. Label a supplied POSIX trace if you did not run it.

Observed on Linux: I ran `python starter/fork_exec_wait.py` on 2026-11-25 (output file written at 20:34:18).

- **fork** created a copy of the parent process: parent PID 221064 printed "created PID=221108".
- **exec** (`execv`) replaced the program inside that copy with Python running `child.py`. The PID did not change: `child.py` printed `PID=221108 PPID=221064`, so exec replaces the program, not the process.
- **wait** (`waitpid`) let the parent observe the child's end: "waited for PID=221108; exit code=7".

Windows has no `fork()`. `CreateProcess` creates a new process and loads the new program in one step, so there is no copy of the parent and no separate exec. I did not run anything on Windows; this part comes from the course materials. The portable `parent.py` uses `subprocess.Popen`, which chooses the platform's own mechanism (on Linux it may use `posix_spawn` instead of fork + exec), so my first run does not prove which call was used.

## 4. Saved synthetic output

- Path you viewed: `output/child-result.txt` (inside the P02 lab folder)
- List two labels/values in the file: `child_pid=196865`, `requested_exit_code=7` (also `cpu_iterations=8796778`)
- Why is this safe for the classroom lab? The file holds only synthetic values (process IDs, platform name, loop count, exit code) and no personal data; it is written only inside the lab folder and needs no administrator rights.

## 5. Controlled change

- I changed: `EXIT_CODE` / `CPU_SECONDS`
- New value: ____________________
- My prediction: ______________________________________________________________
- Visible result: _____________________________________________________________
- Did the output file agree with the result? ___________________________________

## 6. Runtime ADR — provisional project runtime

**Title:** Runtime choice for the future lab-seat enrollment project

- **Decision.** Provisional choice: C#/.NET / lecturer-agreed Java:

  ____________________________________________________________________________

- **Context and evidence.** How do mixed developer laptops affect this? What toolchain/support/portability evidence do you have?

  ____________________________________________________________________________

- **Unknowns to check before committing:**

  ____________________________________________________________________________

- **Consequence and trade-off of the provisional choice:**

  ____________________________________________________________________________

- **Supporting mini decision:** Why does this parent wait for its child before reporting the synthetic result?

  ____________________________________________________________________________

## 7. Exit check

Tick each item you can show the teacher:

- [ ] I identified a PID and the parent/child relationship.
- [ ] I explained why `input()` causes the child to wait.
- [ ] I used an observation or honestly marked a tool unavailable.
- [ ] I identified the intentional exit code and synthetic output file.
- [ ] I completed the boundary diagram, lifecycle, Windows/POSIX comparison and ADR.
- [ ] I explained ready versus blocked and why saved output survives exit.
- [ ] I saved this worksheet in the location the teacher specified.
