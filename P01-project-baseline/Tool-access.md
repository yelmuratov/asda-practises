# Tool access

Checked on 2026-11-25 on my own laptop. Support owner for all items: Salimbay. I use C# with .NET 10 for later labs. PostgreSQL is a planning assumption to confirm with the teacher.

1. Editor
   - Status: available
   - What I checked: vim, Neovim and nano are installed. VS Code and Rider are not installed.
   - Next action: choose and install a C# editor or IDE before the first coding lab.
2. Git
   - Status: available
   - What I checked: git 2.53.0 is installed and my user name is set. The repository has a GitHub remote, and `git ls-remote` reached it. On 2026-11-25 the P01 commits were pushed, and `git status -sb` showed local main matching GitHub.
   - Next action: push after each finished part, so the work is not only on my laptop.
3. Operating system
   - Status: available
   - What I checked: Kali GNU/Linux Rolling 2026.3, kernel 7.2.15.
   - Next action: ask the teacher whether lab instructions assume Windows, and note any commands that differ on Linux.
4. C#/.NET
   - Status: available
   - What I checked: SDKs 10.0.401 and 8.0.425 are installed in ~/.dotnet, and an old 6.0.400 (support ended November 2024) in /usr/share/dotnet. ~/.bashrc put ~/.dotnet first on PATH, but my shell is zsh and ~/.zshrc did not, so typing `dotnet` in zsh ran 6.0.400. On 2026-11-25 I added the same lines to ~/.zshrc; in a new zsh, `dotnet --version` shows 10.0.401.
   - Next action: none; I chose C# with the .NET 10 SDK and it is ready.
5. Database (PostgreSQL)
   - Status: available, not running
   - What I checked: the psql 18.6 client and the PostgreSQL 18 server are installed. The postgresql service is inactive; I did not start it or connect to a database. No GUI tool (pgAdmin, DBeaver) is installed.
   - Next action: confirm with the teacher that PostgreSQL is the lab database; start the service and test a connection when a lab needs it.
6. Other tools
   - Status: available, not tested
   - What I checked: Firefox, Chromium and Google Chrome are installed. Docker 28.5.2 is installed; I did not run a container.
   - Next action: none for P01.
