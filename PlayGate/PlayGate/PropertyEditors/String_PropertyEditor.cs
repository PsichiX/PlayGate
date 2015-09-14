using MetroFramework.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayGate.PropertyEditors
{
    public class String_PropertyEditor : PropertiesControl.PropertyControl
    {
        #region Public static functionalities.

        public new static JToken GetDefaultValue()
        {
            return JValue.FromObject("");
        }

        public new static bool IsValueOf(JToken value)
        {
            return value.Type == JTokenType.String;
        }

        #endregion



        #region Private data.

        private MetroTextBox m_textBox;

        #endregion



        #region Construction and destruction.

        public String_PropertyEditor(string name, PropertiesModel.Property property)
            : base(name, property)
        {
        }

        #endregion



        #region Public functionalities.

        public override void Initialize(PropertiesModel.Property property)
        {
            base.Initialize(property);

            m_textBox = new MetroTextBox();
            MetroSkinManager.ApplyMetroStyle(m_textBox);
            m_textBox.Width = 0;
            m_textBox.Dock = DockStyle.Top;
            m_textBox.TextChanged += m_textBox_TextChanged;
            m_textBox.Leave += m_textBox_Leave;
            Content.Controls.Add(m_textBox);

            UpdateEditor();
        }

        public override void Clear()
        {
            base.Clear();
            m_textBox = null;
        }

        public override void UpdateEditor()
        {
            if (m_textBox != null && PropertyModel != null)
                m_textBox.Text = PropertyModel.Data<string>();
        }

        #endregion



        #region Private events handlers.

        private void m_textBox_TextChanged(object sender, EventArgs e)
        {
            if (PropertyModel != null)
                PropertyModel.Data<string>(m_textBox.Text);
        }

        private void m_textBox_Leave(object sender, EventArgs e)
        {
            UpdateEditor();
        }

        #endregion
    }
}
