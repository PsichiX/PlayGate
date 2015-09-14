using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MetroFramework;
using MetroFramework.Controls;
using MetroFramework.Forms;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace PlayGate
{
    public partial class MainForm : MetroForm
    {
        #region Private nested classes.

        private class InstallationSettings
        {
            #region Public properties.

            public string NodeJsDownloadUrl { get; set; }

            #endregion



            #region Construction and destruction.

            public InstallationSettings()
            {
                NodeJsDownloadUrl = "https://nodejs.org/download/";
            }

            #endregion
        }

        #endregion



        #region Public static data.

        public static readonly string TAB_NAME_WELCOME = "Welcome page";
        public static readonly string TAB_NAME_SETTINGS = "Settings";
        public static readonly string TAB_NAME_PROJECT = "Project";
        public static readonly string TAB_NAME_BUILD = "Build and run";
        public static readonly string TAB_NAME_SCENE_VIEW = "Scene view";

        #endregion



        #region Private static data.

        private static readonly string INSTALLATION_SETTINGS_PATH = "resources/settings/InstallationSettings.json";
        private static readonly string PLAYCANVAS_DEVELOPER_URL = "http://developer.playcanvas.com";
        private static readonly string DEFAULT_APP_TITLE = "PlayGate";

        #endregion



        #region Private data.

        private InstallationSettings m_installationSettings;
        private MetroPanel m_mainPanel;
        private MetroTabControl m_mainPanelTabs;
        private BrowserControl m_welcomePage;
        private SettingsPageControl m_settingsPage;
        private ProjectPageControl m_projectPage;
        private BuildPageControl m_buildPage;
        private SceneViewPageControl m_sceneViewPage;
        private string m_appTitleExtended;

        #endregion



        #region Public properties.

        public string AppTitleExtended { get { return m_appTitleExtended; } set { m_appTitleExtended = value; RefreshAppTitle(); } }
        public SettingsModel SettingsModel { get { return m_settingsPage == null ? null : m_settingsPage.SettingsModel; } }
        public ProjectModel ProjectModel { get { return m_projectPage == null ? null : m_projectPage.ProjectModel; } }

        #endregion



        #region Construction and destruction.

        public MainForm()
        {
            var xulPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "xulrunner");
            Gecko.Xpcom.Initialize(xulPath);
            LoadInstallationSettings();

            InitializeComponent();

            MetroSkinManager.SetManagerOwner(this);
            MetroSkinManager.ApplyMetroStyle(this);
            Load += MainForm_Load;
            FormClosing += MainForm_FormClosing;
            Padding = new Padding(1, 0, 1, 20);
            Size = new Size(800, 600);
            MinimumSize = new Size(640, 480);
            RefreshAppTitle();

            InitializeMainPanel();

            if (!Utils.IsAdministrator())
                MetroFramework.MetroMessageBox.Show(
                    this,
                    "PlayGate need to be run as Administrator in prior to it's some functionalities to work properly!",
                    "Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                    );
        }

        #endregion



        #region Public functionality.

        public void RefreshAppTitle()
        {
            Text = DEFAULT_APP_TITLE + (String.IsNullOrEmpty(AppTitleExtended) ? "" : " - " + AppTitleExtended);
            Invalidate();
        }

        public TabPage AddTabPage(Control page, string name, bool select = false)
        {
            TabPage tab = new TabPage(name);
            tab.Controls.Add(page);
            m_mainPanelTabs.TabPages.Add(tab);
            if (select)
                m_mainPanelTabs.SelectedTab = tab;
            return tab;
        }

        public void RemoveTabPage(string name)
        {
            TabPage page;
            for (int i = m_mainPanelTabs.TabPages.Count - 1; i >= 0; --i)
            {
                page = m_mainPanelTabs.TabPages[i];
                if (page.Text == name)
                    m_mainPanelTabs.TabPages.Remove(page);
            }
        }

        public void SelectTabPage(string name)
        {
            foreach (TabPage page in m_mainPanelTabs.TabPages)
            {
                if (page.Text == name)
                {
                    m_mainPanelTabs.SelectedTab = page;
                    return;
                }
            }
        }

        public void InitializeProjectPages()
        {
            InitializeBuildPage();
            InitializeSceneViewPage();
        }

        public void DeinitializeProjectPages()
        {
            if (m_buildPage != null)
            {
                RemoveTabPage(TAB_NAME_BUILD);
                m_buildPage.Dispose();
                m_buildPage = null;
            }
            if (m_sceneViewPage != null)
            {
                RemoveTabPage(TAB_NAME_SCENE_VIEW);
                m_sceneViewPage.Dispose();
                m_sceneViewPage = null;
            }
        }

        #endregion



        #region Private functionality.

        private void LoadInstallationSettings()
        {
            string json = File.ReadAllText(INSTALLATION_SETTINGS_PATH);
            if (!String.IsNullOrEmpty(json))
                m_installationSettings = JsonConvert.DeserializeObject<InstallationSettings>(json);
        }

        private void InitializeMainPanel()
        {
            m_mainPanel = new MetroPanel();
            MetroSkinManager.ApplyMetroStyle(m_mainPanel);
            m_mainPanel.Dock = DockStyle.Fill;
            m_mainPanel.Padding = new Padding(20, 2, 20, 20);
            Controls.Add(m_mainPanel);

            // tabs.
            m_mainPanelTabs = new MetroTabControl();
            MetroSkinManager.ApplyMetroStyle(m_mainPanelTabs);
            m_mainPanelTabs.Left = m_mainPanel.Padding.Left;
            m_mainPanelTabs.Top = m_mainPanel.Padding.Top;
            m_mainPanelTabs.Width = m_mainPanel.Width - m_mainPanel.Padding.Horizontal;
            m_mainPanelTabs.Height = m_mainPanel.Height - m_mainPanel.Padding.Vertical;
            m_mainPanelTabs.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            m_mainPanelTabs.Selected += m_mainPanelTabs_Selected;
            m_mainPanel.Controls.Add(m_mainPanelTabs);

            // initial pages.
            InitializeWelcomePage();
            InitializeSettingsPage();
            if (m_settingsPage.SettingsModel.IsNodeJsExists)
            {
                InitializeProjectPage();
            }
        }

        private void InitializeWelcomePage()
        {
            m_welcomePage = new BrowserControl();
            m_welcomePage.Dock = DockStyle.Fill;
            m_welcomePage.Navigate(PLAYCANVAS_DEVELOPER_URL, true);

            AddTabPage(m_welcomePage, TAB_NAME_WELCOME);
        }

        private void InitializeSettingsPage()
        {
            m_settingsPage = new SettingsPageControl();
            m_settingsPage.Dock = DockStyle.Fill;

            AddTabPage(m_settingsPage, TAB_NAME_SETTINGS);

            m_settingsPage.RefreshContent();
        }

        private void InitializeProjectPage()
        {
            m_projectPage = new ProjectPageControl();
            m_projectPage.Dock = DockStyle.Fill;

            AddTabPage(m_projectPage, TAB_NAME_PROJECT, true);
        }

        private void InitializeBuildPage()
        {
            m_buildPage = new BuildPageControl();
            m_buildPage.Dock = DockStyle.Fill;

            AddTabPage(m_buildPage, TAB_NAME_BUILD, true);

            m_buildPage.StartServer();
        }

        private void InitializeSceneViewPage()
        {
            m_sceneViewPage = new SceneViewPageControl();
            m_sceneViewPage.Dock = DockStyle.Fill;

            AddTabPage(m_sceneViewPage, TAB_NAME_SCENE_VIEW);

            m_sceneViewPage.RefreshContent();
            m_sceneViewPage.StartServer();
            m_sceneViewPage.RebuildFilesList();
            m_sceneViewPage.RegisterPropertyEditors();
        }

        #endregion



        #region Private event handlers.

        private void MainForm_Load(object sender, EventArgs e)
        {
            MetroSkinManager.RefreshStyles();

            if (SettingsModel != null)
                WindowState = SettingsModel.WindowState;

            if (!m_settingsPage.SettingsModel.IsNodeJsExists &&
                MetroFramework.MetroMessageBox.Show(
                    this,
                    "Node.js is not found on this machine. Install it in prior to use Editor.\nDo you want to install it now?",
                    "Missing requirement",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Error
                ) == DialogResult.Yes)
            {
                SelectTabPage(TAB_NAME_WELCOME);
                m_welcomePage.Navigate(m_installationSettings.NodeJsDownloadUrl, true);
                MetroFramework.MetroMessageBox.Show(this, "Download and install Node.js. After that restart PlayGate application.", "Installation instructions", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SettingsModel != null)
                SettingsModel.WindowState = WindowState;
        }

        private void m_mainPanelTabs_Selected(object sender, TabControlEventArgs e)
        {
            if (e.TabPage.Controls.Count > 0)
            {
                Control c = e.TabPage.Controls[0];
                if (c != null)
                    c.Select();
                if (m_settingsPage != null && m_settingsPage.IsNeedRestart)
                {
                    MetroFramework.MetroMessageBox.Show(this, "Due to sensitive settings change PlayGate application need to restart.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Process.Start(Application.ExecutablePath);
                    this.DoOnUiThread(() => Close());
                }
            }
        }

        #endregion
    }
}
