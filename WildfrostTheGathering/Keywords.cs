using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WildfrostTheGathering
{
    internal class Keywords
    {
        internal static void Load(List<object> assets, WildfrostTheGathering wtg)
        {
            Debug.Log("[WTG] Keywords loading!");

            // Flying
            assets.Add(
                new KeywordDataBuilder(wtg)
                .Create("flying")
                .WithTitle("Flying")  // The in-game name for the upgrade.
                .WithShowName(true)  // Shows name in Keyword box (as opposed to a nonexistant icon).
                .WithDescription("Always hits an enemy boss, if applicable|Hits normally if there are none") //Format is body|note.
                .WithCanStack(false)  // The keyword does not show its stack number.
                );

            // Fireball targeting mode
            assets.Add(
                new KeywordDataBuilder(wtg)
                .Create("fireball")
                .WithTitle("Hits an enemy for each card with Zoomlin in hand")  // The in-game name for the upgrade.
                .WithShowName(true)  // Shows name in Keyword box (as opposed to a nonexistant icon).
                .WithShow(false)
                //.WithShowIcon(false)
                .WithDescription("Hits an enemy for each card with Zoomlin in hand|Hits none if there are none") //Format is body|note.
                .WithCanStack(false)  // The keyword does not show its stack number.
                );

            // Unplayable
            assets.Add(
                new KeywordDataBuilder(wtg)
                .Create("unplayable")
                .WithTitle("Unplayable")  // The in-game name for the upgrade.
                .WithShowName(true)  // Shows name in Keyword box (as opposed to a nonexistant icon).
                .WithDescription("Cannot be played this turn|Goes away at the end of turn") //Format is body|note.
                .WithCanStack(false)  // The keyword does not show its stack number.
                );

            // Eternal
            assets.Add(
                new KeywordDataBuilder(wtg)
                .Create("eternal")
                .WithTitle("Eternal")  // The in-game name for the upgrade.
                .WithShowName(true)  // Shows name in Keyword box (as opposed to a nonexistant icon).
                .WithDescription("Cannot be Injured") //Format is body|note.
                .WithCanStack(false)  // The keyword does not show its stack number.
                );

            // Ongoing
            assets.Add(new KeywordDataBuilder(wtg)
                .Create("ongoing")
                .WithTitle("Ongoing")
                .WithShowName(true)
                .WithDescription("Has the following card text until after the next trigger")
                .WithCanStack(false)
                );

            // Phased Out
            assets.Add(new KeywordDataBuilder(wtg)
                .Create("phasedout")
                .WithTitle("Phased out")
                .WithShowName(true)
                .WithDescription("Cannot be targeted|Remove after triggering")
                .WithCanStack(false)
                );

            // Consipracy
            assets.Add(new KeywordDataBuilder(wtg)
                .Create("conspiracy")
                .WithTitle("Conspiracy")
                .WithShowName(true)
                .WithDescription("Begins with a crown")
                .WithCanStack(false)
                );

            // Discard Pocket
            assets.Add(new KeywordDataBuilder(wtg)
                .Create("discardpocket")
                .WithTitle("Discard Pocket")
                .WithShowName(true)
                .WithDescription($"Items without <keyword=consume> and recalled units go here when played|Shuffles into the Draw Pocket when it runs out")
                .WithCanStack(false)
                );

            // Draw Pocket
            assets.Add(new KeywordDataBuilder(wtg)
                .Create("drawpocket")
                .WithTitle("Draw Pocket")
                .WithShowName(true)
                .WithDescription($"Cards are drawn from this pile|When it runs out, the Discard Pocket is shuffled and moved here")
                .WithCanStack(false)
                );

            // Discard Pocket
            assets.Add(new KeywordDataBuilder(wtg)
                .Create("redrawbell")
                .WithTitle("Redraw Bell")
                .WithShowName(true)
                .WithDescription($"Hitting Redraw Bell draws a new hand|Does not take a turn if it\'s charged!")
                .WithCanStack(false)
                );

            // Trample
            assets.Add(
                new KeywordDataBuilder(wtg)
                .Create("trample")
                .WithTitle("Trample")  // The in-game name for the upgrade.
                .WithShowName(true)  // Shows name in Keyword box (as opposed to a nonexistant icon).
                .WithDescription("Deal excess damage to target behind|Does not wrap lanes") //Format is body|note.
                .WithCanStack(false)  // The keyword does not show its stack number.
                );

            // Prey
            assets.Add(new KeywordDataBuilder(wtg)
                .Create("prey")
                .WithTitle("Prey")
                .WithShowName(true)
                .WithDescription("Tetzimoc is watching...")
                .WithCanStack(false)
                );

            // Ramp
            assets.Add(new KeywordDataBuilder(wtg)
                .Create("ramp")
                .WithTitle("Ramp")
                .WithShowName(true)
                .WithDescription("Add <keyword=zoomlin> to random cards in hand")
                .WithCanStack(true)
                );

            // Suspected
            assets.Add(
                new KeywordDataBuilder(wtg)
                .Create("suspected")
                .WithTitle("Suspected")  // The in-game name for the upgrade.
                .WithShowName(true)  // Shows name in Keyword box (as opposed to a nonexistant icon).
                .WithDescription("Deals half damage, but also hits target behind") //Format is body|note.
                .WithCanStack(false)  // The keyword does not show its stack number.
                );
            Debug.Log("[WTG] Keywords loaded!");
        }
    }
}
