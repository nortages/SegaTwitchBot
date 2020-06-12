# NortagesTwitchBot
This is a twitch bot - **NortagesBot**. He is runnig on the K_i_ra channel. He handles timeouts for channel points and a quiz about guessing the streamer's rating.

## His commands:
- !начатьголосование - starts the quiz;
- !ммр <num> - to vote for rating;
- !закончитьголосование - closes the quiz;
- !показатьрезультат <result> - showes who have been closer to the result;
- !залславы - showes top-3 winners of the month;
- !залславы фулл - sends a link to all winners of the month.
# Deployment on Heroky
  To deploy this bot on Heroku firstly it is necessary to add a custom buildpack. I used [this one](https://github.com/jincod/dotnetcore-buildpack.git). To rewrite the default commands that's added by the buildpack you need to create your own Procfile. If it adds its command to yours, then try [this](https://github.com/jincod/dotnetcore-buildpack/issues/111#issuecomment-643242377).
