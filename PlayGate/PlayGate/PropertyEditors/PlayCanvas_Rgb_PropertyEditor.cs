using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayGate.PropertyEditors
{
    public class PlayCanvas_Rgb_PropertyEditor : PropertiesControl.PropertyControl
    {
        #region Public static functionalities.

        public new static JToken GetDefaultValue()
        {
            return JValue.FromObject(new float[] { 1.0f, 1.0f, 1.0f });
        }

        #endregion



        #region Private data.

        private Panel m_colorBox;

        #endregion



        #region Construction and destruction.

        public PlayCanvas_Rgb_PropertyEditor(string name, PropertiesModel.Property property)
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
            Content.Controls.Add(m_colorBox);

            UpdateEditor();
        }

        public override void Clear()
        {
            base.Clear();
            m_colorBox = null;
        }

        public override void UpdateEditor()
        {
            if (m_colorBox != null && PropertyModel != null)
            {
                PlayCanvas.Color color = PropertyModel.Data<PlayCanvas.Color>();
                m_colorBox.BackColor = Color.FromArgb(
                    (int)(color.A * 255.0f),
                    (int)(color.R * 255.0f),
                    (int)(color.G * 255.0f),
                    (int)(color.B * 255.0f)
                    );
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

        #endregion
    }
}
