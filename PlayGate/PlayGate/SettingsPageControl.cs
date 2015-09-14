using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetroFramework.Controls;
using MetroFramework;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;
using System.Windows.Forms;
using System.Diagnostics;

namespace PlayGate
{
    public class SettingsPageControl : MetroPanel
    {
        #region Private Static Data.

        private static readonly string SETTINGS_FILE_PATH = "UserSettings.json";
        private static readonly int DEFAULT_SEPARATOR = 24;

        #endregion



        #region Private Data.

        private SettingsModel m_settingsModel;
        private MetroComboBox m_styleComboBox;
        private MetroComboBox m_themeComboBox;
        private bool m_needRestart;

        #endregion



        #region Public Properties.

        public SettingsModel SettingsModel { get { return m_settingsModel; } }
        public bool IsNeedRestart { get { return m_needRestart; } }

        #endregion



        #region Construction & Destruction.

        public SettingsPageControl()
        {
            MetroSkinManager.ApplyMetroStyle(this);
            Disposed += SettingsPageControl_Disposed;
            AutoScroll = true;
            m_needRestart = false;

            LoadSettingsModel();
            InitializeContents();
        }

        #endregion



        #region Public Functionality.

        public void RefreshContent()
        {
            m_styleComboBox.SelectedValueChanged -= new EventHandler(comboBox_SelectedValueChanged);
            List<string> items = new List<string>();
            foreach (string item in Enum.GetNames(typeof(MetroColorStyle)))
                items.Add(item);
            m_styleComboBox.DataSource = items;
            string style = m_settingsModel.UiStyle.ToString();
            m_styleComboBox.SelectedItem = style;
            m_styleComboBox.SelectedValueChanged += new EventHandler(comboBox_SelectedValueChanged);

            items = new List<string>();
            foreach (string item in Enum.GetNames(typeof(MetroThemeStyle)))
                items.Add(item);
            m_themeComboBox.SelectedValueChanged -= new EventHandler(comboBox_SelectedValueChanged);
            m_themeComboBox.DataSource = items;
            string theme = m_settingsModel.UiTheme.ToString();
            m_themeComboBox.SelectedItem = theme;
            m_themeComboBox.SelectedValueChanged += new EventHandler(comboBox_SelectedValueChanged);
        }

        public void LoadSettingsModel()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + SETTINGS_FILE_PATH;
            string data = File.Exists(path) ? File.ReadAllText(path) : "{}";
            try
            {
                m_settingsModel = JsonConvert.DeserializeObject<SettingsModel>(data);
                if (m_settingsModel == null)
                    m_settingsModel = new SettingsModel();
            }
            catch { m_settingsModel = new SettingsModel(); }
            MetroSkinManager.Style = m_settingsModel.UiStyle;
            MetroSkinManager.Theme = m_settingsModel.UiTheme;
        }

        public void SaveSettingsModel()
        {
            string data = JsonConvert.SerializeObject(m_settingsModel, Formatting.Indented);
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + SETTINGS_FILE_PATH, data);
        }

        #endregion



        #region Private Functionality.

        private void InitializeContents()
        {
            MetroLabel versionLabel = new MetroLabel();
            MetroSkinManager.ApplyMetroStyle(versionLabel);
            versionLabel.Size = new Size();
            versionLabel.AutoSize = true;
            versionLabel.Text = "Editor version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "\nAuthor: Patryk 'PsichiX' Budzyński";
            versionLabel.FontWeight = MetroLabelWeight.Bold;
            versionLabel.Location = new Point(DEFAULT_SEPARATOR, DEFAULT_SEPARATOR);
            Controls.Add(versionLabel);

            MetroLabel sceneViewerPortLabel = new MetroLabel();
            MetroSkinManager.ApplyMetroStyle(sceneViewerPortLabel);
            sceneViewerPortLabel.Size = new Size();
            sceneViewerPortLabel.AutoSize = true;
            sceneViewerPortLabel.Text = "Scene viewer HTTP server port:";
            sceneViewerPortLabel.Location = new Point(DEFAULT_SEPARATOR, versionLabel.Bottom + DEFAULT_SEPARATOR);
            Controls.Add(sceneViewerPortLabel);

            MetroTextBox sceneViewerPortTextBox = new MetroTextBox();
            MetroSkinManager.ApplyMetroStyle(sceneViewerPortTextBox);
            sceneViewerPortTextBox.Location = new Point(DEFAULT_SEPARATOR, sceneViewerPortLabel.Bottom);
            sceneViewerPortTextBox.Width = 64;
            sceneViewerPortTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            sceneViewerPortTextBox.Text = m_settingsModel.SceneViewerHttpServerPort.ToString();
            sceneViewerPortTextBox.TextChanged += new EventHandler(textBox_TextChanged_sceneViewerPort);
            Controls.Add(sceneViewerPortTextBox);

            MetroLabel runnerPortLabel = new MetroLabel();
            MetroSkinManager.ApplyMetroStyle(runnerPortLabel);
            runnerPortLabel.Size = new Size();
            runnerPortLabel.AutoSize = true;
            runnerPortLabel.Text = "Runner HTTP server port:";
            runnerPortLabel.Location = new Point(DEFAULT_SEPARATOR, sceneViewerPortTextBox.Bottom + DEFAULT_SEPARATOR);
            Controls.Add(runnerPortLabel);

            MetroTextBox runnerPortTextBox = new MetroTextBox();
            MetroSkinManager.ApplyMetroStyle(runnerPortTextBox);
            runnerPortTextBox.Location = new Point(DEFAULT_SEPARATOR, runnerPortLabel.Bottom);
            runnerPortTextBox.Width = 64;
            runnerPortTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            runnerPortTextBox.Text = m_settingsModel.RunnerHttpServerPort.ToString();
            runnerPortTextBox.TextChanged += new EventHandler(textBox_TextChanged_runnerPort);
            Controls.Add(runnerPortTextBox);

            MetroLabel styleLabel = new MetroLabel();
            MetroSkinManager.ApplyMetroStyle(styleLabel);
            styleLabel.Size = new Size();
            styleLabel.AutoSize = true;
            styleLabel.Text = "Application Style:";
            styleLabel.Location = new Point(DEFAULT_SEPARATOR, runnerPortTextBox.Bottom + DEFAULT_SEPARATOR);
            Controls.Add(styleLabel);

            m_styleComboBox = new MetroComboBox();
            MetroSkinManager.ApplyMetroStyle(m_styleComboBox);
            m_styleComboBox.Location = new Point(DEFAULT_SEPARATOR, styleLabel.Bottom);
            Controls.Add(m_styleComboBox);

            MetroLabel themeLabel = new MetroLabel();
            MetroSkinManager.ApplyMetroStyle(themeLabel);
            themeLabel.Size = new Size();
            themeLabel.AutoSize = true;
            themeLabel.Text = "Application Theme:";
            themeLabel.Location = new Point(DEFAULT_SEPARATOR, m_styleComboBox.Bottom + DEFAULT_SEPARATOR);
            Controls.Add(themeLabel);

            m_themeComboBox = new MetroComboBox();
            MetroSkinManager.ApplyMetroStyle(m_themeComboBox);
            m_themeComboBox.Location = new Point(DEFAULT_SEPARATOR, themeLabel.Bottom);
            Controls.Add(m_themeComboBox);
        }

        #endregion



        #region Private Events Handlers.

        private void SettingsPageControl_Disposed(object sender, EventArgs e)
        {
            SaveSettingsModel();
        }

        private void textBox_TextChanged_sceneViewerPort(object sender, EventArgs e)
        {
            int v = m_settingsModel.SceneViewerHttpServerPort;
            if (Int32.TryParse((sender as MetroTextBox).Text, out v))
            {
                m_settingsModel.SceneViewerHttpServerPort = v;
                m_needRestart = true;
            }
        }

        private void textBox_TextChanged_runnerPort(object sender, EventArgs e)
        {
            int v = m_settingsModel.RunnerHttpServerPort;
            if (Int32.TryParse((sender as MetroTextBox).Text, out v))
            {
                m_settingsModel.RunnerHttpServerPort = v;
                m_needRestart = true;
            }
        }

        private void comboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            MetroComboBox comboBox = sender as MetroComboBox;
            if (comboBox == null)
                return;

            if (comboBox == m_styleComboBox)
            {
                MetroColorStyle style = m_settingsModel.UiStyle;
                Enum.TryParse<MetroColorStyle>(comboBox.SelectedItem.ToString(), out style);
                m_settingsModel.UiStyle = style;
                MetroSkinManager.Style = style;
            }
            else if (comboBox == m_themeComboBox)
            {
                MetroThemeStyle theme = m_settingsModel.UiTheme;
                Enum.TryParse<MetroThemeStyle>(comboBox.SelectedItem.ToString(), out theme);
                m_settingsModel.UiTheme = theme;
                MetroSkinManager.Theme = theme;
            }
        }

        #endregion
    }
}
