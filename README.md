# PlayerTrakkar

## Description
This Gorilla Tag mod sends a message to a specified Discord webhook when players wearing specific cosmetics, such as the Finger Painter Badge or the Illustrator Badge, join a lobby. This can be useful for tracking players with unique cosmetics in public or private lobbies.

## Features
- Detects when a player joins the lobby wearing a specified cosmetic.
- Sends a message to a configured Discord webhook with the player's information.
- Supports multiple cosmetics for detection.
- Lightweight and efficient, with minimal performance impact.

## Installation
1. Ensure you have BepInEx installed for Gorilla Tag.
2. Download the latest release of the mod.
3. Place the `.dll` file into the `BepInEx/plugins` folder.
4. Configure the mod by editing the provided configuration file.

## Configuration
A configuration file will be generated upon first launch in the `BepInEx/config` directory. Edit this file to:
- Set the Discord webhook URL.
- Specify which cosmetics should trigger the notification.

## Usage
1. Start Gorilla Tag with the mod installed.
2. When a player with a tracked cosmetic joins, a message will be sent to the configured Discord webhook.
3. Monitor the webhook to receive real-time updates.

## Notes
- This mod does not send any personal or private data beyond the player's cosmetic information and name.
- It does not interact with the game's networking or matchmaking systems.

## Disclaimer
This mod is not affiliated with Another Axiom or Gorilla Tag. Use it at your own risk. Ensure compliance with Gorilla Tag's modding policies when using this mod in public lobbies.
