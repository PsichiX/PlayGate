using MetroFramework.Controls;
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
    public class PlayCanvas_Rgba_PropertyEditor : PropertiesControl.PropertyControl
    {
        #region Public static functionalities.

        public new static JToken GetDefaultValue()
        {
            return JValue.FromObject(new float[] { 1.0f, 1.0f, 1.0f, 1.0f });
        }

        #endregion



        #region Private data.

        private Panel m_colorBox;
        private MetroTrackBar m_alphaTrack;
        private MetroTextBox m_alphaTextBox;
        private bool m_preventUpdateEditor;
        private bool m_isAlphaValidNumber;

        #endregion



        #region Construction and destruction.

        public PlayCanvas_Rgba_PropertyEditor(string name, PropertiesModel.Property property)
            : base(name, property)
        {
        }

        #endregion



        #region Public functionalities.

        public override void Initialize(PropertiesModel.Property property)
        {
            base.Initialize(property);

            m_colorBox = new Panel();
            m_colorBox.Cursor = Cursors.Hand;
            m_colorBox.Width = 0;
            m_colorBox.Height = 48;
            m_colorBox.Dock = DockStyle.Top;
            m_colorBox.Click += m_colorBox_Click;
            m_colorBox.Paint += m_colorBox_Paint;

            m_alphaTrack = new MetroTrackBar();
            MetroSkinManager.ApplyMetroStyle(m_alphaTrack);
            m_alphaTrack.Width = 0;
            m_alphaTrack.Height = 16;
            m_alphaTrack.Dock = DockStyle.Top;
            m_alphaTrack.Maximum = 255;
            m_alphaTrack.Value = 255;
            m_alphaTrack.ValueChanged += m_alphaTrack_ValueChanged;

            m_alphaTextBox = new MetroTextBox();
            MetroSkinManager.ApplyMetroStyle(m_alphaTextBox);
            m_alphaTextBox.Width = 0;
            m_alphaTextBox.Dock = DockStyle.Top;
            m_alphaTextBox.TextChanged += m_alphaTextBox_TextChanged;
            m_alphaTextBox.CustomPaintForeground += m_alphaTextBox_CustomPaintForeground;
            m_alphaTextBox.Leave += m_alphaTextBox_Leave;

            Content.Controls.Add(m_alphaTrack);
            Content.Controls.Add(m_alphaTextBox);
            Content.Controls.Add(m_colorBox);

            UpdateEditor();
            m_alphaTextBox.Refresh();
        }

        public override void Clear()
        {
            base.Clear();
            m_colorBox = null;
            m_alphaTrack = null;
            m_alphaTextBox = null;
            m_preventUpdateEditor = false;
            m_isAlphaValidNumber = false;
        }

        public override void UpdateEditor()
        {
            if (!m_preventUpdateEditor && m_colorBox != null && m_alphaTrack != null && PropertyModel != null)
            {
                m_preventUpdateEditor = true;
                PlayCanvas.Color color = PropertyModel.Data<PlayCanvas.Color>();
                m_colorBox.BackColor = Color.FromArgb(
                    255,
                    (int)(color.R * 255.0f),
                    (int)(color.G * 255.0f),
                    (int)(color.B * 255.0f)
                    );
                m_alphaTrack.Value = (int)(color.A * 255.0f);
                m_alphaTextBox.Text = m_alphaTrack.Value.ToString(CultureInfo.InvariantCulture);
                m_alphaTextBox.Refresh();
                m_preventUpdateEditor = false;
            }
        }

        #endregion



        #region Private events handlers.

        private void m_colorBox_Click(object sender, EventArgs e)
        {
            if (PropertyModel != null)
            {
                ColorDialog dialog = new ColorDialog();
                PlayCanvas.Color color = PropertyModel.Data<PlayCanvas.Color>();
                dialog.Color = Color.FromArgb(
                    0,
                    (int)(color.R * 255.0f),
                    (int)(color.G * 255.0f),
                    (int)(color.B * 255.0f)
                    );
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    color = new PlayCanvas.Color(
                        (float)dialog.Color.R / 255.0f,
                        (float)dialog.Color.G / 255.0f,
                        (float)dialog.Color.B / 255.0f,
                        color.A
                        );
                    PropertyModel.Data<PlayCanvas.Color>(color);
                    UpdateEditor();
                }
            }
        }

        private void m_colorBox_Paint(object sender, PaintEventArgs e)
        {
            Panel panel = sender as Panel;
            if (panel != null)
            {
                ControlPaint.DrawBorder(e.Graphics, panel.DisplayRectangle, Color.Black, ButtonBorderStyle.Solid);
                Rectangle r = new Rectangle(
                    panel.DisplayRectangle.X + 1,
                    panel.DisplayRectangle.Y + 1,
                    panel.DisplayRectangle.Width - 2,
                    panel.DisplayRectangle.Height - 2
                    );
                ControlPaint.DrawBorder(e.Graphics, r, Color.White, ButtonBorderStyle.Solid);
            }
        }

        private void m_alphaTrack_ValueChanged(object sender, EventArgs e)
        {
            if (PropertyModel != null)
            {
                PlayCanvas.Color color = PropertyModel.Data<PlayCanvas.Color>();
                color = new PlayCanvas.Color(
                    color.R,
                    color.G,
                    color.B,
                    (float)m_alphaTrack.Value / (float)m_alphaTrack.Maximum
                    );
                if (!m_preventUpdateEditor)
                    m_alphaTextBox.Text = m_alphaTrack.Value.ToString(CultureInfo.InvariantCulture);
                PropertyModel.Data<PlayCanvas.Color>(color);
                UpdateEditor();
                m_alphaTextBox.Refresh();
            }
        }

        private void m_alphaTextBox_TextChanged(object sender, EventArgs e)
        {
            if (PropertyModel != null)
            {
                PlayCanvas.Color color = PropertyModel.Data<PlayCanvas.Color>();
                double value = color.A;
                m_isAlphaValidNumber = double.TryParse(m_alphaTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                if (m_isAlphaValidNumber)
                {
                    color.A = (float)value / 255.0f;
                    PropertyModel.Data<PlayCanvas.Color>(color);
                    if (!m_preventUpdateEditor)
                        m_alphaTrack.Value = (int)Math.Max(0, Math.Min(m_alphaTrack.Maximum, value));
                }
                m_alphaTextBox.Refresh();
            }
        }

        private void m_alphaTextBox_CustomPaintForeground(object sender, MetroFramework.Drawing.MetroPaintEventArgs e)
        {
            if (!m_isAlphaValidNumber)
            {
                MetroTextBox textBox = sender as MetroTextBox;
                if (textBox != null)
                    ControlPaint.DrawBorder(e.Graphics, textBox.DisplayRectangle, Color.Red, ButtonBorderStyle.Solid);
            }
        }

        private void m_alphaTextBox_Leave(object sender, EventArgs e)
        {
            UpdateEditor();
        }

        #endregion
    }
}
