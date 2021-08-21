﻿using System;
using Microsoft.Extensions.DependencyInjection;

namespace TASagentTwitchBot.Core.Chat
{
    public interface IChatMessageHandler
    {
        void HandleChatMessage(IRC.IRCMessage message);
    }

    public class ChatMessageHandler : IChatMessageHandler
    {
        private readonly Config.BotConfiguration botConfig;

        private readonly ICommunication communication;
        private readonly Notifications.ICheerHandler cheerHandler;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly IBanHandler banHandler;

        public ChatMessageHandler(
            Config.BotConfiguration botConfig,
            ICommunication communication,
            Notifications.ICheerHandler cheerHandler,
            IServiceScopeFactory scopeFactory,
             IBanHandler banHandler
)
        {
            this.botConfig = botConfig;

            this.communication = communication;
            this.cheerHandler = cheerHandler;

            this.scopeFactory = scopeFactory;
            this.banHandler = banHandler;

        }

        public virtual async void HandleChatMessage(IRC.IRCMessage message)
        {
            if (message.ircCommand != IRC.IRCCommand.PrivMsg && message.ircCommand != IRC.IRCCommand.Whisper)
            {
                communication.SendDebugMessage($"Error: Passing forward non-chat message:\n    {message}");
                return;
            }


            IRC.TwitchChatter chatter = await IRC.TwitchChatter.FromIRCMessage(message, communication, scopeFactory);

            if (chatter == null)
            {
                return;
            }

            communication.DispatchChatMessage(chatter);

            if (chatter.Bits != 0 && chatter.Bits >= botConfig.BitTTSThreshold)
            {
                cheerHandler.HandleCheer(chatter.User, chatter.Message, chatter.Bits, true);
            }
            if (banHandler.CheckOrUpdateBanRulesExist())
            {
                await banHandler.HandlePossibleUserBan(chatter);
            }
        }
    }
}
