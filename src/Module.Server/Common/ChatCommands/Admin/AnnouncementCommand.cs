﻿using Crpg.Module.Common.Network;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common.ChatCommands.Admin;

internal class AnnouncementCommand : AdminCommand
{
    public AnnouncementCommand(ChatCommandsComponent chatComponent)
        : base(chatComponent)
    {
        Name = "a";
        Description = $"'{ChatCommandsComponent.CommandPrefix}{Name} message' to send an admin announcement.";
        Overloads = new CommandOverload[]
        {
            new(new[] { ChatCommandParameterType.String }, ExecuteAnnouncement),
        };
    }

    private void ExecuteAnnouncement(NetworkCommunicator fromPeer, object[] arguments)
    {
        string message = (string)arguments[0];

        foreach (NetworkCommunicator targetPeer in GameNetwork.NetworkPeers)
        {
            if (!targetPeer.IsServerPeer && targetPeer.IsSynchronized)
            {
                GameNetwork.BeginModuleEventAsServer(targetPeer);
                GameNetwork.WriteMessage(new CrpgNotification
                {
                    Type = CrpgNotificationType.Announcement,
                    Message = message,
                });
                GameNetwork.EndModuleEventAsServer();
            }
        }
    }
}
