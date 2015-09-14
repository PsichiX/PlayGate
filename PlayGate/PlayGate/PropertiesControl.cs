using MetroFramework;
using MetroFramework.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayGate
{
    public class PropertiesControl : MetroPanel
    {
        #region Public nested classes.

        public class PropertyDescriptor
        {
            [JsonProperty("editorId")]
            public string EditorID { get; set; }
            [JsonProperty("editorData")]
            public JToken EditorData { get; set; }
            [JsonProperty("typeDescriptor")]
            public TypeDescriptor TypeDescriptor { get; set; }
        }

        public class TypeDescriptor
        {
            [JsonProperty("properties")]
            public Dictionary<string, PropertyDescriptor> Properties { get; set; }
            [JsonProperty("defaultValue")]
            public JToken DefaultValue { get; set; }

            public TypeDescriptor()
            {
                Properties = new Dictionary<string, PropertyDescriptor>();
            }
        }

        public class PropertyControlsRegistry
        {
            #region Private data.

            private Dictionary<string, Type> m_types;
            private Dictionary<string, TypeDescriptor> m_descriptors;
            private Dictionary<string, string> m_browserFiles;

            #endregion



            #region Construction and destruction.

            public PropertyControlsRegistry()
            {
                m_types = new Dictionary<string, Type>();
                m_descriptors = new Dictionary<string, TypeDescriptor>();
                m_browserFiles = new Dictionary<string, string>();
            }

            #endregion



            #region Public functionalities.

            public bool RegisterControl(string editorId, Type type)
            {
                if (type != null && !m_types.ContainsKey(editorId) && !m_descriptors.ContainsKey(editorId) && !m_browserFiles.ContainsKey(editorId) && typeof(PropertyControl).IsAssignableFrom(type))
                {
                    m_types.Add(editorId, type);
                    return true;
                }
                else
                    return false;
            }

            public bool RegisterControl<T>(string editorId) where T : PropertyControl
            {
                return RegisterControl(editorId, typeof(T));
            }

            public bool RegisterControl(string editorId, TypeDescriptor descriptor)
            {
                if (descriptor != null && !m_types.ContainsKey(editorId) && !m_descriptors.ContainsKey(editorId) && !m_browserFiles.ContainsKey(editorId))
                {
                    m_descriptors.Add(editorId, descriptor);
                    return true;
                }
                else
                    return false;
            }

            public bool RegisterControl(string editorId, string browserFile)
            {
                if (!String.IsNullOrEmpty(browserFile) && File.Exists(browserFile) && !m_types.ContainsKey(editorId) && !m_descriptors.ContainsKey(editorId) && !m_browserFiles.ContainsKey(editorId))
                {
                    m_browserFiles.Add(editorId, browserFile);
                    return true;
                }
                else
                    return false;
            }

            public bool UnregisterControl(string editorIdOrBrowserFile)
            {
                if (m_types.ContainsKey(editorIdOrBrowserFile))
                {
                    m_types.Remove(editorIdOrBrowserFile);
                    return true;
                }
                else if (m_descriptors.ContainsKey(editorIdOrBrowserFile))
                {
                    m_descriptors.Remove(editorIdOrBrowserFile);
                    return true;
                }
                else if (m_browserFiles.ContainsKey(editorIdOrBrowserFile))
                {
                    m_browserFiles.Remove(editorIdOrBrowserFile);
                    return true;
                }
                else if (m_browserFiles.ContainsValue(editorIdOrBrowserFile))
                {
                    var item = m_browserFiles.First(kv => kv.Value == editorIdOrBrowserFile);
                    m_browserFiles.Remove(item.Key);
                    return true;
                }
                else
                    return false;
            }

            public bool UnregisterControl(Type type)
            {
                if (m_types.ContainsValue(type))
                {
                    var item = m_types.First(kv => kv.Value == type);
                    m_types.Remove(item.Key);
                    return true;
                }
                else
                    return false;
            }

            public bool UnregisterControl<T>() where T : PropertyControl
            {
                return UnregisterControl(typeof(T));
            }

            public bool UnregisterControl(TypeDescriptor descriptor)
            {
                if (m_descriptors.ContainsValue(descriptor))
                {
                    var item = m_descriptors.First(kv => kv.Value == descriptor);
                    m_descriptors.Remove(item.Key);
                    return true;
                }
                else
                    return false;
            }

            public void UnregisterAllControls()
            {
                m_types.Clear();
                m_descriptors.Clear();
                m_browserFiles.Clear();
            }

            public bool ControlExists(string editorId)
            {
                return m_types.ContainsKey(editorId) || m_descriptors.ContainsKey(editorId) || m_browserFiles.ContainsKey(editorId);
            }

            public PropertyControl CreateControl(string editorId, string name, PropertiesModel.Property model)
            {
                if (editorId.Length >= 2)
                {
                    int type = 0;
                    if (editorId[0] == '[' && editorId[editorId.Length - 1] == ']')
                        type = 1;
                    else if (editorId[0] == '{' && editorId[editorId.Length - 1] == '}')
                        type = 2;
                    if (type != 0)
                    {
                        editorId = editorId.Substring(1, editorId.Length - 2);
                        if (type == 1)
                            return new ArrayPropertyEditor(editorId, this, name, model);
                        else if (type == 2)
                            return new ObjectPropertyEditor(editorId, this, name, model);
                    }
                }
                if (m_types.ContainsKey(editorId))
                    return Activator.CreateInstance(m_types[editorId], new object[] { name, model }) as PropertyControl;
                else if (m_descriptors.ContainsKey(editorId))
                    return new CustomPropertyControl(m_descriptors[editorId], this, name, model);
                else if (m_browserFiles.ContainsKey(editorId))
                    return new BrowserPropertyControl(m_browserFiles[editorId], name, model);
                else
                    return null;
            }

            public JToken GetControlDefaultValue(string editorId)
            {
                if (m_types.ContainsKey(editorId))
                {
                    var method = m_types[editorId].GetMethod("GetDefaultValue", BindingFlags.Public | BindingFlags.Static);
                    if (method == null)
                        throw new NotImplementedException("Editor does not provide default value: " + editorId);
                    var result = method.Invoke(null, null);
                    var value = result as JToken;
                    if (value == null)
                        throw new Exception("Got invalid default value for editor: " + editorId);
                    return value;
                }
                if (m_descriptors.ContainsKey(editorId))
                {
                    var descriptor = m_descriptors[editorId];
                    var value = descriptor.DefaultValue;
                    if (value == null)
                        throw new Exception("Got invalid default value for editor: " + editorId);
                    return value;
                }
                else
                    return null;
            }

            #endregion
        }

        public abstract class PropertyControl : MetroPanel
        {
            #region Public static functionalities.

            public static JToken GetDefaultValue()
            {
                throw new NotImplementedException();
            }

            public static bool IsValueOf(JToken value)
            {
                throw new NotImplementedException();
            }

            #endregion



            #region Private data.

            private PropertiesModel.Property m_propertyModel;
            private MetroLabel m_nameLabel;
            private MetroPanel m_propertiesListPanel;

            #endregion



            #region Public properties.

            public PropertiesModel.Property PropertyModel { get { return m_propertyModel; } }

            #endregion



            #region Protected properties.

            protected MetroLabel NameLabel { get { return m_nameLabel; } }
            protected MetroPanel Content { get { return m_propertiesListPanel; } }

            #endregion



            #region Construction and destruction.

            public PropertyControl(string name, PropertiesModel.Property property)
            {
                MetroSkinManager.ApplyMetroStyle(this);

                m_propertiesListPanel = new MetroPanel();
                MetroSkinManager.ApplyMetroStyle(m_propertiesListPanel);
                m_propertiesListPanel.Width = 0;
                m_propertiesListPanel.Height = 0;
                m_propertiesListPanel.AutoSize = true;
                m_propertiesListPanel.Dock = DockStyle.Fill;
                Controls.Add(m_propertiesListPanel);

                m_nameLabel = new MetroLabel();
                MetroSkinManager.ApplyMetroStyle(m_nameLabel);
                m_nameLabel.Dock = DockStyle.Top;
                m_nameLabel.Text = String.IsNullOrEmpty(name) ? property.Name : name;
                m_nameLabel.BorderStyle = BorderStyle.FixedSingle;
                Controls.Add(m_nameLabel);

                MetroPanel padding = new MetroPanel();
                MetroSkinManager.ApplyMetroStyle(padding);
                padding.Width = 0;
                padding.Height = 8;
                padding.Dock = DockStyle.Bottom;
                Controls.Add(padding);

                padding = new MetroPanel();
                MetroSkinManager.ApplyMetroStyle(padding);
                padding.Width = 0;
                padding.Height = 8;
                padding.Dock = DockStyle.Top;
                Controls.Add(padding);

                Initialize(property);
            }

            public new void Dispose()
            {
                Clear();
                base.Dispose();
                m_propertiesListPanel = null;
            }

            #endregion



            #region Public events.

            public event EventHandler ValueChanged;
            public event EventHandler LayoutChanged;

            #endregion



            #region Public functionalities.

            public virtual void Initialize(PropertiesModel.Property property)
            {
                Clear();
                m_propertyModel = property;
                m_propertyModel.ValueChanged += m_propertyModel_ValueChanged;
                OnValueChanged(new EventArgs());
            }

            public virtual void Clear()
            {
                if (m_propertiesListPanel != null)
                    m_propertiesListPanel.Controls.Clear();
                if (m_propertyModel != null)
                    m_propertyModel.ValueChanged -= m_propertyModel_ValueChanged;
                OnValueChanged(new EventArgs());
                m_propertyModel = null;
            }

            public abstract void UpdateEditor();

            #endregion



            #region Protected functionalities.

            protected virtual void OnValueChanged(EventArgs e)
            {
                if (ValueChanged != null)
                    ValueChanged(this, e);
                UpdateEditor();
            }

            protected virtual void OnLayoutChanged(EventArgs e)
            {
                if (LayoutChanged != null)
                    LayoutChanged(this, e);
            }

            #endregion



            #region Private events handlers.

            private void m_propertyModel_ValueChanged(object sender, EventArgs e)
            {
                OnValueChanged(new EventArgs());
            }

            #endregion
        }

        public class CustomPropertyControl : PropertyControl
        {
            #region Public static functionalities.

            public new static JToken GetDefaultValue()
            {
                return JValue.FromObject(new Dictionary<string, JToken>());
            }

            #endregion



            #region Private data.

            private TypeDescriptor m_typeDescriptor;
            private PropertyControlsRegistry m_registry;
            private Dictionary<string, PropertyControl> m_editors;

            #endregion



            #region Construction and destruction.

            public CustomPropertyControl(TypeDescriptor descriptor, PropertyControlsRegistry registry, string name, PropertiesModel.Property property)
                : base(name, property)
            {
                if (descriptor == null)
                    throw new ArgumentNullException("`descriptor` argument cannot be null!");
                if (registry == null)
                    throw new ArgumentNullException("`registry` argument cannot be null!");

                m_typeDescriptor = descriptor;
                m_registry = registry;
                Initialize(property);
            }

            #endregion



            #region Public functionalities.

            public override void Initialize(PropertiesModel.Property property)
            {
                if (m_typeDescriptor == null || m_registry == null)
                    return;

                base.Initialize(property);
                Content.AutoSize = false;
                Content.Width = 0;
                Content.Height = 0;
                Content.AutoSize = true;
                m_editors = new Dictionary<string, PropertyControl>();

                if (PropertyModel.Value == null)
                {
                    MetroButton makeDefaultButton = new MetroButton();
                    MetroSkinManager.ApplyMetroStyle(makeDefaultButton);
                    makeDefaultButton.Dock = DockStyle.Top;
                    makeDefaultButton.Text = "Make default value";
                    makeDefaultButton.Click += makeDefaultButton_Click;
                    Content.Controls.Add(makeDefaultButton);
                }
                else
                {
                    if (m_typeDescriptor.Properties.Count > 0)
                    {
                        MetroPanel content = new MetroPanel();
                        MetroSkinManager.ApplyMetroStyle(content);
                        content.AutoSize = true;
                        content.Dock = DockStyle.Fill;

                        var ordered = m_typeDescriptor.Properties.OrderBy(p => p.Key);
                        KeyValuePair<string, PropertyDescriptor> kv;
                        for (int i = ordered.Count() - 1; i >= 0; --i)
                        {
                            kv = ordered.ElementAt(i);
                            if (!String.IsNullOrEmpty(kv.Value.EditorID))
                            {
                                try
                                {
                                    var value = property.Value[kv.Key];
                                    var model = new PropertiesModel.Property(kv.Key, kv.Value.EditorID, value);
                                    PropertyControl control = m_registry.CreateControl(kv.Value.EditorID, kv.Key, model);
                                    if (control != null)
                                    {
                                        control.Tag = kv.Key;
                                        control.Width = 0;
                                        control.Height = 0;
                                        control.AutoSize = true;
                                        control.Dock = DockStyle.Top;
                                        control.ValueChanged += control_ValueChanged;
                                        content.Controls.Add(control);
                                        m_editors[kv.Key] = control;
                                    }
                                    else
                                    {
                                        Label errorLabel = new Label();
                                        errorLabel.Cursor = Cursors.Help;
                                        errorLabel.Dock = DockStyle.Top;
                                        errorLabel.Padding = new Padding(4);
                                        errorLabel.BackColor = Color.Red;
                                        errorLabel.ForeColor = Color.White;
                                        errorLabel.Text = "Couldn't create " + property.Name + " property editor of type: " + property.Editor;
                                        errorLabel.Click += errorLabel_Click;
                                        errorLabel.Paint += errorLabel_Paint;
                                        content.Controls.Add(errorLabel);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Label errorLabel = new Label();
                                    errorLabel.Cursor = Cursors.Help;
                                    errorLabel.Dock = DockStyle.Top;
                                    errorLabel.Padding = new Padding(4);
                                    errorLabel.BackColor = Color.Red;
                                    errorLabel.ForeColor = Color.White;
                                    errorLabel.Text = ex.Message;
                                    errorLabel.Click += errorLabel_Click;
                                    errorLabel.Paint += errorLabel_Paint;
                                    content.Controls.Add(errorLabel);
                                }
                            }
                        }
                        MetroPanel paddingLeft = new MetroPanel();
                        MetroSkinManager.ApplyMetroStyle(paddingLeft);
                        paddingLeft.Width = 8;
                        paddingLeft.Dock = DockStyle.Left;

                        MetroPanel paddingRight = new MetroPanel();
                        MetroSkinManager.ApplyMetroStyle(paddingRight);
                        paddingRight.Width = 8;
                        paddingRight.Dock = DockStyle.Right;

                        MetroButton deleteButton = new MetroButton();
                        MetroSkinManager.ApplyMetroStyle(deleteButton);
                        deleteButton.Dock = DockStyle.Top;
                        deleteButton.Text = "Delete value";
                        deleteButton.Click += deleteButton_Click;

                        paddingLeft.Height = paddingRight.Height = content.Height;
                        Content.Controls.Add(content);
                        Content.Controls.Add(paddingLeft);
                        Content.Controls.Add(paddingRight);
                        Content.Controls.Add(deleteButton);
                    }
                }
            }

            public override void Clear()
            {
                base.Clear();
                if (m_editors != null)
                    foreach (var kv in m_editors)
                        kv.Value.ValueChanged -= control_ValueChanged;
                m_editors = null;
            }

            public override void UpdateEditor()
            {
                if (m_editors != null)
                    foreach (var kv in m_editors)
                        kv.Value.UpdateEditor();
            }

            #endregion



            #region Private events handlers.

            private void makeDefaultButton_Click(object sender, EventArgs e)
            {
                if (m_typeDescriptor != null && m_typeDescriptor.Properties.Count > 0)
                {
                    var v = new Dictionary<string, object>();
                    foreach (var kv in m_typeDescriptor.Properties)
                        v.Add(kv.Key, null);
                    PropertyModel.Value = JValue.FromObject(v);
                    Initialize(PropertyModel);
                    OnLayoutChanged(new EventArgs());
                }
            }

            private void deleteButton_Click(object sender, EventArgs e)
            {
                PropertyModel.Value = null;
                Initialize(PropertyModel);
                OnLayoutChanged(new EventArgs());
            }

            private void control_ValueChanged(object sender, EventArgs e)
            {
                if (PropertyModel == null)
                    return;

                PropertiesControl.PropertyControl control = sender as PropertiesControl.PropertyControl;
                if (control == null)
                    return;

                string name = control.Tag as string;
                if (String.IsNullOrEmpty(name))
                    return;

                PropertyModel.Value[name] = control.PropertyModel.Value;
                OnValueChanged(new EventArgs());
            }

            private void errorLabel_Click(object sender, EventArgs e)
            {
                Label label = sender as Label;
                if (label == null)
                    return;

                MainForm mainForm = FindForm() as MainForm;
                if (mainForm == null)
                    return;

                MetroMessageBox.Show(mainForm, label.Text, "Property editor creation error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            private void errorLabel_Paint(object sender, PaintEventArgs e)
            {
                Label label = sender as Label;
                if (label != null)
                    ControlPaint.DrawBorder(e.Graphics, label.DisplayRectangle, label.ForeColor, ButtonBorderStyle.Solid);
            }

            #endregion
        }

        public class BrowserPropertyControl : PropertyControl
        {
            #region Public static functionalities.

            public new static JToken GetDefaultValue()
            {
                return JValue.FromObject(new Dictionary<string, JToken>());
            }

            #endregion



            #region Public nested classes.

            public class EditorDialog : MetroFramework.Forms.MetroForm
            {
                #region Private data.

                private BrowserControl m_browser;

                #endregion



                #region Public properties.

                public string BrowserFile { get; set; }
                public JToken Value { get; set; }

                #endregion



                #region Construction and destruction.

                public EditorDialog()
                {
                    MetroSkinManager.ApplyMetroStyle(this);
                    Size = new Size(640, 480);
                    MinimumSize = new Size(240, 120);
                    FormClosing += EditorDialog_FormClosing;
                    FormClosed += EditorDialog_FormClosed;

                    m_browser = new BrowserControl();
                    MetroSkinManager.ApplyMetroStyle(m_browser);
                    m_browser.Dock = DockStyle.Fill;
                    m_browser.Ready += m_browser_Ready;
                    Controls.Add(m_browser);
                }

                #endregion



                #region Public functionalities.

                public new DialogResult ShowDialog()
                {
                    if (String.IsNullOrEmpty(BrowserFile) || !File.Exists(BrowserFile))
                    {
                        DialogResult = DialogResult.Cancel;
                        return DialogResult;
                    }
                    m_browser.Navigate(BrowserFile);
                    return base.ShowDialog();
                }

                #endregion



                #region Private functionalities.

                private void OnEditorCloseMessage(string data)
                {
                    Value = JsonConvert.DeserializeObject<JToken>(data);
                    Close();
                }

                private void OnEditorApplyValueMessage(string data)
                {
                    Value = JsonConvert.DeserializeObject<JToken>(data);
                }

                private void OnEditorRequestValueMessage()
                {
                    string script = String.Format("window.onEditorUpdateValue && window.onEditorUpdateValue({0});", Value == null ? "null" : Value.ToString());
                    m_browser.EvaluateScript(script);
                }

                private void OnEditorApplyWindowSizeMessage(string data)
                {
                    var size = JsonConvert.DeserializeObject<Dictionary<string, int>>(data);
                    if (size != null)
                        Size = new Size(
                            size.ContainsKey("width") ? size["width"] : Size.Width,
                            size.ContainsKey("height") ? size["height"] : Size.Height
                            );
                }

                #endregion



                #region Private events handlers.

                private void EditorDialog_FormClosing(object sender, FormClosingEventArgs e)
                {
                    m_browser.EvaluateScript("window.onEditorClose && window.onEditorClose();");
                }

                private void EditorDialog_FormClosed(object sender, FormClosedEventArgs e)
                {
                    DialogResult = DialogResult.OK;
                }

                private void m_browser_Ready(object sender, EventArgs e)
                {
                    m_browser.AddMessageEventListener("pgEditorClose", data => OnEditorCloseMessage(data), true);
                    m_browser.AddMessageEventListener("pgEditorApplyValue", data => OnEditorApplyValueMessage(data), true);
                    m_browser.AddMessageEventListener("pgEditorRequestValue", data => OnEditorRequestValueMessage(), true);
                    m_browser.AddMessageEventListener("pgEditorApplyWindowSize", data => OnEditorApplyWindowSizeMessage(data), true);
                    string wrapperPath = "resources/templates/api.wrapper.js";
                    if (!File.Exists(wrapperPath))
                    {
                        Close();
                        DialogResult = DialogResult.Cancel;
                        MetroMessageBox.Show(this, "Editor could not load API wrapper!", "Property editor error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    string wrapperScript = File.ReadAllText(wrapperPath);
                    m_browser.EvaluateScript(wrapperScript);
                    string script = String.Format("window.onEditorInitialize ? window.onEditorInitialize({0}) : false;", Value == null ? "null" : Value.ToString());
                    if (!m_browser.EvaluateScript<bool>(script))
                    {
                        Close();
                        DialogResult = DialogResult.Cancel;
                        MetroMessageBox.Show(this, "Editor was not properly initialized!", "Property editor error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                #endregion
            }

            #endregion



            #region Private data.

            private string m_editorFile;

            #endregion



            #region Construction and destruction.

            public BrowserPropertyControl(string editorFile, string name, PropertiesModel.Property property)
                : base(name, property)
            {
                if (String.IsNullOrEmpty(editorFile))
                    throw new ArgumentNullException("`descriptor` argument cannot be null or empty!");

                m_editorFile = editorFile;
                Initialize(property);
            }

            #endregion



            #region Public functionalities.

            public override void Initialize(PropertiesModel.Property property)
            {
                if (String.IsNullOrEmpty(m_editorFile))
                    return;

                base.Initialize(property);

                MetroButton editButton = new MetroButton();
                MetroSkinManager.ApplyMetroStyle(editButton);
                editButton.Dock = DockStyle.Top;
                editButton.Text = "Edit value";
                editButton.Click += editButton_Click;
                Content.Controls.Add(editButton);
            }

            public override void UpdateEditor()
            {
            }

            #endregion



            #region Private event handlers.

            private void editButton_Click(object sender, EventArgs e)
            {
                EditorDialog dialog = new EditorDialog();
                dialog.Text = "Edit property: " + PropertyModel.Name;
                dialog.BrowserFile = m_editorFile;
                dialog.Value = PropertyModel.Value;
                if (dialog.ShowDialog() == DialogResult.OK)
                    PropertyModel.Value = dialog.Value;
            }

            #endregion
        }

        public class ArrayPropertyEditor : PropertyControl
        {
            #region Public nested classes.

            public class EditorDialog : MetroFramework.Forms.MetroForm
            {
                #region Private data.

                private MetroPanel m_content;
                private string m_editorId;
                private PropertyControlsRegistry m_typesRegistry;
                private MetroScrollBar m_scrollBar;

                #endregion



                #region Public properties.

                public JToken Value { get; set; }

                #endregion



                #region Construction and destruction.

                public EditorDialog(string editorId, PropertyControlsRegistry registry)
                {
                    if (String.IsNullOrEmpty(editorId))
                        throw new ArgumentException("`editorsIds` argument cannot be null or empty!");
                    if (registry == null)
                        throw new ArgumentNullException("`registry` argument cannot be null!");

                    m_editorId = editorId;
                    m_typesRegistry = registry;

                    MetroSkinManager.ApplyMetroStyle(this);
                    Text = "Edit array of: " + m_editorId;
                    Size = new Size(480, 640);
                    MinimumSize = new Size(240, 320);
                    FormClosed += EditorDialog_FormClosed;

                    m_content = new MetroPanel();
                    MetroSkinManager.ApplyMetroStyle(m_content);
                    m_content.Dock = DockStyle.Fill;
                    Controls.Add(m_content);

                    MetroPanel toolbar = new MetroPanel();
                    MetroSkinManager.ApplyMetroStyle(toolbar);
                    toolbar.Width = 0;
                    toolbar.Height = 0;
                    toolbar.AutoSize = true;
                    toolbar.Dock = DockStyle.Bottom;
                    Controls.Add(toolbar);

                    MetroButton addButton = new MetroButton();
                    MetroSkinManager.ApplyMetroStyle(addButton);
                    addButton.Text = "Append";
                    addButton.Click += addButton_Click;
                    toolbar.Controls.Add(addButton);

                    m_scrollBar = new MetroScrollBar(MetroScrollOrientation.Vertical);
                    MetroSkinManager.ApplyMetroStyle(m_scrollBar);
                    m_scrollBar.Dock = DockStyle.Right;
                    m_scrollBar.Scroll += m_scrollBar_Scroll;
                    Controls.Add(m_scrollBar);
                }

                #endregion



                #region Public functionalities.

                public new DialogResult ShowDialog()
                {
                    RebuildContent();
                    return base.ShowDialog();
                }

                public void RebuildContent()
                {
                    if (m_content.Controls.Count > 0)
                    {
                        foreach (var c in m_content.Controls)
                        {
                            PropertyControl pc = c as PropertyControl;
                            if (pc != null)
                            {
                                pc.ValueChanged -= control_ValueChanged;
                                pc.LayoutChanged -= control_UpdateScrollbar;
                            }
                        }
                    }
                    m_content.Controls.Clear();
                    if (!m_typesRegistry.ControlExists(m_editorId))
                        throw new Exception("There is no registered editor type named: " + m_editorId);

                    if (Value == null || Value.Type != JTokenType.Array)
                        Value = new JArray();

                    JArray arr = Value as JArray;
                    int i = 0;
                    List<Control> list = new List<Control>();
                    foreach (var v in arr)
                    {
                        PropertiesModel.Property model = new PropertiesModel.Property(i.ToString(), m_editorId, v);
                        PropertyControl control = m_typesRegistry.CreateControl(m_editorId, i.ToString(), model);
                        if (control != null)
                        {
                            control.Tag = i;
                            control.Width = 0;
                            control.Height = 0;
                            control.AutoSize = true;
                            control.Dock = DockStyle.Top;
                            control.ValueChanged += control_ValueChanged;
                            control.LayoutChanged += control_UpdateScrollbar;
                            MetroPanel panel = new MetroPanel();
                            MetroSkinManager.ApplyMetroStyle(panel);
                            panel.Height = 24;
                            panel.Dock = DockStyle.Top;
                            MetroButton deleteButton = new MetroButton();
                            MetroSkinManager.ApplyMetroStyle(deleteButton);
                            deleteButton.Tag = i;
                            deleteButton.Width = 0;
                            deleteButton.Height = 24;
                            deleteButton.AutoSize = true;
                            deleteButton.Dock = DockStyle.Left;
                            deleteButton.Text = "Delete";
                            deleteButton.Click += deleteButton_Click;
                            panel.Controls.Add(deleteButton);
                            MetroButton insertButton = new MetroButton();
                            MetroSkinManager.ApplyMetroStyle(insertButton);
                            insertButton.Tag = i;
                            insertButton.Width = 0;
                            insertButton.Height = 24;
                            insertButton.AutoSize = true;
                            insertButton.Dock = DockStyle.Left;
                            insertButton.Text = "Insert";
                            insertButton.Click += insertButton_Click;
                            panel.Controls.Add(insertButton);
                            list.Add(panel);
                            list.Add(control);
                            ++i;
                        }
                        else
                        {
                            Label errorLabel = new Label();
                            errorLabel.Cursor = Cursors.Help;
                            errorLabel.Dock = DockStyle.Top;
                            errorLabel.Padding = new Padding(4);
                            errorLabel.BackColor = Color.Red;
                            errorLabel.ForeColor = Color.White;
                            errorLabel.Text = "Couldn't create " + model.Name + " property editor of type: " + model.Editor;
                            errorLabel.Click += errorLabel_Click;
                            errorLabel.Paint += errorLabel_Paint;
                            list.Add(errorLabel);
                        }
                    }
                    for (i = list.Count - 1; i >= 0; --i)
                        m_content.Controls.Add(list[i]);

                    UpdateScrollbar();
                }

                public void UpdateScrollbar()
                {
                    Rectangle rect;
                    m_content.CalculateContentsRectangle(out rect);
                    m_scrollBar.Maximum = rect.Height;
                    m_scrollBar.LargeChange = m_content.Height;
                    m_scrollBar.Visible = true;
                    m_content.VerticalScroll.Maximum = m_scrollBar.Maximum;
                    m_content.VerticalScroll.LargeChange = m_scrollBar.LargeChange;
                    m_content.VerticalScroll.Value = Math.Min(m_scrollBar.Value, m_content.VerticalScroll.Maximum);
                }

                #endregion



                #region Private events handlers.

                private void EditorDialog_FormClosed(object sender, FormClosedEventArgs e)
                {
                    DialogResult = DialogResult.OK;
                }

                private void control_UpdateScrollbar(object sender, EventArgs e)
                {
                    UpdateScrollbar();
                }

                private void m_scrollBar_Scroll(object sender, ScrollEventArgs e)
                {
                    m_content.VerticalScroll.Value = Math.Min(e.NewValue, m_content.VerticalScroll.Maximum);
                }

                private void addButton_Click(object sender, EventArgs e)
                {
                    JArray arr = Value as JArray;
                    arr.Add(null);
                    RebuildContent();
                }

                private void control_ValueChanged(object sender, EventArgs e)
                {
                    PropertyControl control = sender as PropertyControl;
                    if (control == null)
                        return;

                    if (!(control.Tag is int))
                        return;

                    JArray arr = Value as JArray;
                    int index = (int)control.Tag;
                    if (index >= 0 && index < arr.Count)
                        arr[index] = control.PropertyModel.Value;
                }

                private void errorLabel_Click(object sender, EventArgs e)
                {
                    Label label = sender as Label;
                    if (label == null)
                        return;

                    MainForm mainForm = FindForm() as MainForm;
                    if (mainForm == null)
                        return;

                    MetroMessageBox.Show(mainForm, label.Text, "Property editor creation error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                private void errorLabel_Paint(object sender, PaintEventArgs e)
                {
                    Label label = sender as Label;
                    if (label != null)
                        ControlPaint.DrawBorder(e.Graphics, label.DisplayRectangle, label.ForeColor, ButtonBorderStyle.Solid);
                }

                private void insertButton_Click(object sender, EventArgs e)
                {
                    Control control = sender as Control;
                    if (control == null)
                        return;

                    if (!(control.Tag is int))
                        return;

                    JArray arr = Value as JArray;
                    int index = (int)control.Tag;
                    if (index >= 0 && index < arr.Count)
                    {
                        arr.Insert(index, null);
                        RebuildContent();
                    }
                }

                private void deleteButton_Click(object sender, EventArgs e)
                {
                    Control control = sender as Control;
                    if (control == null)
                        return;

                    if (!(control.Tag is int))
                        return;

                    JArray arr = Value as JArray;
                    int index = (int)control.Tag;
                    if (index >= 0 && index < arr.Count)
                    {
                        arr.RemoveAt(index);
                        RebuildContent();
                    }
                }

                #endregion
            }

            #endregion



            #region Public static functionalities.

            public new static JToken GetDefaultValue()
            {
                return JValue.FromObject(new List<JToken>());
            }

            #endregion



            #region Private data.

            private PropertyControlsRegistry m_typesRegistry;
            private string m_editorId;
            private MetroButton m_exploreButton;

            #endregion



            #region Construction and destruction.

            public ArrayPropertyEditor(string editorId, PropertyControlsRegistry registry, string name, PropertiesModel.Property property)
                : base(name, property)
            {
                if (String.IsNullOrEmpty(editorId))
                    throw new ArgumentException("`editorId` argument cannot be null or empty!");
                if (registry == null)
                    throw new ArgumentNullException("`registry` argument cannot be null!");

                m_editorId = editorId;
                m_typesRegistry = registry;
                Initialize(property);
            }

            #endregion



            #region Public functionalities.

            public override void Initialize(PropertiesModel.Property property)
            {
                base.Initialize(property);

                m_exploreButton = new MetroButton();
                MetroSkinManager.ApplyMetroStyle(m_exploreButton);
                m_exploreButton.Dock = DockStyle.Top;
                m_exploreButton.Text = "Explore array";
                m_exploreButton.Click += exploreButton_Click;
                Content.Controls.Add(m_exploreButton);
            }

            public override void UpdateEditor()
            {
            }

            #endregion



            #region Private event handlers.

            private void exploreButton_Click(object sender, EventArgs e)
            {
                EditorDialog dialog = new EditorDialog(m_editorId, m_typesRegistry);
                dialog.Value = PropertyModel.Value;
                if (dialog.ShowDialog() == DialogResult.OK)
                    PropertyModel.Value = dialog.Value;
            }

            #endregion
        }

        public class ObjectPropertyEditor : PropertyControl
        {
            #region Public nested classes.

            public class EditorDialog : MetroFramework.Forms.MetroForm
            {
                #region Private data.

                private MetroPanel m_content;
                private string m_editorId;
                private PropertyControlsRegistry m_typesRegistry;
                private MetroScrollBar m_scrollBar;

                #endregion



                #region Public properties.

                public JToken Value { get; set; }

                #endregion



                #region Construction and destruction.

                public EditorDialog(string editorId, PropertyControlsRegistry registry)
                {
                    if (String.IsNullOrEmpty(editorId))
                        throw new ArgumentException("`editorsIds` argument cannot be null or empty!");
                    if (registry == null)
                        throw new ArgumentNullException("`registry` argument cannot be null!");

                    m_editorId = editorId;
                    m_typesRegistry = registry;

                    MetroSkinManager.ApplyMetroStyle(this);
                    Text = "Edit object of: " + m_editorId;
                    Size = new Size(480, 640);
                    MinimumSize = new Size(240, 320);
                    FormClosed += EditorDialog_FormClosed;

                    m_content = new MetroPanel();
                    MetroSkinManager.ApplyMetroStyle(m_content);
                    m_content.Dock = DockStyle.Fill;
                    Controls.Add(m_content);

                    MetroPanel toolbar = new MetroPanel();
                    MetroSkinManager.ApplyMetroStyle(toolbar);
                    toolbar.Width = 0;
                    toolbar.Height = 0;
                    toolbar.AutoSize = true;
                    toolbar.Dock = DockStyle.Bottom;
                    Controls.Add(toolbar);

                    MetroButton addButton = new MetroButton();
                    MetroSkinManager.ApplyMetroStyle(addButton);
                    addButton.Text = "Add";
                    addButton.Click += addButton_Click;
                    toolbar.Controls.Add(addButton);

                    m_scrollBar = new MetroScrollBar(MetroScrollOrientation.Vertical);
                    MetroSkinManager.ApplyMetroStyle(m_scrollBar);
                    m_scrollBar.Dock = DockStyle.Right;
                    m_scrollBar.Scroll += m_scrollBar_Scroll;
                    Controls.Add(m_scrollBar);
                }

                #endregion



                #region Public functionalities.

                public new DialogResult ShowDialog()
                {
                    RebuildContent();
                    return base.ShowDialog();
                }

                public void RebuildContent()
                {
                    if (m_content.Controls.Count > 0)
                    {
                        foreach (var c in m_content.Controls)
                        {
                            PropertyControl pc = c as PropertyControl;
                            if (pc != null)
                            {
                                pc.ValueChanged -= control_ValueChanged;
                                pc.LayoutChanged -= control_UpdateScrollbar;
                            }
                        }
                    }
                    m_content.Controls.Clear();
                    if (!m_typesRegistry.ControlExists(m_editorId))
                        throw new Exception("There is no registered editor type named: " + m_editorId);

                    if (Value == null || Value.Type != JTokenType.Object)
                        Value = new JObject();

                    JObject obj = Value as JObject;
                    List<Control> list = new List<Control>();
                    foreach (var p in obj.Properties().OrderBy(p => p.Name))
                    {
                        PropertiesModel.Property model = new PropertiesModel.Property(p.Name, m_editorId, p.Value.Type == JTokenType.Null ? null : p.Value);
                        PropertyControl control = m_typesRegistry.CreateControl(m_editorId, p.Name, model);
                        if (control != null)
                        {
                            control.Tag = p.Name;
                            control.Width = 0;
                            control.Height = 0;
                            control.AutoSize = true;
                            control.Dock = DockStyle.Top;
                            control.ValueChanged += control_ValueChanged;
                            control.LayoutChanged += control_UpdateScrollbar;
                            MetroPanel panel = new MetroPanel();
                            MetroSkinManager.ApplyMetroStyle(panel);
                            panel.Height = 24;
                            panel.Dock = DockStyle.Top;
                            MetroButton deleteButton = new MetroButton();
                            MetroSkinManager.ApplyMetroStyle(deleteButton);
                            deleteButton.Tag = p.Name;
                            deleteButton.Width = 0;
                            deleteButton.Height = 24;
                            deleteButton.AutoSize = true;
                            deleteButton.Dock = DockStyle.Left;
                            deleteButton.Text = "Delete";
                            deleteButton.Click += deleteButton_Click;
                            panel.Controls.Add(deleteButton);
                            MetroButton renameButton = new MetroButton();
                            MetroSkinManager.ApplyMetroStyle(renameButton);
                            renameButton.Tag = p.Name;
                            renameButton.Width = 0;
                            renameButton.Height = 24;
                            renameButton.AutoSize = true;
                            renameButton.Dock = DockStyle.Left;
                            renameButton.Text = "Rename";
                            renameButton.Click += renameButton_Click;
                            panel.Controls.Add(renameButton);
                            list.Add(panel);
                            list.Add(control);
                        }
                        else
                        {
                            Label errorLabel = new Label();
                            errorLabel.Cursor = Cursors.Help;
                            errorLabel.Dock = DockStyle.Top;
                            errorLabel.Padding = new Padding(4);
                            errorLabel.BackColor = Color.Red;
                            errorLabel.ForeColor = Color.White;
                            errorLabel.Text = "Couldn't create " + model.Name + " property editor of type: " + model.Editor;
                            errorLabel.Click += errorLabel_Click;
                            errorLabel.Paint += errorLabel_Paint;
                            list.Add(errorLabel);
                        }
                    }
                    for (int i = list.Count - 1; i >= 0; --i)
                        m_content.Controls.Add(list[i]);

                    UpdateScrollbar();
                }

                public void UpdateScrollbar()
                {
                    Rectangle rect;
                    m_content.CalculateContentsRectangle(out rect);
                    m_scrollBar.Maximum = rect.Height;
                    m_scrollBar.LargeChange = m_content.Height;
                    m_scrollBar.Visible = true;
                    m_content.VerticalScroll.Maximum = m_scrollBar.Maximum;
                    m_content.VerticalScroll.LargeChange = m_scrollBar.LargeChange;
                    m_content.VerticalScroll.Value = Math.Min(m_scrollBar.Value, m_content.VerticalScroll.Maximum);
                }

                #endregion



                #region Private events handlers.

                private void EditorDialog_FormClosed(object sender, FormClosedEventArgs e)
                {
                    DialogResult = DialogResult.OK;
                }

                private void control_UpdateScrollbar(object sender, EventArgs e)
                {
                    UpdateScrollbar();
                }

                private void m_scrollBar_Scroll(object sender, ScrollEventArgs e)
                {
                    m_content.VerticalScroll.Value = Math.Min(e.NewValue, m_content.VerticalScroll.Maximum);
                }

                private void addButton_Click(object sender, EventArgs e)
                {
                    MetroPromptBox dialog = new MetroPromptBox();
                    dialog.Title = "New item name";
                    dialog.Message = "Enter item name:";
                    if (dialog.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty(dialog.Value))
                    {
                        JObject obj = Value as JObject;
                        if (obj.Property(dialog.Value) == null)
                        {
                            obj.Add(dialog.Value, null);
                            RebuildContent();
                        }
                    }
                }

                private void control_ValueChanged(object sender, EventArgs e)
                {
                    PropertyControl control = sender as PropertyControl;
                    if (control == null)
                        return;

                    if (!(control.Tag is string))
                        return;

                    JObject obj = Value as JObject;
                    string key = (string)control.Tag;
                    if (obj.Property(key) != null)
                        obj[key] = control.PropertyModel.Value;
                }

                private void errorLabel_Click(object sender, EventArgs e)
                {
                    Label label = sender as Label;
                    if (label == null)
                        return;

                    MainForm mainForm = FindForm() as MainForm;
                    if (mainForm == null)
                        return;

                    MetroMessageBox.Show(mainForm, label.Text, "Property editor creation error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                private void errorLabel_Paint(object sender, PaintEventArgs e)
                {
                    Label label = sender as Label;
                    if (label != null)
                        ControlPaint.DrawBorder(e.Graphics, label.DisplayRectangle, label.ForeColor, ButtonBorderStyle.Solid);
                }

                private void renameButton_Click(object sender, EventArgs e)
                {
                    Control control = sender as Control;
                    if (control == null)
                        return;

                    if (!(control.Tag is string))
                        return;

                    JObject obj = Value as JObject;
                    string key = (string)control.Tag;
                    if (obj.Property(key) != null)
                    {
                        MetroPromptBox dialog = new MetroPromptBox();
                        dialog.Title = "Change item name";
                        dialog.Message = "Enter item name:";
                        dialog.Value = key;
                        if (dialog.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty(dialog.Value) && obj.Property(dialog.Value) == null)
                        {
                            obj.Add(dialog.Value, obj[key]);
                            obj.Remove(key);
                            RebuildContent();
                        }
                    }
                }

                private void deleteButton_Click(object sender, EventArgs e)
                {
                    Control control = sender as Control;
                    if (control == null)
                        return;

                    if (!(control.Tag is string))
                        return;

                    JObject obj = Value as JObject;
                    string key = (string)control.Tag;
                    if (obj.Property(key) != null)
                    {
                        obj.Remove(key);
                        RebuildContent();
                    }
                }

                #endregion
            }

            #endregion



            #region Public static functionalities.

            public new static JToken GetDefaultValue()
            {
                return JValue.FromObject(new List<JToken>());
            }

            #endregion



            #region Private data.

            private PropertyControlsRegistry m_typesRegistry;
            private string m_editorId;
            private MetroButton m_exploreButton;

            #endregion



            #region Construction and destruction.

            public ObjectPropertyEditor(string editorId, PropertyControlsRegistry registry, string name, PropertiesModel.Property property)
                : base(name, property)
            {
                if (String.IsNullOrEmpty(editorId))
                    throw new ArgumentException("`editorId` argument cannot be null or empty!");
                if (registry == null)
                    throw new ArgumentNullException("`registry` argument cannot be null!");

                m_editorId = editorId;
                m_typesRegistry = registry;
                Initialize(property);
            }

            #endregion



            #region Public functionalities.

            public override void Initialize(PropertiesModel.Property property)
            {
                base.Initialize(property);

                m_exploreButton = new MetroButton();
                MetroSkinManager.ApplyMetroStyle(m_exploreButton);
                m_exploreButton.Dock = DockStyle.Top;
                m_exploreButton.Text = "Explore object";
                m_exploreButton.Click += exploreButton_Click;
                Content.Controls.Add(m_exploreButton);
            }

            public override void UpdateEditor()
            {
            }

            #endregion



            #region Private event handlers.

            private void exploreButton_Click(object sender, EventArgs e)
            {
                EditorDialog dialog = new EditorDialog(m_editorId, m_typesRegistry);
                dialog.Value = PropertyModel.Value;
                if (dialog.ShowDialog() == DialogResult.OK)
                    PropertyModel.Value = dialog.Value;
            }

            #endregion
        }

        #endregion



        #region Private data.

        private PropertiesModel m_model;
        private PropertyControlsRegistry m_registry;
        private Dictionary<string, PropertyControl> m_properties;
        private MetroPanel m_propertiesListPanel;
        private MetroScrollBar m_propertiesListScrollbar;

        #endregion



        #region Construction and destruction.

        public PropertiesControl(string jsonModel, PropertyControlsRegistry registry)
            : this(String.IsNullOrEmpty(jsonModel) ? null : JsonConvert.DeserializeObject<PropertiesModel>(jsonModel), registry)
        {
        }

        public PropertiesControl(JToken tokenModel, PropertyControlsRegistry registry)
            : this(tokenModel == null ? null : tokenModel.ToObject<PropertiesModel>(), registry)
        {
        }

        public PropertiesControl(PropertiesModel model, PropertyControlsRegistry registry)
        {
            if (registry == null)
                throw new ArgumentNullException("`registry` argument cannot be null!");
            m_model = model == null ? new PropertiesModel() : model;
            m_registry = registry;
            m_properties = new Dictionary<string, PropertyControl>();

            MetroSkinManager.ApplyMetroStyle(this);
            Resize += control_UpdateScrollbar;

            m_propertiesListPanel = new MetroPanel();
            MetroSkinManager.ApplyMetroStyle(m_propertiesListPanel);
            m_propertiesListPanel.Dock = DockStyle.Fill;
            m_propertiesListPanel.Resize += control_UpdateScrollbar;
            Controls.Add(m_propertiesListPanel);

            m_propertiesListScrollbar = new MetroScrollBar(MetroScrollOrientation.Vertical);
            MetroSkinManager.ApplyMetroStyle(m_propertiesListScrollbar);
            m_propertiesListScrollbar.Dock = DockStyle.Right;
            m_propertiesListScrollbar.Scroll += m_propertiesListScrollbar_Scroll;
            Controls.Add(m_propertiesListScrollbar);

            RebuildContent();
        }

        #endregion



        #region Public functionalities.

        public void RebuildContent()
        {
            if (m_properties != null)
                foreach (var kv in m_properties)
                    kv.Value.LayoutChanged -= control_UpdateScrollbar;

            m_propertiesListPanel.Controls.Clear();
            m_properties.Clear();

            if (m_model != null && m_model.Properties != null && m_model.Properties.Count > 0)
            {
                PropertiesModel.Property property;
                for (int i = m_model.Properties.Count - 1; i >= 0; --i)
                {
                    property = m_model.Properties[i];
                    try
                    {
                        PropertyControl control = m_registry.CreateControl(property.Editor, property.Name, property);
                        if (control != null)
                        {
                            control.Width = 0;
                            control.Height = 0;
                            control.AutoSize = true;
                            control.Dock = DockStyle.Top;
                            control.LayoutChanged += control_UpdateScrollbar;
                            m_propertiesListPanel.Controls.Add(control);
                            m_properties[property.Name] = control;
                        }
                        else
                        {
                            Label errorLabel = new Label();
                            errorLabel.Cursor = Cursors.Help;
                            errorLabel.Dock = DockStyle.Top;
                            errorLabel.Padding = new Padding(4);
                            errorLabel.BackColor = Color.Red;
                            errorLabel.ForeColor = Color.White;
                            errorLabel.Text = "Couldn't create " + property.Name + " property editor of type: " + property.Editor;
                            errorLabel.Click += errorLabel_Click;
                            errorLabel.Paint += errorLabel_Paint;
                            m_propertiesListPanel.Controls.Add(errorLabel);
                        }
                    }
                    catch (Exception ex)
                    {
                        Label errorLabel = new Label();
                        errorLabel.Cursor = Cursors.Help;
                        errorLabel.Dock = DockStyle.Top;
                        errorLabel.Padding = new Padding(4);
                        errorLabel.BackColor = Color.Red;
                        errorLabel.ForeColor = Color.White;
                        errorLabel.Text = ex.Message;
                        errorLabel.Click += errorLabel_Click;
                        errorLabel.Paint += errorLabel_Paint;
                        m_propertiesListPanel.Controls.Add(errorLabel);
                    }
                }
            }

            UpdateContent();
            UpdateScrollbar();
        }

        public void UpdateContent(string propertyName = null)
        {
            if (String.IsNullOrEmpty(propertyName))
            {
                foreach (var control in m_properties.Values)
                {
                    PropertyControl property = control as PropertyControl;
                    if (property != null)
                        property.UpdateEditor();
                }
            }
            else
            {
                if (m_properties.ContainsKey(propertyName))
                    m_properties[propertyName].UpdateEditor();
            }
        }

        public void UpdateScrollbar()
        {
            Rectangle rect;
            m_propertiesListPanel.CalculateContentsRectangle(out rect);
            m_propertiesListScrollbar.Maximum = rect.Height;
            m_propertiesListScrollbar.LargeChange = m_propertiesListPanel.Height;
            m_propertiesListScrollbar.Visible = true;
            m_propertiesListPanel.VerticalScroll.Maximum = m_propertiesListScrollbar.Maximum;
            m_propertiesListPanel.VerticalScroll.LargeChange = m_propertiesListScrollbar.LargeChange;
            m_propertiesListPanel.VerticalScroll.Value = Math.Min(m_propertiesListScrollbar.Value, m_propertiesListPanel.VerticalScroll.Maximum);
        }

        #endregion



        #region Private event handlers.

        private void control_UpdateScrollbar(object sender, EventArgs e)
        {
            UpdateScrollbar();
        }

        private void m_propertiesListScrollbar_Scroll(object sender, ScrollEventArgs e)
        {
            m_propertiesListPanel.VerticalScroll.Value = Math.Min(e.NewValue, m_propertiesListPanel.VerticalScroll.Maximum);
        }

        private void errorLabel_Click(object sender, EventArgs e)
        {
            Label label = sender as Label;
            if (label == null)
                return;

            MainForm mainForm = FindForm() as MainForm;
            if (mainForm == null)
                return;

            MetroMessageBox.Show(mainForm, label.Text, "Property editor creation error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void errorLabel_Paint(object sender, PaintEventArgs e)
        {
            Label label = sender as Label;
            if (label != null)
                ControlPaint.DrawBorder(e.Graphics, label.DisplayRectangle, label.ForeColor, ButtonBorderStyle.Solid);
        }

        #endregion
    }
}
