using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WildfrostTheGathering.Generic
{
    internal class Companions
    {
        internal static void Load(List<object> assets, WildfrostTheGathering wtg)
        {
            Debug.Log("[WTG] Generic companions loading!");
            // Fear of Sleep Paralysis
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("fearOfSleepParalysis", "Fear of Sleep Paralysis", idleAnim: "FloatSquishAnimationProfile")
                .SetStats(7, 2, 3)
                .WithPools("SnowUnitPool")
                .SetSprites("fear-of-sleep-paralysis-jraphael.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                                    wtg.TStack("Flying", 1),
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                                    wtg.SStack("Snow", 2),
                    };
                    data.greetMessages = new string[] { "Don\'t fear the darkness. Fear what it hides",
                                                        "Sleep doesn\'t always mean rest"};
                })
                );

            // Mulldrifter
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("mulldrifter", "Mulldrifter", idleAnim: "FloatAnimationProfile")
                .SetStats(3, 2, 2)
                .WithPools("GeneralUnitPool")
                .SetSprites("mulldrifter-efortune.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Flying", 1),
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("When deployed draw", 2),
                    };
                    data.greetMessages = new string[] { "As fleeting as the dreams spawned in its wake" };
                })
                );

            // Nulldrifter
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("nulldrifter", "Nulldrifter", idleAnim: "FloatAnimationProfile")
                .SetStats(4, 2, 4)
                .WithPools("GeneralUnitPool")
                .SetSprites("nulldrifter-jbodin.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Flying", 1),
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("When deployed draw", 2),
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Demonize", 1)
                    };
                    data.greetMessages = new string[] { "\"Look, and see the future.\"\n<b>—Ayli, high priest of the Eternal Pilgrims</b>" };
                })
                );

            // Deadeye Navigator
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("deadeyeNavigator", "Deadeye Navigator", idleAnim: "WaveAnimationProfile")
                .SetStats(7, 4, 4)
                .WithPools("GeneralUnitPool")
                .SetSprites("deadeye-navigator-lsetiawan.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Card Played Cleanse Self And Allies", 1),
                    };
                    data.greetMessages = new string[] { "The navigator guides the ship. The bird guides the navigator" };
                })
                );

            // Beast Whisperer
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("beastWhisperer", "Beast Whisperer", idleAnim: "SwayAnimationProfile")
                .SetStats(4, 3, 3)
                .WithPools("GeneralUnitPool")
                .SetSprites("beast-whisperer-mstewart.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("When Ally Deployed Draw", 1),
                    };
                    data.greetMessages = new string[] { "The tiniest mouse speaks louder to me than all the festival crowds on <b>Tin Street</b>",
                                                        "No symphony can compare to the melody of the ferrox\'s roar"};
                })
                );

            // Warren Soultrader
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("warrenSoultrader", "Warren Soultrader", idleAnim: "SwayAnimationProfile")
                .SetStats(5, 4, 3)
                .WithPools("GeneralUnitPool")
                .SetSprites("warren-soultrader-pventers.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Card Played Sacrifice Ally Behind Needs Health", 1),
                        wtg.SStack("On Card Played Add Gain Treasure Equal To Own Health To Ally Behind", 1),
                    };
                    data.greetMessages = new string[] { "The living take their souls for granted. The dead know what they\'re worth" };
                })
                );

            // Laboratory Maniac
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("laboratoryManiac", "Laboratory Maniac", idleAnim: "ShakeAnimationProfile")
                .SetStats(2, 2, 5)
                .WithPools("GeneralUnitPool")
                .SetSprites("laboratory-maniac-jfelix.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Discard Shuffled Gain Attack Health", 2),
                    };
                    data.greetMessages = new string[] { "His mind whirled with grand plans, never thinking of what might happen if he were to succeed" };
                })
                );

            // Springheart Nantuko
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("springheartNantuko", "Springheart Nantuko", idleAnim: "SwayAnimationProfile")
                .SetStats(2, 1, 5)
                .WithPools("GeneralUnitPool")
                .SetSprites("springheart-nantuko-vluftfullina.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Card Played Summon Ally Behind With Destroy Self", 1),
                    };
                    data.greetMessages = new string[] { "Every seed holds strength and teaches patience" };
                })
                );

            // Death's Shadow
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("deathsShadow", "Death\'s Shadow", idleAnim: "FloatAnimationProfile")
                .SetStats(4, 1, 2)
                .WithPools("GeneralUnitPool")
                .SetSprites("deaths-shadow-hlyon.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("While Active Gain Attack Equal To Damage On Leader", 1),
                    };
                    data.greetMessages = new string[] { "The shadow of the candle looms tall even as its light grows dim" };
                })
                );

            // Hydra Omnivore
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("hydraOmnivore", "Hydra Omnivore", idleAnim: "GiantAnimationProfile")
                .SetStats(8, 8, 6)
                .WithPools("GeneralUnitPool")
                .SetSprites("hydra-omnivore-svelinov.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Barrage", 1)
                    };
                    data.greetMessages = new string[] { "\"We should learn from the hydra. It never plays favorites; it devours everyone equally\"<b>\n—Thrass, elder druid</b>" };
                })
                );

            // Siege Rhino
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("siegeRhino", "Siege Rhino", idleAnim: "GiantAnimationProfile")
                .SetStats(6, 5, 4)
                .WithPools("GeneralUnitPool")
                .SetSprites("siege-rhino-vbaga.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Spark", 1),
                        wtg.TStack("Trample", 1),
                    };
                    data.greetMessages = new string[] { "The mere approach of an Abzan war beast is enough to send enemies fleeing in panic" };
                })
                );

            // Overseer of the damned (no art!)
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("overseerOfTheDamned", "Overseer of the Damned", idleAnim:"FloatAnimationProfile")
                .SetStats(7, 5, 5)
                .WithPools("GeneralUnitPool")
                .SetSprites("placeholder-companion.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Flying", 1),
                        wtg.TStack("Spark", 1),
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Summon Zombie When Enemy Killed", 1)
                    };
                    data.greetMessages = new string[] { "Death in form and function",
                                                        "Rise, my pretty thing. Why rot in the river when you can serve at my bidding?"};
                })
                );

            // Overseer of the damned Zombie Token (no art!)
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("zombieToken", "Zombie Token", idleAnim:"SwayAnimationProfile")
                .SetStats(2, 2, 5)
                .WithCardType("Summoned")
                .SetSprites("dragon-baby.png", "companion-bg.png")
                .WithValue(25)
                );

            // Tetzimoc, Primal Death (no art!)
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("tetzimocPrimalDeath", "Tetzimoc, Primal Death", idleAnim:"GiantAnimationProfile")
                .SetStats(6, 6, 4)
                .WithPools("GeneralUnitPool")
                .SetSprites("placeholder-companion.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Pigheaded", 1),
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("While In Hand Post Turn Apply Prey To Random Enemy", 1),
                        wtg.SStack("When Deployed Kill Enemies With Prey", 1),
                    };
                    data.greetMessages = new string[] { "The embodiment of death itself on <b>Ixalan</b>" };
                })
                );

            // Craterhoof Behemboth (no art!)
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("craterhoofBehemoth", "Craterhoof Behemoth", idleAnim:"GiantAnimationProfile")
                .SetStats(6, 4, 5)
                .WithPools("GeneralUnitPool")
                .SetSprites("placeholder-companion.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Spark", 1),
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("When Deployed Add Ongoing Trample To Self And Allies", 1),
                        wtg.SStack("Instant Apply Spice To Self And Allies Equal To Self And Allies", 1),
                    };
                    data.greetMessages = new string[] { "Its footsteps of today are the lakes of tomorrow" };
                    })
                );

            Debug.Log("[WTG] Generic companions loaded!");
        }
    }
}
