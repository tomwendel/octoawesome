using engenious.UI;
using engenious.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OctoAwesome.UI.Controls
{
    public class HealthBarControl : ProgressBar
    {
        public HealthBarControl(BaseScreenComponent manager, string style = "") : base(manager, style)
        {
            Background = Skin.Current.HorizontalScrollBackgroundBrush;
        }
    }
}
