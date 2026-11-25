# P02 practice: watch a process live

## What you will do today

You will work in a pair, then complete your own worksheet. You will:

1. trace a file request across the user/kernel boundary;
2. run a small parent-and-child program;
3. find the child process while it is waiting for input;
4. explain creation, running, waiting/blocked, exit, and POSIX reaping in a lifecycle diagram;
5. make one small, controlled change; and
6. write a short runtime ADR (a recorded design decision).

You will run supplied Python and change one named constant, but you do not need to build an application. By the end, save your completed `Worksheet.md` (or the class submission copy) and the synthetic `output/child-result.txt` created by your own run. Later lessons will use these ideas when programs handle real tasks and failures.

## Before you start: the project you are modelling

> **Fictional classroom scenario:** A university enrollment system lets students reserve lab seats. The registrar needs accurate capacity information. Our parent/child programs are a small OS helper experiment for that future project: the parent starts one helper, waits for it, and reports its exit result. This is not an enrollment application; it does not access student records or reserve a real seat.

An **operating system (OS)** is the software that shares a computer’s hardware among programs. The **kernel** is the privileged core of the OS: it schedules work, manages memory and devices, and checks sensitive requests. An application usually runs in **user mode**, where it must ask the kernel to perform protected work. A **process** is one running program with its own identity and resources. A **thread** is a path of execution inside a process.

In this lab, the parent asks the OS to start a child. The OS gives the child a PID (process ID). The parent calls `wait()`, so it cannot continue until the child exits. When the child asks for terminal input, it cannot continue until input arrives. These are observable examples, not a complete view of the kernel scheduler.

## Materials and roles

- One computer per pair, an internet connection if Python is missing, and this extracted `p02-lab` folder.
- A terminal: PowerShell on Windows; Terminal on Linux/macOS.
- Your individual `Worksheet.md`.

Start as **Operator** (types and shares the screen) and **Observer** (reads each step, records evidence, and challenges interpretations). Swap roles after process inspection and during the changed-code task in Step 8. Each student completes their own worksheet.

Complete Steps 1-2 before class if possible. During class, follow Steps 3-9 as your teacher introduces each task. Read the completed example before collecting your own evidence.

## Step 1 — Put the lab in a safe working folder (3 minutes)

1. Download `p02-lab.zip` from the lesson page. Put it in Downloads.
2. Extract it; do not run it from inside the ZIP preview. Open the inner `p02-lab` folder that contains `starter` and `Worksheet.md`. Windows Extract All may add an outer folder: open the inner folder before starting the terminal.
   - Windows: right-click ZIP → **Extract All** → **Extract**.
   - macOS: double-click the ZIP, then open the created `p02-lab` folder.
   - Linux: use the file manager’s Extract action, or run `unzip p02-lab.zip`.
3. Open a terminal in that folder.
   - Windows: in File Explorer, open the folder, click its address bar, type `powershell`, press Enter. Example: `cd "$HOME\Downloads\p02-lab"`.
   - macOS: open **Terminal**, type `cd ` (include the space), then drag the extracted `p02-lab` folder onto the Terminal window and press Enter. Example: `cd ~/Downloads/p02-lab`.
   - Linux: open Terminal and run `cd ~/Downloads/p02-lab` if that is where you extracted it. Otherwise type `cd `, then drag the folder into the terminal and press Enter.
4. Confirm the starter files exist:

```text
Windows PowerShell:  Get-ChildItem starter
Linux/macOS:         ls starter
```

You should see `child.py`, `parent.py`, and `fork_exec_wait.py`.

## Step 2 — Check Python before installing anything (2 minutes)

Run the command for your system:

```text
Windows:       py -3 --version
Linux/macOS:   python3 --version
```

Python 3.10 or newer is suitable. If a version appears, continue to Step 3. If Windows cannot find `py`, try `python --version`; when that shows Python 3.10+, use `python` instead of `py -3` in the lab commands. There are no packages to install: this lab uses Python’s standard library only.

### If Python is missing (pre-class setup; do not spend the 80-minute lab installing)

- **Windows:** open [python.org/downloads](https://www.python.org/downloads/), select the current Python 3 download, open the downloaded installer, tick **Add python.exe to PATH** if shown, then choose **Install Now**. Finish, close PowerShell, open a new PowerShell window, and run `py -3 --version`.
- **macOS:** open [python.org/downloads/macos](https://www.python.org/downloads/macos/), download the current universal installer, open the `.pkg`, select **Continue** through the installer, and enter a password only if your institution permits it. Close Terminal, open it again, and run `python3 --version`.
- **Linux:** open the institution-approved Software Center/package manager, search for **Python 3** or `python3`, select the distribution-provided package, install it, reopen Terminal, and run `python3 --version`. Do not use an unfamiliar administrator command in class.

**No-install fallback:** If installation is not allowed or time runs out, use `Captured-output.md`, the teacher’s supplied live trace, and the code-reading boxes below. Mark “teacher trace/no local runtime” on your worksheet. You can still complete the lifecycle and ADR tasks.

## Step 3 — Read the two programs before running (during 10-25)

Open `starter/child.py` and `starter/parent.py` in any text editor. Do not change them yet. Match these operations to the code:

| Code operation | What it means |
| --- | --- |
| `subprocess.Popen(...)` | Parent asks the OS to create and start a child program. |
| `sys.executable` | Uses the same Python runtime that started the parent; no hard-coded Python path. |
| `child.pid` | Reads the PID assigned to the child. |
| `child.wait()` | Parent waits until the child has ended and gets its exit status. |
| `os.getpid()` / `os.getppid()` | Child reports its own PID and its parent’s PID. |
| bounded loop | Short CPU work; it ends after a fixed time. |
| `input(...)` | Child waits for terminal input; it is blocked until input arrives. |
| `write_text(...)` | Writes only synthetic values to `output/child-result.txt` inside this lab. |
| `return EXIT_CODE` (initially 7) | An intentional non-zero exit result for the parent to report. |

The comments and names are part of the explanation. Ask your partner to point at the line for each operation.

## Step 4 — Read the completed example, then trace the boundary (10-35)

**Starting situation:** `parent.py` and `child.py` are unchanged; no `output/child-result.txt` exists yet.

**User action:** run the parent command, wait for the prompt, then press Enter.

```text
Windows:       py -3 starter\parent.py
Linux/macOS:   python3 starter/parent.py
```

**Visible result:** the parent prints a child PID. The child prints its PID and PPID, does a brief CPU phase, waits at the input prompt, then says it wrote an output file and exits with code `7`. The parent reports that code and finishes normally.

**Saved data:** `output/child-result.txt` contains synthetic labels such as `child_pid=...`, `platform=...`, and `requested_exit_code=7`. It contains no personal data.

Read the labelled sample in `Captured-output.md`. Its IDs are invented; yours will differ.

### Boundary task (25-35)

In your worksheet, trace this completed example: synthetic values start in the child's RAM -> Python requests a write -> an OS interface enters kernel filesystem services -> the write returns -> `output/child-result.txt` can be reopened. Label user mode and kernel mode. The bounded calculation itself can run in user mode. A successful write can be buffered, so it does not prove power-loss durability. Draw your own path and add the CPU, working RAM and stored file; explain why saving a file differs from preserving process memory.

## Step 5 — Run and inspect the waiting child (35-45)

1. In **Terminal A**, start the parent using the Step 4 command.
2. Wait until `CHILD: press Enter...` appears. **Do not press Enter yet.** The child is now waiting for input; the parent is waiting for the child.
3. Open **Terminal B** in the same `p02-lab` folder. Use one observation command below. Replace `<PID>` with the child PID shown by Terminal A.

| System | Command | Evidence to record |
| --- | --- | --- |
| Windows PowerShell | `Get-Process -Id <PID> \| Select-Object Id,ProcessName,@{Name='CPUSeconds';Expression={$_.CPU}},@{Name='WorkingSetBytes';Expression={$_.WorkingSet64}},VirtualMemorySize64,@{Name='ThreadCount';Expression={$_.Threads.Count}},Handles` | ID, CPU seconds, working set, virtual size, thread count, handles. |
| Windows PowerShell (parent link) | `Get-CimInstance Win32_Process -Filter "ProcessId = <PID>" \| Select-Object ProcessId,ParentProcessId,Name,CommandLine` | ProcessId and ParentProcessId. |
| Linux | `ps -o pid,ppid,stat,%cpu,vsz,rss,nlwp,comm -p <PID>` | PID/PPID, STAT, virtual size (VSZ), RSS, thread count. |
| Linux (optional) | `ls -l /proc/<PID>/fd` | Count/list open file descriptors if available; do not copy private paths. |
| macOS | `ps -o pid,ppid,state,%cpu,vsz,rss,comm -p <PID>` | PID/PPID, state, virtual size, RSS. Use Activity Monitor for threads if needed. |
| macOS (optional) | `lsof -p <PID>` | Record number/type of open resources only if available; do not paste private paths. |

4. Record your observations in your own worksheet. On Windows, open Task Manager with **Ctrl+Shift+Esc** → **Details** → find `python.exe` by PID (right-click a column heading → **Select columns** → add PID, Threads, Handles if needed). On macOS, open **Activity Monitor** → search `Python` → select the matching PID → click **Info** for threads and memory. A process viewer may label states differently. Task Manager does not expose a universal “ready” or “blocked” label, so do not claim that it does. On Linux/macOS, a `ps` state can change immediately.
Keep Terminal A waiting until your teacher starts Step 6.

## Step 6 - Finish the run and compare Windows/POSIX creation (45-55)

1. Return to Terminal A and press Enter. Watch the child write its synthetic file and exit. The parent’s `wait()` returns.
2. View the saved file:

```text
Windows:       Get-Content output\child-result.txt
Linux/macOS:   cat output/child-result.txt
```

### Compare process creation

Everyone reads the supplied POSIX trace in Captured-output.md and compares it with Windows process creation described in the presentation. Record whether you ran this example or used the supplied trace. On Linux/macOS, if your teacher says to continue, run:

```text
python3 starter/fork_exec_wait.py
```

It calls `fork()`, then the child calls `execv()` to replace its program image, and the original parent calls `waitpid()`. Press Enter when asked. Windows does not provide `fork()`, so Windows students use the supplied POSIX trace for the comparison and run the portable parent/child program locally. `Popen` in the portable program may use a platform-specific mechanism such as `posix_spawn`; do not describe it as “always fork.”

## Step 7 - Explain states and record a runtime decision (55-65)

In the worksheet, draw arrows for this particular run:

```text
new → runnable/running → waiting for terminal input → runnable/running → exited → reaped by parent
```

Label the arrows with `Popen`, the short CPU phase, `input`, Enter, `return EXIT_CODE` (initially 7), and `wait`. A process can move between runnable and running many times; your terminal’s print order does not prove the exact order in which the kernel scheduled CPU time. “Ready/runnable” means able to run; “running” means currently using a CPU; “waiting/blocked” means unable to continue until an event occurs.

### Record your provisional runtime ADR

An **ADR** (architecture decision record) is a short note saying what decision was made, why, and what trade-off it has. The main decision is provisional: for the future enrollment project, should the team use C#/.NET or lecturer-agreed Java on mixed developer laptops? Complete the four prompts in your worksheet:

- Decision: provisional C#/.NET or lecturer-agreed Java choice, with a reason based on mixed laptops.
- Evidence: what you know from the course context and what you still need to verify (toolchain, support, portability).
- Consequence: what the chosen runtime makes easier for the team.
- Trade-off: what risk or limitation remains.

Then add one sentence about the smaller supporting decision: why this particular parent waits for its child before reporting the synthetic result.

Before saving, check that your worksheet names the system you used, marks unavailable tools honestly, distinguishes observation from inference, and has a partner-role record.

## Step 8 - Change one value, quiz and review (65-75)

1. Operator opens `starter/child.py`.
2. Change `EXIT_CODE = 7` to `EXIT_CODE = 0`. If you finish early, also change `CPU_SECONDS = 0.75` to `CPU_SECONDS = 1.50`.
3. Predict one visible result before running. For example: “the parent will report code 0” or “the CPU phase will last longer.”
4. Run the parent again, press Enter, and verify the prediction in Terminal A and `output/child-result.txt`.
5. Record the change and outcome. Restore the original value if your teacher asks.
6. **Swap roles:** the former Observer now runs one command and explains what the parent is waiting for.

Use the presentation quiz after your changed-code run. Then swap worksheets with your partner: check platform, PID, labelled measurements, boundary path, lifecycle and runtime decision. Correct one specific detail. Keep your own copy.

## Step 9 - Save and explain (75-80)

Keep your individual worksheet, boundary path, labelled process observation, lifecycle, creation comparison, changed-code result, provisional ADR and exit reflection together. Label the no-install route as supplied/simulated; it creates no output file. Open My lesson work on the course website, choose P02 and your assigned group, and fill the eight answer sections. The website saves text answers to your signed-in account; check the Saved status, then choose Submit to my teacher. Keep the synthetic output file locally. Your teacher can see saved drafts and submissions. Ask a partner to check whether your commands, platform, PID and conclusions are clear.

## Appendix - the complete runnable code

Read these exact supplied files before running them. You do not need to type them from scratch. The small modification in Step 8 is the coding task.

### starter/parent.py

```python
"""Start, observe, and wait for the P02 child process using portable Python."""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path


def main() -> int:
    child_path = Path(__file__).with_name("child.py")
    command = [sys.executable, "-u", str(child_path)]
    print(f"PARENT: starting child with this Python runtime: {sys.executable}", flush=True)
    child = subprocess.Popen(command)  # stdio is inherited, so the child uses this terminal.
    print(f"PARENT: child PID is {child.pid}; waiting for it to exit.", flush=True)
    interrupted = False
    try:
        exit_code = child.wait()
    except KeyboardInterrupt:
        interrupted = True
        print("\nPARENT: Ctrl+C received. Ending only this lab child process.", flush=True)
        exit_code = None
    finally:
        # The Popen object identifies the one child this lab owns; never target other processes.
        if child.poll() is None:
            child.terminate()
            try:
                exit_code = child.wait(timeout=5)
            except subprocess.TimeoutExpired:
                print("PARENT: child did not end in 5 seconds; killing it.", flush=True)
                child.kill()
                exit_code = child.wait()

    if interrupted:
        print(f"PARENT: child ended after interruption with code {exit_code}.", flush=True)
        return 0

    print(f"PARENT: wait() returned; child exit code was {exit_code}.", flush=True)
    if exit_code == 7:
        print("PARENT: non-zero is intentional in this lab; the parent reports it and exits normally.", flush=True)
    elif exit_code != 0:
        print("PARENT: unexpected child result; read the child's error above and check the lab guide.", flush=True)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
```

### starter/child.py

```python
"""A small child process for P02. It writes only synthetic lab data."""

from __future__ import annotations

import os
import platform
import sys
import time
from pathlib import Path

EXIT_CODE = 7
CPU_SECONDS = 0.75
LAB_ROOT = Path(__file__).resolve().parent.parent
OUTPUT_FILE = LAB_ROOT / "output" / "child-result.txt"


def bounded_cpu_work(seconds: float) -> int:
    """Use CPU for a short, fixed time; the count is only a visible result."""
    deadline = time.perf_counter() + seconds
    count = 0
    while time.perf_counter() < deadline:
        count += 1
    return count


def main() -> int:
    print("CHILD: started", flush=True)
    print(f"CHILD: PID={os.getpid()} PPID={os.getppid()}", flush=True)
    print(f"CHILD: platform={platform.system()} Python={platform.python_version()}", flush=True)
    print(f"CHILD: CPU phase for {CPU_SECONDS} seconds...", flush=True)
    iterations = bounded_cpu_work(CPU_SECONDS)
    print(f"CHILD: CPU phase complete ({iterations} loop iterations).", flush=True)

    try:
        input("CHILD: press Enter to continue and write synthetic lab output: ")
    except EOFError:
        print("CHILD: no terminal input was available. Run in a terminal and press Enter; no new output was written.", file=sys.stderr, flush=True)
        return 1

    OUTPUT_FILE.parent.mkdir(exist_ok=True)
    OUTPUT_FILE.write_text(
        "P02 synthetic lab output\n"
        f"child_pid={os.getpid()}\n"
        f"parent_pid={os.getppid()}\n"
        f"platform={platform.system()}\n"
        f"cpu_iterations={iterations}\n"
        f"requested_exit_code={EXIT_CODE}\n",
        encoding="utf-8",
    )
    print(f"CHILD: wrote synthetic data to {OUTPUT_FILE.relative_to(LAB_ROOT)}", flush=True)
    print(f"CHILD: exiting with code {EXIT_CODE}.", flush=True)
    return EXIT_CODE


if __name__ == "__main__":
    raise SystemExit(main())
```

The parent starts the same Python interpreter with the child path as a separate argument. `-u` makes Python output unbuffered. The `finally` block cleans up only its owned child when interrupted. Code 7 is this lab's deliberate example; other unexpected nonzero results are reported as a failed run.

The child's bounded loop performs CPU work. `input()` waits for Enter; typed input is not stored. If input ends without a line, the child reports an error without writing new output. `write_text()` then creates or replaces the synthetic output file in the lab folder. `SystemExit(main())` turns the function result into a process exit code. Modify only EXIT_CODE as directed, then rerun.
