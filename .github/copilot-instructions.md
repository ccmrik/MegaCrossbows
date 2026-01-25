# MegaCrossbows Copilot Instructions

## Project Overview
This is a BepInEx mod for Valheim that enhances crossbow functionality with advanced features including automatic fire, zoom, magazine system, and customizable projectile physics.

## Project Structure
- **MegaCrossbowsPlugin.cs** (Class1.cs): Main plugin entry point with BepInEx configuration
- **CrossbowPatches.cs**: Harmony patches for crossbow behavior modification

## Coding Standards

### General Guidelines
- Target framework: .NET Framework 4.6.2 (Valheim requirement)
- Use Harmony for all game code modifications
- Follow existing Valheim code patterns and naming conventions
- Keep patches focused and minimal to avoid conflicts with other mods

### BepInEx Specific
- Always check `ModEnabled` configuration before applying patches
- Use `Logger.LogInfo/LogWarning/LogError` for debugging
- Configuration changes should be user-friendly with clear descriptions
- Version increments follow semantic versioning (MAJOR.MINOR.PATCH)

### Harmony Patching
- Use `[HarmonyPrefix]` to prevent original method execution when needed
- Use `[HarmonyPostfix]` to modify return values or add behavior after
- Cache reflection results and data to avoid performance issues
- Always include null checks for game objects and components
- Use try-catch blocks for potentially unsafe operations

### Performance Considerations
- Cache dictionary lookups for per-instance data
- Avoid FindObjectOfType calls in Update methods
- Use object pooling for frequently created objects
- Profile code if frame rate drops are reported

### Unity/Valheim Specific
- Always check if player is local player before UI operations
- Camera modifications should be restored when feature is disabled
- Use `Time.time` for timing, not `DateTime`
- Respect game's input system and key bindings

### Configuration Values
Current configuration structure:
- **General.Enabled**: Master on/off switch
- **General.DamageMultiplier**: Percentage-based damage scaling
- **General.FireRate**: Shots per second
- **Zoom.ZoomMin/ZoomMax**: FOV zoom range
- **Projectile.Velocity**: Bolt speed multiplier
- **Projectile.NoGravity**: Disable projectile drop
- **Magazine.Capacity**: Rounds before reload

### Code Style
- Use meaningful variable names (e.g., `currentMagazine` not `mag`)
- Keep methods focused on single responsibility
- Comment complex game mechanics interactions
- Use early returns for guard clauses

## Testing Strategy
1. Test with vanilla crossbow first
2. Verify compatibility with other weapon types (should not affect them)
3. Test multiplayer sync for projectile modifications
4. Validate configuration changes apply without restart when possible
5. Test edge cases: empty magazine, rapid fire, zoom during reload

## Common Pitfalls
- Don't modify static game data directly (affects all players)
- Remember Valheim uses older Unity version (2020.3)
- Check for null on `Player.m_localPlayer` (null in menus/loading)
- Crossbow detection must work for modded crossbows too
- Animation states must match fire rate or it looks broken

## File Paths
- **Valheim Install**: `C:\Program Files (x86)\Steam\steamapps\common\Valheim`
- **Plugin Output**: `C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins`
- **Assemblies**: `valheim_Data\Managed\` for game code, `BepInEx\core\` for BepInEx

## Build Process
- PostBuild event auto-copies DLL to plugin folder
- References set to `Private=false` to avoid DLL bloat
- Test in-game after each build (r2modman profile: "Valheim Min Mods")

## Git Workflow
- Commit after each feature completion
- Use semantic versioning in commit messages
- Tag releases with version number
- Include changelog in version commits
