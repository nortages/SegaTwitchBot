# NortagesTwitchBot
This is a twitch bot - **NortagesBot**. He is runnig on the K_i_ra channel. He handles timeouts for channel points and a quiz about guessing the streamer's rating. He is made by using [TwitchLib](https://github.com/TwitchLib).

## His commands:
- !начатьголосование - starts the quiz;
- !ммр <num> - to vote for rating;
- !закончитьголосование - closes the quiz;
- !показатьрезультат <result> - showes who have been closer to the result;
- !залславы - showes top-3 winners of the month;
- !залславы фулл - sends a link to all winners of the month.
  
# Getting Started
Firstly, you need to create an account for your future bot if you want to set him its own name. Then you should go [here](https://twitchtokengenerator.com/), specify scopes you need and get the access token, refresh token, and client id. Also my bot uses Google Sheet API and JSONbin API to store information about the quiz, so you also need to provide GoogleApiKey and JsonBinSecret. All these credentials you have to store in the config.json at the root of the project if you start the bot locally or in the Config Vars if it has been deployed to Heroku. So that your bot can give timeouts, you're gonna need moderator rights on the channel where the bot will be.
## Example of config.json
```json
{
    "BotToken": "YourBotToken",
    "BotUsername": "YourBotUsername(shows in the logs)",
    "RefreshToken": "YourBotRefreshToken",
    "ChannelName": "ChannelWhereBotWillBe",
    "ClientID": "YourBotClientID",
    "JsonBinSecret": "YourJsonBinSecret",
    "GoogleApiKey": "YourGoogleApiKey"
}
```

## Notes
- Bot will have the name of account through which you get the credentials. The name can't be set through code.
- If you have any questions about TwitchLib or Twitch API you may ask them in [their Discord channel](https://discord.gg/8NXaEyV).
  
# Deployment on Heroku
To deploy this bot on Heroku, firstly, it is necessary to add a custom buildpack for dotnet apps. I used [this one](https://github.com/jincod/dotnetcore-buildpack.git). To rewrite the default commands that's added by the buildpack you need to create your own Procfile (use *worker* instead of *web*). If it adds its command to yours, then try [this](https://github.com/jincod/dotnetcore-buildpack/issues/111#issuecomment-643242377).
