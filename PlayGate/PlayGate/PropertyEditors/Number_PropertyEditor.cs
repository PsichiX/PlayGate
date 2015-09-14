using MetroFramework.Controls;
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
    public class Number_PropertyEditor : PropertiesControl.PropertyControl
    {
        #region Public nested classes.

        public class NumberInfo
        {
            [JsonProperty("min")]
            public float Min { get; set; }
            [JsonProperty("max")]
            public float Max { get; set; }
            [JsonProperty("step")]
            public float Step { get; set; }
            [JsonProperty("decimalPrecision")]
            public float DecimalPrecision { get; set; }
        }

        #endregion



        #region Public static functionalities.

        public new static JToken GetDefaultValue()
        {
            return JValue.FromObject(default(float));
        }

        public new static bool IsValueOf(JToken value)
        {
            return value.Type == JTokenType.Float || value.Type == JTokenType.Integer;
        }

        #endregion



        #region Private data.

        private MetroTextBox m_textBox;
        private NumberInfo m_info;
        private bool m_isValidNumber;

        #endregion



        #region Construction and destruction.

        public Number_PropertyEditor(string name, PropertiesModel.Property property)
            : base(name, property)
        {
        }

        #endregion



        #region Public functionalities.

        public override void Initialize(PropertiesModel.Property property)
        {
            base.Initialize(property);

            m_info = property.EditorAdditionalData<NumberInfo>();

            m_textBox = new MetroTextBox();
            MetroSkinManager.ApplyMetroStyle(m_textBox);
            m_textBox.Width = 0;
            m_textBox.Dock = DockStyle.Top;
            m_textBox.TextChanged += m_textBox_TextChanged;
            m_textBox.CustomPaintForeground += m_textBox_CustomPaintForeground;
            m_textBox.Leave += m_textBox_Leave;
            Content.Controls.Add(m_textBox);

            UpdateEditor();
            m_textBox.Refresh();
        }

        public override void Clear()
        {
            base.Clear();
            m_textBox = null;
            m_info = null;
            m_isValidNumber = false;
        }

        public override void UpdateEditor()
        {
            if (m_textBox != null && PropertyModel != null)
            {
                m_textBox.Text = PropertyModel.Data<float>().ToString(CultureInfo.InvariantCulture);
                m_textBox.Refresh();
            }
        }

        #endregion



        #region Private events handlers.

        private void m_textBox_TextChanged(object sender, EventArgs e)
        {
            if (PropertyModel != null)
            {
                float value = PropertyModel.Data<float>();
                m_isValidNumber = float.TryParse(m_textBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                m_textBox.Refresh();
            }
        }

        private void m_textBox_CustomPaintForeground(object sender, MetroFramework.Drawing.MetroPaintEventArgs e)
        {
            if (!m_isValidNumber)
            {
                MetroTextBox textBox = sender as MetroTextBox;
                if (textBox != null)
                    ControlPaint.DrawBorder(e.Graphics, textBox.DisplayRectangle, Color.Red, ButtonBorderStyle.Solid);
            }
        }

        private void m_textBox_Leave(object sender, EventArgs e)
        {
            if (m_isValidNumber)
            {
                float value = PropertyModel.Data<float>();
                m_isValidNumber = float.TryParse(m_textBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                if (m_isValidNumber)
                {
                    if (m_info != null)
                        value = Math.Max(m_info.Min, Math.Min(m_info.Max, value));
                    PropertyModel.Data<float>(value);
                }
                m_textBox.Refresh();
            }
            UpdateEditor();
        }

        #endregion
    }
}
