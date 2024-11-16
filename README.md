# BlueskyGitHubBot

A bot that retrieves info from GitHub and publishes it in Bluesky.

Prerequisites:

- Get .NET 9.0: https://dotnet.microsoft.com/en-us/download/dotnet/9.0
- Choose a name for your bot app and register it under your GitHub account: https://github.com/settings/applications/new
- Save the app, then copy the ClientId of your app, you'll need it later.
- Click on "Generate a new client secret", then copy the generated secret, you'll need it later.
- Clone, build and run this project.
- Follow the instructions to generate the access token for GitHub. It will get stored automatically under `bin\<Configuration>\net8.0\appsettings.json`.
- On Windows, run the tool as administrator to ensure event logging works.

Note: The `Debug` value in `appsettings.json` is set to `true` by default, meaning it will initially only print to console and won't publish skeets to Bluesky. You need to set it to `false` to start skeeting.

