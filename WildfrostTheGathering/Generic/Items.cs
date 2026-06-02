using Deadpan.Enums.Engine.Components.Modding;
using UnityEngine;
using System.Collections.Generic;
using static WildfrostTheGathering.WildfrostTheGathering;
using System.Runtime.Remoting.Channels;

namespace WildfrostTheGathering.Generic
{
    internal class Items
    {
        internal static void Load(List<object> assets, WildfrostTheGathering wtg)
        {
            Debug.Log("[WTG] Generic items are loading!");
            // Lightning Bolt 
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("lightningBolt", "Lightning Bolt", idleAnim: "ShakeAnimationProfile")
                .WithFlavour("The sparkmage shrieked, calling on the rage of the storms of his youth. To his surprise, the sky responded with a fierce energy he’d never thought to see again")
                .SetDamage(3)
                .WithPools("GeneralItemPool")
                .SetSprites("lightning-bolt-cmoeller.png", "item-bg.png")
                .WithValue(40)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                            wtg.TStack("Noomlin", 1),
                    };
                })
                );

            // Giant Growth 
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("giantGrowth", "Giant Growth", idleAnim: "GiantAnimationProfile")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .WithFlavour("\"Only the most effective tactics stand the test of time.\"\n<b>—Gamelen, Citanul elder</b>")
                .SetSprites("giant-growth-mcavotta.png", "item-bg.png")
                .WithValue(40)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Spice", 3),
                            wtg.SStack("Shell", 3),
                    };
                    data.canPlayOnHand = true;
                })
                );

            // Dark Ritual 
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateItem("darkRitual", "Dark Ritual", idleAnim: "Heartbeat2AnimationProfile")
                .SetSprites("dark-ritual-clangley.png", "item-bg.png")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .WithCardType("Item")
                .WithFlavour("\"From void evolved <b>Phyrexia</b>. Great <b>Yawgmoth, Father of Machines</b>, saw its perfection. Thus the <b>Grand Evolution</b> began.\"\n—Phyrexian Scriptures")
                .WithValue(40)  // Base price in shop: +-6
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Instant Gain Zoomlin (To Card In Hand)", 1),
                    };

                    data.traits = new List<CardData.TraitStacks>(2)
                    {
                            wtg.TStack("Zoomlin", 1),
                    };
                    data.textInsert = "<keyword=zoomlin>";
                    data.canPlayOnBoard = false;
                    data.canPlayOnHand = true;
                    data.uses = 1;
                })
                );

            // Ancestral Recall 
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("ancestralRecall", "Ancestral Recall", idleAnim: "FloatAnimationProfile")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .WithFlavour("Dwell longest on the thoughts that shine brightest")
                .SetSprites("ancestral-recall-rpancoast.png", "item-bg.png")
                .WithValue(40)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("On Card Played Draw Cards (Not Boostable)", 3),
                    };
                    data.needsTarget = false;
                })
                );

            // Healing Salve 
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("healingSalve", "Healing Salve", idleAnim: "ShakeAnimationProfile")
                .WithFlavour("\"<b>Xantcha</b> is recovering. The medicine is slow, but my magic would have killed her\"\n<b>—Serra</b>, to <b>Urza</b>")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .SetSprites("healing-salve-ghildebrandt-thildebrandt.png", "item-bg.png")
                .WithValue(40)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                            wtg.TStack("Noomlin", 1),
                            wtg.TStack("Combo", 1),
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Heal", 3),
                    };
                    data.canPlayOnHand = true;
                })
                );

            // Disdainful Stroke 
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("disdainfulStroke", "Disdainful Stroke", idleAnim: "FloatAnimationProfile")
                .WithFlavour("\"You are beneath contempt. Your lineage will be forgotten\"")
                .SetDamage(0)
                .WithPools("SnowItemPool")
                .SetSprites("disdainful-stroke-svelinov.png", "item-bg.png")
                .WithValue(40)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Hit All Enemies With 3 Or Greater Counter", 1)
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Snow", 3)
                    };
                })
                );

            // Counterspell 
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("counterspell", "Counterspell", idleAnim: "FloatAnimationProfile")
                .WithFlavour("\"It was probably a lousy spell in the first place\"<b>\n—Ertai, wizard adept</b>")
                .SetDamage(0)
                .WithPools("SnowItemPool")
                .SetSprites("counterspell-zstella.png", "item-bg.png")
                .WithValue(30)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Noomlin", 1),
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Snow", 3)
                    };
                })
                );

            // Rampant Growth 
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("rampantGrowth", "Rampant Growth", idleAnim: "ShakeAnimationProfile")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .SetSprites("rampant-growth-sbelledin.png", "item-bg.png")
                .WithFlavour("Nature grows solutions to its problems")
                .WithValue(55)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Card Played Gain Noomlin To Random Card In Hand", 1),
                    };
                    data.canPlayOnBoard = false;
                    data.needsTarget = false;
                })
                );

            // Time Walk 
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateItem("timeWalk", "Time Walk", idleAnim: "FloatAnimationProfile")
                .SetSprites("time-walk-crahn.png", "item-bg.png")
                .SetDamage(null)
                .WithPools("SnowItemPool")
                .WithFlavour("Time is a marvelous plaything")
                .WithValue(40)  // Base price in shop: +-6
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Card Played Apply Snow To All Enemies", 2)
                    };
                    data.traits = new List<CardData.TraitStacks>()
                    {
                            wtg.TStack("Consume", 1),
                    };
                    data.needsTarget = false;
                    data.uses = 1;
                })
                );

            // An Offer You Can't Refuse 
            assets.Add(
                new CardDataBuilder(wtg)
                .CreateItem("anOfferYouCantRefuse", "An Offer You Can't Refuse", idleAnim: "FloatAnimationProfile")
                .SetSprites("an-offer-you-cant-refuse-dwilliams.png", "item-bg.png")
                .SetDamage(0)
                .WithPools("SnowItemPool")
                .WithFlavour("\"I think you'll find my terms quite agreeable, if you know what’s good for you\"")
                .WithValue(40)  // Base price in shop: +-6
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Snow", 3),
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("On Card Played Add Treasure To Hand", 1),
                    };
                    data.traits = new List<CardData.TraitStacks>()
                    {
                            wtg.TStack("Consume", 1),
                    };
                    data.uses = 1;
                })
                );

            // Molten Duplication 
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("moltenDuplication", "Molten Duplication", idleAnim: "Heartbeat2AnimationProfile")
                .WithText("Summon a copy of an ally with \"Destroy Self\"")
                .WithFlavour("No one had seen this side of Angeline before, not even her")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .SetSprites("molten-duplication-jdura.png", "item-bg.png")
                .WithValue(65)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Instant Summon Copy With Destroy Self", 1),
                    };
                    data.canPlayOnHand = false;
                    data.canPlayOnEnemy = false;
                    data.targetConstraints = new TargetConstraint[]
                    {
                        new Scriptable<TargetConstraintHasHealth>(),
                    };
                })
                );

            // Skullclamp 
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("skullclamp", "Skullclamp", idleAnim: "FloatAnimationProfile")
                .WithFlavour("The mind is a beautiful bounty encased in an annoying bone container")
                .SetDamage(1)
                .WithPools("GeneralItemPool")
                .SetSprites("skullclamp-dljunggren.png", "item-bg.png")
                .WithValue(65)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("On Card Played Add When Destroyed Draw To Target", 2),
                    };
                    data.canPlayOnHand = false;
                    data.canPlayOnEnemy = false;
                })
                );

            // Toxic Deluge
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("toxicDeluge", "Toxic Deluge", idleAnim: "HangAnimationProfile")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .WithFlavour("Armor dissolved to flesh. Flesh melted to bone. Bone fell away to nothing")
                .SetSprites("toxic-deluge-svelinov.png", "item-bg.png")
                .WithValue(65)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]{
                        wtg.SStack("On Card Played Gain Demonize To Field", 1),
                    };
                    data.needsTarget = false;
                })
                );

            // Fog
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("fog", "Fog", idleAnim: "FloatAnimationProfile")
                .SetDamage(0)
                .WithPools("GeneralItemPool")
                .WithFlavour("\"I fear no army or beast, but only the morning fog. Our assault can survive everything else.\"\n<b>—Lord Hilneth</b>")
                .SetSprites("fog-jjones.png", "item-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Hit All Enemies With 1 Counter", 1),
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Frost", 2)
                    };
                })
                );

            // Injury
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("injury", "Injury", idleAnim: "Heartbeat2AnimationProfile")
                .SetDamage(2)
                .WithFlavour("Sticks and stones may break my bones...")
                .SetSprites("injury-lgraciano.png", "item-bg.png")
                .WithValue(10)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("MultiHit", 1),
                    };
                    data.traits = new List<CardData.TraitStacks>()
                    {
                            wtg.TStack("Aimless", 1),
                            wtg.TStack("Consume", 1),
                    };
                    data.uses = 1;
                })
                );

            // Insult
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("insult", "Insult", idleAnim: "FloatSquishAnimationProfile")
                .SetDamage(0)
                .WithPools("GeneralItemPool")
                .WithFlavour("...but words can never hurt me")
                .SetSprites("insult-lgraciano.png", "item-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Demonize", 2),
                            wtg.SStack("Instant Summon Injury In Hand", 1),
                    };
                })
                );

            // Tower Defense
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("towerDefense", "Tower Defense", idleAnim: "SwayAnimationProfile")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .WithFlavour("\"The drakes are practice. We may one day need to bring down a sky swallower, or maybe even <b>Rakdos himself</b>.\"\n<b>—Korun Nar, Rubblebelt hunter</b>")
                .SetSprites("tower-defense-smckinnon.png", "item-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                            wtg.TStack("Barrage", 1),
                            wtg.TStack("Consume", 1),
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Increase Max Health", 2),
                    };
                    data.uses = 1;
                })
                );

            // Annie Joins Up
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("annieJoinsUp", "Annie Joins Up", idleAnim: "FloatAnimationProfile")
                .SetDamage(4)
                .WithPools("GeneralItemPool")
                .WithFlavour("One last job, then she could retire in peace.")
                .SetSprites("annie-joins-up-wbeckert.png", "item-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("On Hit Gain Ongoing Frenzy To Front Ally", 1),
                    };
                })
                );

            // Maddening Cacophony
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("maddeningCacophony", "Maddening Cacophony", idleAnim: "FloatAnimationProfile")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .WithFlavour("<b>Jace</b> traced <b>Nahiri</b> to the <b>Singing City</b>, but the magic of the ancient ruins threatened to overwhelm his mind")
                .SetSprites("maddening-cacophony-mvilleneuve.png", "item-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("On Card Played Increase Own Effects", 2),
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Reduce Max Health", 2),
                    };
                    data.canPlayOnHand = true;
                })
                );

            // Take the Bait
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("takeTheBait", "Take The Bait", idleAnim: "FloatSquishAnimationProfile")
                .SetDamage(0)
                .WithPools("GeneralItemPool")
                .WithFlavour("The light of hope blinded <b>Pantor</b> to the ills of the world")
                .SetSprites("take-the-bait-jgrenier.png", "item-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Hit All Enemies With 1 Counter", 1),
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Haze", 1),
                            wtg.SStack("Trigger With Text", 1),
                    };
                })
                );

            // Reckless Charge
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("recklessCharge", "Reckless Charge", idleAnim: "Heartbeat2AnimationProfile")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .WithFlavour("It's hard to keep the peace if you can't even control your temper")
                .SetSprites("reckless-charge-sargyle.png", "item-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Spice", 2),
                            wtg.SStack("Instant Gain Spark", 1),
                    };
                    data.canPlayOnHand = true;
                })
                );

            // Abrade
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("abrade", "Abrade", idleAnim: "WaveAnimationProfile")
                .SetDamage(3)
                .WithPools("GeneralItemPool")
                .WithFlavour("The desert is a voracious beast, devouring both flesh and stone")
                .SetSprites("abrade-jdro.png", "item-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Kill Clunker Targets", 1),
                    };
                })
                );

            // Slip out the back
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("slipOutTheBack", "Slip Out the Back", idleAnim: "FloatAnimationProfile")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .WithFlavour("\"I was never here\"")
                .SetSprites("slip-out-the-back-zalfonso.png", "item-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Instant Phase Out", 1),
                            wtg.SStack("Spice", 1)
                    };
                })
                );

            // Boros Charm
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("borosCharm", "Boros Charm", idleAnim: "PulseAnimationProfile")
                .SetDamage(3)
                .WithPools("GeneralItemPool")
                .WithFlavour("\"Practice compassion and mercy. But know when they must end\"\n<b>—Aurelia</b>")
                .SetSprites("boros-charm-zboros.png", "item-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("MultiHit", 1),
                    };
                })
                );

            // Beast Within: Beast Token
            assets.Add(new CardDataBuilder(wtg).CreateUnit("beastToken", "Beast Token", idleAnim: "GiantAnimationProfile")
                .SetSprites("beast-token-jejsing.png", "companion-bg.png")
                .SetStats(4, 3, 4)
                .WithCardType("Summoned")
                .WithValue(25)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.greetMessages = new string[1] { "Woah a token in the companion pool? That\'s not supposed to happen" };
                })
                );

            // Beast Within
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("beastWithin", "Beast Within", idleAnim: "Heartbeat2AnimationProfile")
                .SetDamage(3)
                .WithPools("GeneralItemPool")
                .WithFlavour("Monsters dwell in every heart. We only think civilization tames them")
                .SetSprites("beast-within-jejsing.png", "item-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Instant Summon Beast On Other Side", 1),
                    };
                })
                );

            // Natural Unity
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("naturalUnity", "Natural Unity", idleAnim: "FloatAnimationProfile")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .WithFlavour("\"No one hero will save this day. Today we must all be heroes\"\n-<b>Gideon Jura</b>")
                .SetSprites("natural-unity-rpancoast.png", "item-bg.png")
                .WithValue(60)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Consume", 1),
                        wtg.TStack("Conspiracy", 1),
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                            wtg.SStack("Instant Gain Add Attack", 1),
                    };
                    data.uses = 1;
                    data.canPlayOnHand = true;
                    data.createScripts = new CardScript[]
                    {
                            wtg.GiveUpgrade("CrownCursed"),
                    };
                })
                );

            // Power Play
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("powerPlay", "Power Play", idleAnim: "FloatAnimationProfile")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .WithFlavour("\"Schuyler\'s seat was up for grabs so I took it\"")
                .SetSprites("power-play-mstewart.png", "item-bg.png")
                .WithValue(60)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Consume", 1),
                        wtg.TStack("Conspiracy", 1),
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Increase Counter To All Enemies", 1),
                    };
                    data.uses = 1;
                    data.canPlayOnBoard = false;
                    data.needsTarget = false;
                    data.createScripts = new CardScript[]
                    {
                        wtg.GiveUpgrade("CrownCursed"),
                    };
                })
                    );

            // Damnable Pact
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("damnablePact", "Damnable Pact", idleAnim:"PulseAnimationProfile")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .WithFlavour("\"<b>Silumgar\'s</b> mind is a dark labyrinth, full of grim secrets and subtle traps.\"\n<b>—Siara, the Dragon’s Mouth</b>")
                .SetSprites("damnable-pact-zstella.png", "item-bg.png")
                .WithValue(60)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Consume", 1),
                    };
                    data.uses = 1;
                    data.targetConstraints = new TargetConstraint[]
                    {
                        new Scriptable<TargetConstraintIsLeader>(),
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Set Health To 1 And Draw", 1),
                    };
                    data.canPlayOnHand = true;
                    data.canPlayOnBoard = true;
                })
                );

            // Assassinate
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("assassinate", "Assassinate", idleAnim: "FloatAnimationProfile")
                .SetDamage(10)
                .WithPools("GeneralItemPool")
                .WithFlavour("\"This is how wars are won-not with armies of soldiers but with a single knife blade, artfully placed.\"<b>\n—Yurin, royal assassin</b>")
                .SetSprites("assassinate-kwalker.png", "item-bg.png")
                .WithText("Target must be at maximum <keyword=counter>")
                .WithValue(60)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.targetConstraints = new TargetConstraint[]
                    {
                        new Scriptable<TargetConstraintCounterAtMax>(),
                    };
                })
                );

            // Advantageous Proclamation
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("advantageousProclamation", "Advantageous Proclamation", idleAnim: "FloatAnimationProfile")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .WithFlavour("\"The beneficent council deems you worthy of favor. They hope this doesn\'t provoke envy from your peers\"")
                .SetSprites("advantageous-proclamation-izzy.png", "item-bg.png")
                .WithValue(60)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Consume", 1),
                        wtg.TStack("Conspiracy", 1),
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("MultiHit", 1),
                        wtg.SStack("On Card Played Destroy 1 Card In Deck", 1),
                    };
                    data.uses = 1;
                    data.canPlayOnBoard = false;
                    data.needsTarget = false;
                    data.createScripts = new CardScript[]
                    {
                        wtg.GiveUpgrade("CrownCursed"),
                    };
                })
                );

            // Doomsday
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("doomsday", "Doomsday", idleAnim: "FloatAnimationProfile")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .WithFlavour("\"The beneficent council deems you worthy of favor. They hope this doesn\'t provoke envy from your peers\"")
                .SetSprites("doomsday-nbradley.png", "item-bg.png")
                .WithValue(60)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Consume", 1),
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Card Played Destroy Non Crown Deck And Discard", 1),
                    };
                    data.uses = 1;
                    data.canPlayOnBoard = false;
                    data.needsTarget = false;
                })
                );

            // Throes of Chaos
            assets.Add(new CardDataBuilder(wtg)
                .CreateItem("throesOfChaos", "Throes of Chaos", idleAnim:"PulseAnimationProfile")
                .SetDamage(null)
                .WithPools("GeneralItemPool")
                .SetSprites("throes-of-chaos-ikieryluk.png", "item-bg.png")
                .WithValue(35)
                .WithFlavour("When the world is consumed by chaos, the skilled and the foolish are on equal footing")
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        wtg.TStack("Noomlin", 1)
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("On Card Played Random Tutor Add Frenzy", 1),
                        wtg.SStack("On Card Played Unplayable To Hand", 1)
                    };
                    data.canPlayOnBoard = false;
                    data.canPlayOnHand = false;
                    data.needsTarget = false;
                })
                );
            Debug.Log("[WTG] Generic items are loaded!");
        }
    }
}
