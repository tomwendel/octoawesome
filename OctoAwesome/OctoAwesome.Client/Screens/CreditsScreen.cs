using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OctoAwesome.Client.Components;
using engenious.UI;
using engenious;
using OctoAwesome.Client.Crew;
using engenious.UI.Controls;

namespace OctoAwesome.Client.Screens
{
    class CreditsScreen : BaseScreen
    {
        public CreditsScreen(ScreenComponent manager) : base(manager)
        {
            Padding = new Border(0, 0, 0, 0);

            Title = UI.Languages.OctoClient.CreditsCrew;

            SetDefaultBackground();

            List<CrewMember> crew = CrewMember.getCrew(manager);

            ScrollContainer crewScroll = new ScrollContainer(manager)
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Border(10, 10, 10, 10),
                CanFocus = false
            };

            StackPanel crewList = new StackPanel(manager) {
                MinWidth = 700,
                Padding = new Border(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Stretch,
                Orientation = Orientation.Vertical,
            };
            crewScroll.Content = crewList;

            foreach(CrewMember member in crew)
            {
                Button memberButton = new TextButton(manager, member.Username);
                memberButton.HorizontalAlignment = HorizontalAlignment.Stretch;
                memberButton.Margin = new Border(5, 5, 5, 5);

                memberButton.LeftMouseClick += (s, e) =>
                {
                    manager.NavigateToScreen(new CrewMemberScreen(manager, member));
                };

                crewList.Controls.Add(memberButton);
            }
            

            Controls.Add(crewScroll);
        }
    }
}
