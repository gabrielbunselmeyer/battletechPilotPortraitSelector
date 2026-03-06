# Era's Pilot Portrait Selector
In-game pilot selector mod for Battletech.

https://github.com/user-attachments/assets/3f3eb9f8-4c38-4b4f-94ec-c4d48b3b7cb2

### Usage
1. Go to the Barracks.
2. Ctrl + Click on the card of the pilot you want to edit.
3. ???
4. Profit.

### Adding Portraits
You should use [CommanderPortraitLoader](https://github.com/BattletechModders/CommanderPortraitLoader); it's installed by default with BTA and Roguetech.

1. Find the CommanderPortraitLoader mod folder, usually `...\BATTLETECH\Mods\CommanderPortraitLoader`.
2. Go into `/CommanderPortraitLoader/Portraits`
3. Paste your pictures in there. 

And that's it. Works with both *.dds and .png*.

_Technically_ you don't actually need CommanderPortraitLoader, and creating a folder inside this mod's own folder will work: `ErasPortraitSelector/Portraits`. But there's no reason to not be playing with CPL. So do that.

### Known Issues
- Portrait changes are "session-only" until you save and re-load, so it's possible that, in certain situations such as events and the timeline, the old portrait will still be shown.
  Reloading fixes that as the new portrait will be serialized into the save and properly loaded everywhere (I didn't want to do even MORE patches).
