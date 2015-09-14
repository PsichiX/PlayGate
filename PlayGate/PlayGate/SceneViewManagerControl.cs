using MetroFramework.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayGate
{
    public class SceneViewManagerControl : MetroPanel
    {
        #region Construction and destruction.

        public SceneViewManagerControl()
        {
            MetroSkinManager.ApplyMetroStyle(this);
            AutoScroll = true;
            Height = 38;
        }

        #endregion
    }
}
