﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace TASagentTwitchBot.Core.Commands
{
    public class NotificationSystem : ICommandContainer
    {
        private readonly ICommunication communication;
        private readonly Notifications.IActivityDispatcher activityDispatcher;
        private readonly IMessageAccumulator messageAccumulator;

        public NotificationSystem(
            ICommunication communication,
            Notifications.IActivityDispatcher activityDispatcher,
            IMessageAccumulator messageAccumulator)
        {
            this.communication = communication;
            this.activityDispatcher = activityDispatcher;
            this.messageAccumulator = messageAccumulator;
        }

        public void RegisterCommands(
            Dictionary<string, CommandHandler> commands,
            Dictionary<string, HelpFunction> helpFunctions,
            Dictionary<string, SetFunction> setFunctions)
        {
            commands.Add("replay", ReplayNotification);
            commands.Add("skip", SkipNotification);

            commands.Add("approve", (chatter, args) => HandlePendingTTS(chatter, args, true));
            commands.Add("accept", (chatter, args) => HandlePendingTTS(chatter, args, true));
            commands.Add("deny", (chatter, args) => HandlePendingTTS(chatter, args, false));
            commands.Add("reject", (chatter, args) => HandlePendingTTS(chatter, args, false));
        }

        public IEnumerable<string> GetPublicCommands()
        {
            yield break;
        }

        /// <summary>
        /// Replay a notification
        /// </summary>
        private Task ReplayNotification(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
            {
                communication.SendPublicChatMessage($"You are not authorized to replay notifcations, @{chatter.User.TwitchUserName}.");
                return Task.CompletedTask;
            }
            
            if (remainingCommand.Length > 1)
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, incorrectly formatted replay request.");
                return Task.CompletedTask;
            }

            int replayIndex = -1;
            if (remainingCommand.Length == 1)
            {
                if (!int.TryParse(remainingCommand[0], out replayIndex))
                {
                    communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unable to parse replayIndex \"{remainingCommand[0]}\".");
                    return Task.CompletedTask;
                }
            }

            if (!activityDispatcher.ReplayNotification(replayIndex))
            {
                if (replayIndex == -1)
                {
                    communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no notifications to replay.");
                }
                else
                {
                    communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unable to replay notification {replayIndex}.");
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// End current notification
        /// </summary>
        private Task SkipNotification(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
            {
                communication.SendPublicChatMessage($"You are not authorized to do that, @{chatter.User.TwitchUserName}.");
                return Task.CompletedTask;
            }

            activityDispatcher.Skip();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handle accepting or rejecting pending TTS
        /// </summary>
        private Task HandlePendingTTS(IRC.TwitchChatter chatter, string[] remainingCommand, bool accept)
        {
            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
            {
                communication.SendPublicChatMessage($"You are not authorized to replay notifcations, @{chatter.User.TwitchUserName}.");
                return Task.CompletedTask;
            }

            if (remainingCommand.Length != 1)
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, incorrectly formatted {(accept ? "accept" : "reject")} request.");
                return Task.CompletedTask;
            }

            if (!int.TryParse(remainingCommand[0], out int updateIndex))
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unable to parse updateIndex \"{remainingCommand[0]}\".");
                return Task.CompletedTask;
            }

            bool success = activityDispatcher.UpdatePendingRequest(updateIndex, accept);
            if (success)
            {
                messageAccumulator.RemovePendingNotification(updateIndex);
            }
            else
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unable to {(accept ? "accept" : "reject")} TTS {updateIndex}.");
            }

            return Task.CompletedTask;
        }
    }
}