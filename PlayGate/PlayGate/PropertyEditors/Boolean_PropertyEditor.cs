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
    public class Boolean_PropertyEditor : PropertiesControl.PropertyControl
    {
        #region Public static functionalities.

        public new static JToken GetDefaultValue()
        {
            return JValue.FromObject(default(bool));
        }

        public new static bool IsValueOf(JToken value)
        {
            return value.Type == JTokenType.Boolean;
        }

        #endregion



        #region Private data.

        private MetroToggle m_toggle;

        #endregion



        #region Construction and destruction.

        public Boolean_PropertyEditor(string name, PropertiesModel.Property property)
            : base(name, property)
        {
        }

        #endregion



        #region Public functionalities.

        public override void Initialize(PropertiesModel.Property property)
        {
            base.Initialize(property);

            m_toggle = new MetroToggle();
            MetroSkinManager.ApplyMetroStyle(m_toggle);
            m_toggle.Cursor = Cursors.Hand;
            m_toggle.Width = 0;
            m_toggle.Dock = DockStyle.Top;
            m_toggle.CheckedChanged += m_toggle_CheckedChanged;
            Content.Controls.Add(m_toggle);

            UpdateEditor();
        }

        public override void Clear()
        {
            base.Clear();
            m_toggle = null;
        }

        public override void UpdateEditor()
        {
            if (m_toggle != null && PropertyModel != null)
                m_toggle.Checked = PropertyModel.Data<bool>();
        }

        #endregion



        #region Private events handlers.

        private void m_toggle_CheckedChanged(object sender, EventArgs e)
        {
            if (PropertyModel != null)
                PropertyModel.Data<bool>(m_toggle.Checked);
        }

        #endregion
    }
}
