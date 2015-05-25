using System.Windows.Forms;
using xServer.Core;
using Microsoft.Win32;

namespace xServer.Forms
{
    public partial class FrmRegistryEditor : Form
    {
        // Notes: Use Microsoft.Win32.RegistryHive as a part of the packet to and from
        //        the client to represent the referenced top-level node.

        // To-Do: Migrate this code to a RegistryHelper.cs class.

        private readonly Client _connectClient;

        private RegistryHive currentClientHive = RegistryHive.ClassesRoot;

        public FrmRegistryEditor(Client c)
        {
            _connectClient = c;
            _connectClient.Value.FrmRe = this;

            InitializeComponent();

            // Request the top-level node being referenced.
            RegistryKey key = Registry.CurrentUser;
            LoadSubKeyDirectories(key);
        }

        private void LoadSubKeyDirectories(RegistryKey key, int depth = 4)
        {
            LoadSubKeyDirectories(key, string.Empty, depth);
        }

        private void LoadSubKeyDirectories(RegistryKey key, string lastSubKeyName, int depth = 4)
        {
            if (key != null && depth > 0)
            {
                try
                {
                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey SubKey = key.OpenSubKey(subKeyName))
                            {
                                // The lastSubKeyName is empty, so it must be a root node.
                                if (string.IsNullOrEmpty(lastSubKeyName))
                                {
                                    tvRegistryDirectory.Nodes.Add(ParsedKeyNameNodes(SubKey.Name));
                                }
                                // It is a sub-key.
                                else
                                {
                                    tvRegistryDirectory.Nodes.Add(ParsedKeyNameNodes(key.Name));
                                }

                                // Load more sub-key directories, but with decrementing levels of depth.
                                LoadSubKeyDirectories(SubKey, depth--);
                            }
                        }
                        catch
                        { }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Parses a registry key name (as it is similar to a path) and returns the corresponding tree nodes.
        /// </summary>
        /// <param name="keyName">The name of the registry key</param>
        /// <returns>A collection of the parsed registry key nodes.</returns>
        private TreeNode ParsedKeyNameNodes(string keyName)
        {
            if (!string.IsNullOrEmpty(keyName) && keyName.Contains("/"))
            {
                string[] KeyPaths = keyName.Split('/');

                TreeNode ParsedTreeNodes = new TreeNode();

                for (int i = 0; i < KeyPaths.Length; i++)
                {
                    if (!string.IsNullOrEmpty(KeyPaths[i]))
                    {
                        ParsedTreeNodes.Nodes[i].Nodes.Add(KeyPaths[i]);
                    }
                }

                return ParsedTreeNodes;
            }
            else
            {
                return new TreeNode(keyName);
            }
        }
    }
}