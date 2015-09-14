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
    public class PlayCanvas_Enumeration_PropertyEditor : PropertiesControl.PropertyControl
    {
        #region Public nested classes.

        public class EnumerationInfo : Dictionary<string, int> { }

        #endregion



        #region Public static functionalities.

        public new static JToken GetDefaultValue()
        {
            return JValue.FromObject(0);
        }

        #endregion



        #region Private data.

        private MetroComboBox m_comboBox;
        private EnumerationInfo m_info;

        #endregion



        #region Construction and destruction.

        public PlayCanvas_Enumeration_PropertyEditor(string name, PropertiesModel.Property property)
            : base(name, property)
        {
        }

        #endregion



        #region Public functionalities.

        public override void Initialize(PropertiesModel.Property property)
        {
            base.Initialize(property);

            EnumerationInfo info = property.EditorAdditionalData<EnumerationInfo>();
            m_info = info == null ? new EnumerationInfo() : info;

            m_comboBox = new MetroComboBox();
            MetroSkinManager.ApplyMetroStyle(m_comboBox);
            m_comboBox.Cursor = Cursors.Hand;
            m_comboBox.Width = 0;
            m_comboBox.Dock = DockStyle.Top;
            m_comboBox.BindingContext = new BindingContext();
            m_comboBox.DataSource = m_info.Keys.ToList();
            m_comboBox.SelectedValueChanged += m_comboBox_SelectedValueChanged;
            Content.Controls.Add(m_comboBox);

            UpdateEditor();
        }

        public override void Clear()
        {
            base.Clear();
            m_comboBox = null;
            m_info = null;
        }

        public override void UpdateEditor()
        {
            if (m_comboBox != null && PropertyModel != null)
            {
                int value = PropertyModel.Data<int>();
                if (m_info.ContainsValue(value))
                    m_comboBox.SelectedItem = m_info.First(kv => kv.Value == value).Key;
            }
        }

        #endregion



        #region Private events handlers.

        private void m_comboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            if (PropertyModel != null)
            {
                int value = PropertyModel.Data<int>();
                string name = m_comboBox.SelectedItem as string;
                if (m_info.ContainsKey(name))
                    value = m_info[name];
                PropertyModel.Data<int>();
            }
        }

        #endregion
    }
}
