# TASagent Twitch Bot

My extensible, modular C# twitch bot development framework.

## How do I use this?

At the moment, if you aren't comfortable getting your hands dirty with the source or at least json config files, this may not be the project for you.

But once configured, the project should be very simple to use.  It has a number of modular features, and it should be fast and easy to develop new ones.

## Getting Started

To start with, get the appropriate [NetCore 5 SDK (With AspNet)](https://dotnet.microsoft.com/download/dotnet/5.0)

Clone the [TASagentTwitchBotDemos project](https://github.com/TASagent/TASagentTwitchBotDemos) and initialize and update the gitsubmodule (this project):

```cmd
git clone https://github.com/TASagent/TASagentTwitchBotDemos
cd TASagentTwitchBotDemos
git submodule init
git submodule update
```

If you're using this as a twitch bot, you're going to need to make a new Twitch account for that bot.  You also need to go to [The Twitch Dev Console](https://dev.twitch.tv/console/apps) and register an application to receive a ClientID.  Enter any name, use `http://localhost:9000/` as the OAuth Redirect URL, and choose "Chat Bot" as the category.

You may need to forward ports `9000` and `9005` to the computer you intend to use to get everything to work.  Port `9000` is used for OAuth callbacks, and port `9005` is used for follower notifications.  To enable listening on these ports, you'll likely need to run the following commands in powershell in administrator mode:

```powershell
netsh http add urlacl url="http://+:9000/" user=everyone
netsh http add urlacl url="http://+:9005/" user=everyone
```

Compile and run the `BotConfigurator` program to begin setup.  You'll be prompted for several values, and it will prepare some configuration files in your `Documents/TASagentBotDemo` directory.  

### TTS

If you wish to use TTS, you'll have to create a GoogleCloud account with TTS enabled and put the credentials in `TASagentBotDemo/Config/googleCloudCredentials.json` and an AWSPolly account with its credentials stored `TASagentBotDemo/Config/awsPollyCredentials.json`.  Otherwise, you'll have to use a configuration of the bot that doesn't support TTS.  Read and understand the pricing schemes of each service.  It's unlikely one streamer would be able to use enough TTS in a single month to result leave the free tier of either service, but it's your responsibility to understand how the pricing works.

Example `googleCloudCredentials.json` file:
```json
{
  "type": "service_account",
  "project_id": "TheProjectsName",
  "private_key_id": "LONGPRIVATEID",
  "private_key": "-----BEGIN PRIVATE KEY-----\nLOTS OF PRIVATE STUFF HERE\n-----END PRIVATE KEY-----\n",
  "client_email": "examplettssystem@theprojectsname.iam.gserviceaccount.com",
  "client_id": "CLIENTID",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/examplettssystem%theprojectsname.iam.gserviceaccount.com"
}
```

Example `awsPollyCredentials.json` file:
```json
{
    "AccessKey": "AN ACCESS KEY HERE",
    "SecretKey": "A SECRET KEY HERE"
}
```

### Database

Navigate to the directory of the Bot project (where the `.proj` file lives) and create the initial database with `dotnet ef database update`.

## Running and controlling the bot

If everything has been set up properly, you should be able to launch the bot and have it connect to Chat.

Open a web browser to http://localhost:5000/API/ControlPage.html to see the control page, and enter the admin password.

Voila!

## Customizing the bot

There are several major subsystems to help streamline customization, and it's simple to create new ones.  A lot of the action takes place in the in the [Dependency Injection system](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection) that's part of ASP.NET.  Classes are registered in `TASagentTwitchBot.Web.StartupCore`, and when an instance of a class is required, it is instantiated (along with all of its constructor pre-requisites).  As a result, no _cycles_ are allowed (where Class A and Class B both require one-another).

When multiple classes are registered for an interface or class, only the last one to be registered will be instantiated.

If you wish to register a single class to handle multiple interfaces, be careful how you register it.

This block would instantiate only one instance of `Notifications.FullActivityProvider` and provide it for every listed interface:

```csharp
services
    .AddSingleton<Notifications.FullActivityProvider>()
    .AddSingleton<Notifications.ISubscriptionHandler>(x => x.GetRequiredService<Notifications.FullActivityProvider>())
    .AddSingleton<Notifications.ICheerHandler>(x => x.GetRequiredService<Notifications.FullActivityProvider>())
    .AddSingleton<Notifications.IRaidHandler>(x => x.GetRequiredService<Notifications.FullActivityProvider>())
    .AddSingleton<Notifications.IGiftSubHandler>(x => x.GetRequiredService<Notifications.FullActivityProvider>())
    .AddSingleton<Notifications.IFollowerHandler>(x => x.GetRequiredService<Notifications.FullActivityProvider>())
    .AddSingleton<Notifications.ITTSHandler>(x => x.GetRequiredService<Notifications.FullActivityProvider>());
```

Whereas this block would instantiate an independent copy of the class as the handler for each interface:

```csharp
services
    .AddSingleton<Notifications.FullActivityProvider>()
    .AddSingleton<Notifications.ISubscriptionHandler, Notifications.FullActivityProvider>()
    .AddSingleton<Notifications.ICheerHandler, Notifications.FullActivityProvider>()
    .AddSingleton<Notifications.IRaidHandler, Notifications.FullActivityProvider>()
    .AddSingleton<Notifications.IGiftSubHandler, Notifications.FullActivityProvider>()
    .AddSingleton<Notifications.IFollowerHandler, Notifications.FullActivityProvider>()
    .AddSingleton<Notifications.ITTSHandler, Notifications.FullActivityProvider>();
```

However, when a class accepts an `IEnumerable` as a constructor argument, it receives an instance of every class that's been registered as a handler.  For example, the CommandSystem has the following constructor:

```csharp
public CommandSystem(
    ICommunication communication,
    ErrorHandler errorHandler,
    IEnumerable<ICommandContainer> commandContainers)
```

So it recieves and instance of every class registered as an `ICommandContainer`:

```csharp
services
    .AddSingleton<Commands.ICommandContainer, Commands.CustomSimpleCommands>()
    .AddSingleton<Commands.ICommandContainer, Commands.SystemCommandSystem>()
    .AddSingleton<Commands.ICommandContainer, Commands.PermissionSystem>()
    .AddSingleton<Commands.ICommandContainer, Commands.ShoutOutSystem>()
    .AddSingleton<Commands.ICommandContainer, Commands.NotificationSystem>()
    .AddSingleton<Commands.ICommandContainer, Quotes.QuoteSystem>()
    .AddSingleton<Commands.ICommandContainer, TTS.TTSSystem>()
    .AddSingleton<Commands.ICommandContainer, Commands.TestCommandSystem>();
```

However, classes are (deliberately) not automatically registered as the handler for all of their interfaces.  If you created a class called `ExampleClass` that implements `Commands.ICommandContainer`, and in the `Startup` class you called `services.AddSingleton<ExampleClass>()`, then `ExampleClass` would _not_ be passed to the `CommandSystem`.

### Commands

New commands just need to extend the `ICommandSystem` interface, and get registered to that interface in the `Startup` class.

### Channel Point Redemptions

### Notifications

In OBS, add the following four BrowserSources

`http://localsource:5000/BrowserSource/overlay.html` - Image notifications
`http://localsource:5000/BrowserSource/controllerSpy.html` - SNES NintendoSpy Overlay
`http://localsource:5000/BrowserSource/timer.html` - Timer overlay
`http://localsource:5000/BrowserSource/ttsmarquee.html` - Scrolling TTS Marquee
