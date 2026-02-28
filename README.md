# 🔦 Flashlight

Flashlight is a plugin for Counter-Strike 2 that adds a flashlight feature for players. It is written in C# and uses the CounterStrikeSharp API. 🎮

## ⭐ Features

- 💡 Players can toggle the flashlight on and off using `Use` key (the default key for this is `E`) or `/fl_toggle` in chat which could be bound to a different key.
- 💀 The flashlight is automatically turned off when the player dies or respawns.
- 🚫 The flashlight is only available to human players, not bots.

## 🔧 Installation

1. ⬇️ Download the latest release from the GitHub repository.
2. 📁 Extract the ZIP file.
3. 📂 Place the plugin in the `game/csgo/addons/counterstrikesharp/plugins/Flashlight` directory.

## 💻 Usage

⌨️ Use the `Use` key to toggle the flashlight on and off. The default key for this is `E`. Or use the `/fl_toggle` command in chat which could then be bound to a different key.

Example bind:
```
bind f "css_fl_toggle"
```

## 🛠️ Development

### Prerequisites

- .NET 8.0 SDK
- CounterStrikeSharp API v1.0.363+

### Building

```bash
dotnet restore
dotnet build
```

### Testing

```bash
dotnet test
```

## 🤝 Contributing

Contributions are welcome. Please open an issue or submit a pull request on GitHub. 🐙

## 📋 Changelog

### v0.0.6 (Latest)
- ✅ Updated to .NET 8.0
- ✅ Updated to CounterStrikeSharp.API v1.0.363
- ✅ Added xUnit test project with core logic tests
- ✅ Updated GitHub Actions workflow with testing
- ✅ Improved code compatibility with latest CS2/CSS API

### v0.0.5
- Initial release

## 📃 License

This project is licensed under the GNU General Public License. ⚖️

## ✏️ Author

This project was created by [creazy.eth](https://github.com/creazy231)