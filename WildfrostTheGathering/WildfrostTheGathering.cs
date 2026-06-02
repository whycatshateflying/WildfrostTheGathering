using DeadExtensions;
using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using Rewired;
using Steamworks.ServerList;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using static StatusEffectData;
using static UnityEngine.Rendering.DebugUI.Table;

namespace WildfrostTheGathering
{
    public class WildfrostTheGathering : WildfrostMod
    {
        static string guid = "whycats.wildfrost.wildfrostthegathering";
        public WildfrostTheGathering(string modDirectory) : base(modDirectory)
        {
            Instance = this;
        }
        public static WildfrostTheGathering Instance;

        public static List<object> assets = new();  // The list of builders that will build the CardData/StatusEffectData

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
                    if (card.owner != entity.owner && Battle.IsOnBoard(card) && card.canBeHit)
                    {
                        Debug.Log("[WildfrostTheGathering] " + entity.name);
                        return true;
                    }
                }
                return false;

            }

            private void AddBossTargets(Entity entity, HashSet<Entity> targets, int rowIndex)
            {
                // Adds to targets the first boss enemy in row rowIndex
                List<Entity> enemiesInRow = entity.GetEnemiesInRow(rowIndex);
                Entity entity2 = null;
                foreach (Entity enemy in enemiesInRow)
                {
                    if ((bool)enemy && enemy.enabled && enemy.alive && enemy.canBeHit && enemy.data.cardType.miniboss)
                    {
                        // Debug.Log("[WildfrostTheGathering] " + item + " is a target!");
                        entity2 = enemy;
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

        // Random Enemy for each card with Zoomlin in hand, up to 6. (Can't realistically be used on items cause no aimless boxes)
        public class TargetModeFireball : TargetMode
        {
            public TargetConstraint[] constraints;
            public override bool TargetRow => false;
            public override bool NeedsTarget => false;
            public override bool Random => true;
            public override Entity[] GetPotentialTargets(Entity entity, Entity target, CardContainer targetContainer)
            {
                int numZoomlins = GetNumZoomlins();
                if (numZoomlins == 0)
                {
                    return null;
                }

                HashSet<Entity> hashSet = new HashSet<Entity>();
                hashSet.AddRange(from e in entity.GetAllEnemies()
                                 where (bool)e && e.enabled && e.alive && e.canBeHit && CheckConstraints(e)
                                 select e);
                if (hashSet.Count <= 0)
                {
                    return null;
                }
                foreach (Entity entity1 in hashSet.ToArray())
                {
                    Debug.Log("[WildfrostTheGathering] " + entity1.name);
                }
                return hashSet.ToArray();
            }

            public override Entity[] GetTargets(Entity entity, Entity target, CardContainer targetContainer)
            {
                int numZoomlins = GetNumZoomlins();
                List<Entity> potentialTargets = GetPotentialTargets(entity, target, targetContainer)?.ToList<Entity>()?.Clone();
                if (potentialTargets == null)
                {
                    return null;
                }

                List<Entity> newTargets = potentialTargets.TakeRandom(numZoomlins)?.ToList<Entity>()?.Clone();

                if (newTargets.Count <= 0)
                {
                    return null;
                }

                return newTargets.ToArray();
            }
            public override Entity[] GetSubsequentTargets(Entity entity, Entity target, CardContainer targetContainer)
            {
                return GetPotentialTargets(entity, target, targetContainer);
            }
            public bool CheckConstraints(Entity target)
            {
                TargetConstraint[] array = constraints;
                if (array != null && array.Length > 0)
                {
                    return constraints.All((TargetConstraint c) => c.Check(target));
                }

                return true;
            }

            private int GetNumZoomlins()
            {
                int sum = 0;
                foreach (Entity card in References.Player.handContainer)
                {
                    // Debug.Log("[WildfrostTheGathering] " + card.name + " is in hand");
                    foreach (Entity.TraitStacks trait in card.traits)
                    {
                        // Debug.Log("[WildfrostTheGathering] " + trait.data.name);
                        if (trait.data.name.Equals("Zoomlin"))
                        {
                            sum++;
                            break;
                        }
                    }
                }
                return sum;
            }
        }

        // Reduce counter temporarily. Thank you semmie for logic help!
        public class StatusEffectOngoingCounter : StatusEffectOngoing
        {
            public int energyPoints = 0;
            public override IEnumerator Add(int add)
            {
                int toAdd = add;
                Debug.Log("[WildfrostTheGathering] Adding " + add + " Counter to " + target.name);
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
                Debug.Log("[WildfrostTheGathering] Removing " + remove + " Counter from " + target.name);
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
            public bool summonQueue = false;
            public override void Init()
            {
                if (summonQueue)
                {
                    base.OnEnable += ActivateUseQueue;
                    base.OnCardMove += ActivateUseQueue;
                }
                else
                    base.Init();
            }

            private IEnumerator ActivateUseQueue(Entity entity)
            {
                if ((bool)contextEqualAmount)
                {
                    int amount = contextEqualAmount.Get(entity);
                    ActionQueue.Stack(new ActionSequence(Run(GetTargets(hackyHit), amount)));
                    yield return null;
                }
                else
                {
                    ActionQueue.Stack(new ActionSequence(Run(GetTargets(hackyHit))));
                    yield return null;
                }
            }

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
            public TraitData wantedTrait;
            public List<CardData> excludedCards;
            public override bool RunCardMoveEvent(Entity entity)
            {
                if (excludedCards.Count > 0 && excludedCards.Any(card => card.name.Equals(entity.name)))
                {
                    Debug.Log("[WTG] " + entity.name + " was in excluded cards");
                    return false;
                }
                // Debug.Log("[WildfrostTheGathering] " + entity.name + " was added to play");
                foreach (Entity.TraitStacks trait in entity.traits)
                {
                    // Debug.Log("[WildfrostTheGathering] " + entity.name + " has trait " + trait.data.name);
                    if (trait.data.name.Equals(wantedTrait.name))
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
                base.OnEnable += RemoveWhenDrawn;
                Events.OnBattleEnd += RemoveAfterBattle;
            }

            public IEnumerator OnFirstAdd()
            {
                Debug.Log("[WildfrostTheGathering] Unplayable added to " + target.data.name + "!");
                target.data.canPlayOnBoard = false;
                target.data.canPlayOnEnemy = false;
                target.data.canPlayOnFriendly = false;
                target.data.canPlayOnHand = false;
                target.data.playType = Card.PlayType.Play;
                target.data.needsTarget = true;
                return null;
            }

            public IEnumerator RemoveOnTurnEnd(Entity entity)
            {
                RemoveFromSelf();
                yield return Remove();
            }
            public IEnumerator RemoveWhenDrawn(Entity entity)
            {
                if (entity == target && target.InHand())
                {
                    RemoveFromSelf();
                }
                yield return null;
            }

            public void RemoveAfterBattle()
            {
                Debug.Log("[WildfrostTheGathering] Is this battle end?");
                RemoveFromSelf();
            }

            private bool RemoveFromSelf()
            {
                target.data.canPlayOnBoard = target.data.original.canPlayOnBoard;
                target.data.canPlayOnEnemy = target.data.original.canPlayOnEnemy;
                target.data.canPlayOnFriendly = target.data.original.canPlayOnFriendly;
                target.data.canPlayOnHand = target.data.original.canPlayOnHand;
                target.data.playType = target.data.original.playType;
                target.data.needsTarget = target.data.original.needsTarget;
                foreach (Entity.TraitStacks trait in target.traits.Clone())
                {
                    if (trait.data.name.Equals($"{guid}.Unplayable"))
                    {
                        Debug.Log("[WildfrostTheGathering] Unplayable found on " + target.data.name + "! Removing...");
                        if (target.traits.Remove(trait))
                        {
                            target.display.promptUpdateDescription = true;
                            target.PromptUpdate();
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // StatusEffectWhileActiveOnce = Ongoing: ""
        public class StatusEffectWhileActiveXOnce : StatusEffectWhileActiveX
        {
            public bool cardPlayed;
            public override void Init()
            {
                base.OnActionPerformed += ClearAfterAttacking;
                base.Init();
            }

            public override void OnDestroy()
            {
                base.OnActionPerformed -= ClearAfterAttacking;
                base.OnDestroy();
            }

            public override bool RunActionPerformedEvent(PlayAction action)
            {
                if (cardPlayed)
                {
                    return ActionQueue.Empty;
                }

                return false;
            }
            public override bool RunCardPlayedEvent(Entity entity, Entity[] targets)
            {
                if (!cardPlayed && entity == target && count > 0)
                {
                    cardPlayed = true;
                }

                return false;
            }

            private IEnumerator ClearAfterAttacking(PlayAction action)
            {
                cardPlayed = false;
                if (target.enabled && !target.silenced)
                {
                    Debug.Log("[WTG] BEGONE, CREATURE! " + target.name + ". clearing " + name + ". Playaction type: " + action.Name);
                    yield return Deactivate();
                    yield return Remove();
                }
            }
            public override bool RunStackEvent(int stacks)
            {
                target.display.promptUpdateDescription = true;
                target.PromptUpdate();
                return false;
            }
            public override bool RunEndEvent()
            {
                target.display.promptUpdateDescription = true;
                target.PromptUpdate();
                return true;
            }
        }

        // Ongoing retains X
        public class StatusEffectHaltXOnce : StatusEffectData
        {
            public StatusEffectData effectToHalt;
            public bool ignoreSilence = true;
            public bool cardPlayed = false;

            public override void Init()
            {
                Debug.Log($"→ Halting Count Down of [{effectToHalt.name}] for [{target}]");
                base.OnActionPerformed += ClearAfterAttacking;
                Events.OnStatusEffectCountDown += StatusCountDown;
            }

            public void OnDestroy()
            {
                Debug.Log($"→ Resuming Count Down of [{effectToHalt.name}] for [{target}]");
                base.OnActionPerformed -= ClearAfterAttacking;
                Events.OnStatusEffectCountDown -= StatusCountDown;
            }

            public override bool RunActionPerformedEvent(PlayAction action)
            {
                if (cardPlayed)
                {
                    return ActionQueue.Empty;
                }

                return false;
            }
            public override bool RunCardPlayedEvent(Entity entity, Entity[] targets)
            {
                if (!cardPlayed && entity == target && count > 0)
                {
                    cardPlayed = true;
                }

                return false;
            }

            private IEnumerator ClearAfterAttacking(PlayAction action)
            {
                cardPlayed = false;
                if (target.enabled && !target.silenced)
                {
                    Debug.Log("[WTG] BEGONE, CREATURE! " + target.name + ". clearing " + name + ". Playaction type: " + action.Name);
                    yield return Remove();
                }
            }

            public void StatusCountDown(StatusEffectData status, ref int amount)
            {
                if (status.type == effectToHalt.type && status.target == target && !Silenced())
                {
                    amount = 0;
                }
            }

            public bool Silenced()
            {
                if (target.silenced)
                {
                    return !ignoreSilence;
                }

                return false;
            }
            public override bool RunStackEvent(int stacks)
            {
                target.display.promptUpdateDescription = true;
                target.PromptUpdate();
                return false;
            }
            public override bool RunEndEvent()
            {
                target.display.promptUpdateDescription = true;
                target.PromptUpdate();
                return true;
            }
        }

        // Phase out. Mostly copied from abduct by Abigail Thank!
        public class StatusEffectPhasedOut : StatusEffectData
        {
            bool cardPlayed = false;
            public override void Init()
            {
                base.OnStack += Apply;
                base.OnActionPerformed += ClearAfterAttacking;
                base.OnHit += PreventDamage;
            }

            public override bool RunHitEvent(Hit hit)
            {
                return hit.target == target;
            }

            public override bool RunActionPerformedEvent(PlayAction action)
            {
                if (cardPlayed)
                {
                    return ActionQueue.Empty;
                }

                return false;
            }
            public override bool RunCardPlayedEvent(Entity entity, Entity[] targets)
            {
                if (!cardPlayed && entity == target && count > 0)
                {
                    cardPlayed = true;
                }

                return false;
            }

            private static IEnumerator PreventDamage(Hit hit)
            {
                hit.damage = 0;
                yield break;
            }

            private IEnumerator Apply(int stacks)
            {
                ChangeAlpha(0.75f);
                target.cannotBeHitCount += stacks;
                target.display.promptUpdateDescription = true;
                target.PromptUpdate();
                yield break;
            }

            private IEnumerator ClearAfterAttacking(PlayAction action)
            {
                cardPlayed = false;
                if (target.enabled)
                {
                    ChangeAlpha(1f);
                    target.cannotBeHitCount -= count;
                    Debug.Log("[WTG] BEGONE, CREATURE! " + target.name + ". clearing " + name + ". Playaction type: " + action.Name);
                    yield return Remove();
                    target.display.promptUpdateDescription = true;
                    target.PromptUpdate();
                }
            }

            private void ChangeAlpha(float alpha)
            {
                ((Card)target.display).canvasGroup.alpha = alpha;
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
            public bool summonQueue = false;
            public bool whileActive = false;
            public int allowedTimes = 0;
            public int numTimesPlayed = 0;
            public int attackGreaterThan = -100;  // -100 is "don't check". If I make it nullable, it's always null :/
            public CardType allowedCardType;
            public bool countsSelf = false;
            public CardData[] allowedCards = [];
            public TraitData[] allowedTraits = [];
            public Hit _hackyHit;

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
                // Debug.Log("[WTG] " + attackGreaterThan + " " + (attackGreaterThan != -100).ToString());
                Debug.Log("[WildfrostTheGathering] (" + target.name + ") " + entity.name + " detected. " + numTimesPlayed + " Have been played this turn and I've been updated");
                if (whileActive && !Battle.IsOnBoard(target))
                {
                    Debug.Log("[WildfrostTheGathering] ...but I'm not active yet");
                    return false;
                }
                if (allowedTimes > 0 && numTimesPlayed + 1> allowedTimes)
                {
                    Debug.Log("[WildfrostTheGathering] ...and that's too many times for me");
                    return false;
                }
                if (!target.enabled)
                {
                    Debug.Log("[WildfrostTheGathering] ...but I was not enabled");
                    return false;
                }
                if (!countsSelf && target == entity)
                {
                    Debug.Log("[WildfrostTheGathering] ...but was myself (" + target.name + ")");
                    return false;
                }
                if (allowedCardType != null && allowedCardType.name != entity.data.cardType.name)
                {
                    Debug.Log("[WildfrostTheGathering] ...but was the wrong card type (" + allowedCardType.name + ") and (" + entity.data.cardType.name + ")");
                    return false;
                }
                if (attackGreaterThan != -100 && (entity.data.damage <= attackGreaterThan || !entity.data.hasAttack))
                {
                    Debug.Log("[WildfrostTheGathering] ... but had no attack (" + entity.data.hasAttack + "), (" + entity.data.damage + ")");
                    return false;
                }

                IEnumerable<TraitData> source = entity.traits.Select((Entity.TraitStacks t) => t.data);
                List<TraitData> source2 = source.ToList();
                TraitData[] array = allowedTraits;
                if (array != null && array.Length > 0 && !source2.ToList().ContainsAny(allowedTraits))
                {
                    Debug.Log("[WildfrostTheGathering] ...but didn't have the trait");
                    return false;
                }

                _hackyHit = new Hit(entity, null);
                CardData[] array2 = allowedCards;
                Debug.Log("[WildfrostTheGathering] " + array2 + " || " + array2.Length + " || " + allowedCards.ToList().Any((CardData c) => c.name == entity.data.name));
                if (array2 == null || array2.Length <= 0 || allowedCards.ToList().Any((CardData c) => c.name == entity.data.name))
                {
                    numTimesPlayed++;
                    return true;
                }
                return false;
            }

            public new IEnumerator Check(Entity entity, Entity[] targets)
            {
                if (summonQueue)
                {
                    ActionQueue.Stack(new ActionSequence(Run(GetTargets(_hackyHit, StatusEffectApplyXOnCardPlayed.GetWasInRows(entity, targets), null, targets))));
                    return null;
                }
                else
                    return Run(GetTargets(_hackyHit, StatusEffectApplyXOnCardPlayed.GetWasInRows(entity, targets), null, targets));
            }
            public IEnumerator ResetTimes(Entity entity)
            {
                numTimesPlayed = 0;
                return null;
            }
        }

        // This feels degenerate at this point... For use with applyEqualAmount but when I need an "equal to attack"
        public class StatusEffectApplyXEqualToAttackOnCertainCardPlayed : StatusEffectApplyXOnCertainCardPlayed
        {
            public override void Init()
            {
                base.OnCardPlayed += Check;
                if (allowedTimes > 0)
                {
                    base.OnTurnEnd += ResetTimes;
                }
            }
            public new IEnumerator Check(Entity entity, Entity[] targets)
            {
                Debug.Log("[WildfrostTheGathering] Beep. Entity is " + entity.name + " and the damage is " + entity.damage.current);
                if (summonQueue)
                {
                    ActionQueue.Stack(new ActionSequence(Run(GetTargets(_hackyHit, StatusEffectApplyXOnCardPlayed.GetWasInRows(entity, targets), null, targets), entity.damage.current)));
                    return null;
                }
                else
                    return Run(GetTargets(_hackyHit, StatusEffectApplyXOnCardPlayed.GetWasInRows(entity, targets), null, targets), entity.damage.current);
            }
        }

        // For use with applyEqualAmount but when I need an "equal to counter"
        public class StatusEffectApplyXEqualToCounterOnCertainCardPlayed : StatusEffectApplyXOnCertainCardPlayed
        {
            public bool ofTarget;
            public override void Init()
            {
                base.OnCardPlayed += Check;
                if (allowedTimes > 0)
                {
                    base.OnTurnEnd += ResetTimes;
                }
            }
            public new IEnumerator Check(Entity entity, Entity[] targets)
            {
                int sumOfCounters = 0;
                if (ofTarget)
                {
                    foreach (Entity target in targets)
                    {
                        Debug.Log("[WildfrostTheGathering] Entity is " + entity.name + " and the counter of a target is " + target.counter.current);
                        sumOfCounters += target.counter.current;
                    }
                }
                else
                {
                    sumOfCounters = entity.counter.current;
                }
                return Run(GetTargets(_hackyHit, StatusEffectApplyXOnCardPlayed.GetWasInRows(entity, targets), null, targets), sumOfCounters);
            }
        }

        // StatusEffectApplyXPostAttack but now it works with applyEqualAmount. Thank you semmie!
        public class StatusEffectApplyXPostAttackEqualAmount : StatusEffectApplyX
        {
            //StatusEffectApplyXPostAttack
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

        // Applies X equal to the count, each with a stack of 1 (I think, stack size is untested)
        public class StatusEffectApplyXMultipleInstant : StatusEffectApplyX
        {
            public override bool Instant => true;
            public int amountPerStack = 1;
            public int numTimes = 0;  // If numTimes is 1 or greater, apply the stack size N times

            public override int GetAmount(Entity entity, bool equalAmount = false, int equalTo = 0)
            {
                if (!scriptableAmount)
                {
                    if (!equalAmount)
                    {
                        return GetAmount();
                    }

                    return equalTo;
                }

                return scriptableAmount.Get(entity);
            }

            public override int GetAmount()
            {
                return count;
            }

            public override bool TargetSilenced()
            {
                return false;
            }

            public override void Init()
            {
                base.OnBegin += Process;
            }

            public IEnumerator Process()
            {
                if (numTimes == 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        yield return Run(GetTargets(), amountPerStack);
                    }
                }
                else
                {
                    Debug.Log("[WTG] " + target.name + " is going to apply " + count + " " + effectToApply.name + " to the following " + numTimes + " times:");
                    foreach (Entity entity in GetTargets())
                    {
                        Debug.Log("[WTG] " + entity);
                    }
                    for (int i = 0; i < numTimes; i++)
                    {
                        Debug.Log("[WTG] I am running!");
                        yield return Run(GetTargets(), count);
                    }
                }
                yield return Remove();
            }
        }

        // Status effect apply x when something is shuffled
        public class StatusEffectApplyXWhenDiscardShuffled : StatusEffectApplyX
        {
            public override void Init()
            {
                OnShuffle += ApplyXShuffle;
            }
            public void OnDestroy()
            {
                Debug.Log("[WTG] ondestroy called!");
                OnShuffle -= ApplyXShuffle;
            }

            public void ApplyXShuffle()
            {
                Debug.Log("[WTG] applyXshuffle called!");
                if (Battle.IsOnBoard(target))
                {
                    foreach (Entity entity in GetTargets().Distinct())
                    {
                        Debug.Log("[WTG] " + target.name + " is stacking " + effectToApply.name + " to " + entity.name);
                        ActionQueue.Stack(new ActionApplyStatus(entity, target, effectToApply, GetAmount()));
                    }
                }
            }
            
        }

        public class StatusEffectTrample : StatusEffectData
        {
            public List<Entity> targetsBehind = [];

            public override void Init()
            {
                base.PostHit += PiercingDamage;
            }
            public override bool RunHitEvent(Hit hit)
            {
                List<CardContainer> targetsRow = hit.target.actualContainers;
                foreach (CardContainer container in targetsRow)
                {
                    CardContainer group = container.Group;
                    foreach (Entity entity in group)
                    {
                        Debug.Log("[WTG] [" + targetsRow.IndexOf(container) + "] In the target's row there is: " + entity.name + " at index " + group.IndexOf(entity));
                    }

                    int index = group.IndexOf(hit.target);
                    Debug.Log("[WTG] [" + targetsRow.IndexOf(container) + "] Target found in the row! " + index);
                    Entity potentialTarget = group.FirstOrDefault(entity => group.IndexOf(entity) == index + 1);
                    if (index <= group.max && index != -1 && potentialTarget != null)
                    {
                        Debug.Log("[WTG] [" + targetsRow.IndexOf(container) + "] Trying to trigger against target behind! " + potentialTarget?.name);
                        if (!potentialTarget.isActiveAndEnabled)
                        {
                            Debug.Log("[WTG] [" + targetsRow.IndexOf(container) + "] " + potentialTarget.name + " wasn't active and enabled!");
                            continue;
                        }
                        if (!potentialTarget.alive)
                        {
                            Debug.Log("[WTG] [" + targetsRow.IndexOf(container) + "] " + potentialTarget.name + " wasn't alive!");
                            continue;
                        }
                        if (targetsBehind.IndexOf(potentialTarget) != -1)
                        {
                            Debug.Log("[WTG] [" + targetsRow.IndexOf(container) + "] " + potentialTarget.name + " was already in the queue!");
                            continue;
                        }
                        targetsBehind.AddIfNotNull(potentialTarget);
                    }
                    else
                    {
                        Debug.Log("[WTG] [" + targetsRow.IndexOf(container) + "] Nope! it's at the end of the line! The index was " + index + ". The thing at index " + (index + 1) + " was " + group.FirstOrDefault(entity => group.IndexOf(entity) == index + 1));
                    }
                }

                return false;
            }
            private IEnumerator PiercingDamage(Hit hit)
            {
                Debug.Log("[WTG] Target is " + hit.target.name + " and I am " + target.name);
                if (hit.attacker != target || hit.target == target)
                {
                    Debug.Log("[WTG] [" + target.name + "] Abort! Not my hit, not my biz (" + hit.attacker.name + ")");
                    yield break;
                }

                if (hit.target.hp.current >= 0)
                {
                    Debug.Log("[WTG] No excess damage (" + hit.damageDealt + " is less than or equal to " + hit.target.hp.current + ")");
                    yield break;
                }
                int excessDamage = 0 - hit.target.hp.current;
                Debug.Log("[WTG] Excess damage done is " + excessDamage);

                while (targetsBehind.Count > 0)
                {
                    Entity targetBehind = targetsBehind[0];
                    targetsBehind.RemoveAt(0);
                    if (!targetBehind.isActiveAndEnabled)
                    {
                        Debug.Log("[WTG] " + targetBehind.name + " wasn't active and enabled!");
                        continue;
                    }
                    if (!targetBehind.alive)
                    {
                        Debug.Log("[WTG] " + targetBehind.name + " wasn't alive!");
                        continue;
                    }
                    Debug.Log("[WTG] Creating Hit " + target.name + ", " + targetBehind + ", " + excessDamage);
                    Hit trampleHit = new Hit(target, targetBehind, excessDamage);
                    trampleHit.AddAttackerStatuses();
                    yield return trampleHit.Process();
                }
            }
        }
        
        // Thank you again Abigail for the very yoinkable code! :3 And thank you semmie+Abigail for debugging said code when it broke on its own!
        public class ScriptableTargetsOnBoard : ScriptableAmount
        {
            public bool allies;
            public bool enemies;
            public bool inRow = false;
            public int mult = 1;
            public CardType cardType;
            public TraitData hasTrait;

            public override int Get(Entity entity)
            {
                Debug.Log("[WTG] " + entity + " was checking for the number of target on board");
                int num = 0;
                int[] rowIndices = References.Battle.GetRowIndices(entity);
                if (inRow)
                {
                    return InRow(entity, rowIndices);
                }

                if (allies)
                {
                    List<Entity> allies = entity.GetAllAllies();
                    num += allies.Count((Entity e) => (cardType is null || e.data.cardType == cardType) && (hasTrait is null || e.traits.Any(t => t.data.name.Equals(hasTrait.name))));
                }

                if (enemies)
                {
                    List<Entity> enemies = entity.GetAllEnemies();
                    num += enemies.Count((Entity e) => (cardType is null || e.data.cardType == cardType) && (hasTrait is null || e.traits.Any(t => t.data.name.Equals(hasTrait.name))));
                }
                return num * mult;
            }

            private int InRow(Entity entity, int[] rows)
            {
                int num = 0;
                foreach (int rowIndex in rows)
                {
                    if (allies)
                    {
                        List<Entity> allies = entity.GetAlliesInRow(rowIndex);
                        num += allies.Count((Entity e) => (e != null) && (cardType is null || e.data.cardType == cardType) && (hasTrait is null || e.traits.Any(t => t.data.name.Equals(hasTrait.name))));
                    }

                    if (enemies)
                    {
                        List<Entity> enemies = entity.GetEnemiesInRow(rowIndex);
                        num += enemies.Count((Entity e) => (e != null) && (cardType is null || e.data.cardType == cardType) && (hasTrait is null || e.traits.Any(t => t.data.name.Equals(hasTrait.name))));
                    }
                }
                return num;
            }
        }

        // ScriptableAmount equal to counter
        public class ScriptableEqualToCounter : ScriptableAmount
        {
            public bool equalToMax = false;
            public override int Get(Entity entity)
            {
                if (equalToMax)
                {
                    return entity.counter.max;
                }
                return entity.counter.current;
            }
        }

        // For Death's shadow
        public class ScriptableDamageLeaderHasTaken : ScriptableAmount
        {
            public override int Get(Entity entity)
            {
                int sum = 0;
                List<Entity> allies = entity.GetAllAllies();
                Debug.Log("[WTG] ScriptableDamageLeaderHasTaken Get() called on " + entity.name + "!");
                foreach (Entity ally in allies)
                {
                    if (ally.data.cardType.name != "Leader")
                    {
                        continue;
                    }
                    Debug.Log("[WTG] " + ally.name + " is a leader!. ally.hp.max - ally.hp.current = " + (ally.hp.max - ally.hp.current));
                    sum += (ally.hp.max - ally.hp.current);
                }
                return sum;
            }
        }

        // Updates when leader display updates and the health has changed. Thank you semmie for helping debug!
        public class StatusEffectWhileActiveXUpdatesWhenLeaderTakesDamage : StatusEffectWhileActiveX
        {
            public override void Init()
            {
                Events.OnEntityDisplayUpdated += UpdateEffectWhenLeaderDisplayUpdates;
                base.Init();
            }

            private void UpdateEffectWhenLeaderDisplayUpdates (Entity entity)
            {
                Debug.Log("[WTG] " + GetAmount(target) + "  " + currentAmount);
                if (entity.data.cardType.name == "Leader" && currentAmount != GetAmount(target))
                {
                    ActionQueue.Add(new ActionRefreshWhileActiveEffect(this));
                }
            }

        }

        // For when you need to count the number of targets with a trait or type in hand
        public class ScriptableTargetsInHand : ScriptableAmount
        {
            public CardType cardType;
            public TraitData hasTrait;
            public override int Get(Entity entity)
            {
                int num = 0;
                List<Entity> cardsInHand = References.Player.handContainer.ToList();
                num += cardsInHand.Count((Entity e) => (cardType is null || e.data.cardType == cardType) && (hasTrait is null || e.traits.Any(t => t.data.name.Equals(hasTrait.name))));
                return num;
            }
        }

        // Scriptable Health plus constant
        public class ScriptableCurrentHealthPlusConstant : ScriptableCurrentHealth
        {
            public int constant = 0;
            public override int Get(Entity entity)
            {
                Debug.Log("[WTG] health plus constant returning " + (base.Get(entity) + constant));
                return base.Get(entity) + constant;
            }
        }

        // Needed so the while active effect also updates if the certain trait is present (and is being played from hand, if that becomes relevant). Thank you semmie and Abigail for the help!
        public class StatusEffectWhileActiveXUpdatesOnTrait : StatusEffectWhileActiveX
        {
            public TraitData alsoActivate;
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
                            if (trait.data.name.Equals(alsoActivate.name))
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
                    if (trait.data.name.Equals(alsoActivate.name))
                    {
                        yield return Deactivate();
                        yield return Activate();
                        break;
                    }
                }
            }
        }

        // idk why this disappeared... anyways apply x but when this is applied or things move update the desc
        public class StatusEffectApplyXOnCardPlayedUpdateDesc : StatusEffectApplyXOnCardPlayed
        {
            public override bool RunEndEvent()
            {
                target.display.promptUpdateDescription = true;
                target.PromptUpdate();
                return false;
            }
            public override bool RunStackEvent(int stacks)
            {
                target.display.promptUpdateDescription = true;
                target.PromptUpdate();
                return false;
            }
            public override bool RunActionPerformedEvent(PlayAction playAction)
            {
                target.display.promptUpdateDescription = true;
                target.PromptUpdate();
                return false;
            }
        }

        // When this is applied, update the description
        public class StatusEffectApplyXWhenDestroyedUpdateDesc : StatusEffectApplyXWhenDestroyed
        {
            public override bool RunStackEvent(int stacks)
            {
                target.display.promptUpdateDescription = true;
                target.PromptUpdate();
                return false;
            }
            public override bool RunEndEvent()
            {
                target.display.promptUpdateDescription = true;
                target.PromptUpdate();
                return false;
            }
        }

        // Professional Face-Breaker Recycle to add effect
        public class StatusEffectInstantDestroyNumCardsInHandAndApplyXForEach : StatusEffectInstantDestroyCardsInHandAndApplyXForEach
        {
            public int maximum = 1;
            public override IEnumerator Process()
            {
                Character player = References.Player;
                int a = GetAmount();
                yield return ModifiedDestroyCardsSequence(player.handContainer);
                for (int i = 0; i < destroyed; i++)
                {
                    yield return StatusEffectSystem.Apply(target, target, effectToApply, a);
                }

                yield return Remove();
            }
            public IEnumerator ModifiedDestroyCardsSequence(CardContainer container)
            {
                bool pingDone = false;
                List<Entity> list = new List<Entity>(container);
                foreach (Entity item in list)
                {
                    Debug.Log("[WildfrostTheGathering] " + item.name + ". killed: " + destroyed);
                    if (destroyed >= maximum)
                    {
                        break;
                    }
                    if (CheckConstraints(item))
                    {
                        if (!pingDone)
                        {
                            target.curveAnimator.Ping();
                            pingDone = true;
                        }

                        destroyed++;
                        yield return StatusEffectSystem.Apply(item, target, destroyCardEffect, 1, temporary: true);
                    }
                }
            }
        }

        public class StatusEffectBonusDamageEqualToCardsWithTrait : StatusEffectBonusDamageEqualToCards
        {
            public bool doCheckName = false;
            public TraitData checkTrait;
            public override void Init()
            {
                base.PreCardPlayed += GainTrait;
            }
            public IEnumerator GainTrait(Entity entity, Entity[] targets)
            {
                int num = CountTrait();
                if (num > 0)
                {
                    currentAmount = num;
                    target.tempDamage += currentAmount;
                    target.PromptUpdate();
                    if (ping)
                    {
                        target.curveAnimator?.Ping();
                        yield return Sequences.Wait(0.5f);
                    }
                }
            }
            public int CountTrait()
            {
                return 0 + (inHand ? CountInHandTrait() : 0) + (onBoard ? CountOnBoardTrait() : 0);
            }
            public int CountInHandTrait()
            {
                CardContainer handContainer = References.Player.handContainer;
                if (!(bool)handContainer)
                {
                    return 0;
                }

                int num = 0;
                foreach (Entity entity in handContainer)
                {
                    if (!doCheckName || entity.name.Equals(cardName) && (includeSelf || entity != target))
                    {
                        foreach (Entity.TraitStacks trait in entity.traits)
                        {
                            if (trait.data.name.Equals(checkTrait.name))
                            {
                                num++;
                                break;
                            }
                        }
                    }
                }
                return num;
            }

            public int CountOnBoardTrait()
            {
                int num = 0;
                foreach (Entity entity in Battle.GetAllUnits())
                {
                    if (!doCheckName || entity.name.Equals(cardName) && (includeSelf || entity != target))
                    {
                        foreach (Entity.TraitStacks trait in entity.traits)
                        {
                            if (trait.data.name.Equals(checkTrait.name))
                            {
                                num++;
                                break;
                            }
                        }
                    }
                }
                return num;
            }
        }

        // Doomsday code. Technically should work with other effects but haven't tested it yet
        public class StatusEffectInstantApplyToAllInDeck : StatusEffectInstant
        {
            public bool inDeck = false;
            public bool inDiscard = false;
            public bool inHand = false;
            public bool includeCrowned = false;
            public CardData.StatusEffectStacks effectToApply;

            public override IEnumerator Process()
            {
                if (inDeck)
                    AddToAllCardsInList(References.Player.drawContainer.entities);
                if (inDiscard)
                    AddToAllCardsInList(References.Player.discardContainer.entities);
                if (inHand)
                    AddToAllCardsInList(References.Player.handContainer.entities);
                
                yield return Remove();
            }
            private void AddToAllCardsInList(List<Entity> cards)
            {
                foreach (Entity entity in cards)
                {
                    if (includeCrowned || !entity.data.HasCrown)
                    {
                        ActionQueue.RunParallel(new ActionApplyStatus(entity, null, effectToApply.data, effectToApply.count));
                        entity.silenceCount = 1;
                        entity.enabled = true;
                    }
                }
            }
        }

        // For Throes of Chaos. Draw a card (random or top, from deck, discard, or both, using a predicate to choose) to hand, optionally apply some effects to it.
        public class StatusEffectDrawRandomCardWithPredicate : StatusEffectInstant
        {
            public Predicate<CardData> predicate;
            public CardData.StatusEffectStacks[] addEffectStacks;
            public bool equalToCount = false;
            public int drawNumber = 0;  // 0 is default - draw equal to the stack
            public bool random = false;
            public bool deck = true;
            public bool discard = false;
            
            public override IEnumerator Process()
            {
                Character player = References.Player;

                List<Entity> cards = [];
                if (deck)
                    cards = cards.Concat(player.drawContainer.entities).ToList();
                if (discard)
                    cards = cards.Concat(player.discardContainer.entities).ToList();

                var predicate1 = TryGet<StatusEffectDrawRandomCardWithPredicate>(name).predicate;
                if (predicate1 is null)
                    throw new ArgumentException("No predicate found");

                bool flag = false;
                foreach (Entity card in cards)
                {
                    Debug.Log("[WTG] " + target.name + " is checking" + card.name);
                    if (predicate1.Invoke(card.data))
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    Events.InvokeCardDrawEnd();
                    Debug.Log("[WTG] No tutor targets :(");
                    if (NoTargetTextSystem.Exists())
                    {
                        yield return NoTargetTextSystem.Run(target, NoTargetType.NoCardsToDraw);
                    }
                }
                else
                {
                    if (equalToCount)
                    {
                        Debug.Log("[WTG] I have a count of " + count);
                        for (int i = 0; i < addEffectStacks.Count(); i++)
                        {
                            Debug.Log("[WTG] " + addEffectStacks[i].data.name + " has a count of " + addEffectStacks[i].count);
                            addEffectStacks[i].count = count;
                            Debug.Log("[WTG] " + addEffectStacks[i].data.name + " has a new count of " + addEffectStacks[i].count);
                        }
                    }
                    if (drawNumber > 0)
                    {
                        ActionQueue.Stack(new ActionDrawPredicate(player, predicate1, addEffectStacks, drawNumber, caller: target, random: random, deck: deck, discard: discard), fixedPosition: true);
                    }
                    else
                    {
                        ActionQueue.Stack(new ActionDrawPredicate(player, predicate1, addEffectStacks, GetAmount(), caller: target, random: random, deck: deck, discard: discard), fixedPosition: true);
                    }
                }

                yield return base.Process();
            }
        }

        // Needed for Throes of Chaos to work. Checks a predicate against a card. Needs an effect array to apply, but you can just use []
        public class ActionDrawPredicate : ActionDraw
        {
            public readonly new Character character;
            public Entity caller;
            public Predicate<CardData> predicate;
            public CardData.StatusEffectStacks[] addEffectStacks;
            public bool random;
            public bool deck;
            public bool discard;
            public ActionDrawPredicate(Character character, Predicate<CardData> predicate, CardData.StatusEffectStacks[] addEffectStacks, int count = 1, float pauseBetween = 0.1F, Entity caller = null, bool deck = true, bool discard = false, bool random = false) : base(character, count, pauseBetween)
            {
                this.character = character;
                this.count = count;
                this.pauseBetween = pauseBetween;
                this.caller = caller;
                this.predicate = predicate;
                this.random = random;
                this.deck = deck;
                this.discard = discard;
                this.addEffectStacks = addEffectStacks;
            }
            public override IEnumerator Run()
            {
                if (count <= 0 || !character.drawContainer || !character.handContainer || !character.discardContainer)
                {
                    yield break;
                }

                Events.InvokeCardDraw(count);
                while (count > 0)
                {
                    yield return Sequences.Wait(pauseBetween);  // Wait

                    List<Entity> cards = [];
                    if (deck)
                        cards = cards.Concat(character.drawContainer.entities).ToList();
                    if (discard)
                        cards = cards.Concat(character.discardContainer.entities).ToList();

                    if (random)
                        cards = InPettyRandomOrder(cards).ToList();

                    Entity foundCard = null;
                    for (int i = cards.Count - 1; i >= 0; i--)
                    {
                        if (predicate.Invoke(cards[i].data))
                        {
                            foundCard = cards[i];  // Get the top card of the potentially shuffled pile
                            break;
                        }
                    }

                    if (!foundCard)  // If that didn't work, cry about it cause shuffling only works on an empty pile also that'd be weird
                    {
                        Events.InvokeCardDrawEnd();
                        Debug.Log("[WTG] No tutor targets :(");
                    }

                    if ((bool)foundCard)  // Move the new card to hand
                    {
                        Debug.Log("[WTG] Card found is " + foundCard.name + ". Top card in deck is " + character.drawContainer.GetTop().name);
                        yield return Sequences.CardMove(foundCard, new CardContainer[1] { character.handContainer });
                        character.handContainer.TweenChildPositions();

                        foreach (var stack in addEffectStacks)
                            ActionQueue.Stack(new ActionApplyStatus(foundCard, null, stack.data, stack.count));

                        foundCard.display.promptUpdateDescription = true;
                        foundCard.PromptUpdate();

                        ActionQueue.Stack(new ActionSequence(foundCard.UpdateTraits()) { note = $"[{foundCard}] Update Traits" });
                    }

                    count--;
                }

                Events.InvokeCardDrawEnd();
                ActionQueue.Stack(new ActionRevealAll(character.handContainer));
            }
            private static IOrderedEnumerable<T> InPettyRandomOrder<T>(IEnumerable<T> source)
            {
                return source.OrderBy(_ => Dead.PettyRandom.Range(0f, 1f));
            }

        }

        // Tutor code ripped directly from AA. Thank you Abigail! 
        public class StatusEffectInstantTutor : StatusEffectInstant
        {
            public enum CardSource
            {
                Draw,
                Discard,
                Custom // Use Summon Copy
            }

            public CardSource source = CardSource.Draw;
            public string[] customCardList;
            public int amount;
            public StatusEffectInstantSummon summonCopy;
            public CardData.StatusEffectStacks[] addEffectStacks;
            public LocalizedString title;
            public bool addToDeck;

            private CardContainer _cardContainer;
            private GameObject _gameObject;
            private GameObject _objectGroup;

            private Entity _selected;
            private CardPocketSequence _sequence;
            public Predicate<CardData> predicate;

            public override IEnumerator Process()
            {
                _sequence = FindObjectOfType<CardPocketSequence>(true);
                var cc = (CardControllerSelectCard)_sequence.cardController;
                cc.pressEvent.AddListener(ChooseCard);
                cc.canPress = true;
                var container = GetCardContainer();

                if (source == CardSource.Custom)
                    foreach (var entity in container)
                        yield return (entity.display as Card)!.UpdateData();

                CinemaBarSystem.In();
                CinemaBarSystem.SetSortingLayer("UI2");
                if (!title.IsEmpty)
                    CinemaBarSystem.Top.SetPrompt(title.GetLocalizedString(), "Select");
                _sequence.AddCards(container);
                yield return _sequence.Run();

                if (_selected != null) //Card Selected
                {
                    Events.InvokeCardDraw(1);
                    yield return Sequences.CardMove(_selected, [References.Player.handContainer]);
                    References.Player.handContainer.TweenChildPositions();
                    Events.InvokeCardDrawEnd();
                    _selected.flipper.FlipUp();
                    yield return Sequences.WaitForAnimationEnd(_selected);
                    yield return new ActionRunEnableEvent(_selected).Run();
                    _selected.display.hover.enabled = true;

                    foreach (var stack in addEffectStacks)
                        ActionQueue.Stack(new ActionApplyStatus(_selected, null, stack.data, stack.count));

                    _selected.display.promptUpdateDescription = true;
                    _selected.PromptUpdate();

                    ActionQueue.Stack(new ActionSequence(_selected.UpdateTraits()) { note = $"[{_selected}] Update Traits" });

                    _selected = null;
                }

                _cardContainer?.ClearAndDestroyAllImmediately();

                cc.canPress = false;
                cc.pressEvent.RemoveListener(ChooseCard);

                CinemaBarSystem.Clear();
                CinemaBarSystem.Out();

                yield return Remove();
            }

            private void ChooseCard(Entity entity)
            {
                _selected = entity;
                _sequence.promptEnd = true;

                if (!summonCopy)
                    return;

                var cardData = _selected.data;
                summonCopy.targetSummon.summonCard = cardData;
                summonCopy.withEffects = [.. addEffectStacks.Select(s => s.data)];
                ActionQueue.Stack(new ActionApplyStatus(target, target, summonCopy, count));

                AddToDeck(cardData);

                _selected = null;
            }

            private void AddToDeck(CardData cardData)
            {
                if (!addToDeck)
                    return;

                References.PlayerData.inventory.deck.Add(cardData);
                Events.InvokeEntityShowUnlocked(_selected);
            }

            private CardContainer GetCardContainer()
            {
                switch (source)
                {
                    case CardSource.Draw when amount == 0:
                        return References.Player.drawContainer;
                    case CardSource.Draw:
                        var drawContainer = References.Player.drawContainer;

                        _gameObject = new GameObject("SelectCard");
                        var rectangle = _gameObject.AddComponent<RectTransform>();
                        rectangle.sizeDelta = new Vector2(7, 2);

                        _cardContainer = CreateCardGrid(_objectGroup.transform, rectangle);

                        foreach (var entities in InPettyRandomOrder(drawContainer).Take(amount))
                        {
                            var cardData = TryGet<CardData>(entities._data.name).Clone();
                            var card = CardManager.Get(cardData, Battle.instance.playerCardController, References.Player,
                                true,
                                true);
                            _cardContainer.Add(card.entity);
                        }

                        _cardContainer.AssignController(Battle.instance.playerCardController);
                        return _cardContainer;
                    case CardSource.Discard:
                        return References.Player.discardContainer;
                    case CardSource.Custom:
                        _objectGroup = new GameObject("SelectCardRoutine");
                        _objectGroup.SetActive(false);
                        _objectGroup.transform.SetParent(GameObject.Find("Canvas/Padding/HUD/DeckpackLayout").transform.parent
                            .GetChild(0));
                        _objectGroup.transform.SetAsFirstSibling();

                        _gameObject = new GameObject("SelectCard");
                        var rect = _gameObject.AddComponent<RectTransform>();
                        rect.sizeDelta = new Vector2(7, 2);

                        _cardContainer = CreateCardGrid(_objectGroup.transform, rect);

                        FillCardContainer();

                        _cardContainer.AssignController(Battle.instance.playerCardController);

                        return _cardContainer;
                    default:
                        return null;
                }
            }

            private void FillCardContainer()
            {
                if (customCardList.Length <= 0)
                {
                    PredicateContainer();
                    return;
                }

                amount = amount == 0 ? customCardList.Length : amount;
                foreach (var cardName in InPettyRandomOrder(customCardList).Take(amount))
                {
                    var cardData = TryGet<CardData>(cardName).Clone();
                    var card = CardManager.Get(cardData, Battle.instance.playerCardController, References.Player,
                        true,
                        true);
                    _cardContainer.Add(card.entity);
                }
            }

            private void PredicateContainer()
            {
                var predicate1 = TryGet<StatusEffectInstantTutor>(name).predicate;
                if (predicate1 is null)
                    throw new ArgumentException("No predicate found");

                var cards = AddressableLoader.GetGroup<CardData>("CardData")
                    .Where(c => predicate1.Invoke(c) && c.mainSprite?.name != "Nothing")
                    .OrderBy(_ => PettyRandom.Range(0f, 1f)).ToList();
                if (amount != 0)
                    cards = cards.Take(amount).ToList();

                cards.Do(cardData =>
                {
                    var card = CardManager.Get(cardData.Clone(), Battle.instance.playerCardController, References.Player,
                        true,
                        true);
                    _cardContainer.Add(card.entity);
                });
            }

            // Random Order from Pokefrost StatusEffectChangeData
            private static IOrderedEnumerable<T> InPettyRandomOrder<T>(IEnumerable<T> source)
            {
                return source.OrderBy(_ => Dead.PettyRandom.Range(0f, 1f));
            }

            // Card Grid Code by Phan
            private static CardContainerGrid CreateCardGrid(Transform parent, RectTransform bounds = null)
            {
                return CreateCardGrid(parent, new Vector2(2.25f, 3.375f), 5, bounds);
            }

            private static CardContainerGrid CreateCardGrid(Transform parent, Vector2 cellSize, int columnCount,
                RectTransform bounds = null)
            {
                var gridObj = new GameObject("CardGrid", typeof(RectTransform), typeof(CardContainerGrid));
                gridObj.transform.SetParent(bounds ?? parent);
                gridObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                var grid = gridObj.GetComponent<CardContainerGrid>();
                grid.holder = grid.GetComponent<RectTransform>();
                grid.onAdd = new UnityEventEntity(); // Fix null reference
                grid.onAdd.AddListener(entity =>
                    entity.flipper.FlipUp()); // Flip up card when it's time (without waiting for others)
                grid.onRemove = new UnityEventEntity(); // Fix null reference

                grid.cellSize = cellSize;
                grid.columnCount = columnCount;

                AddScrollers(gridObj); // No click-and-drag. That needs Scroll View
                var scroller = gridObj.GetOrAdd<Scroller>();
                scroller.bounds = bounds; // Change scroller.bounds here if it only scrolls partially

                return grid;
            }

            /// <summary>
            ///     Generic way to make scrollable. Click-and-drag uses ScrollView
            /// </summary>
            /// <param name="parentObject"></param>
            private static void AddScrollers(GameObject parentObject)
            {
                var scroller = parentObject.GetOrAdd<Scroller>(); // Scroll with mouse
                parentObject.GetOrAdd<ScrollToNavigation>().scroller = scroller; // Scroll with controllers
                parentObject.GetOrAdd<TouchScroller>().scroller = scroller; // Scroll with touchscreen
            }
        }

        // Target Constraint is true only if there are 2 on board with the required features
        public class TargetConstraintIsFeatureOnBoard : TargetConstraint
        {
            public int requiredAmount;
            public bool allies;
            public bool enemies;
            public CardType cardType;
            public TraitData hasTrait;

            public override bool Check(Entity target)
            {
                int num = 0;
                if (allies)
                {
                    List<Entity> allies = target.GetAllies();
                    num += allies.Count((Entity e) => ((object)cardType == null || e.data.cardType == cardType) && (hasTrait == null || e.traits.Any(t => t.data.name.Equals(hasTrait.name))));
                }

                if (enemies)
                {
                    List<Entity> enemies = target.GetEnemies();
                    num += enemies.Count((Entity e) => ((object)cardType == null || e.data.cardType == cardType) && (hasTrait == null || e.traits.Any(t => t.data.name.Equals(hasTrait.name))));
                }
                Debug.Log("[WildfrostTheGathering] TargetConstraintIsFeatureOnBoard was called for " + target.name + ". Num was " + num);
                return not ^ (num >= requiredAmount);
            }

            public override bool Check(CardData targetData)
            {
                Debug.Log("[WildfrostTheGathering] TargetConstraintIsFeatureOnBoard was called for cardData on " + targetData.name + " :shrug:");
                return false;
            }
        }

        // Target Constraint is true only if the target is a leader (shut up I know I could use the miniboss check I even do that for treasure)
        public class TargetConstraintIsLeader : TargetConstraint
        {
            public override bool Check(Entity target)
            {
                if (target.data.cardType.name.Equals("Leader"))
                {
                    return !not;
                }
                return not;
            }
        }

        // If the target has less than a certain amount of health (shut UP I know I can not a health more than ;-;)
        public class TargetConstraintHealthLessThan : TargetConstraint
        {
            public int value;
            public bool allowNegative = false;
            public override bool Check(Entity target)
            {
                if (target.hp.current >= value || (!allowNegative && target.hp.current < 1))
                {
                    return not;
                }

                return !not;
            }

            public override bool Check(CardData targetData)
            {
                if (targetData.hp >= value || (!allowNegative && targetData.hp < 1))
                {
                    return not;
                }

                return !not;
            }
        }

        // If the target has more than a certain counter
        public class TargetConstraintCounterMoreThan : TargetConstraint
        {
            public int value;
            public override bool Check(Entity target)
            {
                Debug.Log("[WTG] " + target.name);
                if (target.counter.current <= value)
                {
                    return not;
                }

                return !not;
            }

            public override bool Check(CardData targetData)
            {
                Debug.Log("[WTG] " + targetData.name);
                if (targetData.counter <= value)
                {
                    return not;
                }

                return !not;
            }
        }

        // If the target is at their maximum counter
        public class TargetConstraintCounterAtMax : TargetConstraint
        {
            public override bool Check(Entity target)
            {
                Debug.Log("[WTG] " + target.name + ". Counter is at " + target.counter.current + ". and max is at " + target.counter.max);
                if (target.counter.current < target.counter.max)
                {
                    return not;
                }

                return !not;
            }

            public override bool Check(CardData targetData)
            {
                Debug.Log("[WTG] " + targetData.name);
                return !not;
            }
        }

        // Eternal implementation. Thank you semmie!
        [HarmonyPatch(typeof(InjurySystem), nameof(InjurySystem.CanInjure), new Type[] { typeof(CardData) })]
        class PatchNoInjury
        {
            static bool Prefix(ref CardData cardData, ref bool __result)
            {
                foreach (CardData.TraitStacks trait in cardData.traits)
                {
                    if (trait.data.name.Equals($"{guid}.Eternal"))
                    {
                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }

        // Laboratory maniac adding an event to OnShuffle
        public static event UnityAction OnShuffle;
        public static void InvokeOnShuffle()
        {
            Debug.Log("[WTG] On shuffle Invoked!");
            OnShuffle?.Invoke();
        }

        [HarmonyPatch(typeof(Sequences), nameof(Sequences.ShuffleTo))]
        class PatchOnShuffle
        {
            static bool Prefix(CardContainer fromContainer, CardContainer toContainer, float delayBetween)
            {
                if (!toContainer || !toContainer.Empty || !fromContainer || fromContainer.Empty)
                {
                    return true;
                }
                if (fromContainer.Equals(References.Player.discardContainer) && toContainer.Equals(References.Player.drawContainer))
                {
                    InvokeOnShuffle();
                }
                return true;
            }
        }
        
        // TODO: Trample ignores clunkers (test more prob) (then add a check for clunkers)
        // TODO: Balance Manaform Hellkite
        // TODO: Manaform hellkite when in hand
        // TODO: Make Delayed Blast Fireball not clear all spice after played
        // TODO: Make beatable ascendeds
        // TODO: Make Lathliss not summon if precontainer is null. That happens in ascended fg
        // TODO: Make peppernut charm work for custom effects
        // TODO: Make charms that care about target mode recognize the custom ones (gnome, pom so far)
        // TODO: make zoomlin sound not play twice for treasures added to hand (if possible)
        // TODO: Make Eyedata for the ascendeds
        // TODO: Make Manaform Hellkite + Guttersnipe not trigger on each hit of frenzy
        // TODO: Make Manaform Hellkite count current attack
        // TODO: Make the enabled exist better for apply to all deck (If possible. Double check with miya to see how)
        // TODO: eq dragon counter flickers when flying allies in hand are discarded

        private void CreateModAssets()
        {

            {  // Decks

                string[] dragonDeckLeaders = new string[] { "miirymSentinelWyrmLeader", "theUrDragonLeader", "lathlissDragonQueenLeader",
                                                            "oldGnawboneLeader", "ganaxAstralHunterLeader", "drakusethMawOfFlamesLeader" };

                string[] dragonDeckItems = new string[] {   "treasure", "dragonsFire", "delayedBlastFireball", "spitFlame", "draconicLore",
                                                            "loftyDenial", "spellSwindle", "fireball", "sharedAnimosity", "bottleCapBlast",
                                                            "explosiveVegetation", "bootleggersStash", "temptingContract", "moxJasper",
                                                            "ritesOfFlourishing", "gravitationalShift", "revelInRiches", "dragonsHoard",
                                                            "windcragSiege", "resourcefulDefense", "firesOfYavimaya"};

                string[] dragonDeckCompanions = new string[] {   "goldspanDragon", "ancientCopperDragon", "atsushiBlazingSky",
                                                            "utvaraHellkite", "dragonlordsServant", "terrorOfThePeaks",
                                                            "captainLanneryStorm", "academyManufactor", "glorybringer",
                                                            "earthquakeDragon", "ojutaiSoulOfWinter", "professionalFaceBreaker",
                                                            "shivanDragon", "manaformHellkite", "voraciousHydra"};

                string[] dragonDeckCharms = new string[] {  "CardUpgradeSpice", "CardUpgradeEffigy",
                                                            "CardUpgradeMime", "CardUpgradeShellBecomesSpice"};

                string[] genericItems = new string[] {  "lightningBolt", "giantGrowth", "darkRitual", "ancestralRecall",
                                                        "healingSalve", "disdainfulStroke", "counterspell", "rampantGrowth",
                                                        "timeWalk", "anOfferYouCantRefuse", "moltenDuplication", "skullclamp",
                                                        "toxicDeluge", "fog", "insult", "towerDefense", "annieJoinsUp",
                                                        "maddeningCacophony", "takeTheBait", "recklessCharge", "abrade",
                                                        "slipOutTheBack", "borosCharm", "beastWithin", "naturalUnity",
                                                        "powerPlay", "damnablePact", "assassinate", "advantageousProclamation",
                                                        "doomsday", "throesOfChaos"};

                string[] genericCompanions = new string[] { "fearOfSleepParalysis", "mulldrifter", "nulldrifter", "deadeyeNavigator",
                                                            "beastWhisperer", "warrenSoultrader", "laboratoryManiac", "springheartNantuko",
                                                            "deathsShadow", "hydraOmnivore" };

                string[] genericCharms = new string[] { };

                assets.Add(TribeCopy("Basic", "Dragon")  // Snowdweller = "Basic", Shademancer = "Magic", Clunkmaster = "Clunk"
                    .WithFlag("Images/dragon-deck-flag-sleeve.png")  // Loads your DrawFlag.png in your Images subfolder of your mod folder
                    .WithSelectSfxEvent(FMODUnity.RuntimeManager.PathToEventReference("event:/sfx/inventory/backpack_opening"))  // The above line may need one of the FMOD references
                    .SubscribeToAfterAllBuildEvent<ClassData>(data =>
                    {
                        data.leaders = DataList<CardData>(dragonDeckLeaders);
                        Inventory inventory = ScriptableObject.CreateInstance<Inventory>();
                        inventory.deck.list = DataList<CardData>("shock", "shock", "shock", "shock", "cancel", "cancel", "swiftfootBoots", "lotusPetal", "treasure").ToList();
                        data.startingInventory = inventory;

                        RewardPool dragonUnitPool = CreateRewardPool("DragonUnitPool", "Units", DataList<CardData>(dragonDeckCompanions));
                        RewardPool dragonItemPool = CreateRewardPool("DragonItemPool", "Items", DataList<CardData>(dragonDeckItems));
                        RewardPool dragonCharmPool = CreateRewardPool("DragonCharmPool", "Charms", DataList<CardUpgradeData>(dragonDeckCharms));

                        RewardPool genericUnitPool = CreateRewardPool("GenericUnitPool", "Units", DataList<CardData>(genericCompanions));
                        RewardPool genericItemPool = CreateRewardPool("GenericItemPool", "Items", DataList<CardData>(genericItems));
                        // RewardPool genericCharmPool = CreateRewardPool("GenericCharmPool", "Charms", DataList<CardUpgradeData>(genericCharms));
                        data.rewardPools = new RewardPool[]
                        {
                            dragonUnitPool,
                            dragonItemPool,
                            dragonCharmPool,
                            genericUnitPool,
                            genericItemPool,
                            // Extensions.GetRewardPool("GeneralUnitPool"),
                            // Extensions.GetRewardPool("GeneralItemPool"),
                            Extensions.GetRewardPool("GeneralCharmPool"),
                            Extensions.GetRewardPool("GeneralModifierPool"),  // Sun bells. otherwise it crashes ;-;
                            // Extensions.GetRewardPool("SnowUnitPool"),  // The snow pools are not Snowdwellers, there are general snow units/cards/charms
                            // Extensions.GetRewardPool("SnowItemPool"),
                            // Extensions.GetRewardPool("SnowCharmPool"),
                        };
                    })
                    );

                {  // Leaders

                    // The Ur-Dragon
                    assets.Add(
                    new CardDataBuilder(this)
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
                            new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                            new CardData.TraitStacks(Get<TraitData>("Draw"), 2),  // 2
                        };
                        data.startWithEffects = new CardData.StatusEffectStacks[]
                        {
                            SStack("While Active Zoomlin When Drawn To Flying Allies In Hand", 1)
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
                        new CardDataBuilder(this)
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
                                new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                            };
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("On Card Played Summon Copy Of Ally Ahead With X Health And Fragile", 1)
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
                        new CardDataBuilder(this)
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
                                new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                            };
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("When Flying Ally Deployed Summon Big Dragon Token With X Health", 4)
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
                        new CardDataBuilder(this)
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
                                new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                            };
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("On Flying Card Played Add Treasure To Hand", 1)
                            };
                            data.createScripts = new CardScript[]
                            {
                                GiveUpgrade(),
                                AddRandomHealth(0,1),
                                AddRandomDamage(-1,1),
                                AddRandomCounter(0,1)
                            };
                            data.greetMessages = new string[1] { "<i>The ancient green dragon <b>Claugiyliamatar</b> is often seen with a mangled corpse dangling from her mouth.</i>" };
                        })
                        );

                    // Ganax, Astral Hunter 
                    assets.Add(
                        new CardDataBuilder(this)
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
                                new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                            };
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("When Flying Ally Deployed Add Treasure To Hand", 1)  // 1-2
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
                        new CardDataBuilder(this)
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
                                new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                            };
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("On Card Played Deal X Damage To Frontmost Enemy Twice", 3)
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

                }  // Leaders
            }  // Decks

            Effects.Load(assets, this);
            Pets.Load(assets, this);
            Keywords.Load(assets, this);
            Traits.Load(assets, this);

            Generic.Clunkers.Load(assets, this);
            Generic.Companions.Load(assets, this);
            Generic.Items.Load(assets, this);

            DragonDeck.Clunkers.Load(assets, this);
            DragonDeck.Companions.Load(assets, this);
            DragonDeck.Items.Load(assets, this);
            
            preLoaded = true;
        }
        public override void Load()
        {
            if (!preLoaded) { CreateModAssets(); }  // preLoaded makes sure that the builders are not made again on the 2nd load.
            base.Load();  // Actual loading and adding assets.
            GameMode gameMode = TryGet<GameMode>("GameModeNormal");  // GameModeNormal is the standard game mode. 
            gameMode.classes = gameMode.classes.Append(TryGet<ClassData>("Dragon")).ToArray();
            Events.OnEntityCreated += FixImage;

            var uiText = LocalizationHelper.GetCollection("UI Text", SystemLanguage.English);

            uiText.SetString("Instant Tutor Card From Deck To Hand", "Add any card from your Draw Pocket to your hand");  // Demonic Tutor
            uiText.SetString("Instant Destroy Card In Deck", "Destroy 1 card in your deck");  // Advantageous Proclamation
        }

        // Show a tribe flag for leaders when selected
        [HarmonyPatch(typeof(References), nameof(References.Classes), MethodType.Getter)]
        static class FixClassesGetter
        {
            static void Postfix(ref ClassData[] __result) => __result = AddressableLoader.GetGroup<ClassData>("ClassData").ToArray();
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
                    if (pool == null) { continue; }
                    ;  // Skip null pools

                    pool.list.RemoveAllWhere((item) => item == null || item.ModAdded == this);  // Find and remove everything that needs to be removed.
                }
            }
        }
        public override void Unload()
        {
            base.Unload();

            TraitData aimless = TryGet<TraitData>("Aimless");
            TraitData barrage = TryGet<TraitData>("Barrage");
            TraitData longshot = TryGet<TraitData>("Longshot");
            aimless.overrides = RemoveNulls(aimless.overrides);
            barrage.overrides = RemoveNulls(barrage.overrides);
            longshot.overrides = RemoveNulls(longshot.overrides);  // Remove the overrides from flying/fireball
            GameMode gameMode = TryGet<GameMode>("GameModeNormal");
            gameMode.classes = RemoveNulls(gameMode.classes);  // Without this, a non-restarted game would crash on tribe selection
            UnloadFromClasses();
            Events.OnEntityCreated -= FixImage;
        }

        // Taken from Absent Avalanche. Likely Hopeful's code tho from the tutorial. Check them both out anyways they're cool people :3
        public static T TryGet<T>(string datafileName) where T : DataFile
        {
            T dataFile;
            if (typeof(StatusEffectData).IsAssignableFrom(typeof(T)))
                dataFile = Instance.Get<StatusEffectData>(datafileName) as T;
            else if (typeof(KeywordData).IsAssignableFrom(typeof(T)))
            {
                dataFile = Instance.Get<T>(datafileName.ToLower());
            }
            else
                dataFile = Instance.Get<T>(datafileName);

            return dataFile ??
                   throw new Exception(
                       $"TryGet Error: Could not find a [{typeof(T).Name}] with the name [{datafileName}] or [{Extensions.PrefixGUID(datafileName, Instance)}]");
        }
        internal T[] RemoveNulls<T>(T[] data) where T : DataFile
        {
            List<T> list = data.ToList();
            list.RemoveAll(x => x == null || x.ModAdded == this);
            return list.ToArray();
        }

        // Helper methods/functions/data structures. Thank you Hopeful for all of these!
        private T[] DataList<T>(params string[] names) where T : DataFile => names.Select((s) => TryGet<T>(s)).ToArray();

        public CardData.StatusEffectStacks SStack(string name, int amount) => new CardData.StatusEffectStacks(TryGet<StatusEffectData>(name), amount);
        public CardData.TraitStacks TStack(string name, int amount) => new CardData.TraitStacks(TryGet<TraitData>(name), amount);
        public CardData.TraitStacks TStack(string name) => new CardData.TraitStacks(TryGet<TraitData>(name), 1);

        public StatusEffectDataBuilder StatusCopy(string oldName, string newName)
        {
            StatusEffectData data = TryGet<StatusEffectData>(oldName).InstantiateKeepName();
            data.name = GUID + "." + newName;
            data.targetConstraints = new TargetConstraint[0];
            StatusEffectDataBuilder builder = data.Edit<StatusEffectData, StatusEffectDataBuilder>();
            builder.Mod = this;
            return builder;
        }
        private CardDataBuilder CardCopy(string oldName, string newName) => DataCopy<CardData, CardDataBuilder>(oldName, newName);
        private ClassDataBuilder TribeCopy(string oldName, string newName) => DataCopy<ClassData, ClassDataBuilder>(oldName, newName);

        private T DataCopy<Y, T>(string oldName, string newName) where Y : DataFile where T : DataFileBuilder<Y, T>, new()
        {
            Y data = Get<Y>(oldName).InstantiateKeepName();
            data.name = GUID + "." + newName;
            T builder = data.Edit<Y, T>();
            builder.Mod = this;
            return builder;
        }

        // For creating leaders
        internal CardScript GiveUpgrade(string name = "Crown")  // Gives a crown by default
        {
            CardScriptGiveUpgrade script = ScriptableObject.CreateInstance<CardScriptGiveUpgrade>();  // This is the standard way of creating a ScriptableObject
            script.name = $"Give {name}";  // Name only appears in the Unity Inspector. It has no other relevance beyond that.
            script.upgradeData = TryGet<CardUpgradeData>(name);
            return script;
        }
        internal CardScript AddRandomHealth(int min, int max)  // Boost health by a random amount
        {
            CardScriptAddRandomHealth health = ScriptableObject.CreateInstance<CardScriptAddRandomHealth>();
            health.name = "Random Health";
            health.healthRange = new Vector2Int(min, max);
            return health;
        }
        internal CardScript AddRandomDamage(int min, int max)  // Boost damage by a ranom amount
        {
            CardScriptAddRandomDamage damage = ScriptableObject.CreateInstance<CardScriptAddRandomDamage>();
            damage.name = "Give Damage";
            damage.damageRange = new Vector2Int(min, max);
            return damage;
        }
        internal CardScript AddRandomCounter(int min, int max)  // Increase counter by a random amount
        {
            CardScriptAddRandomCounter counter = ScriptableObject.CreateInstance<CardScriptAddRandomCounter>();
            counter.name = "Give Counter";
            counter.counterRange = new Vector2Int(min, max);
            return counter;
        }
        internal CardScript AddRandomBoostEffects(int min, int max)  // Increase effects and trait by a random amount
        {
            CardScriptAddRandomBoost boost = ScriptableObject.CreateInstance<CardScriptAddRandomBoost>();
            boost.name = "Boost Effect";
            boost.boostRange = new Vector2Int(min, max);
            return boost;
        }

        // To not break leader sprites
        private void FixImage(Entity entity)
        {
            if (entity.display is Card card && !card.hasScriptableImage) //These cards should use the static image
            {
                card.mainImage.gameObject.SetActive(true);               //And this line turns them on
            }
        }

        // To add a reward pool
        private RewardPool CreateRewardPool(string name, string type, DataFile[] list)
        {
            RewardPool pool = ScriptableObject.CreateInstance<RewardPool>();
            pool.name = name;
            pool.type = type;  // The usual types are Units, Items, Charms, and Modifiers.
            pool.list = list.ToList();
            return pool;
        }

        // Credits to Hopeful for this AddAssets code.
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
