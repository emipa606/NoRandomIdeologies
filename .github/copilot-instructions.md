# GitHub Copilot Instructions for NoRandomIdeologies Mod

## Mod Overview and Purpose

The NoRandomIdeologies mod for RimWorld allows players to use their saved ideologies when other factions are generated. This mod is developed by Mlie and is available as an open-source project on GitHub. The main purpose of this mod is to provide players with more control over the ideologies used by factions in the game.

## Key Features and Systems

- Use saved ideologies for faction generation and random wanderers/guests
- Select preferred saved ideologies per faction in the mod options
- Option to override any ideology restrictions for factions
- Ignore warnings in the log when the mod caches the ideologies

## Coding Patterns and Conventions

- The mod uses Harmony patching to modify the behavior of existing game code
- C# files are organized into classes that handle specific tasks such as ideology tracking, meme checking, and mod settings
- XML integration is not required for this mod

## XML Integration

NoRandomIdeologies does not require any XML integration.

## Harmony Patching

The mod uses Harmony patching to modify the behavior of existing game code. The following prefixes are used:

- `FactionIdeosTracker_ChooseOrGenerateIdeo.Prefix`
- `IdeoUtility_IsMemeAllowedFor.Prefix`

## Suggestions for Copilot

- Use the `FindAllValidIdeologies` and `FindIdeoForFaction` methods to retrieve saved ideologies for factions
- Use the `ExposeData` and `Reset` methods in the `NoRandomIdeologiesSettings` class to manage mod settings
- Use the `DoSettingsWindowContents` and `WriteSettings` methods in the `NoRandomIdeologiesMod` class to create the mod settings window

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

