# Reporting Bugs
If you encounter a bug with Better Inventory, please report it [here](https://github.com/Spiky-73/BetterInventory/issues) or on the [Steam Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3074374647), including the following in your report if possible:

### 1. What is the bug ?
Add as many details as possible about the bug, such as:
- What happens
- Does it happen in single player or multiplayer
- Your `client.log` files (located in the `tModLoader-Logs` folder in your tModLoader install directory)
- Any details that could be important

### 2. How to reproduce the it ?
List the steps to execute to cause the bug.

### 3. Which feature(s) causes it ?
Since features have no impact on the game if disabled, disabling the ones responsible for the bug allows you to keep using the mod until I fix the bug.

The process is quite simple:
1. Disable a bunch of feature
2. Re-enable them one by one until the bug happens. The last feature you enabled is probably the one causing the bug.
3. Keep this feature disabled and re-enable the rest. If the bug still happens, go back to step 1.

#### Gameplay Bugs
1. Disable all the features in a config.
2. If the bug still happens, re-enable the features and redo step 1 with another config
3. Re-enable the features one by one until the bug happens. The last feature you enabled is probably the one causing the bug.
4. Keep this feature disabled and re-enable the rest. If the bug still happens, go back to step 1.

#### Mod Loading Bugs
1) Disadle all mods except `Better Inventory`
2) In the `Reporting Bugs` config,
   - **Enable `Compatibility Mode`**
   - Initialize `DisableAll`
3) Reload with all your mods enabled
4) Re-enable a feature and Reload. If the loading fails, keep it disabled
5) Repeat step 4 with every features you want to enable

**Include the list of unloaded features and your mod list in your report**