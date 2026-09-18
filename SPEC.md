# Project Setup, General Instructions, and Resources

## Hex of Steel
This project is to make a game mod for the game Hex of Steel.
Sometimes Hex of Steel is shortened to HoS.
This is the URL for the game: https://hex-of-steel.fr/

This is the manual for playing the game. It is probably not that important to you: https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/1240630/manuals/1e929c3c9d09670cb450bdf6272b9522dfb4bc5b/Manual.pdf?t=1761383423

## Modding Hex of Steel
There is no written instructions on how to mod Hex of Steel
There are YouTube videos on how to mod Hex of Steel.
This is the YouTube video is for basic, none code modding: https://www.youtube.com/watch?v=sGADvjll8pU
This is the YouTube video for advanced, code moding. This is probably the most important one: https://www.youtube.com/watch?v=5ckaIcOEZww
There is a steam community forum for modding HoS which might be useful.
https://steamcommunity.com/app/1240630/discussions/2/598538723409482593/

## LLM Support
This mod will likely require an LLM. Set things up so that the user of the mod can connect their own LLM.
Also leave open the possiblity to add a local free LLM in the future. Perhaps they could work together.

## Compatibilty

Version 8.0.0 of the game at least is required since moding the code was not supported before that.

## Installation

This mod will be released through the Steam workshop and follow the standard way mods are distributed and installed for HoS.

# Mod Objective

This mod is primarily targeted at PBEM games, but will work just as well in single player games.

There are two main goals of this mod.

1. Capture all the details of each turn in a file. This file will accumulate all the details for the entire game.
2. When the game ends, a narrative of the game will be created based on what happened.

## Capturing Details

Turn by turn, country by country, document all events.
At a minimum include these items:
- the turn number
- the in game date
- the real world date and time the turn took place
- when new equipment becomes available to a nation
- what a nation sets a policy
- when a nation joins a faction
- when a nation sends money to another nation
- when units earn a new rank
- when a nation declares war
- when a nation capituates
- include any and all diplomatic events
- when a unit destroys another unit
- when a hero emerges
- when a victory point is captured and lost
- the objective count for each nation
- the weather and the forecast

Include a summary of what the nation accomplished or failed to accomplish that turn.

This document of events and summary will be saved along with the saved game.
The file(s) will be properly named so that it is easy to identify what it is and which saved game it is for
There should be user interface in the game that allows the player to view the details file easily.

## Reloads and Alternate Saves

If a player tries to reload a cgame and make an alternate save, the game should warn them of the consequences of doing that.
It should give them the option to cancel.
If they proceed, wipe out the old information and replace it with the new turn information.

### PBEM History Transfer Undefined

For now, just have this mod run for one player.
This means the events from the other players will be missing.
Once we have this working, we can establish

### Player Cheating

With the current design, it is possible for players to use the detailed event document to by pass the fog of war.
This is acceptable for now.
In the next release we will tackle this problem.

# End Game Narrative

Using the detailed information collected during the game, create a narrative that reads like a completing history of what happened in that period. 

Indicate how the game ended.  Was it a victory, surrender or was it just abandoned.

The history must be ground to factual claims in recorded events and distinguish interpretation from facts.
For normal sized games, keep the narrative to a page or two.
For very long games with many nations, you can great a much later document, but break things out into sections

Format this as a PDF document.

Style it like a nice, formal yet readable history document.

Include a copyright for me and my King Tiger branding.

Include images, colours and maps as appropriate.

Mail a copy of this document to all the players of the game.

# Failure Behavior

It is important to not lose information.
In the event of failure of any service or code, still record the events.
Include the failure int he events.

# Email Addresses

The email of each player should come from the game.  
The HoS PBEM knows who each player is and how to contact them.
If this is not possible, raise this as an issue and an alternative solution will be found.