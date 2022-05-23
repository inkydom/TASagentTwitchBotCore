﻿namespace TASagentTwitchBot.Plugin.TTTAS;

public interface ITTTASHandler
{
    void HandleTTTAS(Core.Database.User user, string message, bool approved);
}

public class TTTASHandler : ITTTASHandler
{
    private readonly Core.ICommunication communication;
    private readonly Core.Notifications.IActivityDispatcher activityDispatcher;
    private readonly Core.Notifications.IActivityHandler activityHandler;
    private readonly Core.Audio.ISoundEffectSystem soundEffectSystem;
    private readonly ITTTASRenderer tttasRenderer;

    private readonly TTTASConfiguration tttasConfig;

    public TTTASHandler(
        Core.ICommunication communication,
        Core.Notifications.IActivityDispatcher activityDispatcher,
        Core.Notifications.IActivityHandler activityHandler,
        Core.Audio.ISoundEffectSystem soundEffectSystem,
        ITTTASRenderer tttasRenderer,
        TTTASConfiguration tttasConfig)
    {
        this.communication = communication;
        this.activityDispatcher = activityDispatcher;
        this.activityHandler = activityHandler;
        this.soundEffectSystem = soundEffectSystem;
        this.tttasRenderer = tttasRenderer;
        this.tttasConfig = tttasConfig;
    }

    public async void HandleTTTAS(
        Core.Database.User user,
        string message,
        bool approved)
    {
        activityDispatcher.QueueActivity(
            activity: new TTTASActivityRequest(
                activityHandler: activityHandler,
                description: $"{tttasConfig.FeatureNameBrief} {user.TwitchUserName}: {message}",
                audioRequest: await GetTTTASAudioRequest(user, message),
                marqueeMessage: new Core.Notifications.MarqueeMessage(user.TwitchUserName, message, user.Color)),
            approved: approved);
    }

    private async Task<Core.Audio.AudioRequest?> GetTTTASAudioRequest(Core.Database.User _, string message)
    {
        Core.Audio.AudioRequest? soundEffectRequest = null;
        Core.Audio.AudioRequest? tttasRequest = null;

        if (!string.IsNullOrEmpty(tttasConfig.SoundEffect) && soundEffectSystem.HasSoundEffects())
        {
            Core.Audio.SoundEffect? tttasSoundEffect = soundEffectSystem.GetSoundEffectByName(tttasConfig.SoundEffect);
            if (tttasSoundEffect is null)
            {
                communication.SendWarningMessage($"Expected {tttasConfig.FeatureNameBrief} SoundEffect \"{tttasConfig.SoundEffect}\" not found.  Defaulting to first sound effect.");
                tttasSoundEffect = soundEffectSystem.GetAnySoundEffect();
            }

            if (tttasSoundEffect is not null)
            {
                soundEffectRequest = new Core.Audio.SoundEffectRequest(tttasSoundEffect);
            }
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            tttasRequest = await tttasRenderer.TTTASRequest(
                tttasText: message);
        }

        return Core.Audio.AudioTools.JoinRequests(300, soundEffectRequest, tttasRequest);
    }

    public class TTTASActivityRequest : Core.Notifications.ActivityRequest, Core.Notifications.IAudioActivity, Core.Notifications.IMarqueeMessageActivity
    {
        public Core.Audio.AudioRequest? AudioRequest { get; }
        public Core.Notifications.MarqueeMessage? MarqueeMessage { get; }

        public TTTASActivityRequest(
            Core.Notifications.IActivityHandler activityHandler,
            string description,
            Core.Audio.AudioRequest? audioRequest = null,
            Core.Notifications.MarqueeMessage? marqueeMessage = null)
            : base(activityHandler, description)
        {
            AudioRequest = audioRequest;
            MarqueeMessage = marqueeMessage;
        }
    }
}