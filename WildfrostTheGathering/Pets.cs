using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WildfrostTheGathering
{
    internal class Pets
    {
        internal static void Load(List<object> assets, WildfrostTheGathering wtg)
        {
            Debug.Log("[WTG] Pets loading!");
            // Wall of Omens
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("wallOfOmens", "Wall Of Omens", idleAnim: "SwayAnimationProfile")
                .SetStats(5, null, 3)
                .IsPet((ChallengeData)null, true)
                .SetSprites("wall-of-omensjpaick.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Draw", 1)
                    };
                })
                );

            // Air Elemental
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("airElemental", "Air Elemental", idleAnim: "FloatAnimationProfile")
                .SetStats(6, 4, 4)
                .IsPet((ChallengeData)null, true)
                .SetSprites("air-elemental-kwalker.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Flying", 1),
                    };
                })
                );

            // Ball Lightning
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("ballLightning", "Ball Lightning")
                .SetStats(1, 6, 4)
                .IsPet((ChallengeData)null, true)
                .SetSprites("ball-lightning-tclaxton.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Spark", 1),
                    };
                })
                );

            // Wall of Frost
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("wallOfFrost", "Wall of Frost", idleAnim: "")  // Bad strings freeze it
                .SetStats(6, null, 0)
                .IsPet((ChallengeData)null, true)
                .SetSprites("wall-of-frost-mbierek.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("When Hit Apply Snow To Attacker", 1),
                    };
                })
                );

            // Atog
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("atog", "Atog")
                .SetStats(5, 3, 3)
                .IsPet((ChallengeData)null, true)
                .SetSprites("atog-puddnhead.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("When Ally Is Killed Apply Attack To Self", 1),
                    };
                })
                );

            // Llanowar Elves
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("llanowarElves", "Llanowar Elves")
                .SetStats(5, 1, 3)
                .IsPet((ChallengeData)null, true)
                .SetSprites("llanowar-elves-kwalker.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Card Played Gain Zoomlin To X Random Cards In Hand", 1),
                    };
                })
                );

            // Grizzly Bears
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("grizzlyBears", "Grizzly Bears")
                .SetStats(2, 2, 2)
                .IsPet((ChallengeData)null, true)
                .SetSprites("grizzly-bears-jamenges.png", "companion-bg.png")
                .WithFlavour("Don\'t try to outrun one of Dominia\'s Grizzlies; it\'ll catch you, knock you down, and eat you. Of course, you could run up a tree. In that case you\'ll get a nice view before it knocks the tree down and eats you.")
                .WithValue(50)
                );

            // Juggernaut
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("juggernaut", "Juggernaut")
                .SetStats(5, 5, 3)
                .IsPet((ChallengeData)null, true)
                .SetSprites("juggernaut-kwalker.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Card Played Damage To Self", 1),
                    };
                })
                );

            // Guttersnipe
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("guttersnipe", "Guttersnipe")
                .SetStats(4, 1, 0)
                .IsPet((ChallengeData)null, true)
                .SetSprites("guttersnipe-mkollros.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Trigger On Item With Attack Played", 1),
                    };
                })
                );

            // Thrashing Brontodon
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("thrashingBrontodon", "Thrashing Brontodon", idleAnim: "GiantAnimationProfile")
                .SetStats(4, 2, 3)
                .IsPet((ChallengeData)null, true)
                .SetSprites("thrashing-brontodon-jkasper.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Kill Clunker Targets", 1),
                    };
                })
                );
            Debug.Log("[WTG] Pets loaded!");
        }
    }
}
