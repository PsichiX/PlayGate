using MetroFramework.Controls;
using MetroFramework.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayGate.PropertyEditors
{
    public class PlayCanvas_Curve_PropertyEditor : PropertiesControl.PropertyControl
    {
        #region Public nested classes.

        public class CurveInfo
        {
            [JsonProperty("min")]
            public float Min { get; set; }
            [JsonProperty("max")]
            public float Max { get; set; }
            [JsonProperty("curves")]
            public string[] Curves { get; set; }
        }

        public class CurveEditDialog : MetroForm
        {
            #region Private data.

            private PlayCanvas_Curve_PropertyEditor m_propertyEditor;
            private PlayCanvas_Curve_PropertyEditor.CurveInfo m_info;
            private PlayCanvas.Curve m_curve;
            private PlayCanvas.CurveSet m_curveSet;
            private int m_activeCurve;
            private float m_activeKey;
            private bool m_isModifyingPoint;
            private PictureBox m_editPictureBox;
            private MetroTextBox m_keyTextBox;
            private MetroTextBox m_valueTextBox;
            private MetroComboBox m_typeComboBox;
            private float m_zoomOut;
            private bool m_isKeyValidNumber;
            private bool m_isValueValidNumber;
            private List<MetroCheckBox> m_curvesCheckBoxes;

            #endregion



            #region Construction and destruction.

            public CurveEditDialog(PlayCanvas_Curve_PropertyEditor propertyEditor)
            {
                if (propertyEditor == null)
                    throw new ArgumentNullException("`propertyEditor` argument cannot be null!");

                m_activeCurve = -1;
                m_activeKey = -1.0f;
                m_zoomOut = 1.0f;
                m_propertyEditor = propertyEditor;
                m_info = m_propertyEditor.Info == null ? new PlayCanvas_Curve_PropertyEditor.CurveInfo() : m_propertyEditor.Info;
                if (m_info.Curves != null && m_info.Curves.Length > 0)
                {
                    m_curveSet = m_propertyEditor.PropertyModel.Data<PlayCanvas.CurveSet>();
                    if (m_curveSet == null)
                        m_curveSet = new PlayCanvas.CurveSet();
                    if (m_curveSet.CurvesList.Count < m_info.Curves.Length)
                        for (int i = m_curveSet.CurvesList.Count; i < m_info.Curves.Length; ++i)
                            m_curveSet.CurvesList.Add(new PlayCanvas.Curve(new float[] { 0.0f, 0.0f }));
                }
                else
                {
                    m_curve = m_propertyEditor.PropertyModel.Data<PlayCanvas.Curve>();
                    if (m_curve == null)
                        m_curve = new PlayCanvas.Curve();
                    if (m_curve.KeyFrames.Count == 0)
                        m_curve.KeyFrames.Add(0.0f, 0.0f);
                    float diff = Math.Max(Math.Abs(m_info.Max), Math.Abs(m_info.Min));
                    while (m_zoomOut < diff)
                        m_zoomOut *= 2.0f;
                }
                ValidateCurve();

                MetroSkinManager.ApplyMetroStyle(this);
                Size = new Size(640, 480);
                MinimumSize = new Size(480, 320);
                Text = (m_curve == null ? "Edit curve set: " : "Edit curve: ") + propertyEditor.PropertyModel.Name;
                FormClosed += CurveEditDialog_FormClosed;

                m_editPictureBox = new PictureBox();
                m_editPictureBox.Dock = DockStyle.Fill;
                m_editPictureBox.BackColor = Color.FromArgb(255, 64, 64, 64);
                m_editPictureBox.Resize += m_editPictureBox_Resize;
                m_editPictureBox.Paint += m_editPictureBox_Paint;
                m_editPictureBox.MouseEnter += m_editPictureBox_MouseEnter;
                m_editPictureBox.MouseDown += m_editPictureBox_MouseDown;
                m_editPictureBox.MouseUp += m_editPictureBox_MouseUp;
                m_editPictureBox.MouseMove += m_editPictureBox_MouseMove;
                m_editPictureBox.MouseWheel += m_editPictureBox_MouseWheel;
                Controls.Add(m_editPictureBox);
                m_editPictureBox.Refresh();

                MetroPanel topToolbar = new MetroPanel();
                MetroSkinManager.ApplyMetroStyle(topToolbar);
                topToolbar.Width = 0;
                topToolbar.Height = 30;
                topToolbar.Dock = DockStyle.Top;
                Controls.Add(topToolbar);

                MetroLabel typeLabel = new MetroLabel();
                MetroSkinManager.ApplyMetroStyle(typeLabel);
                typeLabel.Width = 0;
                typeLabel.Height = 0;
                typeLabel.AutoSize = true;
                typeLabel.Dock = DockStyle.Left;
                typeLabel.Text = "Type:";

                if (m_curveSet != null)
                {
                    m_curvesCheckBoxes = new List<MetroCheckBox>();
                    foreach (var name in m_info.Curves)
                    {
                        MetroCheckBox checkBox = new MetroCheckBox();
                        MetroSkinManager.ApplyMetroStyle(checkBox);
                        checkBox.Width = 0;
                        checkBox.AutoSize = true;
                        checkBox.Checked = true;
                        checkBox.Text = name;
                        checkBox.Dock = DockStyle.Right;
                        checkBox.CheckedChanged += checkBox_CheckedChanged;
                        topToolbar.Controls.Add(checkBox);
                        m_curvesCheckBoxes.Add(checkBox);
                    }
                }

                m_typeComboBox = new MetroComboBox();
                MetroSkinManager.ApplyMetroStyle(m_typeComboBox);
                m_typeComboBox.Dock = DockStyle.Left;
                m_typeComboBox.BindingContext = new BindingContext();
                m_typeComboBox.DataSource = Enum.GetNames(typeof(PlayCanvas.Curve.CurveType));
                if (m_curve != null)
                    m_typeComboBox.SelectedItem = Enum.GetName(typeof(PlayCanvas.Curve.CurveType), m_curve.Type);
                else if (m_curveSet != null)
                    m_typeComboBox.SelectedItem = Enum.GetName(typeof(PlayCanvas.Curve.CurveType), m_curveSet.Type);
                m_typeComboBox.SelectedValueChanged += m_typeComboBox_SelectedValueChanged;

                topToolbar.Controls.Add(m_typeComboBox);
                topToolbar.Controls.Add(typeLabel);

                MetroPanel bottomToolbar = new MetroPanel();
                MetroSkinManager.ApplyMetroStyle(bottomToolbar);
                bottomToolbar.Width = 0;
                bottomToolbar.Height = 24;
                bottomToolbar.Dock = DockStyle.Bottom;
                Controls.Add(bottomToolbar);

                MetroLabel keyLabel = new MetroLabel();
                MetroSkinManager.ApplyMetroStyle(keyLabel);
                keyLabel.Width = 0;
                keyLabel.Height = 0;
                keyLabel.AutoSize = true;
                keyLabel.Dock = DockStyle.Left;
                keyLabel.Text = "Key frame:";

                m_keyTextBox = new MetroTextBox();
                MetroSkinManager.ApplyMetroStyle(m_keyTextBox);
                m_keyTextBox.Width = 100;
                m_keyTextBox.Dock = DockStyle.Left;
                m_keyTextBox.TextChanged += m_keyTextBox_TextChanged;
                m_keyTextBox.CustomPaintForeground += m_keyTextBox_CustomPaintForeground;
                m_keyTextBox.Leave += m_keyTextBox_Leave;

                MetroLabel valueLabel = new MetroLabel();
                MetroSkinManager.ApplyMetroStyle(valueLabel);
                valueLabel.Width = 0;
                valueLabel.Height = 0;
                valueLabel.AutoSize = true;
                valueLabel.Dock = DockStyle.Left;
                valueLabel.Text = "Value:";

                m_valueTextBox = new MetroTextBox();
                MetroSkinManager.ApplyMetroStyle(m_valueTextBox);
                m_valueTextBox.Width = 100;
                m_valueTextBox.Dock = DockStyle.Left;
                m_valueTextBox.TextChanged += m_valueTextBox_TextChanged;
                m_valueTextBox.CustomPaintForeground += m_valueTextBox_CustomPaintForeground;
                m_valueTextBox.Leave += m_valueTextBox_Leave;

                MetroButton resetButton = new MetroButton();
                MetroSkinManager.ApplyMetroStyle(resetButton);
                resetButton.Width = 0;
                resetButton.AutoSize = true;
                resetButton.Dock = DockStyle.Right;
                resetButton.Text = m_curve == null ? "Reset visible curves" : "Reset curve";
                resetButton.Click += resetButton_Click;

                bottomToolbar.Controls.Add(resetButton);
                bottomToolbar.Controls.Add(m_valueTextBox);
                bottomToolbar.Controls.Add(valueLabel);
                bottomToolbar.Controls.Add(m_keyTextBox);
                bottomToolbar.Controls.Add(keyLabel);
            }

            #endregion



            #region Private functionalities.

            private void ValidateCurve()
            {
                if (m_curve != null)
                {
                    foreach (var kv in m_curve.KeyFrames)
                        if (kv.Key < 0.0f || kv.Key > 1.0f)
                            m_curve.KeyFrames.Remove(kv.Key);
                }
                else if (m_curveSet != null)
                {
                    foreach (var c in m_curveSet.CurvesList)
                        if (c != null)
                            foreach (var kv in c.KeyFrames)
                                if (kv.Key < 0.0f || kv.Key > 1.0f)
                                    c.KeyFrames.Remove(kv.Key);
                }
            }

            #endregion



            #region Private events handlers.

            private void CurveEditDialog_FormClosed(object sender, FormClosedEventArgs e)
            {
                DialogResult = DialogResult.OK;
                if (m_propertyEditor != null)
                {
                    if (m_curve != null)
                        m_propertyEditor.PropertyModel.Data<PlayCanvas.Curve>(m_curve);
                    else if (m_curveSet != null)
                        m_propertyEditor.PropertyModel.Data<PlayCanvas.CurveSet>(m_curveSet);
                }
            }

            private void m_editPictureBox_Resize(object sender, EventArgs e)
            {
                m_editPictureBox.Refresh();
            }

            void m_editPictureBox_Paint(object sender, PaintEventArgs e)
            {
                Color color = Color.FromArgb(64, 255, 255, 255);
                Pen pen = new Pen(color);
                Font font = new Font("Verdana", 10);
                SolidBrush brush = new SolidBrush(color);
                Rectangle r = m_editPictureBox.ClientRectangle;
                Point p0;
                Point p1;
                for (float x = 0.0f; x <= 1.0f; x += 0.25f)
                {
                    p0 = new Point(r.Left + (int)((float)r.Width * x), r.Top);
                    p1 = new Point(p0.X, r.Bottom);
                    e.Graphics.DrawLine(pen, p0, p1);
                    e.Graphics.DrawString(x.ToString(CultureInfo.InvariantCulture), font, brush, (float)p0.X, (float)r.Top + (float)r.Height * 0.5f);
                }
                for (float y = 0.0f; y <= m_zoomOut; y += 0.25f * m_zoomOut)
                {
                    p0 = new Point(r.Left, r.Top + (int)(((float)r.Height * 0.5f) - (y * 0.5f * (float)r.Height) / m_zoomOut));
                    p1 = new Point(r.Right, p0.Y);
                    e.Graphics.DrawLine(pen, p0, p1);
                    if (y > 0.0f)
                        e.Graphics.DrawString(y.ToString(CultureInfo.InvariantCulture), font, brush, (float)r.Left, (float)p0.Y);
                }
                for (float y = 0.0f; y >= -m_zoomOut; y -= 0.25f * m_zoomOut)
                {
                    p0 = new Point(r.Left, r.Top + 1 + (int)(((float)r.Height * 0.5f) - (y * 0.5f * (float)r.Height) / m_zoomOut));
                    p1 = new Point(r.Right, p0.Y);
                    e.Graphics.DrawLine(pen, p0, p1);
                    if (y < 0.0f)
                        e.Graphics.DrawString(y.ToString(CultureInfo.InvariantCulture), font, brush, (float)r.Left, (float)p0.Y);
                }
                if (m_curve != null)
                {
                    color = Color.Red;
                    pen = new Pen(color);
                    brush = new SolidBrush(color);
                    float step = 2.0f / (float)m_editPictureBox.Width;
                    float v = 0.0f;
                    float lv = 0.0f;
                    for (float t = 0.0f; t <= 1.0f; t += step)
                    {
                        v = m_curve.Value(t);
                        if (t == 0.0f)
                            lv = v;
                        p0 = new Point(r.Left + (int)((t - step) * r.Width), r.Top + (int)(((float)r.Height * 0.5f) - (lv * 0.5f * (float)r.Height) / m_zoomOut));
                        p1 = new Point(r.Left + (int)(t * r.Width), r.Top + (int)(((float)r.Height * 0.5f) - (v * 0.5f * (float)r.Height) / m_zoomOut));
                        e.Graphics.DrawLine(pen, p0, p1);
                        lv = v;
                    }
                    int x;
                    int y;
                    foreach (var kv in m_curve.KeyFrames)
                    {
                        x = r.Left + (int)((float)r.Width * kv.Key);
                        y = r.Top + (int)(((float)r.Height * 0.5f) - (kv.Value * 0.5f * (float)r.Height) / m_zoomOut);
                        e.Graphics.FillEllipse(brush, x - 6, y - 6, 13, 13);
                    }
                }
                else if (m_curveSet != null)
                {
                    Color[] colors = new Color[] { 
                        Color.Red,
                        Color.GreenYellow,
                        Color.BlueViolet,
                        Color.White
                    };
                    for (int i = 0; i < Math.Min(4, m_curveSet.CurvesList.Count); ++i)
                    {
                        if (!m_curvesCheckBoxes[i].Checked)
                            continue;
                        PlayCanvas.Curve curve = m_curveSet.CurvesList[i];
                        color = colors[i];
                        pen = new Pen(color);
                        brush = new SolidBrush(color);
                        float step = 2.0f / (float)m_editPictureBox.Width;
                        float v = 0.0f;
                        float lv = 0.0f;
                        for (float t = 0.0f; t <= 1.0f; t += step)
                        {
                            v = curve.Value(t);
                            if (t == 0.0f)
                                lv = v;
                            p0 = new Point(r.Left + (int)((t - step) * r.Width), r.Top + (int)(((float)r.Height * 0.5f) - (lv * 0.5f * (float)r.Height) / m_zoomOut));
                            p1 = new Point(r.Left + (int)(t * r.Width), r.Top + (int)(((float)r.Height * 0.5f) - (v * 0.5f * (float)r.Height) / m_zoomOut));
                            e.Graphics.DrawLine(pen, p0, p1);
                            lv = v;
                        }
                        int x;
                        int y;
                        foreach (var kv in curve.KeyFrames)
                        {
                            x = r.Left + (int)((float)r.Width * kv.Key);
                            y = r.Top + (int)(((float)r.Height * 0.5f) - (kv.Value * 0.5f * (float)r.Height) / m_zoomOut);
                            e.Graphics.FillEllipse(brush, x - 6, y - 6, 13, 13);
                        }
                    }
                }
            }

            private void m_editPictureBox_MouseEnter(object sender, EventArgs e)
            {
                m_editPictureBox.Focus();
            }

            private void m_editPictureBox_MouseDown(object sender, MouseEventArgs e)
            {
                ValidateCurve();
                if (m_curve != null)
                {
                    float w = (float)m_editPictureBox.Width;
                    float h = (float)m_editPictureBox.Height;
                    int x;
                    int y;
                    int xm = e.X;
                    int ym = e.Y;
                    string keyText = m_keyTextBox.Text;
                    string valueText = m_valueTextBox.Text;
                    foreach (var kv in m_curve.KeyFrames)
                    {
                        x = (int)(kv.Key * w);
                        y = (int)((h * 0.5f) - (kv.Value * 0.5f * h / m_zoomOut));
                        if (xm > x - 6 && xm < x + 6 && ym > y - 6 && ym < y + 6)
                        {
                            if (e.Button == MouseButtons.Left)
                            {
                                m_activeKey = kv.Key;
                                m_isModifyingPoint = true;
                                keyText = kv.Key.ToString(CultureInfo.InvariantCulture);
                                valueText = kv.Value.ToString(CultureInfo.InvariantCulture);
                                break;
                            }
                            else if (e.Button == MouseButtons.Right)
                            {
                                m_isModifyingPoint = false;
                                m_activeKey = -1.0f;
                                keyText = "";
                                valueText = "";
                                m_curve.KeyFrames.Remove(kv.Key);
                                if (m_curve.KeyFrames.Count == 0)
                                    m_curve.KeyFrames.Add(0.0f, 0.0f);
                                break;
                            }
                        }
                    }
                    if (e.Button == MouseButtons.Left && !m_isModifyingPoint && w > 0.0f && h > 0.0f)
                    {
                        float px = (float)xm / w;
                        float pv = m_curve.Value(px);
                        int py = (int)((h * 0.5f) - (pv * 0.5f * h / m_zoomOut));
                        if (Math.Abs(py - ym) < 6)
                        {
                            m_isModifyingPoint = true;
                            m_activeKey = px;
                            m_curve.KeyFrames.Add(px, pv);
                            keyText = px.ToString(CultureInfo.InvariantCulture);
                            valueText = pv.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            m_activeKey = -1;
                            keyText = "";
                            valueText = "";
                        }
                    }
                    m_keyTextBox.Text = keyText;
                    m_valueTextBox.Text = valueText;
                }
                else if (m_curveSet != null)
                {
                    float w = (float)m_editPictureBox.Width;
                    float h = (float)m_editPictureBox.Height;
                    int x;
                    int y;
                    int xm = e.X;
                    int ym = e.Y;
                    string keyText = m_keyTextBox.Text;
                    string valueText = m_valueTextBox.Text;
                    bool stop = false;
                    for (int i = 0; !stop && i < Math.Min(4, m_curveSet.CurvesList.Count); ++i)
                    {
                        if (!m_curvesCheckBoxes[i].Checked)
                            continue;
                        PlayCanvas.Curve curve = m_curveSet.CurvesList[i];
                        foreach (var kv in curve.KeyFrames)
                        {
                            x = (int)(kv.Key * w);
                            y = (int)((h * 0.5f) - (kv.Value * 0.5f * h / m_zoomOut));
                            if (xm > x - 6 && xm < x + 6 && ym > y - 6 && ym < y + 6)
                            {
                                if (e.Button == MouseButtons.Left)
                                {
                                    m_activeCurve = i;
                                    m_activeKey = kv.Key;
                                    m_isModifyingPoint = true;
                                    keyText = kv.Key.ToString(CultureInfo.InvariantCulture);
                                    valueText = kv.Value.ToString(CultureInfo.InvariantCulture);
                                    stop = true;
                                    break;
                                }
                                else if (e.Button == MouseButtons.Right)
                                {
                                    m_isModifyingPoint = false;
                                    m_activeCurve = -1;
                                    m_activeKey = -1.0f;
                                    keyText = "";
                                    valueText = "";
                                    curve.KeyFrames.Remove(kv.Key);
                                    if (curve.KeyFrames.Count == 0)
                                        curve.KeyFrames.Add(0.0f, 0.0f);
                                    stop = true;
                                    break;
                                }
                            }
                        }
                        if (e.Button == MouseButtons.Left && !m_isModifyingPoint && w > 0.0f && h > 0.0f)
                        {
                            float px = (float)xm / w;
                            float pv = curve.Value(px);
                            int py = (int)((h * 0.5f) - (pv * 0.5f * h / m_zoomOut));
                            if (Math.Abs(py - ym) < 6)
                            {
                                m_isModifyingPoint = true;
                                m_activeCurve = i;
                                m_activeKey = px;
                                curve.KeyFrames.Add(px, pv);
                                keyText = px.ToString(CultureInfo.InvariantCulture);
                                valueText = pv.ToString(CultureInfo.InvariantCulture);
                                stop = true;
                            }
                            else
                            {
                                m_activeCurve = -1;
                                m_activeKey = -1;
                                keyText = "";
                                valueText = "";
                            }
                        }
                    }
                    m_keyTextBox.Text = keyText;
                    m_valueTextBox.Text = valueText;
                }
                m_editPictureBox.Refresh();
            }

            private void m_editPictureBox_MouseUp(object sender, MouseEventArgs e)
            {
                m_isModifyingPoint = false;
            }

            private void m_editPictureBox_MouseMove(object sender, MouseEventArgs e)
            {
                ValidateCurve();
                if (m_isModifyingPoint)
                {
                    if (m_curve != null && m_curve.KeyFrames.ContainsKey(m_activeKey))
                    {
                        float w = (float)m_editPictureBox.Width;
                        float h = (float)m_editPictureBox.Height;
                        float xm = (float)e.X;
                        float ym = (float)e.Y;
                        float x = w > 0.0f ? Math.Max(0.0f, Math.Min(1.0f, xm / w)) : 0.0f;
                        float y = h > 0.0f ? ((2.0f * ym / h) - 1.0f) * -m_zoomOut : 0.0f;
                        if (!m_curve.KeyFrames.ContainsKey(x))
                        {
                            m_curve.KeyFrames.Remove(m_activeKey);
                            m_curve.KeyFrames.Add(x, y);
                            m_activeKey = x;
                            m_editPictureBox.Refresh();
                            m_keyTextBox.Text = x.ToString(CultureInfo.InvariantCulture);
                            m_valueTextBox.Text = y.ToString(CultureInfo.InvariantCulture);
                        }
                    }
                    else if (m_curveSet != null && m_activeCurve >= 0 && m_curvesCheckBoxes[m_activeCurve].Checked)
                    {
                        PlayCanvas.Curve curve = m_curveSet.CurvesList[m_activeCurve];
                        if (curve.KeyFrames.ContainsKey(m_activeKey))
                        {
                            float w = (float)m_editPictureBox.Width;
                            float h = (float)m_editPictureBox.Height;
                            float xm = (float)e.X;
                            float ym = (float)e.Y;
                            float x = w > 0.0f ? Math.Max(0.0f, Math.Min(1.0f, xm / w)) : 0.0f;
                            float y = h > 0.0f ? ((2.0f * ym / h) - 1.0f) * -m_zoomOut : 0.0f;
                            if (!curve.KeyFrames.ContainsKey(x))
                            {
                                curve.KeyFrames.Remove(m_activeKey);
                                curve.KeyFrames.Add(x, y);
                                m_activeKey = x;
                                m_editPictureBox.Refresh();
                                m_keyTextBox.Text = x.ToString(CultureInfo.InvariantCulture);
                                m_valueTextBox.Text = y.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                    }
                }
            }

            private void m_editPictureBox_MouseWheel(object sender, MouseEventArgs e)
            {
                if (e.Delta > 0 && m_zoomOut * 2.0f < float.MaxValue * 0.5f)
                {
                    m_zoomOut *= 2.0f;
                    m_editPictureBox.Refresh();
                }
                else if (e.Delta < 0 && m_zoomOut * 0.5f > 0.001f)
                {
                    m_zoomOut *= 0.5f;
                    m_editPictureBox.Refresh();
                }
            }

            private void checkBox_CheckedChanged(object sender, EventArgs e)
            {
                MetroCheckBox checkBox = sender as MetroCheckBox;
                if (checkBox != null)
                {
                    int idx = m_curvesCheckBoxes.IndexOf(checkBox);
                    if (idx >= 0 && idx == m_activeCurve)
                    {
                        m_activeCurve = -1;
                        m_activeKey = -1.0f;
                        m_isModifyingPoint = false;
                    }
                }
                m_editPictureBox.Refresh();
            }

            private void m_keyTextBox_TextChanged(object sender, EventArgs e)
            {
                if (m_activeKey >= 0.0f)
                {
                    if (m_curve != null && m_curve.KeyFrames.ContainsKey(m_activeKey))
                    {
                        float value = m_activeKey;
                        m_isKeyValidNumber = float.TryParse(m_keyTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                        if (m_isKeyValidNumber)
                        {
                            value = Math.Max(0.0f, Math.Min(1.0f, value));
                            if (!m_curve.KeyFrames.ContainsKey(value) && m_activeKey != value)
                            {
                                float v = m_curve.Value(m_activeKey);
                                m_curve.KeyFrames.Remove(m_activeKey);
                                m_curve.KeyFrames.Add(value, v);
                                m_activeKey = value;
                                m_editPictureBox.Refresh();
                            }
                        }
                        m_keyTextBox.Refresh();
                    }
                    else if (m_curveSet != null && m_activeCurve >= 0)
                    {
                        PlayCanvas.Curve curve = m_curveSet.CurvesList[m_activeCurve];
                        if (curve.KeyFrames.ContainsKey(m_activeKey))
                        {
                            float value = m_activeKey;
                            m_isKeyValidNumber = float.TryParse(m_keyTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                            if (m_isKeyValidNumber)
                            {
                                value = Math.Max(0.0f, Math.Min(1.0f, value));
                                if (!curve.KeyFrames.ContainsKey(value) && m_activeKey != value)
                                {
                                    float v = curve.Value(m_activeKey);
                                    curve.KeyFrames.Remove(m_activeKey);
                                    curve.KeyFrames.Add(value, v);
                                    m_activeKey = value;
                                    m_editPictureBox.Refresh();
                                }
                            }
                            m_keyTextBox.Refresh();
                        }
                    }
                }
            }

            private void m_keyTextBox_CustomPaintForeground(object sender, MetroFramework.Drawing.MetroPaintEventArgs e)
            {
                if (!m_isKeyValidNumber)
                    ControlPaint.DrawBorder(e.Graphics, m_keyTextBox.DisplayRectangle, Color.Red, ButtonBorderStyle.Solid);
            }

            private void m_keyTextBox_Leave(object sender, EventArgs e)
            {
                if (m_activeKey >= 0.0f)
                    m_keyTextBox.Text = m_activeKey.ToString(CultureInfo.InvariantCulture);
            }

            private void m_valueTextBox_TextChanged(object sender, EventArgs e)
            {
                if (m_activeKey >= 0.0f)
                {
                    if (m_curve != null && m_curve.KeyFrames.ContainsKey(m_activeKey))
                    {
                        float oldValue = m_curve.Value(m_activeKey);
                        float value = oldValue;
                        m_isValueValidNumber = float.TryParse(m_valueTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                        if (m_isValueValidNumber && oldValue != value)
                        {
                            m_curve.KeyFrames[m_activeKey] = value;
                            m_editPictureBox.Refresh();
                        }
                        m_valueTextBox.Refresh();
                    }
                    else if (m_curveSet != null && m_activeCurve >= 0)
                    {
                        PlayCanvas.Curve curve = m_curveSet.CurvesList[m_activeCurve];
                        if (curve.KeyFrames.ContainsKey(m_activeKey))
                        {
                            float oldValue = curve.Value(m_activeKey);
                            float value = oldValue;
                            m_isValueValidNumber = float.TryParse(m_valueTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                            if (m_isValueValidNumber && oldValue != value)
                            {
                                curve.KeyFrames[m_activeKey] = value;
                                m_editPictureBox.Refresh();
                            }
                            m_valueTextBox.Refresh();
                        }
                    }
                }
            }

            private void m_valueTextBox_CustomPaintForeground(object sender, MetroFramework.Drawing.MetroPaintEventArgs e)
            {
                if (!m_isValueValidNumber)
                    ControlPaint.DrawBorder(e.Graphics, m_valueTextBox.DisplayRectangle, Color.Red, ButtonBorderStyle.Solid);
            }

            private void m_valueTextBox_Leave(object sender, EventArgs e)
            {
                if (m_activeKey >= 0.0f)
                {
                    if (m_curve != null)
                        m_valueTextBox.Text = m_curve.Value(m_activeKey).ToString(CultureInfo.InvariantCulture);
                }
            }

            private void resetButton_Click(object sender, EventArgs e)
            {
                if (m_curve != null)
                {
                    m_curve.KeyFrames.Clear();
                    m_curve.KeyFrames.Add(0.0f, 0.0f);
                    m_activeKey = -1.0f;
                    m_isModifyingPoint = false;
                    m_keyTextBox.Text = "";
                    m_valueTextBox.Text = "";
                }
                else if (m_curveSet != null)
                {
                    for (int i = 0; i < Math.Min(4, m_curveSet.CurvesList.Count); ++i)
                    {
                        if (!m_curvesCheckBoxes[i].Checked)
                            continue;
                        PlayCanvas.Curve curve = m_curveSet.CurvesList[i];
                        curve.KeyFrames.Clear();
                        curve.KeyFrames.Add(0.0f, 0.0f);
                    }
                    m_activeCurve = -1;
                    m_activeKey = -1.0f;
                    m_isModifyingPoint = false;
                    m_keyTextBox.Text = "";
                    m_valueTextBox.Text = "";
                }
                m_editPictureBox.Refresh();
            }

            private void m_typeComboBox_SelectedValueChanged(object sender, EventArgs e)
            {
                if (m_curve != null)
                    m_curve.Type = (PlayCanvas.Curve.CurveType)Enum.Parse(typeof(PlayCanvas.Curve.CurveType), m_typeComboBox.SelectedItem as string);
                else if (m_curveSet != null)
                    m_curveSet.Type = (PlayCanvas.Curve.CurveType)Enum.Parse(typeof(PlayCanvas.Curve.CurveType), m_typeComboBox.SelectedItem as string);
                m_editPictureBox.Refresh();
            }

            #endregion
        }

        #endregion



        #region Public static functionalities.

        public new static JToken GetDefaultValue()
        {
            return JValue.FromObject(new float[] { 0.0f, 0.0f });
        }

        #endregion



        #region Private data.

        private PictureBox m_curvePictureBox;
        private CurveInfo m_info;

        #endregion



        #region Public properties.

        public CurveInfo Info { get { return m_info; } }

        #endregion



        #region Construction and destruction.

        public PlayCanvas_Curve_PropertyEditor(string name, PropertiesModel.Property property)
            : base(name, property)
        {
        }

        #endregion



        #region Public functionalities.

        public override void Initialize(PropertiesModel.Property property)
        {
            base.Initialize(property);

            m_info = property.EditorAdditionalData<CurveInfo>();
            if (m_info != null && m_info.Curves != null && m_info.Curves.Length > 0)
            {
                PlayCanvas.CurveSet curveSet = PropertyModel.Data<PlayCanvas.CurveSet>();
                if (curveSet == null)
                    curveSet = new PlayCanvas.CurveSet();
                if (curveSet.CurvesList.Count < m_info.Curves.Length)
                    for (int i = curveSet.CurvesList.Count; i < m_info.Curves.Length; ++i)
                        curveSet.CurvesList.Add(new PlayCanvas.Curve(new float[] { 0.0f, 0.0f }));
                PropertyModel.Data<PlayCanvas.CurveSet>(curveSet);
            }

            m_curvePictureBox = new PictureBox();
            m_curvePictureBox.Cursor = Cursors.Hand;
            m_curvePictureBox.Width = 0;
            m_curvePictureBox.Height = 48;
            m_curvePictureBox.BackColor = Color.FromArgb(255, 64, 64, 64);
            m_curvePictureBox.Dock = DockStyle.Top;
            m_curvePictureBox.Click += m_curvePictureBox_Click;
            m_curvePictureBox.Resize += m_curvePictureBox_Resize;
            m_curvePictureBox.Paint += m_curvePictureBox_Paint;
            Content.Controls.Add(m_curvePictureBox);

            UpdateEditor();
        }

        public override void Clear()
        {
            base.Clear();
            m_curvePictureBox = null;
            m_info = null;
        }

        public override void UpdateEditor()
        {
            if (m_curvePictureBox != null)
                m_curvePictureBox.Refresh();
        }

        #endregion



        #region Private events handlers.

        private void m_curvePictureBox_Click(object sender, EventArgs e)
        {
            try
            {
                CurveEditDialog dialog = new CurveEditDialog(this);
                if (dialog.ShowDialog() == DialogResult.OK)
                    UpdateEditor();
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(FindForm(), ex.Message, "Curve edit dialog creating error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void m_curvePictureBox_Resize(object sender, EventArgs e)
        {
            m_curvePictureBox.Refresh();
        }

        private void m_curvePictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (m_curvePictureBox != null && PropertyModel != null)
            {
                if (m_info != null && m_info.Curves != null && m_info.Curves.Length > 0)
                {
                    PlayCanvas.CurveSet curveSet = PropertyModel.Data<PlayCanvas.CurveSet>();
                    if (curveSet != null)
                    {
                        Color[] colors = new Color[] { 
                            Color.Red,
                            Color.GreenYellow,
                            Color.BlueViolet,
                            Color.White
                        };
                        float min = float.MaxValue;
                        float max = float.MinValue;
                        for (int i = 0; i < Math.Min(4, curveSet.CurvesList.Count); ++i)
                        {
                            PlayCanvas.Curve curve = curveSet.CurvesList[i];
                            if (curve != null)
                                foreach (var kv in curve.KeyFrames)
                                {
                                    if (kv.Value < min)
                                        min = kv.Value;
                                    if (kv.Value > max)
                                        max = kv.Value;
                                }
                        }
                        float diff = max - min;
                        if (diff == 0.0f)
                            diff = 1.0f;
                        Rectangle r = m_curvePictureBox.ClientRectangle;
                        r.Y += 2;
                        r.Height -= 5;
                        for (int i = 0; i < Math.Min(4, curveSet.CurvesList.Count); ++i)
                        {
                            PlayCanvas.Curve curve = curveSet.CurvesList[i];
                            if (curve != null)
                            {
                                Pen pen = new Pen(colors[i]);
                                Point p0;
                                Point p1;
                                float step = 2.0f / (float)r.Width;
                                float v = 0.0f;
                                float lv = 0.0f;
                                float sv = 0.0f;
                                float slv = 0.0f;
                                for (float t = 0.0f; t <= 1.0f; t += step)
                                {
                                    v = curve.Value(t);
                                    if (t == 0.0f)
                                        lv = v;
                                    sv = (v - min) / diff;
                                    slv = (lv - min) / diff;
                                    p0 = new Point(r.Left + (int)((t - step) * r.Width), r.Top + r.Height - (int)((float)r.Height * slv));
                                    p1 = new Point(r.Left + (int)(t * r.Width), r.Top + r.Height - (int)((float)r.Height * sv));
                                    e.Graphics.DrawLine(pen, p0, p1);
                                    lv = v;
                                }
                            }
                        }
                    }
                }
                else
                {
                    PlayCanvas.Curve curve = PropertyModel.Data<PlayCanvas.Curve>();
                    if (curve != null)
                    {
                        float min = float.MaxValue;
                        float max = float.MinValue;
                        foreach (var kv in curve.KeyFrames)
                        {
                            if (kv.Value < min)
                                min = kv.Value;
                            if (kv.Value > max)
                                max = kv.Value;
                        }
                        float diff = max - min;
                        if (diff == 0.0f)
                            diff = 1.0f;
                        Rectangle r = m_curvePictureBox.ClientRectangle;
                        r.Y += 2;
                        r.Height -= 5;
                        Pen pen = new Pen(Color.Red);
                        Point p0;
                        Point p1;
                        float step = 2.0f / (float)r.Width;
                        float v = 0.0f;
                        float lv = 0.0f;
                        float sv = 0.0f;
                        float slv = 0.0f;
                        for (float t = 0.0f; t <= 1.0f; t += step)
                        {
                            v = curve.Value(t);
                            if (t == 0.0f)
                                lv = v;
                            sv = (v - min) / diff;
                            slv = (lv - min) / diff;
                            p0 = new Point(r.Left + (int)((t - step) * r.Width), r.Top + r.Height - (int)((float)r.Height * slv));
                            p1 = new Point(r.Left + (int)(t * r.Width), r.Top + r.Height - (int)((float)r.Height * sv));
                            e.Graphics.DrawLine(pen, p0, p1);
                            lv = v;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
