# Better Inventory changelog

## v0.9.1.3
- Fixed Deposit Click not respecting item slots contraints
- Fixed Quick Stack on Pickup causing item duplication in Multiplayer

## v0.9.1.2
- Fixed Return to Previous Slot not working as expected when dropping favorited items
- Fixed Void Bag First not handling coins properly
- Fixed RecipeUI consuming keyboard input even when not visible
- Fixed Quick Stack hotkey not refreshing recipes
- Fixed Quick Stack on Pickup not handling multiplayer properly
- Fixed Quick Stack on Pickup not working properly when a chest was opened

## v0.9.1.1
- Fixed Quick Stack on item pickup deleting coins

## v0.9.1
- Save Return to Previous Slot data between reloads
- Added Return to Previous Slot when depositing items in chests or equipping them
- Added a way to clear a marked slot when Fake Item is enabled
- Added Shift-Click to Return to Previous Slot
- Fixed Return To Previous Slot marking all items when crafting
- Fixed Return To Previous Slot causing a crash when a chest with a mark is destroyed
- Fixed Recipe Search Bar not changing the state of the Available Recipes filter
- Fixed Search Previous on Right click not working properly

## v0.9.0.2
- Fixed Refill Mouse Items deleting favorited stackable items
- Fixed Pickup to Void Bag First moving favorited items inside the Void Bag
- Made Move Items in Return to Previous Slot more granular
- Fixed right-clicking items not marking items or removing marks
- Fixed Return to Previous Slot on Consumption marking items when it should not
- Changed Smart Consumption to not be applied if the mouse item is consumed and Include Mouse is disabled
- Fixed Recipe Sort been disabled by default

## v0.9.0.1
- Added Move Items To Return to Previous Slot
- Fixed Refill Mouse Item not been able to be disabled
- Fixed Refill Mouse Item making removing the favorited status of items

## v0.9
- Added Recipe Sorting
- Added Refill Mouse Item to Smart Pickup
- Added Fix Scroll direction to Fixed UI
- Move Stack Trashed into Better Trash
- Added Trash the Trash to Better Trash Slot
- Added Deposit Middle Click
- Added Complete Quick Stack to Better Quick Stack
- Added Limited Personal Quick Stack to Better Quick Stack
- Added Inventory Slots Texture
- Clarified Smart Consumption tooltips
- Clarified Can Consume Itself to Smart Consumption
- Added Quick Stack to Smart Pickup
- Added Consumption to Return to Previous Slot
- Added Quick Stack hotkey
- Added Fix Ammo to Return to Previous Slot
- Added Bring Item To Hovered Slot to Quick Move
- Added Simple search to Recipe Search Bar
- Fixed Quick with Frame Skip Off
- Fixed Extra Item Right Click conflicting with Terraria Overhaul 

## v0.8.2
- Added support for loadouts to Pickup to Previous Slot, Quick Move, Extra Materials and Auto Equip
- Added options to include loadouts or not to Auto Equip and Quick Move
- Change Quick Move hotkey display to show a count instead of a repeating number for large values
- Added Follow Item option to Quick Move
- Quick moving will now remember the original menu pages

## v0.8.1.1
- Fixed a infinite loop when buying a stack of items right after having bought it once

## v0.8.1
- Added Hide Blacklisted Recipe to Favorited Recipes
- Added Follow Recipe on Favorite to Favorited Recipes
- Change Craft Stack's tooltip default state to on
- Fixed some features not been Unloaded in the right conditions
- Fixed labels in the Reporting Bugs config

## v0.8.0.4
- Fixed PostAddRecipes used instead of PostSetupRecipes
- Fixed Typos in ReportingBugs

## v0.8.0.3
- Ported to tML v2025.01
- Fixed Craft Stack not able to load

## v0.8.0.2
- Fixed Craft Stack not properly applying material reductions

## v0.8.0.1
- Fixed Recipe filters not directly visible when loading a world
- Updated Spiky's Lib dependency to v1.3.1.1

## v0.8
- Added Equipped items to available materials
- Added Recipe in Tooltip
- Added Remember Recipe List position
- Added Focus Selected Recipe Button
- Added Pickup to Void Bag First
- Added Auto Open Recipe List
- Improved Tooltip Hover
- Reworked Better Guide
- Reworked Recipes Filters and Available Recipes
- Updated Spiky's Lib dependency to v1.3.1

## v0.7
- Added an options to keep swapped items favorited
- Added the number of visible recipes to the recipe list
- Added a recipe search bar
- Prevented the offset of the recipes when at the bottom of the recipe list
- Prevented the recipe list from closing on its own
- Added grab bag drops in their tooltip
- Added scrollable tooltips
- Added a hotkey to hover tooltips
- Added available materials display
- Added weapon ammo display
- Added Fixed item tooltip position
- Reworked RecipeFilters to use UIElements
- Updated recipe filter icons
- Updated Spiky's Lib dependency to v1.3
- Fixed available materials and CraftStack line displayed on incorrect items

## v0.6.1.3
- Fixed guideTile saving as lens when empty

## v0.6.1.2
- Fixed guideTile not working for condition items
- Fixed guideTile been saved as a lens

## v0.6.1.1
- Added recipe count on Available recipes toggle
- Fixed MoreRecipes always been active
- Fixed been able to craft with a guideTile
- Fixed CraftInMenu and FavoriteRecipe been broken when MoreRecipes was disabled
- Fixed required items positions when guideTile was disabled
- Fixed the recipe list been empty when Favoring an recipe and having a guideTile
- Fixed QuickSearch failing when searching a crafting stations and guideTile is disabled
- Fixed CraftInMenu visibility taken into account when disabled
- Fixed Recipes not updating when change the BetterGuide settings

## v0.6.1
- Updated SPIC dependency to v4.0
- Updated Spiky's Lib dependency to v1.2
- Updated assets loading
- Fixed a bug with Quick move when HotkeyMode was set to FromEnd
- Fixed Craft Stack not working for items with no SPIC requirements
- Fixed Craft Stack crafting items stack greater than maxStack

## v0.6
- Renamed Smart Pickup to Previous Slot
- Grouped pickup related features in Smart Pickup
- Added Hotbar Slots Last in Smart Pickup
- Added Upgrade Items in Smart Pickup
- Added more options to Previous Slot's item display
- Added Spy's Infinite Consumables Requirement to Craft Stack
- Added the Reporting Bug config
- Added Better Shift Click settings
- Remade Quick List and Search Item
- Improved Quick Move under the hood
- Updated dependencies
- Updated Craft cursor icon
- Simplified notifications
- Fixed a bug with Previous Slot
- Fixed FixedUI ScrollButtons
- Fixed some weird behaviour with GuideTile

## v0.5.1.3
- Fixed SmartConsumption not consuming mouse item
- Fixed recipes not update with SmartPickup

## v0.5.1.2
- Fixed Mouse item not been consumed when crafting
- Fixed Recipe groups materials not been displayed as owned
- Fixed the Accept condition on Armor SubInventories

## v0.5.1.1
- Fixed Extra Item Right Click bug in Multiplayer

## v0.5.1
- Added SubInventories ordering
- Added Spiky's Lib mod dependency

## v0.5
- Added the Mod Compatibility Config
- Added an option to sensitive parts of the mod from loading at all
- Added a Compatibility layer to disable part of the mods when they fail loading
- Added Unfavorite favorite recipes on craft
- Added smart consumption for materials
- Added a option for max amount of crafted or bought items with left click
- Auto generation of Builder Accessories keybinds
- Improved recipe filters display (again)
- Reorganized some options in the configs
- Added full feature list on the homepage
- Replaced string with TileDefinition when possible
- Fixed favorite items in banks not been saved
- Fixed crash on load with Better Game UI
- Fixed bugs with shops
- Fixed some weird behaviour with GuideTile

## v0.4.1
- Optimized Better Guide and Recipe Filtering
- Optimized Quick Move display
- Clear marks when changing worlds
- Fixed Unknown items able to be favorited
- Fixed Quick move changing selected slot when it should not

## v0.4
- Replaced chat message with notification
- Added Chat message to Version Config
- Displays Smart pickup ghosts
- Allows items to have ghosts in multiple slots
- Improved SmartPickup and AutoEquip pickup order
- Removes Smart Pickup on Shift Click
- Fast Container Opening now applies to the Extractinator
- Improved recipe filters display
- Fixed recipe scroll SFX
- Added Shift Right click override
- Added click overrides for shops
- Added options to invert Crafting override clicks
- Added item stacking for shops and trash
- Added More Better Bestiary and Guide settings
- Added Search History for Guide and Bestiary
- Fixed guideTile applied when disabled
- Fixed Guide Item removed when searching the same item
- Added Different Quick Move hotkey layouts
- Displays the hotkey and highlights the slots where an item can be quick moved to
- Fixed Quick Moving accessories keeping the effect of some items
- Fixed Quick Move and Smart Pickup losing modded item data
- Fixed Quick move changing the favorite state of items
- Fixed others bugs

## v0.3
- Added Smart Pickup
- Small Config label / tooltip changes
- Searching guideItem now properly swaps with guideTile if possible
- Fixed bugged drops in multiplayer by recoding ModsItems
- Fixed displayed requiredTiles when no recipes are available

## v0.2.1.1
- No Auto Equips when transferring items from inside your inventory (e.g. Accessories -> Inventory)
- Fixed modded accessory slots causing a free when picking up items
- Fixed modded accessory slots Quick Move

## v0.2.1
- Added Version config
- Added on-update chat message
- Fixed a possible endless loop when picking up an item
- Fixed an error when guideTile is a tile and not an item			
- Fixed a null reference error when trying to open personal containers
- Fixed Recipe filters doing nothing
- Fixed some items not been counted as known
- Fixed Quick Move chain not ending when closing the inventory
- Fixed recipes sometimes not been updated when guideTile changes
- Fixed guideItem duplication when saving configs
- Fixed weird behavior with Quick Move

## v0.2
- Auto Equip items
- Favorite items in Personal Storages
- Fixed Quick Move losing item prefix

## v0.1
- Initial Pre-Release