# P02 lab: processes in action

This pack supports an 80-minute practice on operating systems, kernels, and the process lifecycle. Students run a small, safe Python program, inspect it with their own operating system, and record evidence. It works on Windows, Linux, and macOS without Python packages or administrator access.

## Pack contents

- `Student-lab-guide.md` — student-facing, step-by-step activity.
- `Worksheet.md` — individual evidence sheet; submit or photograph it as directed.
- `Instructor-guide.md` — preparation, exact pacing, prompts, answers, and rubric.
- `starter/` — runnable Python examples.
- `Captured-output.md` — labelled illustrative example only; it is not evidence from a student's computer.

## The fictional project

> **Fictional classroom scenario:** A university enrollment system lets students reserve lab seats while a registrar needs accurate capacity information. This lab is a small OS helper experiment for that project: `parent.py` starts `child.py`, waits for it, and reports the child’s exit code. It is not an enrollment application and does not access student records.

## Safe data boundary

`child.py` writes only the synthetic file `output/child-result.txt` inside the extracted `p02-lab` folder. It never reads student files or needs elevated permissions. Delete the extracted lab folder when the course is over if you no longer need it.

The main runtime ADR is provisional: choose C#/.NET or lecturer-agreed Java after considering mixed developer laptops, the evidence available, and remaining unknowns. The synchronous `wait()` decision is a smaller supporting decision.

## Run after extracting the ZIP

Open a terminal in the `p02-lab` folder, then use one of these commands:

| System | Terminal command |
| --- | --- |
| Windows PowerShell | `py -3 starter\parent.py` (or `python starter\parent.py`) |
| Linux | `python3 starter/parent.py` |
| macOS | `python3 starter/parent.py` |

When the child asks, press Enter. Its intentional exit code is `7`; `parent.py` explains that outcome and itself finishes successfully.

`fork_exec_wait.py` is an optional direct POSIX demonstration for Linux/macOS. Windows has no `fork()` system call, so Windows students use the portable `parent.py` activity. The portable program uses `subprocess.Popen`; it does not promise that any particular operating-system creation mechanism is used. On some POSIX systems Python may use `posix_spawn` instead of `fork`.
