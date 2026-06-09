using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using static WildfrostTheGathering.WildfrostTheGathering;

namespace WildfrostTheGathering.DragonDeck
{
    internal class Items
    {
        internal static void Load(List<object> assets, WildfrostTheGathering wtg)
        {
            Debug.Log("[WTG] Dragon items loading!");
            // Shock
            assets.Add(new CardDataBuilder(wtg)
                    .CreateItem("shock", "Shock", idleAnim: "ShakeAnimationProfile")
                    .WithFlavour("\"The beauty of it is they never see it coming. Ever.\"\n<b>-Razzix, sparkmage</b>")
                    .SetDamage(2)
                    .SetSprites("shock-jfoster.png", "item-bg.png")
                    .WithValue(10)
                );

            // Cancel
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("cancel", "Cancel", idleAnim: "FloatAnimationProfile")
                .WithFlavour("\"Even the greatest inferno begins as a spark. And anyone can snuff out a spark.\"\n<b>-Chanyi, mistfire sage</b>")
                .SetDamage(0)
                .SetSprites("cancel-dpalumb.png", "item-bg.png")
                .WithValue(30)  // Base price in shop: 19-31
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                            wtg.TStack("Zoomlin", 1),
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Snow", 2)
                    };
                })
                );

            // Swiftfoot Boots
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("swiftfootBoots", "Swiftfoot Boots", idleAnim: "FloatAnimationProfile")
                .SetDamage(null)
                .SetSprites("swiftfoot-boots-svelinov.png", "item-bg.png")
                .WithValue(50)  // Base price in shop: 35-55
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                            wtg.TStack("Combo", 1)
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Reduce Counter", 1)
                    };
                })
                );

            // Treasure
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateItem("treasure", "Treasure", idleAnim: "ShakeAnimationProfile")
                .SetSprites("treasure-orichards.png", "item-bg.png")
                .SetDamage(null)
                .WithCardType("Item")
                .WithFlavour("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$")
                .WithValue(35)  // Base price in shop: +-6
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Instant Gain Zoomlin (To Card In Hand)", 1),
                            wtg.SStack("Instant Gain Unplayable", 1),
                    };

                    data.traits = new List<CardData.TraitStacks>(2)
                    {
                            wtg.TStack("Consume", 1),
                            wtg.TStack("Zoomlin", 1),
                    };

                    data.titleFallback = "Treasure";
                    data.textInsert = "<keyword=zoomlin>";
                    data.canPlayOnBoard = false;
                    data.canPlayOnHand = true;
                    data.uses = 1;
                    data.greetMessages = new string[2] { "<b><i>*cash money noises*</b></i>", "\"Woah an item token in the companion pool? That\'s not supposed to happen\"" };
                })
                );

            // Dragon's Fire
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateItem("dragonsFire", "Dragon's Fire", idleAnim: "ShakeAnimationProfile")
                .SetSprites("dragons-fire-cwhite.png", "item-bg.png")
                .SetDamage(1)
                .WithCardType("Item")
                .WithFlavour("Very hot... It hurts to look")
                .WithValue(50)  // Base price in shop: +-6
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Trigger Flying", 1)
                    };
                    data.canPlayOnEnemy = false;
                    data.canPlayOnFriendly = true;
                })
                );

            // Delayed Blast Fireball
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateItem("delayedBlastFireball", "Delayed Blast Fireball", idleAnim: "ShakeAnimationProfile")
                .SetDamage(3)
                .WithCardType("Item")
                .WithFlavour("The spell will fall upon a crowd like a dragon, ancient and full of death")
                .SetSprites("delayed-blast-fireball-azafiratos.png", "item-bg.png")
                .WithValue(50)  // Base price in shop: +-6
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            // SStack("On Card Played Clear Own Spice", 1), If I ever learn to order events. bleh.
                            wtg.SStack("On Card Played Apply Spice To Self", 3),
                    };
                    data.traits = new List<CardData.TraitStacks>()
                    {
                            wtg.TStack("Barrage", 1),
                    };
                })
                );

            // Spit Flame
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateItem("spitFlame", "Spit Flame", idleAnim: "ShakeAnimationProfile")
                .SetSprites("spit-flame-crahn.png", "item-bg.png")
                .SetDamage(2)
                .WithCardType("Item")
                .WithFlavour("\"Spread out, you idiots! Spread out!\"\n—<b>Marsden, party leader</b>, last words")
                .WithValue(50)  // Base price in shop: +-6
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Gain Zoomlin When Drawn If 2 Allies With Flying", 1),
                            wtg.SStack("On Card Played Apply Attack To Self", 2)
                    };
                })
                );

            // Draconic Lore
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateItem("draconicLore", "Draconic Lore", idleAnim: "FloatAnimationProfile")
                .SetSprites("draconic-lore-tbabbey.png", "item-bg.png")
                .SetDamage(null)
                .WithCardType("Item")
                .WithFlavour("The wyrmling studied the ancient carvings and dreamed of a day when her own exploits would be immortalized in stone")
                .WithValue(40)  // Base price in shop: +-6
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Gain Zoomlin When Drawn If 2 Allies With Flying", 1),
                    };
                    data.traits = new List<CardData.TraitStacks>()
                    {
                            wtg.TStack("Draw", 2),
                    };
                    data.needsTarget = false;
                })
                );

            // Lofty Denial
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateItem("loftyDenial", "Lofty Denial", idleAnim: "FloatAnimationProfile")
                .SetSprites("lofty-denial-mcastanon.png", "item-bg.png")
                .SetDamage(0)
                .WithCardType("Item")
                .WithFlavour("\"As one, nature lifts its voice to tell you this: \'No.\'\"")
                .WithValue(40)  // Base price in shop: +-6
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Increase Effects By 2 When Drawn If 2 Allies With Flying", 1),
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Snow", 1)
                    };
                })
                );

            // Spell Swindle
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("spellSwindle", "Spell Swindle", idleAnim: "ShakeAnimationProfile")
                .SetSprites("spell-swindle-vaminguez.png", "item-bg.png")
                .SetDamage(0)
                .WithCardType("Item")
                .WithFlavour("Honesty is the first casualty of war")
                .WithValue(50)  // Base price in shop: +-6
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Snow", 1),
                            wtg.SStack("Instant Add Treasure To Hand Equal To Target Counter", 1),
                    };
                })
                );

            // Fireball
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("fireball", "Fireball", idleAnim: "ShakeAnimationProfile")
                .SetSprites("fireball-mtedin.png", "item-bg.png")
                .SetDamage(1)
                .WithCardType("Item")
                .WithFlavour("Burning something is easy. Choosing a target can be more difficult")
                .WithValue(40)  // Base price in shop: +-6
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Bonus Damage Equal To Zoomlin In Hand", 1),
                            wtg.SStack("MultiHit", 2)
                    };
                    data.traits = new List<CardData.TraitStacks>
                    {
                            wtg.TStack("Aimless", 1)
                    };
                })
                );

            // Shared Animosity
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateItem("sharedAnimosity", "Shared Animosity", idleAnim: "Heartbeat2AnimationProfile")
                .SetSprites("shared-animosity-clukacs.png", "item-bg.png")
                .SetDamage(null)
                .WithCardType("Item")
                .WithFlavour("\"It is the nature of souls that they burn more brightly together than apart.\"\n<b>—Vessifrus, flamekin demagogue</b>")
                .WithValue(40)  // Base price in shop: +-6
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Apply Spice To Flying Allies Equal To Twice Flying Allies", 1),
                    };
                    data.needsTarget = false;
                })
                );

            // Bottle-Cap Blast
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("bottleCapBlast", "Bottle-Cap Blast", idleAnim: "ShakeAnimationProfile")
                .SetSprites("bottle-cap-blast-lsmilshkalne.png", "item-bg.png")
                .SetDamage(5)
                .WithCardType("Item")
                .WithFlavour("\"You\'ve got how much gold there??\"")
                .WithValue(40)  // Base price in shop: +-6
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Instant Add Treasure To Hand Equal To Target Health Below Zero", 1),
                    };
                })
                );

            // Explosive Vegetation
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateItem("explosiveVegetation", "Explosive Vegetation", idleAnim: "FloatSquishAnimationProfile")
                .SetSprites("explosive-vegetation-javon.png", "item-bg.png")
                .SetDamage(null)
                .WithCardType("Item")
                .WithFlavour("Despite the flames and devastation of the dragons, Tarkir continued to thrive.")
                .WithValue(40)  // Base price in shop: +-6
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("On Card Played Gain Zoomlin To X Random Cards In Hand", 2),
                    };
                    data.needsTarget = false;
                })
                );

            // Fires of Yavimaya
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("firesOfYavimaya", "Fires of Yavimaya", idleAnim: "Heartbeat2AnimationProfile")
                .SetSprites("fires-of-yavimaya-vmayerik.png", "item-bg.png")
                .SetDamage(null)
                .WithFlavour("Yavimaya lights the quickest path to battle")
                .WithValue(40)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Instant Gain Ongoing While Active Allies Gain Spark", 1),
                            wtg.SStack("Spice", 1)
                    };
                    data.canPlayOnHand = true;
                    data.targetConstraints = new TargetConstraint[]
                    {
                        new Scriptable<TargetConstraintOr>(tco =>
                        {
                            tco.constraints = new TargetConstraint[] {
                                new Scriptable<TargetConstraintHasReaction>(),
                                new Scriptable<TargetConstraintMaxCounterMoreThan>(tcmcmt =>
                                {
                                    tcmcmt.moreThan = 0;
                                })
                            };
                        })
                    };
                    data.canPlayOnEnemy = false;
                })
                );

            // Resourceful Defense
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("resourcefulDefense", "Resourceful Defense", idleAnim: "FloatSquishAnimationProfile")
                .SetSprites("resourceful-defense-ftneh.png", "item-bg.png")
                .SetDamage(null)
                .WithFlavour("One day...")
                .WithValue(40)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Instant Gain Ongoing Halt Spice", 1),
                            wtg.SStack("Spice", 2)
                    };
                    data.canPlayOnHand = true;
                })
                );

            // Monstrous Rage
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("monstrousRage", "Monstrous Rage", idleAnim: "ShakeAnimationProfile")
                .SetSprites("monstrous-rage-bpindado.png", "item-bg.png")
                .SetDamage(null)
                .WithFlavour("Yavimaya lights the quickest path to battle")
                .WithValue(40)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Spice", 2),
                            wtg.SStack("Instant Gain Trample", 1),
                    };
                    data.targetConstraints = new TargetConstraint[]
                    {
                        new Scriptable<TargetConstraintDoesAttack>(),
                        new Scriptable<TargetConstraintIsUnit>()
                    };
                    data.canPlayOnHand = true;
                })
                );

            Debug.Log("[WTG] Dragon items loaded!");
        }
    }
}
