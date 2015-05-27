﻿using ProtoBuf;

namespace xClient.Core.Packets.ServerPackets
{
    [ProtoContract]
    public class RemoveStartupItem : IPacket
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public string Path { get; set; }

        [ProtoMember(3)]
        public int Type { get; set; }

        public RemoveStartupItem()
        {
        }

        public RemoveStartupItem(string name, string path, int type)
        {
            this.Name = name;
            this.Path = path;
            this.Type = type;
        }

        public void Execute(Client client)
        {
            client.Send(this);
        }
    }
}