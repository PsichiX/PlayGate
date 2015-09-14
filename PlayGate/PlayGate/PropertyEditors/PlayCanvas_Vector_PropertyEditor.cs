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
    public class PlayCanvas_Vector_PropertyEditor : PropertiesControl.PropertyControl
    {
        #region Public static functionalities.

        public new static JToken GetDefaultValue()
        {
            return JValue.FromObject(new float[] { 0.0f, 0.0f, 0.0f });
        }

        #endregion



        #region Private data.

        private MetroTextBox m_xTextBox;
        private MetroTextBox m_yTextBox;
        private MetroTextBox m_zTextBox;
        private bool m_isValidNumberX;
        private bool m_isValidNumberY;
        private bool m_isValidNumberZ;
        private bool m_preventUpdateEditor;

        #endregion



        #region Construction and destruction.

        public PlayCanvas_Vector_PropertyEditor(string name, PropertiesModel.Property property)
            : base(name, property)
        {
        }

        #endregion



        #region Public functionalities.

        public override void Initialize(PropertiesModel.Property property)
        {
            base.Initialize(property);
            Resize += PlayCanvas_Vector_PropertyEditor_Resize;

            m_xTextBox = new MetroTextBox();
            MetroSkinManager.ApplyMetroStyle(m_xTextBox);
            m_xTextBox.Width = 0;
            m_xTextBox.TextChanged += m_xTextBox_TextChanged;
            m_xTextBox.CustomPaintForeground += m_xTextBox_CustomPaintForeground;
            m_xTextBox.Leave += m_textBox_Leave;
            Content.Controls.Add(m_xTextBox);

            m_yTextBox = new MetroTextBox();
            MetroSkinManager.ApplyMetroStyle(m_yTextBox);
            m_yTextBox.Width = 0;
            m_yTextBox.TextChanged += m_yTextBox_TextChanged;
            m_yTextBox.CustomPaintForeground += m_yTextBox_CustomPaintForeground;
            m_yTextBox.Leave += m_textBox_Leave;
            Content.Controls.Add(m_yTextBox);

            m_zTextBox = new MetroTextBox();
            MetroSkinManager.ApplyMetroStyle(m_zTextBox);
            m_zTextBox.Width = 0;
            m_zTextBox.TextChanged += m_zTextBox_TextChanged;
            m_zTextBox.CustomPaintForeground += m_zTextBox_CustomPaintForeground;
            m_zTextBox.Leave += m_textBox_Leave;
            Content.Controls.Add(m_zTextBox);

            UpdateEditor();
            m_xTextBox.Refresh();
            m_yTextBox.Refresh();
            m_zTextBox.Refresh();
        }

        private void PlayCanvas_Vector_PropertyEditor_Resize(object sender, EventArgs e)
        {
            if (m_xTextBox != null && m_yTextBox != null && m_zTextBox != null)
            {
                int w = Math.Max(0, Width / 3);
                m_xTextBox.Width = w;
                m_yTextBox.Width = w;
                m_zTextBox.Width = w;
                m_xTextBox.Left = 0;
                m_yTextBox.Left = w;
                m_zTextBox.Left = w + w;
            }
        }

        public override void Clear()
        {
            base.Clear();
            m_xTextBox = null;
            m_yTextBox = null;
            m_zTextBox = null;
            m_isValidNumberX = false;
            m_isValidNumberY = false;
            m_isValidNumberZ = false;
            m_preventUpdateEditor = false;
        }

        public override void UpdateEditor()
        {
            if (!m_preventUpdateEditor && m_xTextBox != null && m_yTextBox != null && m_zTextBox != null && PropertyModel != null)
            {
                PlayCanvas.Vector vector = PropertyModel.Data<PlayCanvas.Vector>();
                m_preventUpdateEditor = true;
                m_xTextBox.Text = vector.X.ToString(CultureInfo.InvariantCulture);
                m_yTextBox.Text = vector.Y.ToString(CultureInfo.InvariantCulture);
                m_zTextBox.Text = vector.Z.ToString(CultureInfo.InvariantCulture);
                m_preventUpdateEditor = false;
                m_xTextBox.Refresh();
                m_yTextBox.Refresh();
                m_zTextBox.Refresh();
            }
        }

        #endregion



        #region Private events handlers.

        private void m_xTextBox_TextChanged(object sender, EventArgs e)
        {
            if (PropertyModel != null)
            {
                PlayCanvas.Vector vector = PropertyModel.Data<PlayCanvas.Vector>();
                double value = vector.X;
                m_isValidNumberX = double.TryParse(m_xTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                m_xTextBox.Refresh();
            }
        }

        private void m_yTextBox_TextChanged(object sender, EventArgs e)
        {
            if (PropertyModel != null)
            {
                PlayCanvas.Vector vector = PropertyModel.Data<PlayCanvas.Vector>();
                double value = vector.Y;
                m_isValidNumberY = double.TryParse(m_yTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                m_yTextBox.Refresh();
            }
        }

        private void m_zTextBox_TextChanged(object sender, EventArgs e)
        {
            if (PropertyModel != null)
            {
                PlayCanvas.Vector vector = PropertyModel.Data<PlayCanvas.Vector>();
                double value = vector.Z;
                m_isValidNumberZ = double.TryParse(m_zTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                m_zTextBox.Refresh();
            }
        }

        private void m_xTextBox_CustomPaintForeground(object sender, MetroFramework.Drawing.MetroPaintEventArgs e)
        {
            if (!m_isValidNumberX)
            {
                MetroTextBox textBox = sender as MetroTextBox;
                if (textBox != null)
                    ControlPaint.DrawBorder(e.Graphics, textBox.DisplayRectangle, Color.Red, ButtonBorderStyle.Solid);
            }
        }

        private void m_yTextBox_CustomPaintForeground(object sender, MetroFramework.Drawing.MetroPaintEventArgs e)
        {
            if (!m_isValidNumberY)
            {
                MetroTextBox textBox = sender as MetroTextBox;
                if (textBox != null)
                    ControlPaint.DrawBorder(e.Graphics, textBox.DisplayRectangle, Color.Red, ButtonBorderStyle.Solid);
            }
        }

        private void m_zTextBox_CustomPaintForeground(object sender, MetroFramework.Drawing.MetroPaintEventArgs e)
        {
            if (!m_isValidNumberZ)
            {
                MetroTextBox textBox = sender as MetroTextBox;
                if (textBox != null)
                    ControlPaint.DrawBorder(e.Graphics, textBox.DisplayRectangle, Color.Red, ButtonBorderStyle.Solid);
            }
        }

        private void m_textBox_Leave(object sender, EventArgs e)
        {
            PlayCanvas.Vector vector = PropertyModel.Data<PlayCanvas.Vector>();
            double value = vector.X;
            m_isValidNumberX = double.TryParse(m_xTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            if (m_isValidNumberX)
                vector.X = (float)value;
            m_xTextBox.Refresh();
            value = vector.Y;
            m_isValidNumberY = double.TryParse(m_yTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            if (m_isValidNumberY)
                vector.Y = (float)value;
            m_yTextBox.Refresh();
            value = vector.Z;
            m_isValidNumberZ = double.TryParse(m_zTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            if (m_isValidNumberZ)
                vector.Z = (float)value;
            m_zTextBox.Refresh();
            if (m_isValidNumberX && m_isValidNumberY && m_isValidNumberZ)
                PropertyModel.Data<PlayCanvas.Vector>(vector);
            UpdateEditor();
        }

        #endregion
    }
}
