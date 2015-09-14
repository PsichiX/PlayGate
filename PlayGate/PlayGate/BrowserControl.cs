using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MetroFramework.Controls;
using Gecko;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace PlayGate
{
    public class BrowserControl : MetroPanel
    {
        #region Public enumerators.

        public enum Status
        {
            None,
            Loading,
            Ready
        }

        #endregion



        #region Private data.

        private MetroProgressBar m_progressBar;
        private MetroLabel m_statusLabel;
        private GeckoWebBrowser m_browser;
        private Status m_status;
        private string m_navigateToWhenActive;

        #endregion



        #region Public properties.

        public Status BrowserStatus { get { return m_status; } }

        #endregion



        #region Public events.

        public event EventHandler Ready;
        public event EventHandler Loading;

        #endregion



        #region Construction and destruction.

        public BrowserControl()
        {
            MetroSkinManager.ApplyMetroStyle(this);
            m_status = Status.None;
            VisibleChanged += BrowserControl_VisibileChanged;

            m_progressBar = new MetroProgressBar();
            MetroSkinManager.ApplyMetroStyle(m_progressBar);
            m_progressBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            m_progressBar.Height = 6;
            m_progressBar.ProgressBarStyle = ProgressBarStyle.Continuous;
            m_progressBar.Maximum = 1;
            m_progressBar.Value = 1;
            Controls.Add(m_progressBar);

            m_statusLabel = new MetroLabel();
            MetroSkinManager.ApplyMetroStyle(m_statusLabel);
            m_statusLabel.Text = "";
            m_statusLabel.Dock = DockStyle.Bottom;
            Controls.Add(m_statusLabel);

            m_browser = new GeckoWebBrowser();
            MetroSkinManager.ExtendMetroStyle(m_browser, false);
            m_browser.NoDefaultContextMenu = true;
            m_browser.Dock = DockStyle.Fill;
            m_browser.DocumentCompleted += m_browser_DocumentCompleted;
            m_browser.ProgressChanged += m_browser_ProgressChanged;
            m_browser.Navigating += m_browser_Navigating;
            Controls.Add(m_browser);
        }

        #endregion



        #region Public functionalities.

        public void Navigate(string url, bool navigateWhenActive = false)
        {
            if (Visible && Parent != null)
                navigateWhenActive = false;
            if (navigateWhenActive)
                m_navigateToWhenActive = url;
            else
            {
                m_navigateToWhenActive = null;
                m_status = Status.Loading;
                m_progressBar.Value = 0;
                m_browser.Navigate(url);
            }
        }

        public JToken EvaluateScript(string script)
        {
            return EvaluateScript<JToken>(script);
        }

        public T EvaluateScript<T>(string script)
        {
            if (String.IsNullOrEmpty(script))
                return default(T);
            try
            {
                using (AutoJSContext context = new AutoJSContext(m_browser.Window.JSContext))
                {
                    var value = context.EvaluateScript(script, m_browser.Window.DomWindow);
                    return JsonConvert.DeserializeObject<T>(value.ToString());
                }
            }
            catch { return default(T); }
        }

        public void AddMessageEventListener(string message, Action<string> action, bool useCapture = false)
        {
            m_browser.AddMessageEventListener(message, action, useCapture);
        }

        #endregion



        #region Private events handlers.

        private void BrowserControl_VisibileChanged(object sender, EventArgs e)
        {
            if (m_navigateToWhenActive != null)
                Navigate(m_navigateToWhenActive);
        }

        private void m_browser_ProgressChanged(object sender, GeckoProgressEventArgs e)
        {
            if (m_status == Status.Loading)
            {
                m_progressBar.Maximum = (int)e.MaximumProgress;
                m_progressBar.Value = (int)e.CurrentProgress;
                m_statusLabel.Text = m_browser.StatusText;
            }
        }

        private void m_browser_Navigating(object sender, Gecko.Events.GeckoNavigatingEventArgs e)
        {
            m_status = Status.Loading;
            m_progressBar.Value = 0;
            if (Loading != null)
                Loading(this, new EventArgs());
        }

        private void m_browser_DocumentCompleted(object sender, Gecko.Events.GeckoDocumentCompletedEventArgs e)
        {
            m_progressBar.Maximum = 1;
            m_progressBar.Value = 1;
            m_statusLabel.Text = "";
            m_status = Status.Ready;
            if (Ready != null)
                Ready(this, new EventArgs());
        }

        private void m_browser_CreateWindow2(object sender, GeckoCreateWindow2EventArgs e)
        {
            e.Cancel = true;
            Navigate(e.Uri);
        }

        #endregion
    }
}
