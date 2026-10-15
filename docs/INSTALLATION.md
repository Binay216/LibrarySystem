# Installation Guide

A step-by-step setup guide for LibrarySystem on a clean Windows machine, written for someone who has never seen the project before. The same instructions work regardless of which branch (Casuarina, Palmerston or Darwin City) this particular installation is set up to represent - that's a one-line configuration change covered near the end, not a different setup process.

> These instructions were verified end-to-end on a clean Windows Server 2019 virtual machine with Visual Studio 2026 Community (a fresh clone, nothing pre-installed beyond the OS).

## What you need

- **Windows**, with administrator rights to install software.
- **Visual Studio 2022 or later** (any edition, including the free Community edition), with the **"ASP.NET and web development"** workload installed. This workload brings in everything else you need - the .NET SDK and SQL Server LocalDB - so if you're installing Visual Studio fresh, just tick that workload during setup and skip the next two bullets.
- **.NET 10 SDK** - already included by the workload above. To check whether you have it separately, open a terminal and run `dotnet --version`; you want `10.x`.
- **SQL Server LocalDB** - also included by the workload above. This is a lightweight, single-user SQL Server that runs on your own machine with no separate install or service to manage; you do not need SQL Server Management Studio, a server, or a network connection.
- **Git** (or GitHub Desktop, or Visual Studio's own built-in Git support) to clone the repository.
- **A modern browser** - Chrome, Edge or Firefox. Internet Explorer (still the default browser on Windows Server) does not render the interface correctly and is not supported.

If Visual Studio is already installed without that workload, open the **Visual Studio Installer** (search for it in the Start menu), find your Visual Studio 2022 install, click **Modify**, tick **ASP.NET and web development**, and click **Modify** again to install it.

## Step 1: Get the code

Clone the repository to a folder on your machine. In a terminal:

```
git clone <repository-url>
```

(Replace `<repository-url>` with the actual clone URL for this repository.) If you'd rather not use the command line, Visual Studio's **Clone a repository** option on its start screen, or GitHub Desktop, do the same thing.

## Step 2: Open and run

1. In the cloned folder, double-click **`LibrarySystem.slnx`**. This opens the solution in Visual Studio.
   - `.slnx` is a newer solution file format than the traditional `.sln`. A recent Visual Studio 2022 build opens it natively with no extra steps. If your Visual Studio is old enough not to recognise it, update Visual Studio via the Visual Studio Installer.
2. Wait for Visual Studio to finish restoring NuGet packages (a progress indicator shows in the status bar at the bottom; this only takes a while the first time).
3. Press **F5** (or click the green ▶ **http** / **https** run button in the toolbar).

That's the entire setup. On this first run:

- A new LocalDB database is created automatically.
- Every EF Core migration is applied automatically.
- The database is seeded automatically: 3 branches, 3 staff logins (Admin/Reception/Manager), 5 borrowers, and around 20 items with a realistic mix of loans, fines and reservations.

No Package Manager Console commands, no `Update-Database`, no manually running SQL scripts. A browser window opens by itself pointing at the running app (`http://localhost:5105` by default).

## Step 3: Confirm it worked

1. On the home page, click **Log in** (or go to `/Identity/Account/Login`).
2. Sign in as the Manager account: email `manager@library.com`, password `Manager@123`.
3. You should land on a dashboard with charts and numbers already populated - that confirms the database was created and seeded correctly.

The full set of seeded logins:

| Role | Email | Password |
|---|---|---|
| Admin | `admin@library.com` | `Admin@123` |
| Reception | `reception@library.com` | `Reception@123` |
| Manager | `manager@library.com` | `Manager@123` |

## Setting which branch this installation represents

Open `appsettings.json` in the project root and find:

```json
"Library": {
  "BranchCode": "CAS"
}
```

Change `"CAS"` to `"PAL"` (Palmerston) or `"DRW"` (Darwin City) to make this installation represent a different branch, then stop and restart the app (press the red ■ stop button, then **F5** again - a config file change isn't picked up by a running app). The app validates this value against the seeded branches every time it starts, so a typo here is reported immediately and clearly rather than causing confusing behaviour later - see [Troubleshooting](#unknown-branch-code-at-startup) below if you see that error.

This setting affects which branch's items can be borrowed through Reception or the kiosk, which branch's transfer queue and overdue list Reception sees, and which branch the kiosk identifies itself as. It does **not** affect the Manager dashboards/reports (which default to showing every branch) or the public catalogue/API (which always search every branch). See the main [README](../README.md#which-branch-is-this-running-as) for the full breakdown.

## Troubleshooting

### I changed the seed data, but nothing's different

This is the single most common point of confusion with this project.

**Seeding only runs the first time the database is created.** EF Core checks whether the database already exists; if it does, seeding is skipped entirely, no matter what you've changed in `Data/SeedData.cs` or `Data/IdentitySeed.cs`. You have to delete the database so the app creates (and seeds) it fresh on the next run.

**To delete the database in Visual Studio:**

1. Open **View → SQL Server Object Explorer**.
2. Expand **`(localdb)\MSSQLLocalDB`** → **Databases**.
3. Find the database - its name starts with `aspnet-LibrarySystem-` (the exact name is in `appsettings.json` under `ConnectionStrings:DefaultConnection`, after `Database=`).
4. Right-click it → **Delete**. If prompted, tick **"Close existing connections to the selected database"**.
5. Press **F5** to run the app again. It recreates the database, reapplies migrations, and reseeds from scratch.

If the database doesn't appear in SQL Server Object Explorer (for example, LocalDB isn't registered as a server there yet), click **Add SQL Server**, choose **Local**, and select `(localdb)\MSSQLLocalDB` from the list first.

**Command-line alternative**, from the project folder, with the [EF Core CLI tool](https://learn.microsoft.com/ef/core/cli/dotnet) installed (`dotnet tool install --global dotnet-ef` if you don't have it):

```
dotnet ef database drop
```

Confirm when prompted, then run the app again.

### LocalDB connection problems

Symptoms: an exception on startup mentioning `(localdb)\mssqllocaldb`, "a network-related or instance-specific error", or "cannot open database".

- **Confirm LocalDB is actually installed**: open a terminal and run `sqllocaldb info`. You should see `MSSQLLocalDB` listed. If the command isn't recognised at all, LocalDB isn't installed - reinstall via the Visual Studio Installer's "ASP.NET and web development" workload (see [What you need](#what-you-need) above).
- **Confirm the instance is running**: `sqllocaldb info mssqllocaldb`. If its state isn't "Running", start it with `sqllocaldb start mssqllocaldb`.
- **Corrupted instance**: as a last resort, `sqllocaldb stop mssqllocaldb` followed by `sqllocaldb delete mssqllocaldb` removes the instance entirely; it's recreated automatically the next time anything connects to `(localdb)\mssqllocaldb`, including this app on its next run.
- The connection string itself (`ConnectionStrings:DefaultConnection` in `appsettings.json`) shouldn't need to change on a standard Visual Studio + LocalDB setup - if you've edited it, reverting to the version in source control is the fastest fix.

### Unknown branch code at startup

If you see an error on startup like:

```
InvalidOperationException: Configured branch code 'XYZ' (Library:BranchCode in appsettings.json) does not match any seeded branch. Valid codes: CAS, PAL, DRW.
```

`Library:BranchCode` in `appsettings.json` is set to something that isn't one of the three seeded branch codes. Fix the typo (valid values are listed in the error itself) and run again - see [Setting which branch this installation represents](#setting-which-branch-this-installation-represents) above.

### Browser shows a certificate warning, or the page won't load over HTTPS

The first time an ASP.NET Core app runs with HTTPS on a machine, the local development certificate may not yet be trusted. From a terminal:

```
dotnet dev-certs https --trust
```

Accept the prompt that appears, then run the app again. Alternatively, choose the **http** launch profile instead of **https** in Visual Studio's run-button dropdown - the app works identically over plain HTTP for local development.

### Build fails with "contains a virus or potentially unwanted software"

Symptoms: the build fails with `MSB3021` or `MSB3030`, naming `QuestPdfSkia.dll` and saying it "contains a virus or potentially unwanted software" or that the file could not be copied/found.

This is **Windows Defender quarantining a false positive**. `QuestPdfSkia.dll` is a legitimate native graphics binary (Skia) bundled inside the QuestPDF NuGet package, used to generate the PDF reports - it is sometimes flagged incorrectly by Defender's heuristics, especially right after it's extracted into the NuGet package cache on a brand-new machine.

To fix it:

1. **Exclude the NuGet package cache from Defender**, so it stops re-scanning (and re-quarantining) files there: open **Windows Security → Virus & threat protection → Manage settings → Add or remove exclusions**, and add the folder `%userprofile%\.nuget\packages` as a **Folder** exclusion.
2. **Restore the quarantined file**: open **Windows Security → Virus & threat protection → Protection history**, find the entry for `QuestPdfSkia.dll`, and choose **Restore** (or **Allow on device**).
   - If it's no longer listed there (Defender can delete rather than quarantine), instead clear the NuGet cache and let it restore fresh now that the exclusion is in place: run `dotnet nuget locals all --clear` from a terminal, then rebuild - NuGet re-downloads and re-extracts the package and the exclusion from step 1 keeps Defender from touching it this time.
3. **Rebuild** the solution.

### "Port already in use" on startup

Another running copy of the app (or something else) is already using port 5105 or 7009. Stop any other running instance of the app (check the Visual Studio **Debug → Windows → ... ** or just close other instances of this solution), or edit the ports in `Properties/launchSettings.json` if you specifically need to run two copies side by side.
