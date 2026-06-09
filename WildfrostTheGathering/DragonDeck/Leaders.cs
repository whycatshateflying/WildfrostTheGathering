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
    internal class Leaders
    {
        internal static void Load(List<object> assets, WildfrostTheGathering wtg)
        {
            Debug.Log("[WTG] Dragon leaders loading!");

            // The Ur-Dragon
            assets.Add(
            new CardDataBuilder(wtg)
            .CreateUnit("theUrDragonLeader", "The Ur-Dragon", idleAnim: "GiantAnimationProfile")
            .WithCardType("Leader")
            .SetSprites("the-ur-dragon-jjones.png", "companion-bg.png")
            .SetStats(9, 5, 6)  // 8-10, 5-6, 6
            .WithFlavour("<i><b>*A dark shadow covers the area*</b></i>")
            .WithValue(25)
            .SubscribeToAfterAllBuildEvent(data =>
            {
                data.traits = new List<CardData.TraitStacks>()
                {
                    wtg.TStack("Flying", 1),
                    wtg.TStack("Draw", 2),
                };
                data.startWithEffects = new CardData.StatusEffectStacks[]
                {
                    wtg.SStack("While Active Zoomlin When Drawn To Flying Allies In Hand", 1)
                };
                data.greetMessages = new string[2] { "<i>Source of all dragons across the multiverse, <b>The Ur-Dragon, Progenitor of Fire</b>, tears holes in the aether to travel</i>",
                                                            "<i><b>*A dark shadow covers the area*</b></i>"};
                data.createScripts = new CardScript[]
                {
                    GiveUpgrade(),
                    AddRandomHealth(-1,1),
                    AddRandomDamage(0,1),
                };
            })
            );

            // Miirym, Sentinel Wyrm
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateUnit("miirymSentinelWyrmLeader", "Miirym, Sentinel Wyrm", idleAnim: "FloatAnimationProfile")
                .WithCardType("Leader")
                .SetSprites("miirym-sentinel-wyrm-kkotaki.png", "companion-bg.png")
                .SetStats(8, 3, 5)  // 8-9, 3-4, 5
                .WithFlavour("\"Do you have tales of the world outside? It\'s been so long since I've left <b>Candlekeep</b>\"")
                .WithValue(25)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>()
                    {
                        wtg.TStack("Flying")
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Card Played Summon Copy Of Ally Ahead With X Health And Fragile", 1)
                    };
                    data.greetMessages = new string[1] { "\"Do you have tales of the world outside? It's been so long since I've left <b>Candlekeep</b>\"" };
                    data.createScripts = new CardScript[]
                    {
                        GiveUpgrade(),
                        AddRandomHealth(0,1),
                        AddRandomDamage(0,1),
                    };
                })
                );

            // Lathliss, Dragon Queen
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateUnit("lathlissDragonQueenLeader", "Lathliss, Dragon Queen", idleAnim: "ShakeAnimationProfile")
                .WithCardType("Leader")
                .SetSprites("lathliss-dragon-queen-akonstad.png", "companion-bg.png")
                .SetStats(7, 3, 4)  // 7-8, 3-4, 3-5
                .WithFlavour("<i>Sages whisper that <b>Lathliss</b> still keeps the heart of the old dragon queen sealed within a jewel, its smoldering warmth an eternal reminder that no monarch is untouchable</i>")
                .WithValue(25)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>()
                    {
                        wtg.TStack("Flying")
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("When Flying Ally Deployed Summon Big Dragon Token With X Health", 4)
                    };
                    data.greetMessages = new string[1] { "<i>Sages whisper that <b>Lathliss</b> still keeps the heart of the old dragon queen sealed within a jewel, its smoldering warmth an eternal reminder that no monarch is untouchable</i>" };
                    data.createScripts = new CardScript[]
                    {
                        GiveUpgrade(),
                        AddRandomHealth(0,1),
                        AddRandomDamage(0,1),
                        AddRandomCounter(-1,1)
                    };
                })
                );

            // Old Gnawbone
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateUnit("oldGnawboneLeader", "Old Gnawbone", idleAnim: "FloatSquishAnimationProfile")
                .WithCardType("Leader")
                .SetSprites("old-gnawbone-fburburan.png", "companion-bg.png")
                .SetStats(7, 6, 5)  // 7-8, 5-7, 5-6
                .WithFlavour("The ancient green dragon <b>Claugiyliamatar</b> is often seen with a mangled corpse dangling from her mouth.")
                .WithValue(25)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>()
                    {
                        wtg.TStack("Flying")
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Flying Card Played Add Treasure To Hand", 1)
                    };
                    data.createScripts = new CardScript[]
                    {
                        GiveUpgrade(),
                        AddRandomHealth(0,1),
                        AddRandomDamage(-1,1),
                        AddRandomCounter(0,1)
                    };
                    data.greetMessages = new string[1] { "The ancient green dragon <b>Claugiyliamatar</b> is often seen with a mangled corpse dangling from her mouth" };
                })
                );

            // Ganax, Astral Hunter 
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateUnit("ganaxAstralHunterLeader", "Ganax, Astral Hunter", idleAnim: "FloatAnimationProfile")
                .WithCardType("Leader")
                .SetSprites("ganax-astral-hunter-amiller.png", "companion-bg.png")
                .SetStats(8, 5, 5)  // 7-9, 5-6, 5
                .WithFlavour("\"The hum of the universe is never off-key.\"")
                .WithValue(25)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>()
                    {
                        wtg.TStack("Flying")
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("When Flying Ally Deployed Add Treasure To Hand", 1)  // 1-2
                    };
                    data.greetMessages = new string[1] { "\"The hum of the universe is never off-key.\"" };
                    data.createScripts = new CardScript[]
                    {
                        GiveUpgrade(),
                        AddRandomHealth(-1,1),
                        AddRandomDamage(0,1),
                        AddRandomBoostEffects(0,1),
                    };
                })
                );

            // Drakuseth, Maw of Flames 
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateUnit("drakusethMawOfFlamesLeader", "Drakuseth, Maw of Flames", idleAnim: "ShakeAnimationProfile")
                .WithCardType("Leader")
                .SetSprites("drakuseth-maw-of-flames-grutkowski.png", "companion-bg.png")
                .SetStats(6, 3, 5)  // 6-7, 3-4, 5
                .WithFlavour("\"Spread out, you idiots! Spread out!\"\n<b>—Marsden, party leader, last words</b>")
                .WithValue(25)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>()
                    {
                        wtg.TStack("Flying")
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Card Played Deal X Damage To Frontmost Enemy Twice", 3)
                    };
                    data.greetMessages = new string[1] { "\"Spread out, you idiots! Spread out!\"\n<b>—Marsden, party leader, last words</b>" };
                    data.createScripts = new CardScript[]
                    {
                        GiveUpgrade(),
                        AddRandomHealth(0,1),
                        AddRandomDamage(0,1),
                    };
                })
                );

            // Gadrak (no art!)
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("gadrakTheCrownScourgeLeader", "Gadrak, the Crown-Scourge", idleAnim: "FloatAnimationProfile")
                .WithCardType("Leader")
                .SetStats(7, 3, 2)  // 7-8, 3-4, 2
                .SetSprites("placeholder-companion.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Flying", 1),
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Only Count Down While Treasure In Hand", 1),
                    };
                    data.greetMessages = new string[] { "Its footsteps of today are the lakes of tomorrow" };
                    data.createScripts = new CardScript[]
                    {
                        GiveUpgrade(),
                        AddRandomHealth(0,1),
                        AddRandomDamage(0,1),
                    };
                })
                );

            Debug.Log("[WTG] Dragon leaders loaded!");
        }
    }
}
