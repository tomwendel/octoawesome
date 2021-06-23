using engenious.UI;
using OctoAwesome.Components;
using OctoAwesome.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Basics.EntityComponents.UIComponents
{
    public abstract class UIComponent : Component, IEntityComponent, IFunctionalBlockComponent
    {
        protected BaseScreenComponent ScreenComponent { get; }
        public AssetComponent AssetComponent { get; }

        public UIComponent()
        {
            ScreenComponent = TypeContainer.Get<BaseScreenComponent>();
            AssetComponent = TypeContainer.Get<AssetComponent>();
        }

    }
}
