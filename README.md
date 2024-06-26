# MastodonGitHubBot

A bot that retrieves info from GitHub and publishes it in Mastodon.

Prerequisites:

- Get .NET 8.0: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
- Choose a name for your bot app and register it under your GitHub account: https://github.com/settings/applications/new
- Save the app, then copy the ClientId of your app, you'll need it later.
- Click on "Generate a new client secret", then copy the generated secret, you'll need it later.
- Clone, build and run this project.
- Follow the instructions to generate the access tokens for GitHub and Mastodon. They will get stored automatically under `bin\<Configuration>\net8.0\appsettings.json`.
  - For Mastodon, make sure you add these two permissions: `read:statuses` and `write:statuses`.
- On Windows, run the tool as administrator to ensure event logging works.

Note: The `Debug` value in `appsettings.json` is set to `true` by default, meaning it will initially only print to console and won't publish toots to Mastodon. You need to set it to `false` to start tooting.

