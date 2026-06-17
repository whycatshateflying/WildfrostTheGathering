using AbsentAvalanche.GameSystems;
using Deadpan.Enums.Engine.Components.Modding;
using FrostEyeMakerExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static WildfrostTheGathering.WildfrostTheGathering;
namespace WildfrostTheGathering.Generic
{
    internal class Leaders
    {
        internal static void Load(List<object> assets, WildfrostTheGathering wtg)
        {
            Debug.Log("[WTG] Generic leaders loading!");
            
            // Helga leader
            assets.Add(new CardDataBuilder(wtg)
            .CreateUnit("helgaSkittishSeerLeader", "Helga, Skittish Seer", idleAnim: "SwayAnimationProfile")
            .WithCardType("Leader")
            .SetSprites("helga-skittish-seer-apiparo.png", "companion-bg.png")
            .SetStats(7, 2, 3)  // 5-9, 2, 3
            .WithFlavour("\"The kings in the dark will return, the mage in blue will bring about the end\"")
            .WithValue(25)
            .AddEye(-0.2f, 1.25f, 0.5f, 0.5f).AddEye(-0.44f, 1.26f, 0.5f, 0.5f)
            .SubscribeToAfterAllBuildEvent(data =>
            {
                data.startWithEffects = new CardData.StatusEffectStacks[]
                {
                    wtg.SStack("Gain Attack And Health And Draw When Ally with 4 Counter Is Deployed", 1)
                };
                data.greetMessages = new string[] { "\"The kings in the dark will return, the mage in blue will bring about the end\"" };
                data.createScripts = new CardScript[]
                {
                    GiveUpgrade(),
                    AddRandomHealth(-2,2),
                };
            })
            );

            // Omnath, locus of Creation
            assets.Add(new CardDataBuilder(wtg)
            .CreateUnit("omnathLocusOfCreationLeader", "Omnath, Locus of Creation", idleAnim: "Heartbeat2AnimationProfile")
            .WithCardType("Leader")
            .SetSprites("omnath-locus-of-creation-crahn.png", "companion-bg.png")
            .SetStats(6, 4, 0)  // 5-7, 4-5, null
            .WithFlavour("A being of pure elemental energy from the plane of <b>Zendikar</b>")
            .WithValue(25)
            .SubscribeToAfterAllBuildEvent(data =>
            {
                data.startWithEffects = new CardData.StatusEffectStacks[]
                {
                    wtg.SStack("Hit All Enemies", 1),
                    wtg.SStack("Restore Health On First Hit Item", 1),
                    wtg.SStack("Zoomlin On Second Hit Item", 1),
                    wtg.SStack("Trigger On Third Hit Item", 1),
                };
                data.greetMessages = new string[] { "A being of pure elemental energy from the plane of <b>Zendikar</b>"};
                data.createScripts = new CardScript[]
                {
                    GiveUpgrade(),
                    AddRandomHealth(-1,1),
                    AddRandomDamage(0,1),
                };
            })
            );

            // Isshin leader
            assets.Add(new CardDataBuilder(wtg)
            .CreateUnit("isshinTwoHeavensAsOne", "Isshin, Two Heavens as One", idleAnim: "SwayAnimationProfile")
            .WithCardType("Leader")
            .SetSprites("isshin-two-heavens-as-one-rpancoast.png", "companion-bg.png")
            .SetStats(5, 2, 5)  // 5-6, 2-3, 5
            .WithFlavour("Trained by the <b>Imperials</b> but disillusioned by their rigidity, he gave his heart-and his swords-to the <b>Asari Uprising</b>")
            .WithValue(25)
            .AddEye(-0.03f, 1.24f, 0.25f, 0.25f).AddEye(0.07f, 1.25f, 0.25f, 0.25f)
            .SubscribeToAfterAllBuildEvent(data =>
            {
                data.startWithEffects = new CardData.StatusEffectStacks[]
                {
                    wtg.SStack("On Card Played Ongoing Frenzy To AlliesInRow", 1)
                };
                data.greetMessages = new string[] { "Trained by the <b>Imperials</b> but disillusioned by their rigidity, he gave his heart-and his swords-to the <b>Asari Uprising</b>" };
                data.createScripts = new CardScript[]
                {
                    GiveUpgrade(),
                    AddRandomHealth(0,1),
                    AddRandomDamage(0,1),
                };
            })
            );

            // Valgavoth, Harrower of Souls (no art!)
            assets.Add(new CardDataBuilder(wtg)
            .CreateUnit("valgavothHarrowerOfSoulsLeader", "Valgavoth, Harrower of Souls", idleAnim: "FloatAnimationProfile")
            .WithCardType("Leader")
            .SetSprites("placeholder-companion.png", "companion-bg.png")
            .SetStats(7, 1, 6)  // 7-8, 1-2, 6-7
            .WithFlavour("Bound to a single house, <b>Valgavoth</b> simply expanded its boundaries until it consumed the plane, turning a vibrant world into his playground of terror")
            .WithValue(25)
            .SubscribeToAfterAllBuildEvent(data =>
            {
                data.traits = new List<CardData.TraitStacks>
                {
                    wtg.TStack("Flying"),
                };
                data.startWithEffects = new CardData.StatusEffectStacks[]
                {
                    wtg.SStack("Gain Attack On First Time Enemy Damaged Each Turn", 1)
                };
                data.greetMessages = new string[] { "Bound to a single house, <b>Valgavoth</b> simply expanded its boundaries until it consumed the plane, turning a vibrant world into his playground of terror" };
                data.createScripts = new CardScript[]
                {
                    GiveUpgrade(),
                    AddRandomHealth(0,1),
                    AddRandomDamage(0,1),
                    AddRandomCounter(0, 1),
                };
            })
            );

            // Yargle and Multani (no art!)
            assets.Add(new CardDataBuilder(wtg)
            .CreateUnit("yargleAndMultaniLeader", "Yargle and Multani", idleAnim: "PulseAnimationProfile")
            .WithCardType("Leader")
            .SetSprites("placeholder-companion.png", "companion-bg.png")
            .SetStats(5, 12, 5)  // 5-6, 11-14, 5
            .WithFlavour("\"I\'ve heard much about you from my daughter,\" <b>Multani</b> rumbled. \"There was a time when I\'d balk at your aid, phantom, but she has shown me the merit in <b>Urborg\'s</b> strange ways.\"\n\"Gnshhagghkkapphribbit,\" replied <b>Yargle</b>")
            .WithValue(25)
            .SubscribeToAfterAllBuildEvent(data =>
            {
                data.greetMessages = new string[] { "Gnshhagghkkapphribbit" };
                data.createScripts = new CardScript[]
                {
                    GiveUpgrade(),
                    AddRandomHealth(0,1),
                    AddRandomDamage(-1,2),
                };
            })
            );

            // Obeka Leader (no art!) (flavor!)
            assets.Add(new CardDataBuilder(wtg)
            .CreateUnit("obekaBruteChronologistLeader", "Obeka, Brute Chronologist", idleAnim: "SwayAnimationProfile")
            .WithCardType("Leader")
            .SetSprites("placeholder-companion.png", "companion-bg.png")
            .SetStats(10, 4, 4)  // 9-11, 3-5, 4
            .WithFlavour("Trained by the <b>Imperials</b> but disillusioned by their rigidity, he gave his heart-and his swords-to the <b>Asari Uprising</b>")
            .WithValue(25)
            .SubscribeToAfterAllBuildEvent(data =>
            {
                data.startWithEffects = new CardData.StatusEffectStacks[]
                {
                    wtg.SStack("On Card Played Add Null To Allies And Enemies", 2),
                    wtg.SStack("On Card Played Charge Redraw Bell Fully", 1),
                };
                data.greetMessages = new string[] { "Trained by the <b>Imperials</b> but disillusioned by their rigidity, he gave his heart-and his swords-to the <b>Asari Uprising</b>" };
                data.createScripts = new CardScript[]
                {
                    GiveUpgrade(),
                    AddRandomHealth(-1,1),
                    AddRandomDamage(-1,1),
                    AddRandomBoostEffects(0,1),
                };
            })
            );
            
            // Zozu Leader (no art!) (flavor!)
            assets.Add(new CardDataBuilder(wtg)
            .CreateUnit("zozuThePunisherLeader", "Zozu the Punisher", idleAnim: "SwayAnimationProfile")
            .WithCardType("Leader")
            .SetSprites("placeholder-companion.png", "companion-bg.png")
            .SetStats(10, 2, 2)  // 10-11, 2-3, 2
            .WithFlavour("Trained by the <b>Imperials</b> but disillusioned by their rigidity, he gave his heart-and his swords-to the <b>Asari Uprising</b>")
            .WithValue(25)
            .SubscribeToAfterAllBuildEvent(data =>
            {
                data.startWithEffects = new CardData.StatusEffectStacks[]
                {
                    wtg.SStack("On Card With Counter Deployed Trigger Against Them", 1)
                };
                data.greetMessages = new string[] { "Trained by the <b>Imperials</b> but disillusioned by their rigidity, he gave his heart-and his swords-to the <b>Asari Uprising</b>" };
                data.createScripts = new CardScript[]
                {
                    GiveUpgrade(),
                    AddRandomHealth(0,1),
                    AddRandomDamage(0,1),
                };
            })
            );

            // Minnllusion Token (no art!) (flavor!)
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("minnllusionToken", "Illusion Token", idleAnim: "FloatSquishAnimationProfile")
                .SetSprites("dragon-baby.png", "companion-bg.png")
                .SetStats(2, 0, 3)
                .WithCardType("Summoned")
                .WithValue(25)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("While Active Gain Attack For Minnllusion tokens", 1),
                    };
                    data.greetMessages = new string[1] { "Woah a token in the companion pool? That\'s not supposed to happen" };
                })
                );

            // Minn Leader (no art!) (flavor!)
            assets.Add(new CardDataBuilder(wtg)
            .CreateUnit("minnWilyIllusionist", "Minn, Wily Illusionist", idleAnim: "SwayAnimationProfile")
            .WithCardType("Leader")
            .SetSprites("placeholder-companion.png", "companion-bg.png")
            .SetStats(5, 2, 3)  // 5-6, 2-3, 3
            .WithFlavour("Trained by the <b>Imperials</b> but disillusioned by their rigidity, he gave his heart-and his swords-to the <b>Asari Uprising</b>")
            .WithValue(25)
            .SubscribeToAfterAllBuildEvent(data =>
            {
                data.startWithEffects = new CardData.StatusEffectStacks[]
                {
                    wtg.SStack("Summon Minnllusion When Charged Redraw Bell Hit", 1)
                };
                data.greetMessages = new string[] { "Trained by the <b>Imperials</b> but disillusioned by their rigidity, he gave his heart-and his swords-to the <b>Asari Uprising</b>" };
                data.createScripts = new CardScript[]
                {
                    GiveUpgrade(),
                    AddRandomHealth(0,1),
                    AddRandomDamage(0,1),
                };
            })
            );

            // Rankle Leader (no art!) (flavor!) (ugly!)
            assets.Add(new CardDataBuilder(wtg)
            .CreateUnit("rankleMasterOfPranks", "Rankle, Master of Pranks", idleAnim: "ShakeAnimationProfile")
            .WithCardType("Leader")
            .SetSprites("placeholder-companion.png", "companion-bg.png")
            .SetStats(7, 3, 3)  // 7-8, 3, 3
            .WithFlavour("Trained by the <b>Imperials</b> but disillusioned by their rigidity, he gave his heart-and his swords-to the <b>Asari Uprising</b>")
            .WithValue(25)
            .SubscribeToAfterAllBuildEvent(data =>
            {
                data.startWithEffects = new CardData.StatusEffectStacks[]
                {
                    wtg.SStack("After Turn Randomly Gain Ongoing Flying Or Ongoing Barrage", 1),
                    wtg.SStack("Ongoing Flying", 1),
                };
                data.greetMessages = new string[] { "Trained by the <b>Imperials</b> but disillusioned by their rigidity, he gave his heart-and his swords-to the <b>Asari Uprising</b>" };
                data.createScripts = new CardScript[]
                {
                    GiveUpgrade(),
                    AddRandomHealth(0,1),
                };
            })
            );

            // Clue Token (no art!) (flavor!)
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("clueToken", "Clue Token", idleAnim: "FloatAnimationProfile")
                .WithCardType("Clunker")
                .SetSprites("placeholder-item.png", "companion-bg.png")
                .SetStats(null, null, 0)
                .WithValue(25)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Scrap", 1),
                        wtg.SStack("When Destroyed Draw", 1),
                    };
                    data.greetMessages = new string[1] { "Woah a token in the companion pool? That\'s not supposed to happen" };
                })
                );

            // Alquist Proft Leader (no art!) (flavor!)
            assets.Add(new CardDataBuilder(wtg)
            .CreateUnit("alquistProftMasterSleuthLeader", "Alquist Proft, Master Sleuth", idleAnim: "SwayAnimationProfile")
            .WithCardType("Leader")
            .SetSprites("placeholder-companion.png", "companion-bg.png")
            .SetStats(6, 2, 5)  // 5-7, 2-3, 5
            .WithFlavour("Trained by the <b>Imperials</b> but disillusioned by their rigidity, he gave his heart-and his swords-to the <b>Asari Uprising</b>")
            .WithValue(25)
            .SubscribeToAfterAllBuildEvent(data =>
            {
                data.startWithEffects = new CardData.StatusEffectStacks[]
                {
                    wtg.SStack("When Deployed Summon Multiple ClueTokens", 2),
                    wtg.SStack("On Card Played Apply Instant Draw Restore To Applier And Kill To Clue Behind", 2)
                };
                data.greetMessages = new string[] { "Trained by the <b>Imperials</b> but disillusioned by their rigidity, he gave his heart-and his swords-to the <b>Asari Uprising</b>" };
                data.createScripts = new CardScript[]
                {
                    GiveUpgrade(),
                    AddRandomHealth(-1,1),
                    AddRandomDamage(0,1),
                };
            })
            );

            // Nelly Borca Leader (no art!) (flavor!)
            assets.Add(new CardDataBuilder(wtg)
            .CreateUnit("nellyBorcaImpulsiveAccuser", "Nelly Borca, Impulsive Accuser", idleAnim: "SwayAnimationProfile")
            .WithCardType("Leader")
            .SetSprites("placeholder-companion.png", "companion-bg.png")
            .SetStats(5, 1, 4)  // 5-6, 1-2, 4
            .WithFlavour("Trained by the <b>Imperials</b> but disillusioned by their rigidity, he gave his heart-and his swords-to the <b>Asari Uprising</b>")
            .WithValue(25)
            .SubscribeToAfterAllBuildEvent(data =>
            {
                data.startWithEffects = new CardData.StatusEffectStacks[]
                {
                    wtg.SStack("On Card Played Add Suspected And Ongoing Frenzy To Random Enemy", 1),
                    wtg.SStack("On Card Played Apply Haze To Suspected Enemies", 1)
                };
                data.greetMessages = new string[] { "Trained by the <b>Imperials</b> but disillusioned by their rigidity, he gave his heart-and his swords-to the <b>Asari Uprising</b>" };
                data.createScripts = new CardScript[]
                {
                    GiveUpgrade(),
                    AddRandomHealth(0,1),
                    AddRandomDamage(0,1),
                };
            })
            );

            Debug.Log("[WTG] Generic leaders loaded!");
        }
    }
}
