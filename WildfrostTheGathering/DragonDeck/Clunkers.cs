using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static WildfrostTheGathering.WildfrostTheGathering;

namespace WildfrostTheGathering.DragonDeck
{
    internal class Clunkers
    {
        internal static void Load(List<object> assets, WildfrostTheGathering wtg)
        {
            Debug.Log("[WTG] Dragon clunkers loading!");
            // Lotus Petal
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("lotusPetal", "Lotus Petal", idleAnim: "ShakeAnimationProfile")
                .WithCardType("Clunker")
                .SetStats(null, null, 0)
                .SetSprites("lotus-petal-alee.png", "clunker-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Scrap", 1),
                        wtg.SStack("When Destroyed Count Down Leader By X", 1),
                    };
                })
                );

            // Bootlegger's Stash
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("bootleggersStash", "Bootlegger's Stash", idleAnim: "FloatAnimationProfile")
                .WithCardType("Clunker")
                .SetStats(null, null, 0)
                .SetSprites("bootleggers-stash-aovchinnikova.png", "clunker-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Scrap", 1),
                        wtg.SStack("While Active Treasure To AlliesInRow", 1),
                    };
                })
                );

            // Rites of Flourishing
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("ritesOfFlourishing", "Rites of Flourishing", idleAnim: "FloatSquishAnimationProfile")
                .WithCardType("Clunker")
                .SetStats(null, null, 1)
                .SetSprites("rites-of-flourishing-bkitkouski.png", "clunker-bg.png")
                .WithFlavour("\"Dance, and bring forth the coil! It is an umbilical to <b>Gaea</b> herself, fattening us with the earth\'s rich bounty.\"")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Scrap", 3),
                        wtg.SStack("On Card Played Reduce Counter To Allies In Row", 1),
                        wtg.SStack("On Card Played Reduce Counter To Front Enemy", 1),
                        wtg.SStack("On Card Played Lose Scrap To Self", 1),
                    };
                })
                );

            // Mox Jasper
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("moxJasper", "Mox Jasper", idleAnim: "FloatAnimationProfile")
                .WithCardType("Clunker")
                .SetStats(null, null, 0)
                .SetSprites("mox-jasper-sbelledin.png", "clunker-bg.png")
                .WithValue(25)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                            wtg.TStack("Zoomlin", 1)
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Scrap", 1),
                        wtg.SStack("When Destroyed Gain Noomlin To Flying Allies In Hand And In Play", 1)
                    };
                })
                );

            // Tempting Contract
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("temptingContract", "Tempting Contract", idleAnim: "FloatSquishAnimationProfile")
                .WithCardType("Clunker")
                .SetStats(null, null, 2)
                .SetSprites("tempting-contract-tduchek.png", "clunker-bg.png")
                .WithFlavour("\"Dont think of it as debt. Think of it as an investment in yourself.\"\n-<b>Diriga, demon bursar</b>")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Scrap", 1),
                            wtg.SStack("On Card Played Add Treasure To Hand", 1),
                            wtg.SStack("On Card Played Reduce Counter To Random Enemy", 1)
                    };
                })
                );

            // Gravitational Shift
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("gravitationalShift", "Gravitational Shift", idleAnim: "FloatAnimationProfile")
                .WithCardType("Clunker")
                .SetStats(null, null, 0)
                .SetSprites("gravitational-shift-svelinov.png", "clunker-bg.png")
                .WithFlavour("As they awakened, the Eldrazi reasserted their mastery over all of <b>Zendikar\'s</b> natural forces.")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Scrap", 1),
                            wtg.SStack("While Active Increase Attack To Flying Allies", 1),
                            wtg.SStack("While Active Reduce Attack To Non Flying", 1)
                    };
                })
                );

            // Revel in Riches
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("revelInRiches", "Revel In Riches", idleAnim: "SwayAnimationProfile")
                .WithCardType("Clunker")
                .SetStats(null, 0, 0)
                .SetSprites("revel-in-riches-edeschamps.png", "clunker-bg.png")
                .WithFlavour("\"I can prove I won fair and square — I have the receipt.\"")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                            wtg.TStack("Barrage", 1)
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Scrap", 2),
                            wtg.SStack("Gain Spice On Treasure", 1),
                            wtg.SStack("Add Treasure To Hand When Enemy Killed", 1),
                            wtg.SStack("When Spice X Applied To Self Trigger To Self", 5),
                    };
                })
                );

            // Dragon's Hoard
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("dragonsHoard", "Dragon's Hoard", idleAnim: "FloatAnimationProfile")
                .WithCardType("Clunker")
                .SetStats(null, null, 0)
                .SetSprites("dragons-hoard-apaquette.png", "clunker-bg.png")
                .WithFlavour("\"Unimaginable riches? I assure you, I have a vivid imagination.\"")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                            wtg.TStack("Draw", 1)
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Scrap", 1),
                            wtg.SStack("When Flying Ally Deployed Trigger Self", 1),
                    };
                })
                );

            // Windcrag Siege
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("windcragSiege", "Windcrag Siege", idleAnim: "Heartbeat2AnimationProfile")
                .WithCardType("Clunker")
                .SetStats(null, null, 3)
                .SetSprites("windcrag-siege-noleal.png", "clunker-bg.png")
                .WithFlavour("\"We are the swift, the strong, the blade's sharp shriek! Fear nothing, and strike!\"")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Scrap", 1),
                            wtg.SStack("On Card Played Ongoing Frenzy To AllyBehind", 1),
                    };
                })
                );

            Debug.Log("[WTG] Dragon clunkers loaded!");
        }
    }
}
