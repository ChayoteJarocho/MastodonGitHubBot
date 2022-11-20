# MastodonGitHubBot

A bot that retrieves info from GitHub and publishes it in Mastodon.

Prerequisites:

- Get .NET 7.0: https://dotnet.microsoft.com/en-us/download/dotnet/7.0
- Choose a name for your bot app.
- Register the bot app under your GitHub account: https://github.com/settings/applications/new
- Save, then copy the ClientId of your app.
- Click on "Generate a new client secret", then copy the generated secret.
- Build the project.
- Open the `bin\{Configuration}\net7.0\appsettings.json` file and then:
    - Change `AppName` to the value you chose.
    - Paste the ClientId in `GitHubClientId`.
    - Paste the Secret in `GitHubSecret`.
    - Set the value of `GitHubUserName` to your GitHub handle with which the bot will perform requests.
    - Set the name of the Mastodon server where your account is hosted.
- Run the app.
- Follow the instructions to generate the access tokens for GitHub and Mastodon. They will get stored automatically in `appsettings.json`.
