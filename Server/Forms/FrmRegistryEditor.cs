using System;
using System.Collections.Generic;
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
                                    TreeNode NewNode = ParsedKeyNameNodes(SubKey.Name);

                                    if (NewNode != null)
                                    {
                                        if (tvRegistryDirectory.Nodes.ContainsKey(NewNode.Text))
                                        {
                                            tvRegistryDirectory.Nodes[tvRegistryDirectory.Nodes.IndexOfKey(NewNode.Text)].Nodes.Add(NewNode);
                                        }
                                        else
                                        {
                                            tvRegistryDirectory.Nodes.Add(NewNode.Text, NewNode.Text);
                                        }
                                    }
                                }
                                // It is a sub-key.
                                else
                                {
                                    TreeNode NewNode = ParsedKeyNameNodes(key.Name);

                                    if (NewNode != null)
                                    {
                                        tvRegistryDirectory.Nodes.Add(NewNode);

                                        if (tvRegistryDirectory.Nodes.ContainsKey(NewNode.Text))
                                        {
                                            tvRegistryDirectory.Nodes[tvRegistryDirectory.Nodes.IndexOfKey(NewNode.Text)].Nodes.Add(NewNode);
                                        }
                                        else
                                        {
                                            tvRegistryDirectory.Nodes.Add(NewNode.Text, NewNode.Text);
                                        }
                                    }
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
            if (!string.IsNullOrEmpty(keyName))
            {
                // Contains "\\". It must be a sub-node.
                if (keyName.Contains("\\"))
                {
                    string[] KeyPaths = keyName.Split('\\');

                    // Not a valid TreeNode if the root node has no name!
                    if (string.IsNullOrEmpty(KeyPaths[0]))
                    {
                        return null;
                    }

                    TreeNode ParsedTreeNodes = new TreeNode(KeyPaths[0]);

                    // The loop below assumes that the length of KeyPaths[] is atleast 2.
                    // Since the array is known to contain an item [>> keyName.Contains("\\") is true <<],
                    // we know the length is atleast one. After splitting the string based on that element,
                    // the results are atleast 2 (even though one or both elements can be equal to string.Empty.
                    for (int i = 1; i < KeyPaths.Length; i++)
                    {
                        ParsedTreeNodes.Nodes.Add(new TreeNode(KeyPaths[i]));
                    }

                    return ParsedTreeNodes;
                }
                // Does not contain "\\". It must be a root node.
                else
                {
                    return new TreeNode(keyName);
                }
            }
            else
            {
                // Not a valid TreeNode if the root node has a keyName equal to string.Empty.
                return null;
            }
        }

        private void PopulateLstRegistryKeys(RegistryKey[] keys)
        {
            for (int i = 0; i < lstRegistryKeys.Items.Count; i++)
            {
                RegistryKey key = lstRegistryKeys.Items[i].Tag as RegistryKey;
                if (key != null)
                {
                    key.Close();
                }
            }

            lstRegistryKeys.Clear();

            if (keys != null)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    // To-Do: Use a custom ListViewItem for a better style.
                    lstRegistryKeys.Items.Add(new ListViewItem() { Text = keys[i].Name, Tag = keys[i] });
                }
            }
        }

        private void tvRegistryDirectory_Click(object sender, EventArgs e)
        {
            if ((tvRegistryDirectory.SelectedNode != null) && (tvRegistryDirectory.SelectedNode.Tag != null))
            {
                List<RegistryKey> KeysFromNode = new List<RegistryKey>();
                foreach (object obj in (object[])tvRegistryDirectory.SelectedNode.Tag)
                {
                    RegistryKey key = obj as RegistryKey;
                    if (key != null)
                    {
                        KeysFromNode.Add(key);
                    }
                }

                PopulateLstRegistryKeys(KeysFromNode.ToArray());
            }
        }

        private void FrmRegistryEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_connectClient.Value.FrmRe != null)
                _connectClient.Value.FrmRe = null;
        }
    }
}