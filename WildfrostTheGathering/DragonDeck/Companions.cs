using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WildfrostTheGathering.DragonDeck
{
    internal class Companions
    {
        internal static void Load(List<object> assets, WildfrostTheGathering wtg)
        {
            Debug.Log("[WTG] Dragon companions loading!");
            // Dragon Token
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("dragonToken", "Dragon Token", idleAnim: "FloatSquishAnimationProfile")
                .SetSprites("dragon-token-kyanner.png", "companion-bg.png")
                .SetStats(1, 4, 3)
                .WithCardType("Summoned")
                .WithFlavour("rawr!")
                .WithValue(25)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>()
                    {
                        wtg.TStack("Flying", 1),
                    };
                    data.greetMessages = new string[1] { "Woah a token in the companion pool? That\'s not supposed to happen" };
                })
                );

            // Dragon Token with Spark
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("dragonTokenSpark", "Dragon Illusion Token", idleAnim: "FloatSquishAnimationProfile")
                .SetSprites("dragon-illusion-token-amar.png", "companion-bg.png")
                .SetStats(1, 1, 3)
                .WithCardType("Summoned")
                .WithFlavour("rawr!")
                .WithValue(25)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>()
                    {
                        wtg.TStack("Flying", 1),
                        wtg.TStack("Spark", 1),
                    };
                    data.greetMessages = new string[1] { "Woah a token in the companion pool? That\'s not supposed to happen" };
                })
                );

            // Goldspan Dragon
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("goldspanDragon", "Goldspan Dragon", idleAnim: "FloatSquishAnimationProfile")
                .SetSprites("goldspan-dragon-amar.png", "companion-bg.png")
                .SetStats(7, 4, 4)
                .WithCardType("Friendly")
                .WithFlavour("<i>\"You see, most places have mice or mosquitoes...\"</i>")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Card Played Add Treasure To Hand", 1)
                    };
                    data.traits = new List<CardData.TraitStacks>()
                    {
                        wtg.TStack("Flying", 1),
                        wtg.TStack("Spark", 1),
                    };
                    data.greetMessages = new string[2] { "<b>*Breathes Fire Loudly*</b>\n<i>How was it stuck in ice?</i>",
                            "<i>\"You see, most places have mice or mosquitoes...\"</i>"};
                })
                );

            // Ancient Copper Dragon
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("ancientCopperDragon", "Ancient Copper Dragon", idleAnim: "GiantAnimationProfile")
                .SetSprites("ancient-copper-dragon-ajmanzan.png", "companion-bg.png")
                .SetStats(8, 5, 5)
                .WithCardType("Friendly")
                .WithFlavour("<i>You can never have enough gold</i>")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Card Played Add Treasure To Hand", 2)
                    };
                    data.traits = new List<CardData.TraitStacks>()
                    {
                        wtg.TStack("Flying", 1)
                    };
                    data.greetMessages = new string[1] { "<i>You can never have enough gold</i>" };
                })
                );

            // Atsushi, Blazing Sky
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("atsushiBlazingSky", "Atsushi, the Blazing Sky", idleAnim: "FloatSquishAnimationProfile")
                .SetSprites("atsushi-blazing-sky-vaminguez.png", "companion-bg.png")
                .SetStats(7, 1, 3)
                .WithCardType("Friendly")
                .WithFlavour("\"<i>The reborn form of <b>Ryusei</b>, protector of <b>Sokenzan</b></i>")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("When Destroyed Add Treasure To Hand", 2),
                            wtg.SStack("When Destroyed Draw", 2),
                    };
                    data.traits = new List<CardData.TraitStacks>()
                    {
                            wtg.TStack("Flying", 1),
                            wtg.TStack("Eternal", 1),
                    };
                    data.greetMessages = new string[2] { "<i><b>Ryusei</b> and <b>Jugan</b> sealed themselves and the three other dragon spirits in an egg under <b>Boseiju</b>. They hatched 50 years later, reborn as <b>Ao</b>, <b>Kairi</b>, <b>Junji</b>, <b>Atsushi</b>, and <b>Kura</b></i>",
                            "<i>The reborn form of <b>Ryusei</b>, protector of <b>Sokenzan</b></i>" };
                })
                );

            // Utvara Hellkite
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("utvaraHellkite", "Utvara Hellkite", idleAnim: "Heartbeat2AnimationProfile")
                .SetSprites("utvara-hellkite-mzug.png", "companion-bg.png")
                .SetStats(8, 4, 5)
                .WithCardType("Friendly")
                .WithFlavour("<i>The fear of dragons is as old and as powerful as the fear of death itself</i>")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Summon Dragon Token", 1),
                    };
                    data.traits = new List<CardData.TraitStacks>()
                    {
                        wtg.TStack("Flying", 1),
                    };
                    data.greetMessages = new string[1] { "<i>The fear of dragons is as old and as powerful as the fear of death itself</i>" };
                })
                );

            // Dragonlord's Servant
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("dragonlordsServant", "Dragonlord's Servant")
                .SetSprites("dragonlords-servant-sprescott.png", "companion-bg.png")
                .SetStats(4, 1, 3)
                .WithCardType("Friendly")
                .WithFlavour("<i>The tastiest morsels rarely make it to their intended destination</i>")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("While Active Decrease Counter Of Flying Allies", 1),
                    };
                    data.greetMessages = new string[2] { "<i>Atarka serving-goblins coat themselves with grease imbued with noxious herbs, hoping to discourage their ravenous masters from adding them to the meal</i>",
                            "<i>The tastiest morsels rarely make it to their intended destination</i>" };
                })
                );

            // Terror of the Peaks
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("terrorOfThePeaks", "Terror of the Peaks", idleAnim: "Heartbeat2AnimationProfile")
                .SetSprites("terror-of-the-peaks-jraphael.png", "companion-bg.png")
                .SetStats(6, 5, 5)
                .WithCardType("Friendly")
                .WithFlavour("<i>If it comes for you, die boldly or die swiftly—for die you will</i>")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("When Flying Ally Deployed Trigger Self", 1),
                    };
                    data.traits = new List<CardData.TraitStacks>()
                    {
                        wtg.TStack("Flying", 1),
                    };
                    data.greetMessages = new string[1] { "<i>If it comes for you, die boldly or die swiftly — for die you will</i>" };
                })
                );

            // Captain Lannery Storm
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("captainLanneryStorm", "Captain Lannery Storm", idleAnim: "ShakeAnimationProfile")
                .SetSprites("captain-lannery-storm-crallis.png", "companion-bg.png")
                .SetStats(3, 2, 3)
                .WithCardType("Friendly")
                .WithFlavour("\"I believe in love at first shine\"")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Gain Attack On Treasure", 1),
                    };
                    data.greetMessages = new string[6] { "\"I believe in love at first shine\"",
                        "\"Skim all the gold and magic rocks you want, but if I see one greasy fingerprint on my new boots, you'll be drinking bilgewater for a month\"",
                        "\"Charge like a red-hot cannonball straight to your target. You slow down, you sink\"",
                        "\"Just imagine what's waiting around the bend. Adventure. Discovery. Riches for the taking. This is why I sail\"",
                        "\"Opposable thumbs, opposable toes, prehensile tails, boundary issues ... no treasure is safe from a goblin\"",
                        "\"The best kind of treasure is the kind that leads to <i>more</i> treasure!\""};
                })
                );

            // Academy Manufactor
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("academyManufactor", "Academy Manufactor")
                .SetSprites("academy-manufactor-cwhite.png", "companion-bg.png")
                .SetStats(4, 1, 0)
                .WithCardType("Friendly")
                .WithFlavour("<i>Automated systems at the <b>Tolarian Academy</b> sort new acquisitions for optimal use, determining which should be studied, eaten, or sold</i>")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Trigger On First Treasure", 1),
                    };
                    data.traits = new List<CardData.TraitStacks>()
                    {
                            wtg.TStack("Draw", 1)
                    };
                    data.greetMessages = new string[2] { "<i>Automated systems at the <b>Tolarian Academy</b> sort new acquisitions for optimal use, determining which should be studied, eaten, or sold</i>",
                        "<i>It shapes wonders beyond our wildest dreams...</i>\n<i>Like sandwiches!</i>"};
                })
                );

            // Glorybringer
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("glorybringer", "Glorybringer", idleAnim: "ShakeAnimationProfile")
                .SetSprites("glorybringer-sburley.png", "companion-bg.png")
                .SetStats(5, 3, 3)
                .WithCardType("Friendly")
                .WithFlavour("<i>What the initiates face in the final trial is completely at <b>Hazoret's</b> discretion</i>")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>()
                    {
                            wtg.TStack("Spark", 1),
                            wtg.TStack("Flying", 1),
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("On Hit Equal Damage to Front Enemy", 1),
                    };
                    data.greetMessages = new string[1] { "<i>What the initiates face in the final trial is completely at <b>Hazoret's</b> discretion</i>" };
                })
                );

            // Earthquake Dragon
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("earthquakeDragon", "Earthquake Dragon", idleAnim: "GiantAnimationProfile")
                .SetSprites("earthquake-dragon-jgrenier.png", "companion-bg.png")
                .SetStats(10, 10, 8)
                .WithCardType("Friendly")
                .WithFlavour("<i>An empire can take centuries to build but mere moments to destroy</i>")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>()
                    {
                            wtg.TStack("Flying", 1),
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("While Active Reduce Counter By Allies With Flying", 1),
                    };
                    data.greetMessages = new string[2] { "<i>An empire can take centuries to build but mere moments to destroy</i>",
                            "*The ground rumbles beneath your feet*" };
                })
                );

            // Ojutai, Soul of Winter
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("ojutaiSoulOfWinter", "Ojutai, Soul of Winter", idleAnim: "FloatAnimationProfile")
                .SetSprites("ojutai-soul-of-winter-cstone.png", "companion-bg.png")
                .SetStats(9, 2, 4)
                .WithCardType("Friendly")
                .WithFlavour("<i>\"Human enlightenment is a firefly that sparks in the night. Dragon enlightenment is a beacon that disperses all darkness.\"</i>")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>()
                    {
                            wtg.TStack("Flying", 1),
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("On Flying Card Played Apply Snow To Random Enemy", 1),
                    };
                    data.greetMessages = new string[1] { "<i>\"Human enlightenment is a firefly that sparks in the night. Dragon enlightenment is a beacon that disperses all darkness.\"</i>" };
                })
                );

            // Professional Face-Breaker
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("professionalFaceBreaker", "Professional Face-Breaker")
                .SetSprites("professional-face-breaker-dscott.png", "companion-bg.png")
                .SetStats(5, 2, 3)
                .WithCardType("Friendly")
                .WithFlavour("<i>When you lose <b>Jetmir's</b> trust, his family makes sure you lose everything else</i>")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("On Card Played Destroy Right Treasure In Hand And Draw", 1),
                            wtg.SStack("MultiHit", 1),
                    };
                    data.greetMessages = new string[1] { "<i>When you lose <b>Jetmir's</b> trust, his family makes sure you lose everything else</i>" };
                })
                );

            // Shivan Dragon
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("shivanDragon", "Shivan Dragon", idleAnim: "FloatSquishAnimationProfile")
                .SetSprites("shivan-dragon-dgiancola.png", "companion-bg.png")
                .SetStats(7, 4, 3)
                .WithCardType("Friendly")
                .WithFlavour("<i>The undisputed master of the mountains of Shiv</i>")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Pre Turn Count Zoomlin In Hand & Gain Spice For Each", 1),
                    };
                    data.traits = new List<CardData.TraitStacks>()
                    {
                            wtg.TStack("Flying", 1),
                    };
                    data.greetMessages = new string[2] { "<b>*Breathes Fire Loudly*</b>\n<i>How was it stuck in ice?</i>",
                            "<i>The undisputed master of the mountains of Shiv</i>"};
                })
                );

            // Manaform Hellkite
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("manaformHellkite", "Manaform Hellkite", idleAnim: "FloatSquishAnimationProfile")
                .SetSprites("manaform-hellkite-amar.png", "companion-bg.png")
                .SetStats(5, 2, 4)
                .WithCardType("Friendly")
                .WithFlavour("<i>Just because it's a big, strong, unthinking beast of the sky intent on burning your house doesn't mean it can't use magic<i>")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Summon Dragon Token On Item Played", 1),
                    };
                    data.traits = new List<CardData.TraitStacks>()
                    {
                            wtg.TStack("Flying", 1),
                    };
                    data.greetMessages = new string[1] { "<i>Just because it's a big, strong, unthinking beast of the sky intent on burning your house doesn't mean it can't use magic</i>" };
                })
                );

            // Voracious Hydra
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("voraciousHydra", "Voracious Hydra", idleAnim: "Heartbeat2AnimationProfile")
                .SetSprites("voracious-hydra-wreynolds.png", "companion-bg.png")
                .SetStats(2, 1, 2)
                .WithCardType("Friendly")
                .WithFlavour("<i>Even baloths fear its feeding time<i>")
                .WithValue(45)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("When Deployed Apply Attack And Health To Self Equal To Zoomlin", 1),
                    };
                    data.traits = new List<CardData.TraitStacks>()
                    {
                            wtg.TStack("Fireball", 1),
                    };
                    data.greetMessages = new string[1] { "<i>Even baloths fear its feeding time</i>" };
                })
                );

            Debug.Log("[WTG] Dragon companions loaded!");
        }
    }
}
