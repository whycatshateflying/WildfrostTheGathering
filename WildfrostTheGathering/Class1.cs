using AbsentAvalanche;
using AbsentAvalanche.Builders.Cards.Items;
using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using static Steamworks.InventoryItem;

namespace WildfrostTheGathering
{
    public class WildfrostTheGathering : WildfrostMod{
        public WildfrostTheGathering(string modDirectory) : base(modDirectory)
        {
            Instance = this;
        }
        public static WildfrostTheGathering Instance;

        public static List<object> assets = new List<object>();  // The list of builders that will build the CardData/StatusEffectData

        private bool preLoaded = false;  // Used to prevent redundantly reconstructing the data. Not truly necessary.

        // More mess lmaooo. Needed for target constraints. Thank you Abigail for the code!
        public class Scriptable<T> where T : ScriptableObject, new()
        {
            private readonly Action<T> _modifier;
            private readonly string _name;

            public Scriptable()
            {
            }

            public Scriptable(Action<T> modifier)
            {
                _name = typeof(T).Name;
                _modifier = modifier;
            }

            public Scriptable(string name, Action<T> modifier)
            {
                _name = name;
                _modifier = modifier;
            }

            public static implicit operator T(Scriptable<T> script)
            {
                var result = ScriptableObject.CreateInstance<T>();
                result.name = script._name;
                script._modifier?.Invoke(result);
                return result;
            }
        }

        // Flying target mode. Thank you Michael C for the help!
        public class TargetModePrioritizeBosses : TargetModeBasic
        {
            public override Entity[] GetPotentialTargets(Entity entity, Entity target, CardContainer targetContainer)
            {
                // Debug.Log("[WildfrostTheGathering] GetPotentialTargets called for " + entity.name);
                if (!IsAnyEnemyBosses(entity))  // If there were no bosses, go to normal code
                {
                    //Debug.Log("[WildfrostTheGathering] No enemy bosses found :(");
                    return base.GetPotentialTargets(entity, target, targetContainer);
                }

                // Or just do the normal code but with a modified check for bosses
                HashSet<Entity> hashSet = new HashSet<Entity>();
                if ((bool)target)
                {
                    // Debug.Log("[WildfrostTheGathering] Target found! " + target.name);
                    hashSet.Add(target);
                }
                else
                {
                    int[] rowIndices = Battle.instance.GetRowIndices(entity);
                    if (rowIndices.Length != 0)
                    {
                        int[] array = rowIndices;
                        foreach (int rowIndex in array)
                        {
                            AddBossTargets(entity, hashSet, rowIndex);
                        }

                        if (hashSet.Count == 0)
                        {
                            int rowCount = Battle.instance.rowCount;
                            for (int j = 0; j < rowCount; j++)
                            {
                                if (!rowIndices.Contains(j))
                                {
                                    AddBossTargets(entity, hashSet, j);
                                }
                            }
                        }
                    }
                }

                if (hashSet.Count <= 0)
                {
                    return null;
                }

                return hashSet.ToArray();
            }

            private bool IsAnyEnemyBosses(Entity entity)
            {
                foreach (Entity card in Battle.instance.minibosses)
                {
                    // Debug.Log("[WildfrostTheGathering] " + card.name);
                    if (card.owner != entity.owner && Battle.IsOnBoard(card))
                    {
                        return true;
                    }
                }
                return false;

            }

            /*
            private Entity[] FindValidBosses(Entity entity)  // Michael C's code
            {

               List<Entity> validBosses = new List<Entity>();
               foreach (Entity card in Battle.instance.minibosses)
               {
                   if (card.owner != entity.owner && Battle.IsOnBoard(card))
                   {
                       validBosses.Add(card);
                   }
               }
               return validBosses.ToArray();
            }*/
            private void AddBossTargets(Entity entity, HashSet<Entity> targets, int rowIndex)
            {
                // Adds to targets the first boss enemy in row rowIndex
                List<Entity> enemiesInRow = entity.GetEnemiesInRow(rowIndex);
                Entity entity2 = null;
                foreach (Entity item in enemiesInRow)
                {
                    if ((bool)item && item.enabled && item.alive && item.canBeHit && item.data.cardType.miniboss)
                    {
                        // Debug.Log("[WildfrostTheGathering] " + item + " is a target!");
                        entity2 = item;
                        break;
                    }
                }

                if ((bool)entity2)
                {
                    targets.Add(entity2);
                    return;
                }

                entity2 = GetEnemyCharacter(entity);
                if ((bool)entity2)
                {
                    targets.Add(entity2);
                }
            }
        }

        // Reduce counter temporarily
        public class StatusEffectOngoingCounter : StatusEffectOngoing
        {
            public int energyPoints = 0;
            public override IEnumerator Add(int add)
            {
                int toAdd = add;
                Debug.Log("[WildfrostTheGathering] Adding " + add + " Remove Counter to " + target.name);
                while (toAdd < 0 && energyPoints > 0 && target.counter.max == 1)
                {
                    toAdd++;
                    energyPoints--;
                }

                target.counter.max += toAdd;

                if (target.counter.max < 1)
                {
                    energyPoints += 1 - target.counter.max;
                    target.counter.max = 1;
                }


                target.counter.current = Math.Min(Math.Max(0, target.counter.current + toAdd), target.counter.max);

                target.PromptUpdate();
                yield break;
            }

            public override IEnumerator Remove(int remove)
            {
                int toRemove = remove;
                Debug.Log("[WildfrostTheGathering] Removing " + remove + " Remove Counter to " + target.name);
                while (toRemove < 0 && energyPoints > 0 && target.counter.max == 1)
                {
                    toRemove++;
                    energyPoints--;
                }

                target.counter.max -= toRemove;

                if (target.counter.max < 1)
                {
                    energyPoints += 1 - target.counter.max;
                    target.counter.max = 1;
                }

                target.counter.current = Math.Min(Math.Max(0, target.counter.current - toRemove), target.counter.max);

                target.PromptUpdate();
                yield break;
            }
        }

        // Make the StatusEffectApplyXWhenDeployed not trigger while in hand. Thank you Abigail for the code!
        public class StatusEffectApplyXWhenDeployedNoHand : StatusEffectApplyXWhenDeployed
        {
            public override bool RunCardMoveEvent(Entity entity)
            {
                if (!Battle.IsOnBoard(target))
                {
                    return false;
                }
                return base.RunCardMoveEvent(entity);
            }
        }

        // Only apply the deployed effect if the deployed has a certain trait (Thank you ME for BEING AWESOME and DOING IT MYSELF)
        public class StatusEffectApplyXWhenDeployedNoHandIfTrait : StatusEffectApplyXWhenDeployedNoHand
        {
            public string wantedTrait;
            public override bool RunCardMoveEvent(Entity entity)
            {
                // Debug.Log("[WildfrostTheGathering] " + entity.name + " was added to play");
                foreach (Entity.TraitStacks trait in entity.traits)
                {
                    // Debug.Log("[WildfrostTheGathering] " + entity.name + " has trait " + trait.data.name);
                    if (trait.data.name.Equals(wantedTrait))
                    {
                        // Debug.Log("[WildfrostTheGathering] returning base.RunCardMoveEvent");
                        return base.RunCardMoveEvent(entity);
                    }
                }
                // Debug.Log("[WildfrostTheGathering] returning false");
                return false;
            }

        }

        // Treasure's "Temporary Unplayable". Thank you Abigail and Hopeful for the code to read and for the misc help!
        public class StatusEffectUnplayable : StatusEffectData
        {
            public override void Init()
            {
                base.OnBegin += OnFirstAdd;
                base.OnTurnEnd += RemoveOnTurnEnd;
            }

            public IEnumerator OnFirstAdd()
            {
                Debug.Log("[WildfrostTheGathering] Unplayable added to " + target.data.name + "!");
                target.data.canPlayOnBoard = false;
                target.data.canPlayOnEnemy = false;
                target.data.canPlayOnFriendly = false;
                target.data.canPlayOnHand = false;
                target.data.playType = Card.PlayType.Play;
                return null;
            }

            public IEnumerator RemoveOnTurnEnd(Entity entity)
            {
                target.data.canPlayOnBoard = target.data.original.canPlayOnBoard;
                target.data.canPlayOnEnemy = target.data.original.canPlayOnEnemy;
                target.data.canPlayOnFriendly = target.data.original.canPlayOnFriendly;
                target.data.canPlayOnHand = target.data.original.canPlayOnHand;
                target.data.playType = target.data.original.playType;
                foreach (Entity.TraitStacks trait in target.traits.Clone())
                {
                    if (trait.data.name.Equals("whycats.wildfrost.wildfrostthegathering.Unplayable"))
                    {
                        Debug.Log("[WildfrostTheGathering] Unplayable found on " + target.data.name + "! Removing...");
                        if (target.traits.Remove(trait))
                        {
                            target.display.promptUpdateDescription = true;
                            target.PromptUpdate();
                            break;
                        }
                    };
                }
                yield return Remove();
            }
        }

        // Allow multiple traits to be applied at once. Thank you Abigail for the code and the help!
        public class StatusEffectSafeTemporaryTrait : StatusEffectTemporaryTrait
        {
            private static int _finished;
            private static int _queued;

            public override IEnumerator StackRoutine(int stacks)
            {
                int current = _queued;
                _queued++;
                yield return new WaitUntil(() => _finished == current);
                yield return base.StackRoutine(stacks);
                _finished++;
            }
        }

        // Apply X on certain card played. Taken from Abigail's AbsentAvalanche mod directly. (then modified for # of times code) Thank you! :3
        public class StatusEffectApplyXOnCertainCardPlayed : StatusEffectApplyXOnCardPlayed
        {
            public int allowedTimes = 0;
            public int numTimesPlayed = 0;
            public CardType allowedCardType;
            public CardData[] allowedCards = Array.Empty<CardData>();
            public TraitData[] allowedTraits = Array.Empty<TraitData>();
            private Hit _hackyHit;

            public override void Init()
            {
                base.OnCardPlayed += Check;
                if (allowedTimes > 0)
                {
                    base.OnTurnEnd += ResetTimes;
                }
            }

            public override bool RunCardPlayedEvent(Entity entity, Entity[] targets)
            {
                //Debug.Log("[WildfrostTheGathering] " + entity.name + " detected. " + numTimesPlayed + " Have been played this turn");
                if (allowedTimes > 0 && numTimesPlayed++ > allowedTimes)
                {
                    //Debug.Log("[WildfrostTheGathering] ...and that's too many times for me");
                    return false;
                }
                if (!target.enabled)
                {
                    //Debug.Log("[WildfrostTheGathering] ...but was not enabled");
                    return false;
                }
                if (target == entity)
                {
                    //Debug.Log("[WildfrostTheGathering] ...but was myself (" + target.name + ")");
                    return false;
                }
                if ((object)allowedCardType != null && allowedCardType.name != entity.data.cardType.name)
                {
                    //Debug.Log("[WildfrostTheGathering] ...but was the wrong card type");
                    return false;
                }

                IEnumerable<TraitData> source = entity.traits.Select((Entity.TraitStacks t) => t.data);
                List<TraitData> source2 = source.ToList();
                TraitData[] array = allowedTraits;
                if (array != null && array.Length > 0 && !source2.ToList().ContainsAny(allowedTraits))
                {
                    //Debug.Log("[WildfrostTheGathering] ...but didn't have the trait");
                    return false;
                }

                _hackyHit = new Hit(entity, null);
                CardData[] array2 = allowedCards;
                return array2 == null || array2.Length <= 0 || allowedCards.ToList().Any((CardData c) => c.name == entity.data.name);
            }

            public new IEnumerator Check(Entity entity, Entity[] targets)
            {
                return Run(GetTargets(_hackyHit, StatusEffectApplyXOnCardPlayed.GetWasInRows(entity, targets), null, targets));
            }
            public IEnumerator ResetTimes(Entity entity)
            {
                numTimesPlayed = 0;
                return null;
            }
        }

        // StatusEffectApplyXPostAttack but now it works with applyEqualAmount. Thank you semmie!
        public class StatusEffectApplyXPostAttackEqualAmount : StatusEffectApplyX
        {
            public override void Init()
            {
                base.PostAttack += CheckHit;
            }

            public override bool RunPostAttackEvent(Hit hit)
            {
                if (target.enabled && hit.attacker == target && target.alive)
                {
                    return Battle.IsOnBoard(target);
                }

                return false;
            }
            public IEnumerator CheckHit(Hit hit)
            {
                yield return Run(GetTargets(hit), hit.damage + hit.damageBlocked);
            }
        }

        // Thank you again Abigail for the very yoinkable code! :3
        public class ScriptableTargetsOnBoard : ScriptableAmount
        {
            public bool allies;
            public bool enemies;
            public bool inRow = false;
            public CardType cardType;
            public string hasTrait;

            public override int Get(Entity entity)
            {
                int num = 0;
                int[] rowIndices = References.Battle.GetRowIndices(entity);
                if (inRow)
                {
                    return InRow(entity, rowIndices);
                }

                if (allies)
                {
                    List<Entity> allies = entity.GetAllies();
                    num += allies.Count((Entity e) => ((object)cardType == null || e.data.cardType == cardType) && (hasTrait == null || e.traits.Any(t => t.data.name.Equals(hasTrait))));
                }

                if (enemies)
                {
                    List<Entity> enemies = entity.GetEnemies();
                    num += enemies.Count((Entity e) => ((object)cardType == null || e.data.cardType == cardType) && (hasTrait == null || e.traits.Any(t => t.data.name.Equals(hasTrait))));
                }
                return num;
            }

            private int InRow(Entity entity, int[] rows)
            {
                int num = 0;
                foreach (int rowIndex in rows)
                {
                    if (allies)
                    {
                        List<Entity> allies = entity.GetEnemiesInRow(rowIndex);
                        num += allies.Count((Entity e) => ((object)cardType == null || e.data.cardType == cardType) && (hasTrait == null || e.traits.Any(t => t.data.name.Equals(hasTrait))));
                    }

                    if (enemies)
                    {
                        List<Entity> enemies = entity.GetEnemiesInRow(rowIndex);
                        num += entity.GetEnemiesInRow(rowIndex).Count((Entity e) => ((object)cardType == null || e.data.cardType == cardType) && (hasTrait == null || e.traits.Any(t => t.data.name.Equals(hasTrait))));
                    }
                }
                return num;
            }
        }

        // Needed so the while active effect also updates if the certain trait is present (and is being played from hand, if that becomes relevant). Thank you semmie for the concept help!
        public class StatusEffectWhileActiveXUpdatesOnTrait : StatusEffectWhileActiveX
        {
            public string alsoActivate;
            public override void Init()
            {
                base.OnEntityDestroyed += Check;
                base.Init();
            }
            public override IEnumerator CardMove(Entity entity)
            {
                if (target == entity)
                {
                    CardContainer[] preContainers = entity.preContainers;
                    if (active)
                    {
                        if (CheckDeactivateOnMove(preContainers, entity.containers))
                        {
                            yield return Deactivate();
                        }
                        else if (AffectsRow())
                        {
                            if (!CompareContainerArrays(preContainers, entity.containers))
                            {
                                yield return Deactivate();
                                yield return Activate();
                            }
                        }
                        else if (AffectsSlot())
                        {
                            yield return Deactivate();
                            yield return Activate();
                        }
                    }
                    else if (CheckActivateOnMove(preContainers, entity.containers))
                    {
                        yield return Activate();
                    }
                }
                else
                {
                    if (!active)
                    {
                        yield break;
                    }

                    if (AffectsSlot())
                    {
                        CardContainer[] other = containersToAffect.Select((CardContainer a) => a.Group).ToArray();
                        if (entity.containers.ContainsAny(other) || entity.preContainers.ContainsAny(other))
                        {
                            yield return Deactivate();
                            yield return Activate();
                        }
                    }
                    else if (affected.Contains(entity))
                    {
                        if (!containersToAffect.ContainsAny(entity.containers))
                        {
                            yield return UnAffect(entity);
                        }
                    }
                    else if (containersToAffect.ContainsAny(entity.containers))
                    {
                        yield return Affect(entity);
                    }
                    else if (entity.preContainers.Length == 0 || !(entity.preContainers[0].GetType() == entity.containers[0].GetType()))
                    {
                        foreach (Entity.TraitStacks trait in entity.traits)
                        {
                            if (trait.data.name.Equals(alsoActivate))
                            {
                                //Debug.Log("[WildfrostTheGathering] beep " + entity.name + " was moved to " + entity.containers[0].name);
                                yield return Deactivate();
                                yield return Activate();
                                break;
                            }
                        }
                    }
                }
            }
            public override bool RunEntityDestroyedEvent(Entity entity, DeathType deathType)
            {
                int num = affected.IndexOf(entity);
                if (num >= 0)
                {
                    affected.RemoveAt(num);
                }

                return true;
            }
            public IEnumerator Check(Entity entity, DeathType deathType)
            {
                //Debug.Log("[WildfrostTheGathering] beep " + entity.name + " was killed");
                foreach (Entity.TraitStacks trait in entity.traits)
                {
                    if (trait.data.name.Equals(alsoActivate))
                    {
                        yield return Deactivate();
                        yield return Activate();
                        break;
                    }
                }
            }
        }

        private void CreateModAssets()
        {
            {  // Effects

                // Adding Treasure: Summon Treasure
                // TODO: don't use statuscopy, instead use a databuilder replica (also do the others im lazy rn)
                assets.Add(
                    // From the ground up: Instant Spawn Treasure In Hand -> Summon Treasure
                    StatusCopy("Summon Junk", "Summon Treasure")  // Copy Summon Junk effect but change it to summon a Treasure
                    .SubscribeToAfterAllBuildEvent<StatusEffectSummon>(data =>  // Only once the cards are loaded
                    {
                        data.summonCard = TryGet<CardData>("treasure");
                    })
                    );

                // Adding Treasure: Instant Summon Treasure
                assets.Add(
                    StatusCopy("Instant Summon Junk In Hand", "Instant Summon Treasure In Hand")  // Copy Instant Summon Junk In Hand but make it call Summon Treaure
                    .SubscribeToAfterAllBuildEvent<StatusEffectInstantSummon>(data =>  // Only once the cards are loaded
                    {
                        data.targetSummon = TryGet<StatusEffectSummon>("Summon Treasure");
                    })
                    );

                // Add Treasure on trigger
                assets.Add(
                    StatusCopy("On Card Played Add Junk To Hand", "On Card Played Add Treasure To Hand") // Copy Trash but change it to apply Instant Summon Junk
                    .WithText("Add <{a}> {0} to your hand")
                    .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.treasure>")  // Any {0} in the line above is replaced with the text insert. The html tag must be of the form <card=[GUID name].[card name]>. No spaces around the equal sign. This creates the card pop-up.
                    .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>  // Only once the cards are loaded
                    {
                        data.effectToApply = TryGet<StatusEffectData>("Instant Summon Treasure In Hand");
                    })
                    );

                // Add Treasure on destroy
                assets.Add(
                    StatusCopy("When Destroyed Summon Dregg", "When Destroyed Add Treasure To Hand")  // Copy Summon Dregg, but make it Summon Treasure
                    .WithText("When destoryed, add <{a}> {0} to your hand")
                    .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.treasure>")  // Any {0} in the line above is replaced with the text insert. The html tag must be of the form <card=[GUID name].[card name]>. No spaces around the equal sign. This creates the card pop-up.
                    .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDestroyed>(data =>  // Only once the cards are loaded
                    {
                        data.effectToApply = TryGet<StatusEffectData>("Instant Summon Treasure In Hand");
                    })
                    );

                // Summon Dragon Token
                assets.Add( 
                    StatusCopy("Summon Beepop", "Summon Dragon Token")  // Copy Summon Fallow effect but change it to summon a Dragon Spawn
                    .WithText("Summon {0}")
                    .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.dragonToken>")
                    .SubscribeToAfterAllBuildEvent<StatusEffectSummon>(data =>  // Only once the cards are loaded
                    {
                        data.summonCard = TryGet<CardData>("dragonToken");
                    })
                    );

                // Treasure: Funny temporary Zoomlin
                assets.Add(new StatusEffectDataBuilder(this)
                    .Create<StatusEffectSafeTemporaryTrait>("Safe Temporary Zoomlin")
                    .WithIsKeyword(true)    // This effect adds text to the card. 
                    .SubscribeToAfterAllBuildEvent<StatusEffectSafeTemporaryTrait>(data =>
                    {
                        data.targetConstraints = new TargetConstraint[]
                        {
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.not = true;
                                tcht.trait = TryGet<TraitData>("Noomlin");
                                tcht.ignoreSilenced = false;
                            }),
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.not = true;
                                tcht.trait = TryGet<TraitData>("Zoomlin");
                                tcht.ignoreSilenced = false;
                            }),
                        };
                        data.trait = TryGet<TraitData>("Zoomlin");
                    })
                    );

                // Treasure: Add Zoomlin to a card in hand (attack effect)
                assets.Add(new StatusEffectDataBuilder(this)
                    .Create<StatusEffectApplyXInstant>("Instant Gain Zoomlin (To Card In Hand)")
                    .WithText("Add {0} to a card in your hand")
                    .WithTextInsert("<keyword=zoomlin>")
                    .WithStackable(false)
                    .WithCanBeBoosted(false)
                    .WithOffensive(false)
                    .WithMakesOffensive(false)
                    .WithDoesDamage(false)
                    .WithType("")
                    .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                    {
                        data.effectToApply = TryGet<StatusEffectSafeTemporaryTrait>("Safe Temporary Zoomlin");
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;

                        data.targetConstraints = new TargetConstraint[]
                        {
                        new Scriptable<TargetConstraintHasTrait>(tcht =>
                        {
                            tcht.not = true;
                            tcht.trait = TryGet<TraitData>("Noomlin");
                            tcht.ignoreSilenced = false;
                        }),
                        new Scriptable<TargetConstraintHasTrait>(tcht =>
                        {
                            tcht.not = true;
                            tcht.trait = TryGet<TraitData>("Zoomlin");
                            tcht.ignoreSilenced = false;
                        }),
                            new Scriptable<TargetConstraintHasStatus>(tchs =>
                        {
                            tchs.not = true;
                            tchs.status = TryGet<StatusEffectFreeAction>("Free Action");
                        }),
                            new Scriptable<TargetConstraintHasStatus>(tchs =>
                        {
                            tchs.not = true;
                            tchs.status = TryGet<StatusEffectFreeAction>("Free Action (Zoomlin)");
                        }),
                        };
                    })
                    );

                // Treasure: Temporary Unplayable
                assets.Add(new StatusEffectDataBuilder(this)
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
                        data.trait = TryGet<TraitData>("Unplayable");
                    })
                    );

                // Treasure: Add Unplayable
                assets.Add(new StatusEffectDataBuilder(this)
                    .Create<StatusEffectApplyXInstant>("Instant Gain Unplayable")
                    .WithOffensive(true)  // As an attack effect, this is treated as a negative status
                    .WithText("It can't be played this turn")
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
                        data.effectToApply = TryGet<StatusEffectSafeTemporaryTrait>("Temporary Unplayable");
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    })
                    );

                // Treasure: You can't play it this turn
                assets.Add(new StatusEffectDataBuilder(this)
                    .Create<StatusEffectUnplayable>("Unplayable")
                    .WithStackable(true)
                    .WithCanBeBoosted(false)
                    .WithOffensive(true)  // As an attack effect, this is treated as a negative status
                    .WithIsStatus(true)
                    .WithVisible(true)
                    );

                // Flying: Change the target mode
                assets.Add(new StatusEffectDataBuilder(this)
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
                assets.Add(new StatusEffectDataBuilder(this)
                    .Create<StatusEffectOngoingCounter>("Ongoing Decrease Counter")
                    .WithStackable(false)
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

                // Dragonlord's Servant: While active, reduce max counter of allies with flying by 1
                assets.Add(new StatusEffectDataBuilder(this)
                    .Create<StatusEffectWhileActiveX>("While Active Decrease Counter Of Flying Allies")
                    .WithText("While active, reduce max <keyword=counter> of {0} allies by 1")
                    .WithTextInsert($"<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                    .WithStackable(false)  // See below
                    .WithCanBeBoosted(false)
                    .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveX>(data =>
                    {
                        data.hiddenKeywords = new KeywordData[]
                        {
                            TryGet<KeywordData>("Active"),
                        };
                        data.eventPriority = 10;
                        data.effectToApply = TryGet<StatusEffectOngoingCounter>("Ongoing Decrease Counter");
                        data.applyConstraints = new TargetConstraint[]
                        {
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.trait = TryGet<TraitData>("Flying");
                                tcht.ignoreSilenced = false;
                            }),
                        };
                        // data.applyEqualAmount = true;  NOPE too annoying cause overflow. Also if you end up fixing this, add <a> to the thingy instead of 1. oh hey test overflow again I think I solved it
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies;
                    })
                    );

                // (unused) When flying ally deployed, count them down by 1
                /*assets.Add(new StatusEffectDataBuilder(this)
                    .Create<StatusEffectApplyXWhenDeployedNoHand>("When Flying Ally Deployed Decrease Counter To Self")
                    .WithStackable(false)
                    .WithCanBeBoosted(false)
                    .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDeployedNoHand>(data =>
                    {
                        data.effectToApply = TryGet<StatusEffectOngoingCounter>("Ongoing Decrease Counter");
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.whenSelfDeployed = false;
                        data.whenAllyDeployed = true;
                        data.applyConstraints = new TargetConstraint[]
                        {
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.trait = TryGet<TraitData>("Flying");
                                tcht.ignoreSilenced = false;
                            }),
                        };
                    })
                    );*/

                // (unused) When deployed, count down allies with flying by 1
                /*assets.Add(new StatusEffectDataBuilder(this)
                    .Create<StatusEffectApplyXWhenDeployed>("When Deployed Reduce Counter Of Allies With Flying")
                    .WithStackable(false)
                    .WithCanBeBoosted(false)
                    .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDeployed>(data =>
                    {
                        data.effectToApply = TryGet<StatusEffectInstantReduceCounter>("Reduce Counter");
                        data.applyConstraints = new TargetConstraint[]
                        {
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.trait = TryGet<TraitData>("Flying");
                                tcht.ignoreSilenced = false;
                            }),
                        };
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies;
                    })
                    );*/

                // Terror of the Peaks: Trigger when Flying ally deployed
                assets.Add(new StatusEffectDataBuilder(this)
                    .Create<StatusEffectApplyXWhenDeployedNoHandIfTrait>("When Flying Ally Deployed Trigger Self")
                    .WithText("Trigger when a {0} ally is deployed")
                    .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                    .WithStackable(false)
                    .WithCanBeBoosted(false)
                    .WithIsReaction(true)
                    .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDeployedNoHandIfTrait>(data =>
                    {
                        data.descColorHex = "F99C61";
                        data.effectToApply = TryGet<StatusEffectInstantTrigger>("Trigger");
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.whenSelfDeployed = false;
                        data.whenAllyDeployed = true;
                        data.wantedTrait = "whycats.wildfrost.wildfrostthegathering.Flying";
                    })
                    );

                // Gain attack when Treasure is played
                assets.Add(new StatusEffectDataBuilder(this)
                    .Create<StatusEffectApplyXOnCertainCardPlayed>("Gain Attack On Treasure")
                    .WithText("Gain <+{a}><keyword=attack> whenever a {0} is played")
                    .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.treasure>")
                    .WithStackable(true)
                    .WithCanBeBoosted(true)
                    .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCertainCardPlayed>(data =>
                    {
                        data.effectToApply = TryGet<StatusEffectInstantIncreaseAttack>("Increase Attack");
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.allowedCards = new CardData[1] { TryGet<CardData>("treasure") };
                    })
                    );

                // Trigger when Treasure is played
                assets.Add(new StatusEffectDataBuilder(this)
                    .Create<StatusEffectApplyXOnCertainCardPlayed>("Trigger On Treasure")
                    .WithText("Trigger whenever a {0} is played")
                    .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.treasure>")
                    .WithStackable(true)
                    .WithCanBeBoosted(false)
                    .WithIsReaction(true)
                    .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCertainCardPlayed>(data =>
                    {
                        data.descColorHex = "F99C61";
                        data.effectToApply = TryGet<StatusEffectInstantTrigger>("Trigger");
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.allowedCards = new CardData[1] { TryGet<CardData>("treasure") };
                    })
                    );

                // Glorybringer: Damage frontmost enemy equal to attack
                assets.Add(new StatusEffectDataBuilder(this)
                    .Create<StatusEffectApplyXPostAttackEqualAmount>("On Hit Equal Damage to Front Enemy")
                    .WithText("Deal damage to frontmost enemy equal to damage done")
                    .WithStackable(false)
                    .WithDoesDamage(true)  // Its entity can kill with this effect, eg for Bling Charm
                    .SubscribeToAfterAllBuildEvent<StatusEffectApplyXPostAttackEqualAmount>(data =>
                    {
                        data.eventPriority = -999;
                        data.dealDamage = true;
                        data.effectToApply = TryGet<StatusEffectSnow>("Snow");  // Crashes when I give it no effect O_O
                        data.countsAsHit = true;
                        data.doPing = false;
                        data.applyEqualAmount = true;
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.FrontEnemy;
                        data.waitForAnimationEnd = true;
                        data.queue = true;
                        data.noTargetType = NoTargetType.NoTargetToAttack;

                    })
                    );


                // Earthquake Dragon: Ongoing reduce counter (stackable)
                assets.Add(new StatusEffectDataBuilder(this)
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
                // TODO: Make it go away when an ally is removed
                assets.Add(new StatusEffectDataBuilder(this)
                    .Create<StatusEffectWhileActiveXUpdatesOnTrait>("While Active Reduce Counter By Allies With Flying")
                    .WithText("While active, reduce own <keyword=counter> by the number of {0} allies")
                    .WithTextInsert($"<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                    .WithStackable(false)
                    .WithCanBeBoosted(false)
                    .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveXUpdatesOnTrait>(data =>
                    {
                        data.alsoActivate = "whycats.wildfrost.wildfrostthegathering.Flying";
                        ScriptableTargetsOnBoard scriptAmount = ScriptableTargetsOnBoard.CreateInstance<ScriptableTargetsOnBoard>();
                        scriptAmount.allies = true;
                        scriptAmount.hasTrait = "whycats.wildfrost.wildfrostthegathering.Flying";
                        data.applyEqualAmount = true;
                        data.scriptableAmount = scriptAmount;
                        data.effectToApply = TryGet<StatusEffectOngoingCounter>("Ongoing Decrease Counter Stackable");
                        data.affectsSelf = true;
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self; 
                    })
                    );

                /* Ojutai, Soul of Winter
                assets.Add(new StatusEffectDataBuilder(this)
                    .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Apply Snow To Random Enemy")
                    .WithText("Apply <{a}> <keyword=snow> to a random enemy whenever a {0} ally attacks")
                    .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying")
                    .WithStackable(true)
                    .WithCanBeBoosted(true)
                    .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                    {
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.RandomEnemy;
                        data.effectToApply = TryGet<StatusEffectSnow>("Snow");
                    })
                    );*/

            }  // Effects

            {  // Cards

                // Dragon Token
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("dragonToken", "Dragon Token")
                    .SetSprites("dragon-baby.png", "dragon-baby-bg.png")
                    .SetStats(1, 4, 4)
                    .WithCardType("Summoned")
                    .WithFlavour("rawr!")
                    .WithValue(25)
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.traits = new List<CardData.TraitStacks>()
                        {
                            new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                        };
                        data.greetMessages = new string[1] { "Woah a token in the companion pool? That\'s not supposed to happen"};
                    })
                    );

                // Treasure
                assets.Add(
                    new CardDataBuilder(this)
                    .CreateItem(name: "treasure", englishTitle: "Treasure", idleAnim: "ShakeAnimationProfile")
                    .SetDamage(null)
                    .WithCardType("Item")
                    .WithFlavour("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$")
                    .SetSprites("treasure-orichards.png", "dragon-baby-bg.png")
                    .WithPools("GeneralItemPool")
                    .WithValue(40)  // Base price in shop: +-6
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.attackEffects = new CardData.StatusEffectStacks[]
                        {
                            SStack("Instant Gain Zoomlin (To Card In Hand)", 1),
                            SStack("Instant Gain Unplayable", 1),
                        };

                        data.traits = new List<CardData.TraitStacks>(2)
                        {
                            TStack("Consume", 1),
                            TStack("Zoomlin", 1),
                        };

                        data.titleFallback = "Treasure";
                        data.textInsert = "<keyword=zoomlin>";
                        data.canPlayOnBoard = false;
                        data.canPlayOnHand = true;
                        data.uses = 1;
                        data.greetMessages = new string[2] { "<b><i>*cash money noises*</b></i>", "\"Woah an item token in the companion pool? That\'s not supposed to happen\"" };
                    })
                    );

                // Goldspan Dragon
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("goldspanDragon", "Goldspan Dragon")
                    .SetSprites("dragon-lord.png", "dragon-lord-bg.png")
                    .SetStats(6, 4, 4)
                    .WithCardType("Friendly")
                    .WithFlavour("\"You see, most places have mice or mosquitoes...\"")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.startWithEffects = new CardData.StatusEffectStacks[]
                        {
                        new CardData.StatusEffectStacks(Get<StatusEffectData>("On Card Played Add Treasure To Hand"), 1),
                        };
                        data.traits = new List<CardData.TraitStacks>()
                        {
                            new CardData.TraitStacks(Get<TraitData>("Spark"), 1),
                            new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                        };
                        data.greetMessages = new string[2] { "<b>*Breathes Fire Loudly*</b>\n<i>How was it stuck in ice?</i>",
                            "\"You see, most places have mice or mosquitoes...\""};
                    })
                    );

                // Ancient Copper Dragon
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("ancientCopperDragon", "Ancient Copper Dragon")
                    .SetSprites("dragon-lord.png", "dragon-lord-bg.png")
                    .SetStats(8, 5, 5)
                    .WithCardType("Friendly")
                    .WithFlavour("You can never have enough gold")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.startWithEffects = new CardData.StatusEffectStacks[]
                        {
                        new CardData.StatusEffectStacks(Get<StatusEffectData>("On Card Played Add Treasure To Hand"), 2),
                        };
                         data.traits = new List<CardData.TraitStacks>()
                        {
                            new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                        };
                        data.greetMessages = new string[1] { "You can never have enough gold" };
                    })
                    );

                // Atsushi, Blazing Sky
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("atsushiBlazingSky", "Atsushi, the Blazing Sky")
                    .SetSprites("dragon-lord.png", "dragon-lord-bg.png")
                    .SetStats(7, 3, 3)
                    .WithCardType("Friendly")
                    .WithFlavour("Deep crimson in color and one hundred feet long, she is the reincarnation of the dragon spirit <b>Ryusei</b>")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.startWithEffects = new CardData.StatusEffectStacks[]
                        {
                        new CardData.StatusEffectStacks(Get<StatusEffectData>("When Destroyed Add Treasure To Hand"), 2),
                        };
                         data.traits = new List<CardData.TraitStacks>()
                        {
                            new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                        };
                        data.greetMessages = new string[3] { "Deep crimson in color and one hundred feet long, she is the reincarnation of the dragon spirit <b>Ryusei</b>",
                            "<b>Ryusei</b> and <b>Jugan</b> sealed themselves and the three other dragon spirits in an egg under <b>Boseiju</b>. They hatched 50 years later, reborn as <b>Ao</b>, <b>Kairi</b>, <b>Junji</b>, <b>Atsushi</b>, and <b>Kura</b>",
                            "The reborn form of <b>Ryusei</b>, protector of <b>Sokenzan</b>" };
                    })
                    );

                // Utvara Hellkite
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("utvaraHellkite", "Utvara Hellkite")
                    .SetSprites("dragon-lord.png", "dragon-lord-bg.png")
                    .SetStats(8, 4, 5)
                    .WithCardType("Friendly")
                    .WithFlavour("The fear of dragons is as old and as powerful as the fear of death itself")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.startWithEffects = new CardData.StatusEffectStacks[]
                        {
                            new CardData.StatusEffectStacks(Get<StatusEffectData>("Summon Dragon Token"), 1),
                        };
                         data.traits = new List<CardData.TraitStacks>()
                        {
                            new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                        };
                        data.greetMessages = new string[1] { "The fear of dragons is as old and as powerful as the fear of death itself" };
                    })
                    );

                // Dragonlord's Servant
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("dragonlordsServant", "Dragonlord's Servant")
                    .SetSprites("dragon-lord.png", "dragon-lord-bg.png")
                    .SetStats(2, 1, 3)
                    .WithCardType("Friendly")
                    .WithFlavour("The tastiest morsels rarely make it to their intended destination")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.startWithEffects = new CardData.StatusEffectStacks[]
                        {
                            new CardData.StatusEffectStacks(TryGet<StatusEffectData>("While Active Decrease Counter Of Flying Allies"), 1),
                        };
                        data.greetMessages = new string[2] { "Atarka serving-goblins coat themselves with grease imbued with noxious herbs, hoping to discourage their ravenous masters from adding them to the meal",
                            "The tastiest morsels rarely make it to their intended destination" };
                    })
                    );

                // Terror of the Peaks
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("terrorOfThePeaks", "Terror of the Peaks")
                    .SetSprites("dragon-lord.png", "dragon-lord-bg.png")
                    .SetStats(5, 5, 5)
                    .WithCardType("Friendly")
                    .WithFlavour("If it comes for you, die boldly or die swiftly—for die you will")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.startWithEffects = new CardData.StatusEffectStacks[]
                        {
                            SStack("When Flying Ally Deployed Trigger Self", 1),
                        };
                        data.traits = new List<CardData.TraitStacks>()
                        {
                            new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                        };
                        data.greetMessages = new string[1] { "If it comes for you, die boldly or die swiftly — for die you will" };
                    })
                    );

                // Captain Lannery Storm
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("captainLanneryStorm", "Captain Lannery Storm")
                    .SetSprites("dragon-lord.png", "dragon-lord-bg.png")
                    .SetStats(1, 1, 4)
                    .WithCardType("Friendly")
                    .WithFlavour("I believe in love at first shine")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.startWithEffects = new CardData.StatusEffectStacks[]
                        {
                            SStack("Gain Attack On Treasure", 1),
                        };
                        data.greetMessages = new string[6] { "I believe in love at first shine",
                        "Skim all the gold and magic rocks you want, but if I see one greasy fingerprint on my new boots, you’ll be drinking bilgewater for a month",
                        "Charge like a red-hot cannonball straight to your target. You slow down, you sink",
                        "Just imagine what’s waiting around the bend. Adventure. Discovery. Riches for the taking. This is why I sail",
                        "Opposable thumbs, opposable toes, prehensile tails, boundary issues … no treasure is safe from a goblin",
                        "The best kind of treasure is the kind that leads to <i>more</i> treasure!"};
                    })
                    );

                // Academy Manufactor
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("academyManufactor", "Academy Manufactor")
                    .SetSprites("dragon-lord.png", "dragon-lord-bg.png")
                    .SetStats(4, 1, 0)
                    .WithCardType("Friendly")
                    .WithFlavour("Automated systems at the <b>Tolarian Academy</b> sort new acquisitions for optimal use, determining which should be studied, eaten, or sold")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.startWithEffects = new CardData.StatusEffectStacks[]
                        {
                            SStack("Trigger On Treasure", 1),
                        };
                        data.traits = new List<CardData.TraitStacks>()
                        {
                            TStack("Draw", 1)
                        };
                        data.greetMessages = new string[2] { "Automated systems at the <b>Tolarian Academy</b> sort new acquisitions for optimal use, determining which should be studied, eaten, or sold",
                        "It shapes wonders beyond our wildest dreams\nLike sandwiches"};
                    })
                    );

                // Glorybringer
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("glorybringer", "Glorybringer")
                    .SetSprites("dragon-lord.png", "dragon-lord-bg.png")
                    .SetStats(4, 3, 3)
                    .WithCardType("Friendly")
                    .WithFlavour("What the initiates face in the final trial is completely at <b>Hazoret's</b> discretion")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.traits = new List<CardData.TraitStacks>()
                        {
                            TStack("Spark", 1),
                            TStack("Flying", 1),
                        };
                        data.startWithEffects = new CardData.StatusEffectStacks[]
                        {
                            SStack("On Hit Equal Damage to Front Enemy", 1),
                        };
                        data.greetMessages = new string[1] { "What the initiates face in the final trial is completely at <b>Hazoret's</b> discretion" };
                    })
                    );

                // Earthquake Dragon
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("earthquakeDragon", "Earthquake Dragon")
                    .SetSprites("dragon-lord.png", "dragon-lord-bg.png")
                    .SetStats(10, 10, 10)
                    .WithCardType("Friendly")
                    .WithFlavour("An empire can take centuries to build but mere moments to destroy")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.traits = new List<CardData.TraitStacks>()
                        {
                            TStack("Flying", 1),
                        };
                        data.startWithEffects = new CardData.StatusEffectStacks[]
                        {
                            SStack("While Active Reduce Counter By Allies With Flying", 2),
                        };
                        data.greetMessages = new string[2] { "An empire can take centuries to build but mere moments to destroy", "*The ground rumbles beneath your feet*" };
                    })
                    );

                // Ojutai, Soul of Winter
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("ojutaiSoulOfWinter", "Ojutai, Soul of Winter")
                    .SetSprites("dragon-lord.png", "dragon-lord-bg.png")
                    .SetStats(6, 2, 4)
                    .WithCardType("Friendly")
                    .WithFlavour("\"Human enlightenment is a firefly that sparks in the night. Dragon enlightenment is a beacon that disperses all darkness.\"")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.traits = new List<CardData.TraitStacks>()
                        {
                            TStack("Flying", 1),
                        };
                        data.startWithEffects = new CardData.StatusEffectStacks[]
                        {
                            SStack("While Active Reduce Counter By Allies With Flying", 2),
                        };
                        data.greetMessages = new string[1] { "\"Human enlightenment is a firefly that sparks in the night. Dragon enlightenment is a beacon that disperses all darkness.\"" };
                    })
                    );

            }  // Cards

            {  // Keywords

                // Flying
                assets.Add(
                    new KeywordDataBuilder(this)
                    .Create("flying")
                    .WithTitle("Flying")  // The in-game name for the upgrade.
                    .WithShowName(true)  // Shows name in Keyword box (as opposed to a nonexistant icon).
                    .WithDescription("Always hits an enemy boss, if applicable|Hits normally if there are none") //Format is body|note.
                    .WithCanStack(false)  // The keyword does not show its stack number.
                    );

                // Unplayable
                assets.Add(
                    new KeywordDataBuilder(this)
                    .Create("unplayable")
                    .WithTitle("Unplayable")  // The in-game name for the upgrade.
                    .WithShowName(true)  // Shows name in Keyword box (as opposed to a nonexistant icon).
                    .WithDescription("Cannot be played this turn|Goes away at the end of turn") //Format is body|note.
                    .WithCanStack(false)  // The keyword does not show its stack number.
                    );

            }  // Keywords

            {  // Traits

                // Flying
                assets.Add(
                    new TraitDataBuilder(this)
                    .Create("Flying")
                    .WithOverrides(Get<TraitData>("Aimless"), Get<TraitData>("Longshot"), Get<TraitData>("Barrage"))
                    .SubscribeToAfterAllBuildEvent((trait) =>
                    {
                        trait.keyword = Get<KeywordData>("flying");
                        trait.effects = new StatusEffectData[] { Get<StatusEffectData>("Prioritize Bosses") };
                    })
                    );

                // Unplayable
                assets.Add(
                    new TraitDataBuilder(this)
                    .Create("Unplayable")
                    .SubscribeToAfterAllBuildEvent((trait) =>
                    {
                        trait.keyword = TryGet<KeywordData>("unplayable");
                        trait.effects = new StatusEffectData[] { TryGet<StatusEffectUnplayable>("Unplayable") };
                    })
                    );

            }  // Traits

            {  // Charms

            }  // Charms
            preLoaded = true;
        }
        public override void Load()
        {
            if (!preLoaded) { CreateModAssets(); }  // preLoaded makes sure that the builders are not made again on the 2nd load.
            base.Load();  // Actual loading and adding assets.
        }

        // Thank you Hopeful for the loading/unloading/templates/tutorial/TryGet/AddAssets/help during the making of this mod
        public void UnloadFromClasses()
        {
            List<ClassData> tribes = AddressableLoader.GetGroup<ClassData>("ClassData");
            foreach (ClassData tribe in tribes)
            {
                if (tribe == null || tribe.rewardPools == null) { continue; }  // Skip null tribes

                foreach (RewardPool pool in tribe.rewardPools)
                {
                    if (pool == null) { continue; };  // Skip null pools

                    pool.list.RemoveAllWhere((item) => item == null || item.ModAdded == this);  // Find and remove everything that needs to be removed.
                }
            }
        }
        public override void Unload()
        {
            base.Unload();
            UnloadFromClasses();
        }

        public T TryGet<T>(string name) where T : DataFile
        {
            T data;
            if (typeof(StatusEffectData).IsAssignableFrom(typeof(T)))
                data = base.Get<StatusEffectData>(name) as T;
            else if (typeof(KeywordData).IsAssignableFrom(typeof(T)))
                data = (AddressableLoader.Get<KeywordData>("KeywordData", Extensions.PrefixGUID(name, this))
                ?? base.Get<KeywordData>(name.ToLower())) as T;
            else
                data = base.Get<T>(name);

            if (data == null)
                throw new Exception($"TryGet Error: Could not find a [{typeof(T).Name}] with the name [{name}] or [{Extensions.PrefixGUID(name, this)}]");

            return data;
        }

        public CardData.StatusEffectStacks SStack(string name, int amount) => new CardData.StatusEffectStacks(TryGet<StatusEffectData>(name), amount);
        public CardData.TraitStacks TStack(string name, int amount) => new CardData.TraitStacks(TryGet<TraitData>(name), amount);

        public StatusEffectDataBuilder StatusCopy(string oldName, string newName)
        {
            StatusEffectData data = TryGet<StatusEffectData>(oldName).InstantiateKeepName();
            data.name = GUID + "." + newName;
            data.targetConstraints = new TargetConstraint[0];
            StatusEffectDataBuilder builder = data.Edit<StatusEffectData, StatusEffectDataBuilder>();
            builder.Mod = this;
            return builder;
        }


        //Credits to Hopeful for this AddAssets code.
        // AddAssets is called somewhere inside base.Load(). It is called multiple times for each DataFile type, with T being the DataFileBuilder of Y
        public override List<T> AddAssets<T, Y>()
        {
            if (assets.OfType<T>().Any())
                Debug.LogWarning($"[{Title}] adding {typeof(Y).Name}s: {assets.OfType<T>().Select(a => a._data.name).Join()}");
            return assets.OfType<T>().ToList();
        }
        public override string GUID => "whycats.wildfrost.wildfrostthegathering"; //[creator name].[game name].[mod name] is standard convention. LOWERCASE!
        public override string[] Depends => new string[] { }; //The GUIDs of other mods that must load before yours. Usually empty
        public override string Title => "Wildfrost: The Gathering";
        public override string Description => "This is intended to be a fairly balanced mod that implements some Magic: The Gathering cards.";
    }
}
