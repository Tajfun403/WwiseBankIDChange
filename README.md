# WwiseBankIDChange
## Overview
This tool can automatically change every event (and sound) ID in a Wwise bank from ME2, LE2 or LE3.

As audio events are referred in the game by their global ID - not directly associated with the bank they come from - this is needed when replacing sound effects to avoid overriding vanilla sounds.

Otherwise, modified audio tracks, even in a theoretically renamed bank, would still override original audio tracks, as their global personal IDs would not be changed.

## Disclaimer
This guide focuses on reusing and editing an existing bank. This might be necessary if you need to reuse some of its events which you cannot recreate. Otherwise, if you only need to add a single sound effect, you might want to look into creating a new bank instead through a different method.

## How to use
This guide assumes you have basic knowledge of Package Editor tool from the [Legendary Explorer toolset](https://github.com/ME3Tweaks/LegendaryExplorer).

1. First, prepare a bank which you want to uniqueify: clone its tree, and rename both the tree and all entries within it.

   In the example, we are going to be using `Wwise_Weapon_DLC_Des_Assault.Wwise_Weapon_DLC_Des_Assault` from `SFXWeapon_DesertAssaultRifle.pcc`. I put it into a new file (drag and drop the tree to a new file -> clone with references) and renamed its entries (through metadata tab):
   <p align="center"><img alt="An image showing a tree of a WwiseBank and its WwiseEvents cloned and rename in a new file" src="https://i.imgur.com/dNEIxE7.png" width=80%></p>

2. Next, right click the bank export, then `Export...` -> `Binary data only`
   <p align="center"><img alt="An image showing Export to Binary data only right-click menu option" src="https://i.imgur.com/h6ePVUQ.png" width=80%></p>

3. Drag-and-drop the extracted bin file onto the exe file. Let it run.
   <p align="center"><img alt="An image showing how to drag and drop a file onto an executable file" src="https://i.imgur.com/x51sS2l.png" width=80%></p>

4. This will automatically update all IDs inside the bank's binary. Right click on the bank's export inside Package Editor once again, but this time choose `Import...` -> `Binary data only` and import the generated `new bank.bin` file.
5. You should now update the ID of the bank itself, by editing both the Id property from `Properties` tab, and `Chunks -> BHKD -> ID` property inside `Binary Interpreter` tab. You should edit both of those Int32 values to a new, random value. The new value needs to be the same in both places, or the audio will be silent in game.
   <p align="center"><img alt="An image showing two tabs which need to be edited" src="https://i.imgur.com/34QUgt3.png" width=80%></p>
6. The last step will be to update external references to your new bank - that is - point any old WwiseEvents to newly generated event IDs.
7. Find four last bytes of each WwiseEvent inside your bank's tree - that's the ID reference. Compare it with the old ID in generated `Diff results.txt` file. Change the old bytes to the newly assigned bytes, as stated in the file. In this example, you should replace `DF 4D 20 38` with `0E 53 03 60`. Repeat this for every WwiseEvent you are interested in.
   <p align="center"><img alt="An image showing two tabs which need to be edited" src="https://i.imgur.com/MIaoV5o.png" width=80%></p>
   
8. Done! You can now freely replace the audio tracks inside the bank.

## Other questions

### The audio is silent in game
You have forgotten some of the steps.

### Are you going to integrate this into LEX?
A bank not only defines audio events, but also some of its events can reference each other. While event defintions are readable, their references couldn't be easily parsed as of when this app was written. As such, I used a bruteforce replace - where all 4-byte patterns matching an old ID would be updated to the new randomized one. While extremely unlikely, this might result in some false positives, which LEX team deemed too risky.
Currently, different people are working on a Wwise parser which would allow a non-bruteforce solution to be created, which could then be integrated into LEX.
