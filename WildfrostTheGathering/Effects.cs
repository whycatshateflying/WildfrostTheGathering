using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static WildfrostTheGathering.WildfrostTheGathering;

namespace WildfrostTheGathering
{
    internal class Effects
    {
        internal static void Load(List<object> assets, WildfrostTheGathering wtg)
        {
            Debug.Log("[WTG] Effects loading!");
            // Guttersnipe: trigger when you play an item with attack
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCertainCardPlayed>("Trigger On Item With Attack Played")
                .WithText("Trigger when you plan an item with<keyword=attack>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .WithIsReaction(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCertainCardPlayed>(data =>
                {
                    data.descColorHex = "F99C61";
                    data.allowedCardType = ScriptableObject.CreateInstance<CardType>();
                    data.allowedCardType.item = true;
                    data.allowedCardType.name = "Item";
                    data.attackGreaterThan = -1;
                    data.effectToApply = wtg.TryGet<StatusEffectInstantTrigger>("Trigger");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Kill Clunker Targets
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantKill>("Kill Clunker Targets")
                .WithText("Kill Clunker targets")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantKill>(data =>
                {
                    data.killConstraints = new TargetConstraint[]
                    {
                        new Scriptable<TargetConstraintIsCardType>(tcict =>
                        {
                            CardType clunkerCardType = ScriptableObject.CreateInstance<CardType>();
                            clunkerCardType.name = "Clunker";
                            tcict.allowedTypes = new CardType[]{clunkerCardType};
                        }),
                    };
                })
                );

            // Draw cards on played (not boostable)
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Draw Cards (Not Boostable)")
                .WithText("Draw {a} cards")
                .WithStackable(true)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantDraw>("Instant Draw");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Disdainful Stroke: Hits all enemies with 3 or Greater Counter
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectChangeTargetMode>("Hit All Enemies With 3 Or Greater Counter")
                .WithText("Hits all enemies with 3 or greater <keyword=counter>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectChangeTargetMode>(data =>
                {
                    data.targetMode = new Scriptable<TargetModeAll>(tma =>
                    {
                        tma.constraints = new TargetConstraint[]
                        {
                            new Scriptable<TargetConstraintCounterMoreThan>(tccmt =>
                            {
                                tccmt.value = 2;
                            })
                        };
                    });
                })
                );

            // Rampant Growth Apply Noomlin to a random card in hand on card played
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Gain Noomlin To Random Card In Hand")
                .WithText("Add <keyword=noomlin> to a random card in your hand")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.targetConstraints = new TargetConstraint[]
                    {
                        new Scriptable<TargetConstraintHasTrait>(tcht =>
                        {
                            tcht.not = true;
                            tcht.trait = wtg.TryGet < TraitData >("Noomlin");
                            tcht.ignoreSilenced = false;
                        }),
                        new Scriptable<TargetConstraintHasStatus>(tchs =>
                        {
                            tchs.not = true;
                            tchs.status = wtg.TryGet < StatusEffectFreeAction >("Free Action");
                        }),
                    };
                    data.effectToApply = wtg.TryGet<StatusEffectTemporaryTrait>("Temporary Noomlin");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.RandomCardInHand;
                    data.doPing = false;
                    data.targetMustBeAlive = false;
                })
                );

            // Time walk: Apply Snow to all enemies
            assets.Add(new StatusEffectDataBuilder(wtg)
            .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Apply Snow To All Enemies")
            .WithText("Apply <{a}><keyword=snow> to all enemies")
            .WithStackable(true)
            .WithCanBeBoosted(true)
            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
            {
                data.effectToApply = wtg.TryGet<StatusEffectSnow>("Snow");
                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Enemies;
            })
            );

            // Molten Duplication: Instant summon copy with destroy self
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantSummon>("Instant Summon Copy With Destroy Self")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantSummon>(data =>
                {
                    data.hiddenKeywords = new KeywordData[]
                    {
                        wtg.TryGet < KeywordData >("Copy"),
                    };
                    data.eventPriority = 99999;
                    data.targetSummon = wtg.TryGet<StatusEffectSummon>("Summon Plep");
                    data.summonCopy = true;
                    data.withEffects = new StatusEffectData[]
                    {
                        wtg.TryGet < StatusEffectDestroySelfAfterTurn >("Destroy Self After Turn")
                    };
                })
                );

            // Skullclamp: Add "when destroyed draw" to target
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Add When Destroyed Draw To Target")
                .WithText("Add \"When destroyed, <keyword=draw> <{a}>\" to an ally")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectApplyXWhenDestroyedUpdateDesc>("When Destroyed Draw");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Target;
                })
                );

            // Toxic Deluge: Apply Demonize fo field
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Gain Demonize To Field")
                .WithText("Apply <{a}><keyword=demonize> to all allies and enemies")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectDemonize>("Demonize");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies | StatusEffectApplyX.ApplyToFlags.Enemies;
                })
                );

            // Hits all enemies with 1 Counter
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectChangeTargetMode>("Hit All Enemies With 1 Counter")
                .WithText("Hits all enemies with 1 <keyword=counter>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectChangeTargetMode>(data =>
                {
                    data.targetMode = new Scriptable<TargetModeAll>(tma =>
                    {
                        tma.constraints = new TargetConstraint[]
                        {
                            new Scriptable<TargetConstraintCounterMoreThan>(tccmt =>
                            {
                                tccmt.not = true;
                                tccmt.value = 1;
                            }),

                            new Scriptable<TargetConstraintCounterMoreThan>(tccmt =>
                            {
                                tccmt.value = 0;
                            })
                        };
                    });
                })
                );

            // Insult: Summon Injury
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectSummon>("Summon Injury")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectSummon>(data =>
                {
                    data.eventPriority = 99999;
                    data.summonCard = wtg.TryGet<CardData>("injury");
                    data.effectPrefabRef = new UnityEngine.AddressableAssets.AssetReference("FlipCreateCard");
                })
                );

            // Insult: Instant Summon Injury
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantSummon>("Instant Summon Injury In Hand")
                .WithText("Add {0} to your hand")
                .WithTextInsert($"<card={wtg.GUID}.injury>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantSummon>(data =>
                {
                    data.eventPriority = 99999;
                    data.canSummonMultiple = true;
                    data.targetSummon = wtg.TryGet<StatusEffectSummon>("Summon Injury");
                    data.summonPosition = StatusEffectInstantSummon.Position.Hand;
                })
                );

            // Annie Joins Up: Add Ongoing Frenzy to front ally
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnHit>("On Hit Gain Ongoing Frenzy To Front Ally")
                .WithText("Add \"{0} - <x{a}><keyword=frenzy>\" to frontmost ally")
                .WithTextInsert($"<keyword={wtg.GUID}.ongoing>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnHit>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectWhileActiveXOnce>("Ongoing Frenzy");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.FrontAlly;
                    data.noTargetType = NoTargetType.None;
                    data.eventPriority = -999;
                    data.postHit = true;
                })
                );

            // Ongoing Frenzy
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectWhileActiveXOnce>("Ongoing Frenzy")
                .WithText("{0} - <x{a}><keyword=frenzy>")
                .WithTextInsert($"<keyword={wtg.GUID}.ongoing>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveXOnce>(data =>
                {
                    data.hiddenKeywords = new KeywordData[]
                    {
                                    wtg.TryGet < KeywordData >("Active"),
                    };
                    data.effectToApply = wtg.TryGet<StatusEffectMultiHit>("MultiHit");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Maddening Cacophony: Increase own effects
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Increase Own Effects")
                .WithText("Increase by {a}")
                .WithStackable(true)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantIncreaseEffects>("Increase Effects");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Reckless Charge: Instant gain Spark
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXInstant>("Instant Gain Spark")
                .WithText("Add <keyword=spark> to the target")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectSafeTemporaryTrait>("Temporary Spark");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Phased Out
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectPhasedOut>("Phased Out")
                .WithText("{0}")
                .WithTextInsert($"<keyword={wtg.GUID}.phasedout>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                );

            // Instant phase out. Apply this to phase out target
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXInstant>("Instant Phase Out")
                .WithText("<Phase out> target")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectPhasedOut>("Phased Out");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.hiddenKeywords = new KeywordData[]
                    {
                        wtg.TryGet <KeywordData>("phasedout"),
                    };
                    data.doPing = true;
                })
                );

            // Summon Beast Token
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectSummon>("Summon Beast Token")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectSummon>(data =>
                {
                    data.eventPriority = 99999;
                    data.summonCard = wtg.TryGet<CardData>("beastToken");
                    data.gainTrait = wtg.TryGet<StatusEffectTemporaryTrait>("Temporary Summoned");
                    data.setCardType = wtg.TryGet<CardType>("Summoned");
                    data.effectPrefabRef = new UnityEngine.AddressableAssets.AssetReference("SummonCreateCard");
                })
                );

            // Beast Within: Summon Beast Token on other side
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantSummon>("Instant Summon Beast On Other Side")
                .WithText("Summon {0} on the other side")
                .WithTextInsert($"<card={wtg.GUID}.beastToken>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantSummon>(data =>
                {
                    data.eventPriority = 99999;
                    data.targetSummon = wtg.TryGet<StatusEffectSummon>("Summon Beast Token");
                    data.summonPosition = StatusEffectInstantSummon.Position.EnemyRow;
                })
                );

            // Natural Unity: Add "Gain 1 attack" to target
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXInstant>("Instant Gain Add Attack")
                .WithText("Add \"Gain <+{a}><keyword=attack>\" to the target")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectApplyXOnCardPlayed>("On Card Played Apply Attack To Self Update Desc");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.doPing = true;
                })
                );

            // Natural Unity: Gain attack on trigger but it updates desc
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayedUpdateDesc>("On Card Played Apply Attack To Self Update Desc")
                .WithText("Gain <+{a}><keyword=attack>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayedUpdateDesc>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantIncreaseAttack>("Increase Attack");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.waitForAnimationEnd = true;
                    data.waitForApplyToAnimationEnd = true;
                    data.queue = true;
                })
                );

            // Juggernaut: take 1 damage
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Damage To Self")
                .WithText("Take <{a}> damage")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .WithDoesDamage(true)     // Its entity can activate "On kill" effects with this effect, eg for Bling Charm
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.dealDamage = true;
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.countsAsHit = true;
                    data.doPing = false;
                })
                );

            // Adding Treasure: Summon Treasure
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectSummon>("Summon Treasure")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectSummon>(data =>
                {
                    data.eventPriority = 99999;
                    data.summonCard = wtg.TryGet<CardData>("treasure");
                    data.effectPrefabRef = new UnityEngine.AddressableAssets.AssetReference("FlipCreateCard");
                })
                );

            // Adding Treasure: Instant Summon Treasure
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantSummon>("Instant Summon Treasure In Hand")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantSummon>(data =>
                {
                    data.eventPriority = 99999;
                    data.canSummonMultiple = true;
                    data.targetSummon = wtg.TryGet<StatusEffectSummon>("Summon Treasure");
                    data.summonPosition = StatusEffectInstantSummon.Position.Hand;
                })
                );

            // Add Treasure on trigger
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Add Treasure To Hand")
                .WithText("Add <{a}> {0} to your hand")
                .WithTextInsert($"<card={wtg.GUID}.treasure>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantSummon>("Instant Summon Treasure In Hand");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.queue = true;
                    data.separateActions = true;
                    data.doPing = false;
                })
                );

            // Add Treasure on destroy
            assets.Add(new StatusEffectDataBuilder(wtg)
                    .Create<StatusEffectApplyXWhenDestroyed>("When Destroyed Add Treasure To Hand")
                    .WithText("When destroyed, add <{a}> {0} to your hand")
                    .WithTextInsert($"<card={wtg.GUID}.treasure>")  // Any {0} in the line above is replaced with the text insert. The html tag must be of the form <card=[GUID name].[card name]>. No spaces around the equal sign. This creates the card pop-up.
                    .WithStackable(true)
                    .WithCanBeBoosted(true)
                    .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDestroyed>(data =>
                    {
                        data.eventPriority = 99999;
                        data.effectToApply = wtg.TryGet<StatusEffectData>("Instant Summon Treasure In Hand");
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.doPing = false;
                        data.targetMustBeAlive = false;
                    })
                    );

            // Draw on destroy
            assets.Add(new StatusEffectDataBuilder(wtg)
                    .Create<StatusEffectApplyXWhenDestroyedUpdateDesc>("When Destroyed Draw")
                    .WithText("When destroyed, <Draw> <{a}>")
                    .WithStackable(true)
                    .WithCanBeBoosted(true)
                    .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDestroyedUpdateDesc>(data =>
                    {
                        data.eventPriority = 99999;
                        data.effectToApply = wtg.TryGet<StatusEffectInstantDraw>("Instant Draw");
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.doPing = false;
                        data.targetMustBeAlive = false;
                    })
                    );

            // Summon Dragon Token
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectSummon>("Summon Dragon Token")
                .WithText("Summon {0}")
                .WithTextInsert($"<card={wtg.GUID}.dragonToken>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectSummon>(data =>
                {
                    data.eventPriority = 99999;
                    data.summonCard = wtg.TryGet<CardData>("dragonToken");
                    data.gainTrait = wtg.TryGet<StatusEffectTemporaryTrait>("Temporary Summoned");
                    data.setCardType = wtg.TryGet<CardType>("Summoned");
                    data.effectPrefabRef = new UnityEngine.AddressableAssets.AssetReference("SummonCreateCard");
                })
                );

            // Instant summon Dragon Token
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantSummon>("Instant Summon Dragon Token")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantSummon>(data =>
                {
                    data.eventPriority = 99999;
                    data.targetSummon = wtg.TryGet<StatusEffectSummon>("Summon Dragon Token");
                    data.summonPosition = StatusEffectInstantSummon.Position.InFrontOfOrOtherRow;
                })
                );

            // Summon Dragon Token with Spark
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectSummon>("Summon Spark Dragon Token")
                .WithText("Summon {0}")
                .WithTextInsert($"<card={wtg.GUID}.dragonTokenSpark>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectSummon>(data =>
                {
                    data.eventPriority = 99999;
                    data.summonCard = wtg.TryGet<CardData>("dragonTokenSpark");
                    data.gainTrait = wtg.TryGet<StatusEffectTemporaryTrait>("Temporary Summoned");
                    data.setCardType = wtg.TryGet<CardType>("Summoned");
                    data.effectPrefabRef = new UnityEngine.AddressableAssets.AssetReference("SummonCreateCard");
                })
                );

            // Flying: Change the target mode
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectChangeTargetMode>("Prioritize Bosses")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .WithOffensive(false)  // As an attack effect, this is treated as a buff
                .WithMakesOffensive(false)  // As a starting effect, its entity should target allies
                .WithDoesDamage(false)  // Its entity cannot kill with this effect, eg for Bling Charm
                .SubscribeToAfterAllBuildEvent<StatusEffectChangeTargetMode>(data =>
                {
                    data.targetMode = new Scriptable<TargetModePrioritizeBosses>();
                })
                );

            // Ongoing reduce counter
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectOngoingCounter>("Ongoing Decrease Counter")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .WithOffensive(true)  // For blank mask shenanigans... so it doesn't get copied
                .SubscribeToAfterAllBuildEvent<StatusEffectOngoingCounter>(data =>
                {
                    data.reverse = true;
                    data.targetConstraints = new TargetConstraint[]
                    {
                    new Scriptable<TargetConstraintMaxCounterMoreThan>(tcmcmt =>
                    {
                        tcmcmt.moreThan = 1;
                    }),
                    };
                })
                );

            // Dragonlord's Servant: While active, reduce max counter of allies with flying by 1
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectWhileActiveX>("While Active Decrease Counter Of Flying Allies")
                .WithText("While active, reduce max <keyword=counter> of {0} allies by <{a}>")
                .WithTextInsert($"<keyword={wtg.GUID}.flying>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveX>(data =>
                {
                    data.hiddenKeywords = new KeywordData[]
                    {
                        wtg.TryGet<KeywordData>("Active"),
                    };
                    data.eventPriority = 10;
                    data.effectToApply = wtg.TryGet<StatusEffectOngoingCounter>("Ongoing Decrease Counter");
                    data.applyConstraints = new TargetConstraint[]
                    {
                    new Scriptable<TargetConstraintHasTrait>(tcht =>
                    {
                        tcht.trait = wtg.TryGet<TraitData>("Flying");
                        tcht.ignoreSilenced = false;
                    }),
                    };
                    data.applyEqualAmount = true;  // NOPE too annoying cause overflow. Also if you end up fixing this, add <a> to the thingy instead of 1. oh hey test overflow again I think I solved it
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies;
                })
                );

            // Terror of the Peaks: Trigger when Flying ally deployed
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXWhenDeployedNoHandIfTrait>("When Flying Ally Deployed Trigger Self")
                .WithText("Trigger when a {0} ally is deployed")
                .WithTextInsert($"<keyword={wtg.GUID}.flying>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .WithIsReaction(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDeployedNoHandIfTrait>(data =>
                {
                    data.descColorHex = "F99C61";
                    data.effectToApply = wtg.TryGet<StatusEffectInstantTrigger>("Trigger");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.whenSelfDeployed = false;
                    data.whenAllyDeployed = true;
                    data.wantedTrait = wtg.TryGet<TraitData>("Flying");
                })
                );

            // Captain Lannery Storm: Gain attack when Treasure is played
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCertainCardPlayed>("Gain Attack On Treasure")
                .WithText("Gain <+{a}><keyword=attack> whenever a {0} is played")
                .WithTextInsert($"<card={wtg.GUID}.treasure>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCertainCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantIncreaseAttack>("Increase Attack");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.allowedCards = new CardData[1] { wtg.TryGet<CardData>("treasure") };
                })
                );

            // Trigger when Treasure is played
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCertainCardPlayed>("Trigger On First Treasure")
                .WithText("Trigger the first time you play a {0} each turn")
                .WithTextInsert($"<card={wtg.GUID}.treasure>")
                .WithStackable(true)
                .WithCanBeBoosted(false)
                .WithIsReaction(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCertainCardPlayed>(data =>
                {
                    data.descColorHex = "F99C61";
                    data.effectToApply = wtg.TryGet<StatusEffectInstantTrigger>("Trigger");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.allowedCards = new CardData[1] { wtg.TryGet<CardData>("treasure") };
                    data.allowedTimes = 1;
                })
                );

            // Glorybringer: Damage frontmost enemy equal to attack
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXPostAttackEqualAmount>("On Hit Equal Damage to Front Enemy")
                .WithText("Deal damage to frontmost enemy equal to damage done")
                .WithStackable(false)
                .WithDoesDamage(true)  // Its entity can kill with this effect, eg for Bling Charm
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXPostAttackEqualAmount>(data =>
                {
                    data.dealDamage = true;
                    data.effectToApply = wtg.TryGet<StatusEffectSnow>("Snow");  // Crashes when I give it no effect O_O
                    data.applyEqualAmount = true;
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.FrontEnemy;
                    data.waitForAnimationEnd = true;
                    data.eventPriority = -999;
                    data.countsAsHit = true;
                    data.doPing = true;
                    data.queue = true;
                    data.noTargetType = NoTargetType.NoTargetToAttack;

                })
                );

            // Earthquake Dragon: Ongoing reduce counter (stackable)
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectOngoingCounter>("Ongoing Decrease Counter Stackable")
                .WithStackable(true)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectOngoingCounter>(data =>
                {
                    data.reverse = true;
                    data.targetConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintMaxCounterMoreThan>(tcmcmt =>
                            {
                                tcmcmt.moreThan = 1;
                            }),
                    };
                })
                );

            // Earthquake Dragon: While active, reduce counter by number of allies with flying
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectWhileActiveXUpdatesOnTrait>("While Active Reduce Counter By Allies With Flying")
                .WithText("While active, reduce own <keyword=counter> by the number of {0} allies")
                .WithTextInsert($"<keyword={wtg.GUID}.flying>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveXUpdatesOnTrait>(data =>
                {
                    data.alsoActivate = wtg.TryGet<TraitData>("Flying");
                    ScriptableTargetsOnBoard scriptAmount = ScriptableTargetsOnBoard.CreateInstance<ScriptableTargetsOnBoard>();
                    scriptAmount.allies = true;
                    scriptAmount.hasTrait = wtg.TryGet<TraitData>("Flying");
                    data.scriptableAmount = scriptAmount;
                    data.applyEqualAmount = true;
                    data.effectToApply = wtg.TryGet<StatusEffectOngoingCounter>("Ongoing Decrease Counter Stackable");
                    data.affectsSelf = true;
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Ojutai, Soul of Winter: When self or Flying ally attacks, apply 1 Snow to a random enemy
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCertainCardPlayed>("On Flying Card Played Apply Snow To Random Enemy")
                .WithText("Apply <{a}> <keyword=snow> to a random enemy whenever self or {0} ally attacks")
                .WithTextInsert($"<keyword={wtg.GUID}.flying>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCertainCardPlayed>(data =>
                {
                    data.allowedTraits = new TraitData[1] { wtg.TryGet<TraitData>($"{wtg.GUID}.Flying") };
                    data.countsSelf = true;
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.RandomEnemy;
                    data.effectToApply = wtg.TryGet<StatusEffectSnow>("Snow");
                    data.whileActive = true;
                })
                );

            // Professional Face-Breaker: Recycle Treasure to Draw on attack
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Destroy Right Treasure In Hand And Draw")
                .WithText("Destroy rightmost {0} in hand to <keyword=draw {a}>")
                .WithTextInsert($"<card={wtg.GUID}.treasure>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantDestroyNumCardsInHandAndApplyXForEach>("Instant Destroy Treasure In Hand And Draw For Each");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Professional Face-Breaker: Recycle Treasure to Draw
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantDestroyNumCardsInHandAndApplyXForEach>("Instant Destroy Treasure In Hand And Draw For Each")
                .WithStackable(true)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantDestroyNumCardsInHandAndApplyXForEach>(data =>
                {
                    data.destroyConstraints = new TargetConstraint[]
                    {
                    new Scriptable<TargetConstraintIsSpecificCard>(tcisc =>
                    {
                        tcisc.allowedCards = new CardData[]
                        {
                            wtg.TryGet < CardData >("treasure"),
                        };
                    }),
                    };
                    data.destroyCardEffect = wtg.TryGet<StatusEffectInstantKill>("Kill");
                    data.effectToApply = wtg.TryGet<StatusEffectInstantDraw>("Instant Draw");
                })
                );

            // Shivan Dragon: Apply Spice to applier
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXInstant>("Instant Apply Spice To Applier")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectSpice>("Spice");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Applier;
                    data.doPing = false;
                    data.targetMustBeAlive = false;
                })
                );

            // Shivan Dragon: Count Zoomlin cards in hand and gain Spice (I don't know why I did it this way leave me alone)
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXPreTurn>("Pre Turn Count Zoomlin In Hand & Gain Spice For Each")
                .WithText("Before attacking, gain <{a}> <keyword=spice> for each card with <keyword=zoomlin> in hand")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXPreTurn>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectApplyXInstant>("Instant Apply Spice To Applier");
                    data.applyConstraints = new TargetConstraint[]
                    {
                    new Scriptable<TargetConstraintHasTrait>(tcht =>
                    {
                        tcht.trait = wtg.TryGet < TraitData >("Zoomlin");
                        tcht.ignoreSilenced = false;
                    }),
                    };
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Hand;
                })
                );

            // Manaform Hellkite: Instant Summon Dragon Token on Item played
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXEqualToAttackOnCertainCardPlayed>("Summon Dragon Token On Item Played")
                .WithText("Summon a {0} with <keyword=attack> equal to the <keyword=attack> of items you play")
                .WithTextInsert($"<card={wtg.GUID}.dragonTokenSpark>")
                .WithStackable(true)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXEqualToAttackOnCertainCardPlayed>(data =>
                {
                    data.summonQueue = true;
                    data.allowedCardType = ScriptableObject.CreateInstance<CardType>();
                    data.allowedCardType.item = true;
                    data.allowedCardType.name = "Item";
                    data.attackGreaterThan = 0;
                    data.effectToApply = wtg.TryGet<StatusEffectInstantSummon>("Instant Summon Spark Dragon Token With X Health and Attack");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.applyEqualAmount = true;
                })
                );

            // Manaform Hellkite: Instant summon Dragon Token with equal attack
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantSummon>("Instant Summon Spark Dragon Token With X Health and Attack")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantSummon>(data =>
                {
                    data.eventPriority = 99999;
                    data.targetSummon = wtg.TryGet<StatusEffectSummon>("Summon Spark Dragon Token");
                    data.summonPosition = StatusEffectInstantSummon.Position.InFrontOfOrOtherRow;
                    data.withEffects = new StatusEffectData[]
                    {
                        wtg.TryGet < StatusEffectInstantSetAttack >("Set Attack"),
                    };
                    data.queue = false;
                })
                );

            // Change the target mode to "fireball"
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectChangeTargetMode>("Random Enemy For Zoomlin")
                .SubscribeToAfterAllBuildEvent<StatusEffectChangeTargetMode>(data =>
                {
                    data.targetMode = new Scriptable<TargetModeFireball>();
                })
                );

            // Voracious Hydra: When deployed, gain attack equal to zoomlined cards in hand
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXWhenDeployed>("When Deployed Apply Attack And Health To Self Equal To Zoomlin")
                .WithText("When deployed, gain <keyword=attack> and <keyword=health> equal to cards with <keyword=zoomlin> in hand")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDeployed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantMultiple>("Increase Attack & Health (No Constraints)");
                    ScriptableTargetsInHand scriptAmount = ScriptableTargetsInHand.CreateInstance<ScriptableTargetsInHand>();
                    scriptAmount.hasTrait = wtg.TryGet<TraitData>("Zoomlin");
                    data.scriptableAmount = scriptAmount;
                    data.applyEqualAmount = true;
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Treasure: Funny temporary Zoomlin
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectSafeTemporaryTrait>("Safe Temporary Zoomlin")
                .WithIsKeyword(true)    // This effect adds text to the card. 
                .SubscribeToAfterAllBuildEvent<StatusEffectSafeTemporaryTrait>(data =>
                {
                    data.targetConstraints = new TargetConstraint[]
                    {
                        new Scriptable<TargetConstraintHasTrait>(tcht =>
                        {
                            tcht.not = true;
                            tcht.trait = wtg.TryGet < TraitData >("Noomlin");
                            tcht.ignoreSilenced = false;
                        }),
                        new Scriptable<TargetConstraintHasTrait>(tcht =>
                        {
                            tcht.not = true;
                            tcht.trait = wtg.TryGet < TraitData >("Zoomlin");
                            tcht.ignoreSilenced = false;
                        }),
                    };
                    data.trait = wtg.TryGet<TraitData>("Zoomlin");
                })
                );

            // Treasure: Add Zoomlin to self
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXInstant>("Instant Gain Zoomlin (To Card In Hand)")
                .WithText("Add {0} to a card in your hand")
                .WithTextInsert("<keyword=zoomlin>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectSafeTemporaryTrait>("Safe Temporary Zoomlin");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;

                    data.targetConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.not = true;
                                tcht.trait = wtg.TryGet < TraitData >("Noomlin");
                                tcht.ignoreSilenced = false;
                            }),
                                new Scriptable<TargetConstraintHasStatus>(tchs =>
                            {
                                tchs.not = true;
                                tchs.status = wtg.TryGet < StatusEffectFreeAction >("Free Action");
                            }),
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.not = true;
                                tcht.trait = wtg.TryGet < TraitData >("Zoomlin");
                                tcht.ignoreSilenced = false;
                            }),
                                new Scriptable<TargetConstraintHasStatus>(tchs =>
                            {
                                tchs.not = true;
                                tchs.status = wtg.TryGet < StatusEffectFreeAction >("Free Action (Zoomlin)");
                            }),
                    };
                })
                );

            // Treasure: Temporary Unplayable
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectSafeTemporaryTrait>("Temporary Unplayable")
                .WithIsKeyword(true)
                .WithOffensive(true)  // As an attack effect, this is treated as a negative status
                .SubscribeToAfterAllBuildEvent<StatusEffectSafeTemporaryTrait>(data =>
                {
                    data.removeOnDiscard = false;
                    data.targetConstraints = new TargetConstraint[]
                    {
                        new Scriptable<TargetConstraintIsUnit>(tciu =>
                        {
                            tciu.not = true;
                            tciu.mustBeMiniboss = true;
                        }),
                    };
                    data.trait = wtg.TryGet<TraitData>("Unplayable");
                })
                );

            // Treasure: Add Unplayable
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXInstant>("Instant Gain Unplayable")
                .WithOffensive(true)  // As an attack effect, this is treated as a negative status
                .WithText("It can't be played this turn")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                {
                    data.targetConstraints = new TargetConstraint[]
                    {
                                new Scriptable<TargetConstraintIsUnit>(tciu =>
                                {
                                    tciu.not = true;
                                    tciu.mustBeMiniboss = true;
                                }),
                    };
                    data.effectToApply = wtg.TryGet<StatusEffectSafeTemporaryTrait>("Temporary Unplayable");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Treasure: You can't play it this turn
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectUnplayable>("Unplayable")
                .WithStackable(true)
                .WithCanBeBoosted(false)
                .WithOffensive(true)  // As an attack effect, this is treated as a negative status
                .WithIsStatus(true)
                .WithVisible(true)
                );

            // Delayed Blast Fireball: Gain Spice
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Apply Spice To Self")
                .WithText("Gain <{a}><keyword=spice>")
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectSpice>("Spice");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.waitForAnimationEnd = true;
                })
                );

            // Gain Zoomlin when drawn if 2+ allies have Flying
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXWhenDrawn>("Gain Zoomlin When Drawn If 2 Allies With Flying")
                .WithText("Gain <keyword=zoomlin> when drawn if 2 allies have {0}")
                .WithTextInsert($"<keyword={wtg.GUID}.flying>")
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDrawn>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectSafeTemporaryTrait>("Safe Temporary Zoomlin");
                    data.applyConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintIsFeatureOnBoard>(tcifob =>
                            {
                                tcifob.allies = true;
                                tcifob.hasTrait = wtg.TryGet < TraitData >("Flying");
                                tcifob.requiredAmount = 2;
                            }),
                    };
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Increase Effects when drawn if 2+ allies have Flying
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXWhenDrawn>("Increase Effects By 2 When Drawn If 2 Allies With Flying")
                .WithText("Increase effects by 2 when drawn if 2 allies have {0}")
                .WithTextInsert($"<keyword={wtg.GUID}.flying>")
                .WithStackable(true)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDrawn>(data =>
                {
                    ScriptableFixedAmount scriptAmount = ScriptableFixedAmount.CreateInstance<ScriptableFixedAmount>();
                    scriptAmount.amount = 2;
                    data.effectToApply = wtg.TryGet<StatusEffectInstantIncreaseEffects>("Increase Effects");
                    data.applyConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintIsFeatureOnBoard>(tcifob =>
                            {
                                tcifob.allies = true;
                                tcifob.hasTrait = wtg.TryGet < TraitData >("Flying");
                                tcifob.requiredAmount = 2;
                            }),
                    };
                    data.scriptableAmount = scriptAmount;
                    data.applyEqualAmount = true;
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Add Treasure equal to counter
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXInstant>("Instant Add Treasure To Hand Equal To Target Counter")
                .WithText("Add {0} to your hand equal to the target's <keyword=counter>")
                .WithTextInsert($"<card={wtg.GUID}.treasure>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                {
                    ScriptableEqualToCounter scriptAmount = ScriptableEqualToCounter.CreateInstance<ScriptableEqualToCounter>();
                    data.effectToApply = wtg.TryGet<StatusEffectInstantSummon>("Instant Summon Treasure In Hand");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Target;
                    data.applyEqualAmount = true;
                    data.scriptableAmount = scriptAmount;
                    data.doPing = false;
                })
                );

            // Fireball: damage equal to zoomlined cards
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectBonusDamageEqualToCardsWithTrait>("Bonus Damage Equal To Zoomlin In Hand")
                .WithText("Deal additional damage equal to the number of cards with <keyword=zoomlin> in hand")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectBonusDamageEqualToCardsWithTrait>(data =>
                {
                    data.doCheckName = false;
                    data.checkTrait = wtg.TryGet<TraitData>("Zoomlin");
                })
                );

            // Shared Animosity: Apply Sice equal to Flying allies to all Flying allies
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("Apply Spice To Flying Allies Equal To Twice Flying Allies")
                .WithText("Apply <keyword=spice> to {0} allies equal to twice the number of {0} allies")
                .WithTextInsert($"<keyword={wtg.GUID}.flying>")
                .WithStackable(true)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectSpice>("Spice");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies;
                    data.applyConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.trait = wtg.TryGet < TraitData >("Flying");
                                tcht.ignoreSilenced = false;
                            }),
                    };
                    ScriptableTargetsOnBoard scriptAmount = ScriptableTargetsOnBoard.CreateInstance<ScriptableTargetsOnBoard>();
                    scriptAmount.allies = true;
                    scriptAmount.hasTrait = wtg.TryGet<TraitData>("Flying");
                    scriptAmount.mult = 2;
                    data.scriptableAmount = scriptAmount;
                    data.applyEqualAmount = true;
                })
                );

            // Bottle-Cap Blast: Instant gain treasures equal to health
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXInstant>("Instant Add Treasure To Hand Equal To Target Health Below Zero")
                .WithText("Add {0} to your hand equal to the excess damage done")
                .WithTextInsert($"<card={wtg.GUID}.treasure>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantSummon>("Instant Summon Treasure In Hand");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Target;
                    ScriptableCurrentHealth scriptAmount = ScriptableCurrentHealth.CreateInstance<ScriptableCurrentHealth>();
                    scriptAmount.multiplier = -1;
                    data.applyConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintHealthLessThan>(tchlt =>
                            {
                                tchlt.value = 0;
                                tchlt.allowNegative = true;
                            }),
                    };
                    data.scriptableAmount = scriptAmount;
                    data.applyEqualAmount = true;
                    data.targetMustBeAlive = false;
                })
                );

            // Explosive Vegetation: Add "Instant add zoomlin to X random cards in hand" to self
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Gain Zoomlin To X Random Cards In Hand")
                .WithText("Add <keyword=zoomlin> to <{a}> random cards in hand")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectApplyXMultipleInstant>("Instant Gain Zoomlin To Random Card In Hand");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Explosive Vegetation: Add Zoomlin to a random card in hand
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXMultipleInstant>("Instant Gain Zoomlin To Random Card In Hand")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXMultipleInstant>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectSafeTemporaryTrait>("Safe Temporary Zoomlin");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.RandomCardInHand;
                    data.applyConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.not = true;
                                tcht.trait = wtg.TryGet < TraitData >("Noomlin");
                                tcht.ignoreSilenced = false;
                            }),
                            new Scriptable<TargetConstraintHasStatus>(tchs =>
                            {
                                tchs.not = true;
                                tchs.status = wtg.TryGet < StatusEffectFreeAction >("Free Action");
                            }),
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.not = true;
                                tcht.trait = wtg.TryGet < TraitData >("Zoomlin");
                                tcht.ignoreSilenced = false;
                            }),
                            new Scriptable<TargetConstraintHasStatus>(tchs =>
                            {
                                tchs.not = true;
                                tchs.status = wtg.TryGet < StatusEffectFreeAction >("Free Action (Zoomlin)");
                            }),
                    };
                })
                );

            // Fires of Yavimaya: instant gain Ongoing - While Active allies gain Spark
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXInstant>("Instant Gain Ongoing While Active Allies Gain Spark")
                .WithText("Add \"{0} - While Active, allies gain <keyword=spark>\" to the target")
                .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.ongoing>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectWhileActiveXOnce>("Ongoing While Active Allies Gain Spark");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Fires of Yavimaya: Ongoing - While active allies gain Spark
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectWhileActiveXOnce>("Ongoing While Active Allies Gain Spark")
                .WithText("{0} - While Active, allies gain <keyword=spark>")
                .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.ongoing>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveXOnce>(data =>
                {
                    data.hiddenKeywords = new KeywordData[]
                    {
                                wtg.TryGet < KeywordData >("Active"),
                    };
                    data.eventPriority = 10;
                    data.effectToApply = wtg.TryGet<StatusEffectSafeTemporaryTrait>("Temporary Spark");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies | StatusEffectApplyX.ApplyToFlags.Hand;
                })
                );

            // Fires of Yavimaya: Temporary Spark
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectSafeTemporaryTrait>("Temporary Spark")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .WithIsKeyword(true)    // This effect adds text to the card. 
                .SubscribeToAfterAllBuildEvent<StatusEffectSafeTemporaryTrait>(data =>
                {
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
                    data.trait = wtg.TryGet<TraitData>("Spark");
                })
                );

            // Resourceful Defense: Instant gain Ongoing retain spice
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXInstant>("Instant Gain Ongoing Halt Spice")
                .WithText("Add \"{0} - Retains <keyword=spice>\" to the target")
                .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.ongoing>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectHaltXOnce>("Ongoing Halt Spice");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Resourceful Defense: Ongoing retain spice
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectHaltXOnce>("Ongoing Halt Spice")
                .WithText("{0} - Retains <keyword=spice>")
                .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.ongoing>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectHaltXOnce>(data =>
                {
                    data.effectToHalt = wtg.TryGet<StatusEffectSpice>("Spice");
                    data.eventPriority = -99999;
                    data.ignoreSilence = false;
                })
                );

            // Sol Ring: When destroyed, counter leader down by X
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXWhenDestroyed>("When Destroyed Count Down Leader By X")
                .WithCanBeBoosted(true)
                .WithStackable(true)
                .WithText("When destroyed, count down your leader by <{a}>")
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDestroyed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantReduceCounter>("Reduce Counter");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies;
                    data.applyConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintIsLeader>(),
                    };
                    data.targetMustBeAlive = false;
                })
                );

            // Bootlegger's Stash: While active, add "Add a treasure to your hand" to allies in row
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectWhileActiveX>("While Active Treasure To AlliesInRow")
                .WithText("While active, add \"Add <{a}> {0} to your hand\" to allies in row")
                .WithTextInsert($"<card=whycats.wildfrost.wildfrostthegathering.treasure>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveX>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectApplyXOnCardPlayedUpdateDesc>("On Card Played Add Treasure To Hand Update Desc");
                    data.affectsSelf = false;
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.AlliesInRow;
                    data.hiddenKeywords = new KeywordData[]
                    {
                            wtg.TryGet < KeywordData >("Active"),
                    };
                    data.applyConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintOr>(tco =>
                            {
                                tco.constraints = new TargetConstraint[] {
                                    new Scriptable<TargetConstraintHasReaction>(),
                                    new Scriptable<TargetConstraintMaxCounterMoreThan>(tcmcmt =>
                                    {
                                        tcmcmt.moreThan = 0;
                                    })};
                            })
                    };
                })
                );

            // Bootlegger's Stash: Add treasure but updates description when it changes
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayedUpdateDesc>("On Card Played Add Treasure To Hand Update Desc")
                .WithText("Add <{a}> {0} to your hand")
                .WithTextInsert($"<card={wtg.GUID}.treasure>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayedUpdateDesc>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantSummon>("Instant Summon Treasure In Hand");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.queue = true;
                    data.separateActions = true;
                    data.doPing = true;
                })
                );

            // Rites of Flourishing: Count frontmost enemy down by 1
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Reduce Counter To Front Enemy")
                .WithText("Count down frontmost enemy's <sprite name=counter> by <{a}>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantReduceCounter>("Reduce Counter");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.FrontEnemy;
                })
                );

            // Rites of Flourishing: Count allies in row down by 1
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Reduce Counter To Allies In Row")
                .WithText("Count down <sprite name=counter> of allies in row by <{a}>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantReduceCounter>("Reduce Counter");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.AlliesInRow;
                })
                );

            // Mox Jasper: When destroyed, add Zoomlin to Flying allies in hand
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXWhenDestroyed>("When Destroyed Gain Noomlin To Flying Allies In Hand And In Play")
                .WithCanBeBoosted(true)
                .WithStackable(true)
                .WithText("When destroyed, add <keyword=noomlin> to {0} allies in hand and in play")
                .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDestroyed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectTemporaryTrait>("Temporary Noomlin");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Hand;
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies;
                    data.applyConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintIsUnit>(),
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.trait = wtg.TryGet < TraitData >("Flying");
                                tcht.ignoreSilenced = false;
                            }),
                    };
                    data.targetMustBeAlive = false;
                })
                );

            // Tempting Contract: count down random enemy by 1
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Reduce Counter To Random Enemy")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .WithText("Count down a random enemy's <keyword=counter> by <{a}>")
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantReduceCounter>("Reduce Counter");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.RandomEnemy;
                })
                );

            // Gravitational Shift: While active, add attack to flying allies
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectWhileActiveX>("While Active Increase Attack To Flying Allies")
                .WithText("While active, add <+{a}><keyword=attack> to all {0} allies")
                .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveX>(data =>
                {
                    data.hiddenKeywords = new KeywordData[]
                    {
                            wtg.TryGet < KeywordData >("Active"),
                    };
                    data.eventPriority = 10;
                    data.effectToApply = wtg.TryGet<StatusEffectOngoingAttack>("Ongoing Increase Attack");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies;
                    data.applyConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.trait = wtg.TryGet < TraitData >("Flying");
                                tcht.ignoreSilenced = false;
                            }),
                    };
                })
                );

            // Gravitational Shift: While active, reduce attack to non Flying
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectWhileActiveX>("While Active Reduce Attack To Non Flying")
                .WithText("And everyone else gets <-{a}><keyword=attack>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveX>(data =>
                {
                    data.hiddenKeywords = new KeywordData[]
                    {
                            wtg.TryGet < KeywordData >("Active"),
                    };
                    data.eventPriority = 10;
                    data.effectToApply = wtg.TryGet<StatusEffectOngoingAttack>("Ongoing Reduce Attack");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies | StatusEffectApplyX.ApplyToFlags.Enemies;
                    data.applyConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.not = true;
                                tcht.trait = wtg.TryGet < TraitData >("Flying");
                                tcht.ignoreSilenced = false;
                            }),
                    };
                })
                );

            // Revel in Riches: Gain 1 treasure when an enemy is killed
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXWhenUnitIsKilled>("Add Treasure To Hand When Enemy Killed")
                .WithText("Add <{a}> {0} to your hand when an enemy is killed")
                .WithTextInsert($"<card={wtg.GUID}.treasure>")
                .WithStackable(true)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenUnitIsKilled>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantSummon>("Instant Summon Treasure In Hand");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.queue = true;
                    data.enemy = true;
                    data.ally = false;
                })
                );

            // Revel in Riches: Gain Spice when Treasure is played
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCertainCardPlayed>("Gain Spice On Treasure")
                .WithText("Gain <{a}><keyword=spice> whenever a {0} is played")
                .WithTextInsert($"<card={wtg.GUID}.treasure>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCertainCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectSpice>("Spice");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.allowedCards = new CardData[1] { wtg.TryGet<CardData>("treasure") };
                })
                );

            // Windcrag Siege: Add Ongoing Gain 1 frenzy to ally behind
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Ongoing Frenzy To AllyBehind")
                .WithText("Add \"{0} - <x{a}><keyword=frenzy>\" to ally behind")
                .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.ongoing>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.hiddenKeywords = new KeywordData[]
                    {
                                wtg.TryGet < KeywordData >("Active"),
                    };
                    data.eventPriority = 10;
                    data.effectToApply = wtg.TryGet<StatusEffectWhileActiveXOnce>("Ongoing Frenzy");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.AllyBehind;
                })
                );

            // The Ur-Dragon: While active, Flying allies gain zoomlin when drawn
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectWhileActiveX>("While Active Zoomlin When Drawn To Flying Allies In Hand")
                .WithText("While active, {0} allies gain <keyword=zoomlin> when drawn")
                .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveX>(data =>
                {
                    data.eventPriority = 10;
                    data.effectToApply = wtg.TryGet<StatusEffectApplyXWhenDrawn>("When Drawn Gain Zoomlin");
                    data.applyConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.trait = wtg.TryGet < TraitData >("Flying");
                                tcht.ignoreSilenced = false;
                            }),
                            new Scriptable<TargetConstraintIsUnit>(tciu =>
                            {
                                tciu.mustBeMiniboss = false;
                            }),
                    };
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Hand;
                })
                );

            // Miirym: Instant Gain Fragile
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXInstant>("Instant Gain Fragile")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectTemporaryTrait>("Temporary Fragile");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.targetConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintIsUnit>(tciu =>
                            {
                                tciu.mustBeMiniboss = false;
                            }),
                            new Scriptable<TargetConstraintHasHealth>(),
                    };
                })
                );

            // Miirym: Temporary Fragile
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectTemporaryTrait>("Temporary Fragile")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .WithIsKeyword(true)    // This effect adds text to the card. 
                .SubscribeToAfterAllBuildEvent<StatusEffectTemporaryTrait>(data =>
                {
                    data.targetConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintIsUnit>(tciu =>
                            {
                                tciu.mustBeMiniboss = false;
                            }),
                            new Scriptable<TargetConstraintHasHealth>(),
                    };
                    data.trait = wtg.TryGet<TraitData>("Fragile");
                })
                );

            // Miirym: Summon copy of ally ahead with 1 health and fragile
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantSummon>("Instant Summon Copy With X Health And Fragile")
                .WithStackable(true)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantSummon>(data =>
                {
                    data.hiddenKeywords = new KeywordData[]
                    {
                            wtg.TryGet < KeywordData >("Copy"),
                    };
                    data.eventPriority = 99999;
                    data.targetSummon = wtg.TryGet<StatusEffectSummon>("Summon Plep");
                    data.summonCopy = true;
                    data.withEffects = new StatusEffectData[]
                    {
                            wtg.TryGet < StatusEffectInstantSetHealth >("Set Health"),
                            wtg.TryGet < StatusEffectApplyXInstant >("Instant Gain Fragile")
                    };

                    data.targetConstraints = new TargetConstraint[]
                    {
                            new Scriptable<TargetConstraintHasHealth>(),
                            new Scriptable<TargetConstraintIsUnit>(tciu =>
                            {
                                tciu.mustBeMiniboss = false;
                            }),
                    };
                })
                );

            // Miirym: On Card Played, summon copy of ally ahead
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Summon Copy Of Ally Ahead With X Health And Fragile")
                .WithText("Summon a copy of ally ahead with <{a}><keyword=health> and <keyword=fragile>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantSummon>("Instant Summon Copy With X Health And Fragile");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.AllyInFrontOf;

                })
                );

            // Lathliss: Summon Dragon Token when Flying ally deployed
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXWhenDeployedNoHandIfTrait>("When Flying Ally Deployed Summon Big Dragon Token With X Health")
                .WithText("Summon a <card=whycats.wildfrost.wildfrostthegathering.bigDragonToken> with <{a}><keyword=health> whenever a {0} ally is deployed")
                .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDeployedNoHandIfTrait>(data =>
                {
                    data.summonQueue = true;
                    data.effectToApply = wtg.TryGet<StatusEffectInstantSummon>("Instant Summon Big Dragon Token With X Health");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.whenSelfDeployed = false;
                    data.whenAllyDeployed = true;
                    data.wantedTrait = wtg.TryGet<TraitData>("Flying");
                    data.excludedCards = new List<CardData> { wtg.TryGet<CardData>("bigDragonToken") };
                })
                );

            // Lathliss: Instant summon Dragon Token With X Health
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantSummon>("Instant Summon Big Dragon Token With X Health")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantSummon>(data =>
                {
                    data.eventPriority = 99999;
                    data.targetSummon = wtg.TryGet<StatusEffectSummon>("Summon Big Dragon Token");
                    data.summonPosition = StatusEffectInstantSummon.Position.InFrontOfOrOtherRow;
                    data.withEffects = new StatusEffectData[]
                    {
                            wtg.TryGet < StatusEffectInstantSetHealth >("Set Health"),
                    };
                })
                );

            // Lathliss: Dragon Token with 4 health
            assets.Add(
                new CardDataBuilder(wtg).CreateUnit("bigDragonToken", "Dragon Token", idleAnim: "FloatSquishAnimationProfile")
                .SetSprites("dragon-token-kyanner.png", "companion-bg.png")
                .SetStats(4, 4, 3)
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

            // Summon Big Dragon Token
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectSummon>("Summon Big Dragon Token")
                .WithText("Summon {0}")
                .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.bigDragonToken>")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectSummon>(data =>
                {
                    data.eventPriority = 99999;
                    data.summonCard = wtg.TryGet<CardData>("bigDragonToken");
                    data.gainTrait = wtg.TryGet<StatusEffectTemporaryTrait>("Temporary Summoned");
                    data.setCardType = wtg.TryGet<CardType>("Summoned");
                    data.effectPrefabRef = new UnityEngine.AddressableAssets.AssetReference("SummonCreateCard");
                })
                );

            // Old Gnawbone: When self or Flying ally attacks, gain treasure
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCertainCardPlayed>("On Flying Card Played Add Treasure To Hand")
                .WithText("Add <{a}> <card=whycats.wildfrost.wildfrostthegathering.treasure> to your hand whenever self or {0} ally attacks")
                .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCertainCardPlayed>(data =>
                {
                    data.allowedTraits = new TraitData[1] { wtg.TryGet<TraitData>("whycats.wildfrost.wildfrostthegathering.Flying") };
                    data.countsSelf = true;
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.effectToApply = wtg.TryGet<StatusEffectInstantSummon>("Instant Summon Treasure In Hand");
                    data.whileActive = true;
                })
                );

            // Ganax: Add Treasure to hand when flying ally deployed
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXWhenDeployedNoHandIfTrait>("When Flying Ally Deployed Add Treasure To Hand")
                .WithText("Add <{a}> <card=whycats.wildfrost.wildfrostthegathering.treasure> to your hand whenever a {0} ally is deployed")
                .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDeployedNoHandIfTrait>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantSummon>("Instant Summon Treasure In Hand");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.whenSelfDeployed = false;
                    data.whenAllyDeployed = true;
                    data.wantedTrait = wtg.TryGet<TraitData>("Flying");
                })
                );

            // Drakuseth: On card played, gain "Deal X damage to frontmost enemy twice"
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXPostAttack>("On Card Played Deal X Damage To Frontmost Enemy Twice")
                .WithText("Deal <{a}> damage to frontmost enemy twice")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXPostAttack>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectApplyXMultipleInstant>("Intstant Damage Frontmost Enemy Twice");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Drakuseth: Damage Front enemy twice
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXMultipleInstant>("Intstant Damage Frontmost Enemy Twice")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXMultipleInstant>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectSnow>("Snow");
                    data.dealDamage = true;
                    data.numTimes = 2;
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.FrontEnemy;
                    data.waitForAnimationEnd = true;
                    data.eventPriority = -999;
                    data.countsAsHit = true;
                    data.doPing = true;
                    data.noTargetType = NoTargetType.NoTargetToAttack;
                })
                );

            // Trigger (With text)
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantTrigger>("Trigger With Text")
                .WithText("Trigger the target")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantTrigger>(data =>
                {
                    data.targetConstraints = new TargetConstraint[]
                    {
                        new Scriptable<TargetConstraintOnBoard>(),
                    };
                })
                );

            // Dragon's Fire: Trigger a flying unit
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantTrigger>("Trigger Flying")
                .WithText($"Trigger a <keyword={wtg.GUID}.flying> ally")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantTrigger>(data =>
                {
                    data.targetConstraints = new TargetConstraint[]
                    {
                        new Scriptable<TargetConstraintOnBoard>(),
                        new Scriptable<TargetConstraintHasTrait>(tcht =>
                        {
                            tcht.trait = wtg.TryGet<TraitData>("Flying");
                            tcht.ignoreSilenced = false;
                        }),
                    };
                })
                );

            // Damnable Pact: Set health to 1 and draw that many cards
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantMultiple>("Set Health To 1 And Draw")
                .WithText("Set your leader's <keyword=health> to 1. <keyword=draw> equal to the <keyword=health> lost this way")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantMultiple>(data =>
                {
                    data.applyXEffects = new StatusEffectApplyXInstant[]
                    {
                        wtg.TryGet<StatusEffectApplyXInstant>("Instant Draw Equal To Health Minus 1"),
                        wtg.TryGet<StatusEffectApplyXInstant>("Instant Set Own Health")
                    };
                })
                );

            // Damnable Pact: Instant Set Own Health (just for ordering)
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXInstant>("Instant Set Own Health")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                {
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    data.effectToApply = wtg.TryGet<StatusEffectInstant>("Set Max Health");
                })
                );

            // Damnable Pact: Draw Equal To Health Minus 1
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXInstant>("Instant Draw Equal To Health Minus 1")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantDraw>("Instant Draw");
                    ScriptableCurrentHealthPlusConstant scriptAmount = ScriptableObject.CreateInstance<ScriptableCurrentHealthPlusConstant>();
                    scriptAmount.constant = -1;
                    data.scriptableAmount = scriptAmount;
                    data.applyEqualAmount = true;
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Power Play: Increase all enemy counter by 1
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("Increase Counter To All Enemies")
                .WithText("Increase the maximum <keyword=counter> of all enemies by <{a}>")
                .WithStackable(true)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantIncreaseMaxCounter>("Increase Max Counter");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Enemies;
                })
                );

            // Demonic Tutor: Take any card from deck
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantTutor>("Instant Tutor Card From Deck To Hand")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantTutor>(data =>
                {
                    data.title = LocalizationHelper.GetCollection("UI Text", SystemLanguage.English).GetString("Instant Tutor Card From Deck To Hand");
                })
                );

            // Demonic Tutor: Tutor card on play
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Tutor Card From Deck To Hand")
                .WithText("Add any card from your Draw Pocket to your hand")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantTutor>("Instant Tutor Card From Deck To Hand");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Advantageous Proclamation: Destroy any card in deck
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantTutor>("Instant Destroy Card In Deck")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantTutor>(data =>
                {
                    data.title = LocalizationHelper.GetCollection("UI Text", SystemLanguage.English).GetString("Instant Destroy Card In Deck");
                    data.addEffectStacks = new CardData.StatusEffectStacks[]
                    {
                        wtg.SStack("Kill", 1)
                    };
                })
                );

            // Advantageous Proclamation: Apply destroy 2 cards in deck
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Destroy 1 Card In Deck")
                .WithText("Destroy 1 card in your deck")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantTutor>("Instant Destroy Card In Deck");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            // Doomsday: Destroy all non crowned cards in deck
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectInstantApplyToAllInDeck>("Instant Destroy Non Crown Deck And Discard")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectInstantApplyToAllInDeck>(data =>
                {
                    data.effectToApply = wtg.SStack("Kill", 1);
                    data.includeCrowned = false;
                    data.inDeck = true;
                    data.inDiscard = true;
                    data.inHand = false;
                })
                );

            // Doomsday: On Card Played Destroy Deck And Discard
            assets.Add(new StatusEffectDataBuilder(wtg)
                .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Destroy Non Crown Deck And Discard")
                .WithText("Destroy all cards without <sprite name=crown> in your draw and discard pockets")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                {
                    data.effectToApply = wtg.TryGet<StatusEffectInstantApplyToAllInDeck>("Instant Destroy Non Crown Deck And Discard");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );
            Debug.Log("[WTG] Effects loaded!");
        }
    }
}
