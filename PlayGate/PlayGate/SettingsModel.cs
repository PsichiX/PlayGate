using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayGate
{
    public class SettingsModel
    {
        #region Public properties.

        public MetroFramework.MetroColorStyle UiStyle { get; set; }
        public MetroFramework.MetroThemeStyle UiTheme { get; set; }
        public FormWindowState WindowState { get; set; }
        public bool IsSceneViewHierarchyPanelRolled { get; set; }
        public bool IsSceneViewInspectPanelRolled { get; set; }
        public bool IsSceneViewAssetsPanelRolled { get; set; }
        public int SceneViewerHttpServerPort { get; set; }
        public int RunnerHttpServerPort { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public bool IsNodeJsExists { get; set; }

        #endregion



        #region Construction and destruction.

        public SettingsModel()
        {
            UiStyle = MetroFramework.MetroColorStyle.Orange;
            UiTheme = MetroFramework.MetroThemeStyle.Dark;
            WindowState = FormWindowState.Maximized;
            IsSceneViewHierarchyPanelRolled = false;
            IsSceneViewInspectPanelRolled = false;
            IsSceneViewAssetsPanelRolled = false;
            SceneViewerHttpServerPort = 51000;
            RunnerHttpServerPort = 51001;
            IsNodeJsExists = Utils.IsNodeJsExists;
        }

        #endregion
    }
}
