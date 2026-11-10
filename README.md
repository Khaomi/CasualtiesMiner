# CasualtiesMiner

A suite of data mining tools for [Casualties: Unknown](https://store.steampowered.com/app/4576490/Casualties_Unknown/).

It is primarily used for automation work on the [Miraheze wiki](https://casualtiesunknown.miraheze.org/) for the game.

# Installation

You can go to [here](https://github.com/Khaomi/CasualtiesMiner/actions), and click on the latest action to download the latest build for your platform.

# Building

You can easily build the project by running
```sh
dotnet publish
```

> [!TIP]
> `-c Release` for production
> nothing for development

The result binary file should be in
```
CasualtiesMiner.Cli/bin/${config}/net10.0/${platform}/native/
```

# Structure

The suite is split into multiple .NET projects:
- CasualtiesMiner.Generators, a C# source generator that creates DTOs from game classes and strips them of Unity types
- CasualtiesMiner.Shared, a C# library holding generated DTOs and code reused between the projects
- CasualtiesMiner.Dumper, a C# library that dumps data from the game prefabs and the game's .NET assembly
- CasualtiesMiner.Uploader, a C# library that uploads dumped data to MediaWiki through Bucket tables and Lua data modules
- CasualtiesMiner.Cli, a CLI frontend for the tool

## CasualtiesMiner.Generators

The source generator creates DTOs (read: C# classes) derived from the game's own MonoBehaviours and types. It strips or
replaces any Unity specific types (such as `AudioSource`, `Sprite`, etc.), so that the derived type can be used in a
plain .NET project. This is used to synchronize the properties between the tools and the game and warn of any potential
breaking changes caused by game updates.

## CasualtiesMiner.Shared

The shared library defines partial classes for the DTOs created by the generator. In addition to the game's own
properties, the library adds additional ones, implements equality operators, etc.

## CasualtiesMiner.Dumper

The data dumper does two things:
* Parses .NET IL code from the game's `Assembly-CSharp.dll` and extracts game object properties, such as
  item stats, moodle effects, tiles, recipes, etc.
* Extracts prefab information from the game's asset bundles for game objects like items, buildings and entities

Currently, the parsing of data from delegates like `OnUse`, `LimbUse`, etc. is not implemented yet.

## CasualtiesMiner.Uploader

The uploader library sends the dumped item data to MediaWiki in the form of 
[Bucket](https://meta.weirdgloop.org/w/Extension:Bucket) tables and Lua data modules. The wiki must have the Bucket and
Scribunto extensions installed (which is the case for the [Miraheze wiki](https://casualtiesunknown.miraheze.org)).

### Uploaded data

#### Bucket

The uploader creates and uploads a Bucket schema for each type of extracted data:

- `Bucket:Block` - tile data, like the health, hit sound and sleep quality
- `Bucket:Bodyfield` - holds info about body timers
- `Bucket:Building` - building and entity data, like health, items dropped when destroyed, etc.
- `Bucket:Gamefield` - holds specific constants for gameplay
- `Bucket:Item` - items, like their value and weight
- `Bucket:Item_liquid` - extra liquid container data, like the liquid capacity
- `Bucket:Item_battery` - extra battery data, like max charge
- `Bucket:Item_container` - extra item container data, like maximum weight and encumbrance reduction
- `Bucket:Item_gun` - extra gun data, like damage and loudness
- `Bucket:Item_page` - links an item ID with its page on MediaWiki; this is populated through the wiki
- `Bucket:Liquid` - liquid data, like color and qualities
- `Bucket:Moodle` - moodle data, like icon, conditions, and whenever they can be seen by unchipped players
- `Bucket:Recipe`, `Bucket:Recipe ingridient`, `Bucket:Recipe result` - all item crafting recipes

The uploader also creates a trigger page (like `Project:Item data`) and Lua data module (like `Module:Item/data`)
for the purpose of inserting the extracted data into the Bucket tables.

#### Localization (i18n)

Game locale files (`CasualtiesUnknown_Data/Lang/EN.json`, community translations, etc.) are uploaded as Scribunto
modules that are logically grouped under the `Module:Locale` parent module:

- `Module:Locale/{lang}/blocks` - tile names
- `Module:Locale/{lang}/buildings` - building and entity names and descriptions
- `Module:Locale/{lang}/items` - item names and descriptions
- `Module:Locale/{lang}/liquids` - liquid names and descriptions
- `Module:Locale/{lang}/moodles` - mooodle names and descriptions
- `Module:Locale/{lang}/notes` - in-game survivor notes, indexed by layer
- `Module:Locale/{lang}/pauseQuotes` - pause menu pause quotes
- `Module:Locale/{lang}/pdaNotes` - in-game PDA notes
- `Module:Locale/{lang}/character/{character name}` - trader and player dialogue for characters
- `Module:Locale/EN/ui`, … — infobox labels and category names.
- `Module:Locale/WikiUi` — wiki-only labels for moodle cause expressions (`body.*` → readable names).

Locale JSON files come from the game (`<game>/CU_Data/Lang/`) or the
[community locale repository](https://github.com/Orsoniks/scavgame-locale).

## CasualtiesMiner.Cli

The frontend tool provides two commands: `extract` and `upload`.

### Extract

The extract command takes the path to `Assembly-CSharp.dll`, or alternatively the game's installation
directory.

The resulting extracted data is written to `data.json`, next to the CLI executable. Alternatively, if a path is provided
as a second parameter, it is written to that path instead.

Usage:
```sh
./CasualtiesMiner.Cli extract path/to/Assembly-CSharp.dll
```

### Upload

The upload command uploads all of the data to Mediawiki in the form of Lua data modules and trigger pages. 

The command either takes an existing data files as extracted by the extract command, or runs the extraction process
beforehand.

Preview without editing (no login needed):
```sh
./CasualtiesMiner.Cli upload all --dry-run --data data.json --assembly-path path/to/assembly
```

Upload data to Mediawiki (bot credentials via Special:BotPasswords):
```sh
./CasualtiesMiner.Cli upload all --user "Bot@uploader" --password "<botpassword>" --assembly-path path/to/assembly
```

Use an existing data file instead of the game assembly:
```sh
./CasualtiesMiner.Cli upload all --data path/to/data.json --dry-run --data data.json --assembly-path path/to/assembly
```

Credentials may also be provided via the `CU_WIKI_USER` / `CU_WIKI_PASSWORD` environment variables.

#### Upload modes

Bucket can only be written through `bucket.put` calls executed while a page is parsed, so the uploader
is a bot that edits pages which trigger those calls.

- **`table_schema`** — upload `Bucket:*` table definitions (run once or after schema changes).
- **`locales`** — upload `Module:Locale` and per-language item/UI modules.
- **`table_data`** — upload `Module:*/data`, and refresh Bucket via trigger pages (`Project:* Data`).
- **`all`** — uploads all of the above in the following order: `table_schema`, `locales`, and `table_data`.

### Notes / prerequisites

- Create a bot account at `Special:BotPasswords` with the *Edit existing/new pages* grants.
- Table data uploads also refresh `Project:* data` trigger articles. The uploader creates them on first run if missing.
- The dumper currently emits a placeholder for `tags` (the `//TEMP` line in `Dumper.cs`), so the `tags`
  column will be empty until that field stores the raw tag string; the uploader handles this gracefully.
