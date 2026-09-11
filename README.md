# Grok-ServUO

Private Ultima Online shard based on **[ServUO](https://www.servuo.com) Publish 57.4.1**.

Upstream source: [ServUO/ServUO](https://github.com/ServUO/ServUO) tag [`57.4.1`](https://github.com/ServUO/ServUO/releases/tag/57.4.1)  
Commit: `aa4c139fd64f1a2445ec186a134f0e2f2450ba53`

ServUO is a community-driven Ultima Online server emulator written in C#.

## Build

### Windows

- Run `_windebug.bat` for development (debugger / extended output).
- Run `_winrelease.bat` for production.

### Other platforms (macOS / Linux)

- Run `_makedebug` for development.
- Run `_makerelease` for production.

These produce `ServUO.exe` (or the platform equivalent via the make scripts).

## First run

1. Edit `/Config/Server.cfg` — set shard `Name`, listen address, public address, and port (default `2593`). Host/IP are left for the owner.
2. Review the other `Config/*.cfg` files (especially `DataPath.cfg`, `Client.cfg`, `Expansion.cfg`).
3. Build with `_winrelease.bat` or `_makerelease`.
4. Run `ServUO`.

See also [docs/CUSTOM.md](docs/CUSTOM.md) for where to put customizations.

## Client / mul files

ServUO needs Ultima Online client data (`.mul` / UOP assets). On Windows it can auto-detect a default Classic client install. On non-Windows (and for custom installs), set `CustomPath` in `Config/DataPath.cfg` to your client directory. You must own a legitimate UO client; client files are not included in this repository.

## Customization

- Shard-specific notes: [docs/CUSTOM.md](docs/CUSTOM.md)
- Custom C# scripts: [Scripts/Custom/](Scripts/Custom/)
- Dude companions (catch/summon/jobs/roster): [Scripts/Custom/Dudes/README.md](Scripts/Custom/Dudes/README.md)

## License

Same as upstream ServUO — see [LICENSE](LICENSE).
