using MetroFramework;
using MetroFramework.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayGate
{
    public class SceneViewPageControl : MetroPanel
    {
        #region Private data.

        private BrowserControl m_browser;
        private SceneViewManagerControl m_manager;
        private MetroSidePanel m_hierarchyPanel;
        private MetroSidePanel m_inspectPanel;
        private MetroSidePanel m_assetsPanel;
        private SceneViewAssetsControl m_assetsContent;
        private Process m_httpServer;
        private bool m_cannotUseSymlinks;
        private bool m_isRefreshingContent;
        private PropertiesControl.PropertyControlsRegistry m_editorsRegistry;

        #endregion



        #region Public properties.

        public MetroSidePanel HierarchyPanel { get { return m_hierarchyPanel; } }
        public MetroSidePanel InspectPanel { get { return m_inspectPanel; } }
        public MetroSidePanel AssetsPanel { get { return m_assetsPanel; } }

        #endregion



        #region Construction and destruction.

        public SceneViewPageControl()
        {
            MetroSkinManager.ApplyMetroStyle(this);
            AutoScroll = false;
            Disposed += SceneViewPageControl_Disposed;
            Resize += SceneViewPageControl_Resize;
            m_cannotUseSymlinks = true;
            m_isRefreshingContent = false;
            m_editorsRegistry = new PropertiesControl.PropertyControlsRegistry();

            m_browser = new BrowserControl();
            m_browser.Width = Width;
            m_browser.Height = Height;
            Controls.Add(m_browser);

            m_manager = new SceneViewManagerControl();
            m_manager.Width = Width;
            Controls.Add(m_manager);
            m_manager.BringToFront();

            m_hierarchyPanel = new MetroSidePanel();
            MetroSkinManager.ApplyMetroStyle(m_hierarchyPanel);
            m_hierarchyPanel.Width = 250;
            m_hierarchyPanel.Height = Height;
            m_hierarchyPanel.Side = DockStyle.Left;
            m_hierarchyPanel.IsRolled = false;
            m_hierarchyPanel.IsDocked = false;
            m_hierarchyPanel.IsDockable = false;
            m_hierarchyPanel.AnimatedRolling = false;
            m_hierarchyPanel.OffsetPadding = new Padding(0, m_manager.Height, 0, 24);
            m_hierarchyPanel.Text = "HIERARCHY";
            m_hierarchyPanel.Rolled += sidePanel_RolledUnrolled;
            m_hierarchyPanel.Unrolled += sidePanel_RolledUnrolled;
            Controls.Add(m_hierarchyPanel);
            m_hierarchyPanel.BringToFront();

            m_inspectPanel = new MetroSidePanel();
            MetroSkinManager.ApplyMetroStyle(m_inspectPanel);
            m_inspectPanel.Width = 250;
            m_inspectPanel.Height = Height;
            m_inspectPanel.Side = DockStyle.Right;
            m_inspectPanel.IsRolled = false;
            m_inspectPanel.IsDocked = false;
            m_inspectPanel.IsDockable = false;
            m_inspectPanel.AnimatedRolling = false;
            m_inspectPanel.OffsetPadding = new Padding(0, m_manager.Height, 0, 24);
            m_inspectPanel.Text = "INSPECT";
            m_inspectPanel.Rolled += sidePanel_RolledUnrolled;
            m_inspectPanel.Unrolled += sidePanel_RolledUnrolled;
            Controls.Add(m_inspectPanel);
            m_inspectPanel.BringToFront();

            m_assetsPanel = new MetroSidePanel();
            MetroSkinManager.ApplyMetroStyle(m_assetsPanel);
            m_assetsPanel.Width = Width;
            m_assetsPanel.Height = 222;
            m_assetsPanel.Side = DockStyle.Bottom;
            m_assetsPanel.IsRolled = false;
            m_assetsPanel.IsDocked = false;
            m_assetsPanel.IsDockable = false;
            m_assetsPanel.AnimatedRolling = false;
            m_assetsPanel.Text = "ASSETS";
            m_assetsPanel.Rolled += sidePanel_RolledUnrolled;
            m_assetsPanel.Unrolled += sidePanel_RolledUnrolled;
            Controls.Add(m_assetsPanel);
            m_assetsPanel.BringToFront();

            m_assetsContent = new SceneViewAssetsControl(this);
            m_assetsContent.Dock = DockStyle.Fill;
            m_assetsPanel.Content.Controls.Clear();
            m_assetsPanel.Content.Controls.Add(m_assetsContent);
        }

        public class CustomType
        {
            public float value { get; set; }
            public string text { get; set; }
            public bool on { get; set; }
        }

        #endregion



        #region Public functionality.

        public void RebuildFilesList()
        {
            m_assetsContent.RefreshContent();
        }

        public void RefreshContent()
        {
            MainForm mainForm = FindForm() as MainForm;
            if (mainForm == null || mainForm.SettingsModel == null)
                return;

            m_isRefreshingContent = true;
            m_hierarchyPanel.IsRolled = mainForm.SettingsModel.IsSceneViewHierarchyPanelRolled;
            m_inspectPanel.IsRolled = mainForm.SettingsModel.IsSceneViewInspectPanelRolled;
            m_assetsPanel.IsRolled = mainForm.SettingsModel.IsSceneViewAssetsPanelRolled;
            m_isRefreshingContent = false;
        }

        public void StartServer()
        {
            if (m_httpServer != null)
            {
                try
                {
                    m_httpServer.Kill();
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.ToString()); }
                m_httpServer = null;
            }

            MainForm mainForm = FindForm() as MainForm;
            if (mainForm == null || mainForm.SettingsModel == null || mainForm.ProjectModel == null || !mainForm.SettingsModel.IsNodeJsExists)
                return;

            BuildEditorProject();

            string workdir = Path.Combine(Path.GetFullPath(mainForm.ProjectModel.WorkingDirectory), "editor");
            m_httpServer = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.WorkingDirectory = workdir;
            info.FileName = "node";
            info.Arguments = "../lib/httpServer.js " + mainForm.SettingsModel.SceneViewerHttpServerPort;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            m_httpServer.StartInfo = info;
            m_httpServer.Exited += m_httpServer_Exited;
            m_httpServer.Start();
        }

        public void RegisterPropertyEditors()
        {
            MainForm mainForm = FindForm() as MainForm;
            if (mainForm == null || mainForm.ProjectModel == null)
                return;

            m_editorsRegistry.UnregisterAllControls();
            m_editorsRegistry.RegisterControl<PropertyEditors.Number_PropertyEditor>("number");
            m_editorsRegistry.RegisterControl<PropertyEditors.String_PropertyEditor>("string");
            m_editorsRegistry.RegisterControl<PropertyEditors.Boolean_PropertyEditor>("boolean");
            //m_controlsRegistry.RegisterControl<PropertyEditors.Asset_PropertyEditor>("asset");
            //m_controlsRegistry.RegisterControl<PropertyEditors.Entity_PropertyEditor>("entity");
            m_editorsRegistry.RegisterControl<PropertyEditors.PlayCanvas_Rgb_PropertyEditor>("rgb");
            m_editorsRegistry.RegisterControl<PropertyEditors.PlayCanvas_Rgba_PropertyEditor>("rgba");
            m_editorsRegistry.RegisterControl<PropertyEditors.PlayCanvas_Vector_PropertyEditor>("vector");
            m_editorsRegistry.RegisterControl<PropertyEditors.PlayCanvas_Enumeration_PropertyEditor>("enumeration");
            m_editorsRegistry.RegisterControl<PropertyEditors.PlayCanvas_Curve_PropertyEditor>("curve");
            m_editorsRegistry.RegisterControl<PropertyEditors.PlayCanvas_ColorCurve_PropertyEditor>("colorcurve");

            string assetsPath = Path.Combine(mainForm.ProjectModel.WorkingDirectory, "assets");
            DirectoryInfo info = new DirectoryInfo(assetsPath);
            if (info.Exists)
            {
                FileInfo[] files = info.GetFiles("*.editor.json", SearchOption.AllDirectories);
                if (files != null && files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        try
                        {
                            string name = file.Name.Remove(file.Name.Length - ".editor.json".Length);
                            string content = File.ReadAllText(file.FullName);
                            if (!String.IsNullOrEmpty(content))
                            {
                                PropertiesControl.TypeDescriptor typeDesc = JsonConvert.DeserializeObject<PropertiesControl.TypeDescriptor>(content);
                                if (typeDesc == null || !m_editorsRegistry.RegisterControl(name, typeDesc))
                                    throw new Exception("Cannot register custom editor: " + name);
                            }
                            else
                                throw new Exception("Cannot load custom editor: " + file.FullName);
                        }
                        catch (Exception ex)
                        {
                            MetroMessageBox.Show(mainForm, ex.Message, "Registering custom editor error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                files = info.GetFiles("*.editor.html", SearchOption.AllDirectories);
                if (files != null && files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        try
                        {
                            string name = file.Name.Remove(file.Name.Length - ".editor.html".Length);
                            if (!m_editorsRegistry.RegisterControl(name, file.FullName))
                                throw new Exception("Cannot register custom editor: " + name);
                        }
                        catch (Exception ex)
                        {
                            MetroMessageBox.Show(mainForm, ex.Message, "Registering custom editor error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

            PropertiesModel model = new PropertiesModel();
            var numberInfo = new PropertyEditors.Number_PropertyEditor.NumberInfo() { Min = 0.5f, Max = 1.5f };
            model.Properties.Add(PropertiesModel.Property.Create<float, PropertyEditors.Number_PropertyEditor.NumberInfo>("number", "number", 1.0f, numberInfo));
            model.Properties.Add(PropertiesModel.Property.Create<string>("string", "string", "text"));
            model.Properties.Add(PropertiesModel.Property.Create<bool>("boolean", "boolean", true));
            //model.Properties.Add(PropertiesModel.Property.Create<PlayCanvas.Assets>("asset", "asset", null));
            //model.Properties.Add(PropertiesModel.Property.Create<PlayCanvas.Entity>("entity", "entity", null));
            model.Properties.Add(PropertiesModel.Property.Create<PlayCanvas.Color>("rgb", "rgb", new PlayCanvas.Color(1.0f, 0.5f, 0.0f)));
            model.Properties.Add(PropertiesModel.Property.Create<PlayCanvas.Color>("rgba", "rgba", new PlayCanvas.Color(0.0f, 0.5f, 1.0f, 0.5f)));
            model.Properties.Add(PropertiesModel.Property.Create<PlayCanvas.Vector>("vector", "vector", new PlayCanvas.Vector(0.0f, 1.0f, 0.0f)));
            var enumsInfo = new PropertyEditors.PlayCanvas_Enumeration_PropertyEditor.EnumerationInfo();
            enumsInfo["Cat"] = 0;
            enumsInfo["Dog"] = 1;
            model.Properties.Add(PropertiesModel.Property.Create<int, PropertyEditors.PlayCanvas_Enumeration_PropertyEditor.EnumerationInfo>("enumeration", "enumeration", 0, enumsInfo));
            var curveData = new float[] {
                0.0f, 1.0f,
                1.0f, 0.0f
            };
            model.Properties.Add(PropertiesModel.Property.Create<PlayCanvas.Curve>("curve", "curve", new PlayCanvas.Curve(curveData)));
            var curveSetInfo = new PropertyEditors.PlayCanvas_Curve_PropertyEditor.CurveInfo() { Curves = new string[] { "x", "y", "z" } };
            var curveSetData = new float[][] { 
                new float[]{
                    0.0f, 1.0f,
                    1.0f, 0.0f
                },
                new float[]{
                    0.0f, 0.5f,
                    1.0f, 0.5f
                }
            };
            model.Properties.Add(PropertiesModel.Property.Create<PlayCanvas.CurveSet, PropertyEditors.PlayCanvas_Curve_PropertyEditor.CurveInfo>("curveSet", "curve", new PlayCanvas.CurveSet(curveSetData), curveSetInfo));
            var colorCurveSetInfo = new PropertyEditors.PlayCanvas_ColorCurve_PropertyEditor.ColorCurveInfo() { Type = PropertyEditors.PlayCanvas_ColorCurve_PropertyEditor.ColorCurveType.RGB };
            model.Properties.Add(PropertiesModel.Property.Create<PlayCanvas.CurveSet, PropertyEditors.PlayCanvas_ColorCurve_PropertyEditor.ColorCurveInfo>("colorCurve", "colorcurve", new PlayCanvas.CurveSet(), colorCurveSetInfo));
            model.Properties.Add(PropertiesModel.Property.Create<CustomType>("CustomType", "CustomType"));
            model.Properties.Add(PropertiesModel.Property.Create<CustomType>("Date", "Date"));
            model.Properties.Add(new PropertiesModel.Property("Array", "[string]"));
            model.Properties.Add(new PropertiesModel.Property("Object", "{CustomType}"));

            PropertiesControl properties = new PropertiesControl(model, m_editorsRegistry);
            properties.Dock = DockStyle.Fill;
            m_inspectPanel.Content.Controls.Clear();
            m_inspectPanel.Content.Controls.Add(properties);
        }

        public void OpenScene(string path)
        {
        }

        public void CloseScene()
        {
        }

        public void ShowAssetProperties(string path)
        {
            MainForm mainForm = FindForm() as MainForm;
            if (mainForm == null || mainForm.ProjectModel == null)
                return;

            string ext = Path.GetExtension(path);
            if (ext == ".scene")
                OpenScene(path);
            else
                Process.Start(Path.Combine(mainForm.ProjectModel.WorkingDirectory, "assets", path));
        }

        #endregion



        #region Private functionality.

        private void BuildEditorProject()
        {
            MainForm mainForm = FindForm() as MainForm;
            if (mainForm == null || mainForm.SettingsModel == null || mainForm.ProjectModel == null)
                return;

            string workdir = Path.Combine(Path.GetFullPath(mainForm.ProjectModel.WorkingDirectory), "editor");
            string assetsPath = Path.Combine(workdir, "assets");
            string symAssetsPath = Path.GetFullPath(Path.Combine(workdir, "..", "assets"));
            if (!Directory.Exists(workdir))
                Directory.CreateDirectory(workdir);
            // TODO:
            //if (!Directory.Exists(assetsPath) && !Utils.CreateSymbolicLink(assetsPath, symAssetsPath, Utils.SymLinkFlag.Directory))
            //    MetroMessageBox.Show(mainForm, "Cannot create symbolic link to assets for editor usage in path: " + assetsPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //m_cannotUseSymlinks = !Utils.IsSymbolicLink(assetsPath);
            if (m_cannotUseSymlinks)
            {
                Directory.CreateDirectory(assetsPath);
                Utils.SynchronizeDirectories(symAssetsPath, assetsPath);
            }

            File.Copy("resources/templates/editor.wrapper.html", Path.Combine(workdir, "index.html"), true);
            File.Copy("resources/templates/editor.empty.html", Path.Combine(workdir, "empty.html"), true);
            File.Copy("resources/icons/playgatelogo32.png", Path.Combine(workdir, "playgatelogo32.png"), true);
            File.Copy("resources/icons/playgatelogo.png", Path.Combine(workdir, "playgatelogo.png"), true);
            File.Copy("resources/lib/playcanvas.js", Path.Combine(workdir, "playcanvas.js"), true);
            File.Copy("resources/templates/editor.app.js", Path.Combine(workdir, "app.js"), true);
            File.Copy("resources/templates/editor.app.PlayGateApplication.js", Path.Combine(workdir, "app.PlayGateApplication.js"), true);
            File.Copy("resources/templates/editor.communication.js", Path.Combine(workdir, "communication.js"), true);

            m_browser.Navigate("localhost:" + mainForm.SettingsModel.SceneViewerHttpServerPort + "/empty.html");
        }

        private void UpdateLayout()
        {
            int left = !m_hierarchyPanel.IsRolled ? m_hierarchyPanel.Width : MetroSidePanel.ROLLED_PART_SIZE;
            int right = !m_inspectPanel.IsRolled ? m_inspectPanel.Width : MetroSidePanel.ROLLED_PART_SIZE;
            int bottom = !m_assetsPanel.IsRolled ? m_assetsPanel.Height : MetroSidePanel.ROLLED_PART_SIZE;
            m_browser.Left = left;
            m_browser.Top = m_manager.Height;
            m_browser.Width = Width - left - right;
            m_browser.Height = Height - m_manager.Height - bottom;
            m_manager.Width = Width;
            m_hierarchyPanel.OffsetPadding = new Padding(0, 0, 0, bottom);
            m_inspectPanel.OffsetPadding = new Padding(0, 0, 0, bottom);
        }

        #endregion



        #region Private events handlers.

        private void SceneViewPageControl_Disposed(object sender, EventArgs e)
        {
            if (m_httpServer != null)
            {
                try
                {
                    m_httpServer.Kill();
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.ToString()); }
                m_httpServer = null;
            }
        }

        private void m_httpServer_Exited(object sender, EventArgs e)
        {
            m_httpServer = null;
        }

        private void SceneViewPageControl_Resize(object sender, EventArgs e)
        {
            UpdateLayout();
        }

        private void sidePanel_RolledUnrolled(object sender, EventArgs e)
        {
            UpdateLayout();

            MainForm mainForm = FindForm() as MainForm;
            if (mainForm == null || mainForm.SettingsModel == null)
                return;

            if (!m_isRefreshingContent)
            {
                mainForm.SettingsModel.IsSceneViewHierarchyPanelRolled = m_hierarchyPanel.IsRolled;
                mainForm.SettingsModel.IsSceneViewInspectPanelRolled = m_inspectPanel.IsRolled;
                mainForm.SettingsModel.IsSceneViewAssetsPanelRolled = m_assetsPanel.IsRolled;
            }
        }

        #endregion
    }
}
