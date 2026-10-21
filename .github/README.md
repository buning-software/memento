<div id="top"></div>

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![GNU Affero General Public License v3.0 License][license-shield]][license-url]

<div align="center">
  <img src="../assets/memento.png" alt="Memento logo" height="96">
  <h3 align="center">Memento</h3>
  <p align="center">
    <a href="https://github.com/buning-software/memento/issues">Report Bug</a>
    ·
    <a href="https://github.com/buning-software/memento/issues">Request Feature</a>
  </p>
</div>

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#features">Features</a></li>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
        <li><a href="#install-a-release">Install a release</a></li>
      </ul>
    </li>
    <li><a href="#architecture">Architecture</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
  </ol>
</details>



## About The Project

Memento is a keyboard-first quick-capture app for Windows. Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Space</kbd> anywhere, type what you want to remember, and press <kbd>Enter</kbd>:

> call Anna tomorrow 3pm #work

Memento reads the deadline and the tags from the line as you type, stores the task on your device, and gets out of the way.

### Features

* **Global hotkey overlay:** a Fluent (Mica) capture window that opens instantly over any app and hides when you click away.
* **Natural input:** dates (`14-10-2026`, `2026-10-14`), relative days (`today`, `tomorrow`, `friday`, `tonight`), times (`15:00`, `3pm`) and `#tags`, in English and Dutch.
* **Keyboard only:** <kbd>↑</kbd>/<kbd>↓</kbd> select a task, <kbd>Enter</kbd> or <kbd>Space</kbd> completes it, <kbd>F2</kbd> edits it in place, <kbd>Esc</kbd> closes.
* **Private:** everything is stored locally in SQLite under `%APPDATA%\Memento`; nothing is sent anywhere.
* **Settings:** capture shortcut, start with Windows, monitor, default deadline time and theme (light, dark or system).

### Built With

* [.NET 10](https://dotnet.microsoft.com/) and WPF
* [WPF-UI](https://github.com/lepoco/wpfui) for the Fluent design
* [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
* [Entity Framework Core](https://learn.microsoft.com/ef/core/) with SQLite

## Getting Started

### Prerequisites

* Windows 10 (version 2004 or later) or Windows 11
* [.NET 10 SDK](https://dotnet.microsoft.com/download), only to build from source

### Installation

1.  Clone the repository:

    ```bash
    git clone https://github.com/buning-software/memento.git
    cd memento
    ```

2.  Build, test and run:

    ```bash
    dotnet build
    dotnet test
    dotnet run --project src/Memento
    ```

    Memento starts in the tray; press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Space</kbd> to open it.

    `dotnet test` also runs the UI tests (`tests/Memento.UiTests`). They type and click on the real desktop, so leave the keyboard and mouse alone while they run, and run only the unit tests with `dotnet test --project tests/Memento.UnitTests` (or one of the other unit test projects). Each UI test starts Memento on its own temporary data folder (`MEMENTO_DATA_DIR`) with its own capture shortcut, so it never touches your tasks or settings, even while your own Memento runs.

    Debug builds keep their data in `%APPDATA%\Memento.Dev`, so developing never touches the database of an installed Memento, and they can run next to it. Both register the same global shortcut, so change one of them in the settings if you run both at once.

### Install a release

Each [release](https://github.com/buning-software/memento/releases) has a signed MSIX per platform (`x86`, `x64`, `ARM64`) and `Memento.cer`, the certificate it is signed with.

1.  Trust the certificate once, from an elevated PowerShell. Skip this if you already trust a Buning Software app such as Surveil: it is the same certificate.

    ```powershell
    Import-Certificate -FilePath .\Memento.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
    ```

2.  Open `Memento_<version>_<platform>.msix` and choose **Install**.

"Start with Windows" in the settings works through the package's startup task. If you turn it off in Task Manager, Windows no longer lets Memento turn it back on; use Task Manager's Startup apps tab instead.

The installed app keeps its data in `%APPDATA%\Memento`, the same place as a build from source, so switching between the two keeps your tasks. Uninstalling the package leaves that folder in place.

## Architecture

A modular monolith in three projects; the dependencies point one way only: `Memento` → `Memento.EntityFramework` → `Memento.Core`.

| Project | Contents |
| --- | --- |
| `src/Memento` | WPF app and composition root (`Program.cs`): overlay and settings windows with their view models, tray icon, and the Windows adapters (global hotkey, and autostart through the Run registry key, or through the startup task when installed from the MSIX). `Package.appxmanifest` and `Assets/` are the MSIX package definition. |
| `src/Memento.EntityFramework` | The data layer on EF Core and SQLite: `MementoDbContext`, the `Tasks` entity and its configuration, the `ITaskRepository` implementation, migrations, and the startup initializer (WAL, applying migrations). |
| `src/Memento.Core` | Everything without UI or database access, in feature folders: `Tasks` (input parser, task service, `ITaskRepository`), `Settings`, `Hotkeys`, `Storage` (paths, `StorageException`). No WPF, Win32 or EF Core. |
| `tests/Memento.Core.UnitTests` | Unit tests for Core. `TestSupport/` holds the shared fakes. |
| `tests/Memento.EntityFramework.UnitTests` | Tests for the data layer against real SQLite files in a temp folder. |
| `tests/Memento.UnitTests` | Unit tests for the view models and presentation helpers. |
| `tests/Memento.UiTests` | End-to-end tests that launch the built `Memento.exe` and drive it through UI Automation with [WinUia](https://github.com/buning-software/win-uia) and NUnit. |

Views only bind to view models. View models and services reach the outside world through small interfaces (`ITaskRepository`, `ISettingsStore`, `IHotkeyRegistrar`, `IDialogService`, `IAutostart`, `IShellLauncher`) and `TimeProvider`, so the behaviour is tested without windows, disk or registry.

<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b features/feature-title`)
3. Commit your Changes (`git commit -m 'Added feature'`)
4. Push to the Branch (`git push origin features/feature-title`)
5. Open a Pull Request

### Publishing a release

Publishing a GitHub release runs `.github/workflows/release.yaml`: it runs the tests, then builds, signs and attaches an MSIX for `x86`, `x64` and `ARM64`, plus `Memento.cer`.

* Name the tag `v1.2.3` (three numbers). The workflow rejects anything else, and the tag becomes the package version.
* Signing uses the `CERTIFICATE_BASE64` (the `.pfx`, base64 encoded) and `CERTIFICATE_PASSWORD` secrets. They are organization secrets of `buning-software`, shared with Surveil, and each repository that releases has to be allowed to use them (organization Settings, Secrets and variables, Actions). The certificate subject must equal `Publisher` in `src/Memento/Package.appxmanifest` (`CN=BuningSoftware`).
* The certificate is valid for one year. Generate a new one before it expires, update both secrets, and ask users to trust the new `Memento.cer`.
* The test run (every pull request, and a release before it signs anything) also builds an unsigned x64 MSIX, so a broken manifest shows up early. The packaging is the `.github/actions/package-msix` action, shared with the release workflow.
* To try the packaging locally, run the same command as the workflow, without signing:

  ```bash
  dotnet publish src/Memento/Memento.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false -p:AppxBundle=Never "-p:AppxPackageDir=out/"
  ```

  The MSIX tooling is only active when `GenerateAppxPackageOnBuild` is set, so normal builds and `dotnet test` do not need a platform flag.
* To change the database schema, run `dotnet tool restore` once, then `dotnet ef migrations add Name --project src/Memento.EntityFramework`. The tool version is pinned to match EF Core.


<!-- LICENSE -->
## License
Distributed under the GNU Affero General Public License v3.0 License. See `LICENSE` for more information.


<p align="right">(<a href="#top">back to top</a>)</p>



<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/buning-software/memento.svg?style=for-the-badge
[contributors-url]: https://github.com/buning-software/memento/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/buning-software/memento.svg?style=for-the-badge
[forks-url]: https://github.com/buning-software/memento/network/members
[stars-shield]: https://img.shields.io/github/stars/buning-software/memento.svg?style=for-the-badge
[stars-url]: https://github.com/buning-software/memento/stargazers
[issues-shield]: https://img.shields.io/github/issues/buning-software/memento.svg?style=for-the-badge
[issues-url]: https://github.com/buning-software/memento/issues
[license-shield]: https://img.shields.io/github/license/buning-software/memento.svg?style=for-the-badge
[license-url]: https://github.com/buning-software/memento/blob/main/LICENSE
