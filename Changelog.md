# Better Inventory changelog

## 0.8
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

## 0.7
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