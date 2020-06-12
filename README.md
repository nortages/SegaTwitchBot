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
Firstly, you need to create an account for your future bot if you want to set him its own name. Then you should go [here](https://twitchtokengenerator.com/), specify scopes you need and get the access token, refresh token, and client id. Bot will have the name of account through which you get the credentials. So that your bot can give timeouts, you're gonna need moderator rights on the channel where the bot will be.
If you have any questions about TwitchLib or Twitch API you may ask them in [heir Discord channel](https://discord.gg/8NXaEyV).
  
# Deployment on Heroku
To deploy this bot on Heroku firstly it is necessary to add a custom buildpack for dotnet apps. I used [this one](https://github.com/jincod/dotnetcore-buildpack.git). To rewrite the default commands that's added by the buildpack you need to create your own Procfile (use *worker* instead of *web*). If it adds its command to yours, then try [this](https://github.com/jincod/dotnetcore-buildpack/issues/111#issuecomment-643242377).
