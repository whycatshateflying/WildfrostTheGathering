using AbsentAvalanche;
using AbsentAvalanche.Builders.Cards.Items;
using AbsentAvalanche.StatusEffectImplementations;
using Deadpan.Enums.Engine.Components.Modding;
//using FMOD;
using HarmonyLib;
using NaughtyAttributes.Test;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using static Names;
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
                    if (trait.data.name.Equals("whycats.wildfrost.wildfrostthegathering.Unplayable"))
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
            public bool whileActive = false;
            public int allowedTimes = 0;
            public int numTimesPlayed = 0;
            public bool hasAttack = false;
            public CardType allowedCardType;
            public bool countsSelf = false;
            public CardData[] allowedCards = Array.Empty<CardData>();
            public TraitData[] allowedTraits = Array.Empty<TraitData>();
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
                // Debug.Log("[WildfrostTheGathering] " + entity.name + " detected. " + numTimesPlayed + " Have been played this turn");
                if (whileActive && !Battle.IsOnBoard(target))
                {
                    // Debug.Log("[WildfrostTheGathering] ...but I'm not active yet");
                    return false;
                }
                if (allowedTimes > 0 && numTimesPlayed++ > allowedTimes)
                {
                    // Debug.Log("[WildfrostTheGathering] ...and that's too many times for me");
                    return false;
                }
                if (!target.enabled)
                {
                    // Debug.Log("[WildfrostTheGathering] ...but was not enabled");
                    return false;
                }
                if (!countsSelf && target == entity)
                {
                    // Debug.Log("[WildfrostTheGathering] ...but was myself (" + target.name + ")");
                    return false;
                }
                if ((object)allowedCardType != null && allowedCardType.name != entity.data.cardType.name)
                {
                    // Debug.Log("[WildfrostTheGathering] ...but was the wrong card type (" + allowedCardType.name + ") and (" + entity.data.cardType.name + ")");
                    return false;
                }
                if (hasAttack && !entity.data.hasAttack)
                {
                    // Debug.Log("[WildfrostTheGathering] ... but had no attack (" + entity.data.hasAttack + "), (" + entity.data.damage + ")");
                    return false;
                }

                IEnumerable<TraitData> source = entity.traits.Select((Entity.TraitStacks t) => t.data);
                List<TraitData> source2 = source.ToList();
                TraitData[] array = allowedTraits;
                if (array != null && array.Length > 0 && !source2.ToList().ContainsAny(allowedTraits))
                {
                    // Debug.Log("[WildfrostTheGathering] ...but didn't have the trait");
                    return false;
                }

                _hackyHit = new Hit(entity, null);
                CardData[] array2 = allowedCards;
                // Debug.Log("[WildfrostTheGathering] " + array2 + " || " + array2.Length + " || " + allowedCards.ToList().Any((CardData c) => c.name == entity.data.name));
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
                    foreach(Entity entity in GetTargets())
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

        // Thank you again Abigail for the very yoinkable code! :3
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
                int num = 0;
                int[] rowIndices = References.Battle.GetRowIndices(entity);
                if (inRow)
                {
                    return InRow(entity, rowIndices);
                }

                if (allies)
                {
                    List<Entity> allies = entity.GetAllies();
                    num += allies.Count((Entity e) => (cardType is null || e.data.cardType == cardType) && (hasTrait is null || e.traits.Any(t => t.data.name.Equals(hasTrait.name))));
                }

                if (enemies)
                {
                    List<Entity> enemies = entity.GetEnemies();
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
                        num += allies.Count((Entity e) => (cardType is null || e.data.cardType == cardType) && (hasTrait is null || e.traits.Any(t => t.data.name.Equals(hasTrait.name))));
                    }

                    if (enemies)
                    {
                        List<Entity> enemies = entity.GetEnemiesInRow(rowIndex);
                        num += enemies.Count((Entity e) => (cardType is null || e.data.cardType == cardType) && (hasTrait is null || e.traits.Any(t => t.data.name.Equals(hasTrait.name))));
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

        // Cause while active ally behind is broken otherwise. Thanks While Active
        public class StatusEffectWhileActiveXUpdatesOnOtherMove : StatusEffectWhileActiveX
        {
            public override IEnumerator CardMove(Entity entity)
            {
                // Literally just the old code but add a call to FindContainers
                if (target == entity)
                {
                    yield return base.CardMove(entity);  // For code tidiness
                }
                else
                {
                    if (!active)
                    {
                        yield break;
                    }

                    if (AffectsSlot())
                    {
                        FindContainersClone();
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
                }
            }

            public void FindContainersClone()
            {
                // containersToAffect.Clear();  // This breaks it for some reason... Other than that, no changes
                Character opponent = Battle.GetOpponent(target.owner);
                int[] rowIndices = Battle.instance.GetRowIndices(target);
                affectsSelf = AppliesTo(ApplyToFlags.Self);
                if (AppliesTo(ApplyToFlags.Allies))
                {
                    containersToAffect.AddRange(Battle.instance.GetRows(target.owner));
                }
                else if (AppliesTo(ApplyToFlags.AlliesInRow))
                {
                    int[] array = rowIndices;
                    foreach (int rowIndex in array)
                    {
                        containersToAffect.Add(Battle.instance.GetRow(target.owner, rowIndex));
                    }
                }
                else
                {
                    if (AppliesTo(ApplyToFlags.FrontAlly))
                    {
                        int[] array = rowIndices;
                        foreach (int rowIndex2 in array)
                        {
                            if (Battle.instance.GetRow(target.owner, rowIndex2) is CardSlotLane cardSlotLane)
                            {
                                CardSlot value = cardSlotLane.slots.FirstOrDefault((CardSlot a) => !a.Empty);
                                containersToAffect.AddIfNotNull(value);
                            }
                        }
                    }

                    if (AppliesTo(ApplyToFlags.BackAlly))
                    {
                        int[] array = rowIndices;
                        foreach (int rowIndex3 in array)
                        {
                            if (Battle.instance.GetRow(target.owner, rowIndex3) is CardSlotLane cardSlotLane2)
                            {
                                CardSlot value2 = cardSlotLane2.slots.LastOrDefault((CardSlot a) => !a.Empty);
                                containersToAffect.AddIfNotNull(value2);
                            }
                        }
                    }

                    if (AppliesTo(ApplyToFlags.AllyInFrontOf))
                    {
                        int[] array = rowIndices;
                        foreach (int rowIndex4 in array)
                        {
                            if (!(Battle.instance.GetRow(target.owner, rowIndex4) is CardSlotLane cardSlotLane3))
                            {
                                continue;
                            }

                            for (int num = cardSlotLane3.IndexOf(target) - 1; num >= 0; num--)
                            {
                                CardSlot cardSlot = cardSlotLane3.slots[num];
                                if (!cardSlot.Empty)
                                {
                                    containersToAffect.Add(cardSlot);
                                    break;
                                }
                            }
                        }
                    }

                    if (AppliesTo(ApplyToFlags.AllyBehind))
                    {
                        int[] array = rowIndices;
                        foreach (int rowIndex5 in array)
                        {
                            if (!(Battle.instance.GetRow(target.owner, rowIndex5) is CardSlotLane cardSlotLane4))
                            {
                                continue;
                            }

                            for (int num2 = cardSlotLane4.IndexOf(target) + 1; num2 < cardSlotLane4.slots.Count; num2++)
                            {
                                CardSlot cardSlot2 = cardSlotLane4.slots[num2];
                                if (!cardSlot2.Empty)
                                {
                                    containersToAffect.Add(cardSlot2);
                                    break;
                                }
                            }
                        }
                    }
                }

                if (AppliesTo(ApplyToFlags.Enemies))
                {
                    containersToAffect.AddRange(Battle.instance.GetRows(opponent));
                }
                else if (AppliesTo(ApplyToFlags.EnemiesInRow))
                {
                    int[] array = rowIndices;
                    foreach (int rowIndex6 in array)
                    {
                        containersToAffect.Add(Battle.instance.GetRow(opponent, rowIndex6));
                    }
                }

                if (AppliesTo(ApplyToFlags.Hand) && (bool)References.Player)
                {
                    containersToAffect.AddIfNotNull(References.Player.handContainer);
                }

                if (AppliesTo(ApplyToFlags.EnemyHand) && (bool)opponent)
                {
                    containersToAffect.AddIfNotNull(opponent.handContainer);
                }
            }
        }

        // If this is applied to a card, then it'll update whenever something that might happen does happen
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

        // Remove Spice from self to not be confusing
        public class StatusEffectInstantRemoveSpecificEffect : StatusEffectInstant
        {
            public string effectToClean;
            public override IEnumerator Process()
            {
                int num = target.statusEffects.Count;
                for (int i = num - 1; i >= 0; i--)
                {
                    StatusEffectData statusEffectData = target.statusEffects[i];
                    Debug.Log("[WildfrostTheGathering] Found " + statusEffectData.name + " on " + target.name);
                    if (statusEffectData.name.Equals(effectToClean))
                    {
                        yield return statusEffectData.Remove();
                    }
                }

                target.PromptUpdate();
                yield return base.Process();
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
                return (num >= requiredAmount);
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

        // If the target has less than a certain amount of health (shut up I know I could use countermorethan)
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

        // Eternal implementation. Thank you semmie!
        [HarmonyPatch(typeof(InjurySystem), nameof(InjurySystem.CanInjure), new Type[] {typeof(CardData)})]
        class PatchNoInjury
        {
            static bool Prefix(ref CardData cardData, ref bool __result)
            {
                foreach (CardData.TraitStacks trait in cardData.traits)
                {
                    if (trait.data.name.Equals("whycats.wildfrost.wildfrostthegathering.Eternal"))
                    {
                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }

        // TODO: Make flying and other targeting modes override each other visually
        // TODO: make zoomlin sound not play twice for treasures added to hand (if possible)
        // TODO: Make Delayed Blast Fireball not clear all spice after played (if possible)
        // TODO: Make beatable ascendeds
        // TODO: Make Dragonlord's Servant stackable for the funnies
        // TODO: Make mox jasper function when a card dies and moves up
        // TODO: Make Manaform Hellkite not trigger on each hit of frenzy
        private void CreateModAssets()
        {

            {  // Decks
                assets.Add(TribeCopy("Basic", "Dragon")  // Snowdweller = "Basic", Shademancer = "Magic", Clunkmaster = "Clunk"
                    .WithFlag("Images/dragon-deck-flag-sleeve.png")  // Loads your DrawFlag.png in your Images subfolder of your mod folder
                    .WithSelectSfxEvent(FMODUnity.RuntimeManager.PathToEventReference("event:/sfx/inventory/backpack_opening"))  // The above line may need one of the FMOD references
                    .SubscribeToAfterAllBuildEvent<ClassData>(data =>
                    {
                        string[] dragonDeckLeaders = new string[] { "miirymSentinelWyrmLeader", "theUrDragonLeader", "lathlissDragonQueenLeader",
                                                                    "oldGnawboneLeader", "ganaxAstralHunterLeader", "drakusethMawOfFlamesLeader" };
                        data.leaders = DataList<CardData>(dragonDeckLeaders);
                        Inventory inventory = ScriptableObject.CreateInstance<Inventory>();
                        inventory.deck.list = DataList<CardData>("shock", "shock", "shock", "shock", "cancel", "cancel", "swiftfootBoots", "lotusPetal", "treasure").ToList();
                        data.startingInventory = inventory;

                        RewardPool unitPool = CreateRewardPool("DragonUnitPool", "Units", DataList<CardData>(
                            "goldspanDragon", "ancientCopperDragon", "atsushiBlazingSky",
                            "utvaraHellkite", "dragonlordsServant", "terrorOfThePeaks",
                            "captainLanneryStorm", "academyManufactor", "glorybringer",
                            "earthquakeDragon", "ojutaiSoulOfWinter", "professionalFaceBreaker",
                            "shivanDragon", "manaformHellkite", "voraciousHydra"));

                        RewardPool itemPool = CreateRewardPool("DragonItemPool", "Items", DataList<CardData>(
                            "treasure", "dragonsFire", "delayedBlastFireball", "spitFlame", "draconicLore",
                            "loftyDenial", "spellSwindle", "fireball", "sharedAnimosity", "bottleCapBlast",
                            "explosiveVegetation", "bootleggersStash", "temptingContract", "moxJasper",
                            "ritesOfFlourishing", "gravitationalShift", "revelInRiches", "dragonsHoard",
                            "windcragSiege"));

                        RewardPool charmPool = CreateRewardPool("DragonCharmPool", "Charms", DataList<CardUpgradeData>(
                            "CardUpgradeSpice", "CardUpgradeEffigy",
                            "CardUpgradeMime", "CardUpgradeShellBecomesSpice"));

                        data.rewardPools = new RewardPool[]
                        {
                            unitPool,
                            itemPool,
                            charmPool,
                            Extensions.GetRewardPool("GeneralUnitPool"),
                            Extensions.GetRewardPool("GeneralItemPool"),
                            Extensions.GetRewardPool("GeneralCharmPool"),
                            Extensions.GetRewardPool("GeneralModifierPool"),
                            Extensions.GetRewardPool("SnowUnitPool"),  // The snow pools are not Snowdwellers, there are general snow units/cards/charms
                            Extensions.GetRewardPool("SnowItemPool"),
                            Extensions.GetRewardPool("SnowCharmPool"),
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
                    .SetStats(9, 5, 6)  // 8-10, 5-6, 6-7
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
                            AddRandomCounter(0, 1),
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

                    // Old Gnawbone (no art!)
                    assets.Add(
                        new CardDataBuilder(this)
                        .CreateUnit("oldGnawboneLeader", "Old Gnawbone", idleAnim: "FloatSquishAnimationProfile")
                        .WithCardType("Leader")
                        .SetSprites("dragon-baby.png", "companion-bg.png")
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

                    // Ganax, Astral Hunter (no art!)
                    assets.Add(
                        new CardDataBuilder(this)
                        .CreateUnit("ganaxAstralHunterLeader", "Ganax, Astral Hunter", idleAnim: "FloatAnimationProfile")
                        .WithCardType("Leader")
                        .SetSprites("dragon-baby.png", "companion-bg.png")
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

                    // Drakuseth, Maw of Flames (no art!)
                    assets.Add(
                        new CardDataBuilder(this)
                        .CreateUnit("drakusethMawOfFlamesLeader", "Drakuseth, Maw of Flames", idleAnim: "ShakeAnimationProfile")
                        .WithCardType("Leader")
                        .SetSprites("dragon-baby.png", "companion-bg.png")
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

            {  // Generic Stuff

                {  // Effects
                    // Draw cards on played (not boostable)
                    assets.Add(new StatusEffectDataBuilder(this)
                        .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Draw Cards (Not Boostable)")
                        .WithText("Draw {a} cards")
                        .WithStackable(true)
                        .WithCanBeBoosted(false)
                        .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                        {
                            data.effectToApply = TryGet<StatusEffectInstantDraw>("Instant Draw");
                            data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        })
                        );

                    // Disdainful Stroke: Hits all enemies with X or Greater Counter
                    assets.Add(new StatusEffectDataBuilder(this)
                        .Create<StatusEffectChangeTargetMode>("Hit All Enemies With 4 Or Greater Counter")
                        .WithText("Hits all enemies with 4 or greater <keyword=counter>")
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
                                        tccmt.value = 3;
                                    })
                                };
                            });
                        })
                        );

                    // Rampant Growth Apply Noomlin to a random card in hand on card played
                    assets.Add(new StatusEffectDataBuilder(this)
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
                                    tcht.trait = TryGet<TraitData>("Noomlin");
                                    tcht.ignoreSilenced = false;
                                }),
                                new Scriptable<TargetConstraintHasStatus>(tchs =>
                                {
                                    tchs.not = true;
                                    tchs.status = TryGet<StatusEffectFreeAction>("Free Action");
                                }),
                            };
                            data.effectToApply = TryGet<StatusEffectTemporaryTrait>("Temporary Noomlin");
                            data.applyToFlags = StatusEffectApplyX.ApplyToFlags.RandomCardInHand;
                            data.doPing = false;
                            data.targetMustBeAlive = false;
                        })
                        );

                    // Rampant Growth: Funny temporary Noomlin
                    assets.Add(new StatusEffectDataBuilder(this)
                        .Create<StatusEffectSafeTemporaryTrait>("Safe Temporary Noomlin")
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
                                new Scriptable<TargetConstraintHasStatus>(tchs =>
                                {
                                    tchs.not = true;
                                    tchs.status = TryGet<StatusEffectFreeAction>("Free Action");
                                }),
                            };
                            data.trait = TryGet<TraitData>("Noomlin");
                        })
                        );

                    // Time walk: Apply Snow to all enemies
                    assets.Add(new StatusEffectDataBuilder(this)
                        .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Apply Snow To All Enemies")
                        .WithText("Apply <{a}><keyword=snow> to all enemies")
                        .WithStackable(true)
                        .WithCanBeBoosted(true)
                        .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                        {
                            data.effectToApply = TryGet<StatusEffectSnow>("Snow");
                            data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Enemies;
                        })
                        );
                }  // Effects

                {  // Items

                    // Lightning Bolt (no art!)
                    assets.Add(new CardDataBuilder(this)
                        .CreateItem("lightningBolt", "Lightning Bolt", idleAnim:"ShakeAnimationProfile")
                        .WithFlavour("The sparkmage shrieked, calling on the rage of the storms of his youth. To his surprise, the sky responded with a fierce energy he’d never thought to see again.")
                        .SetDamage(3)
                        .SetSprites("placeholder-item.png", "item-bg.png")
                        .WithValue(10)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.traits = new List<CardData.TraitStacks>
                            {
                                TStack("Noomlin", 1),
                            };
                        })
                        );

                    // Giant Growth (no art!)
                    assets.Add(new CardDataBuilder(this)
                        .CreateItem("giantGrowth", "Giant Growth", idleAnim: "PulseAnimationProfile")
                        .SetDamage(null)
                        .WithFlavour("\"Only the most effective tactics stand the test of time.\"\n<b>—Gamelen, Citanul elder</b>")
                        .SetSprites("placeholder-item.png", "item-bg.png")
                        .WithValue(10)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.attackEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Spice", 3),
                                SStack("Shell", 3),
                            };
                        })
                        );

                    // Dark Ritual (no art!)
                    assets.Add(
                        new CardDataBuilder(this)
                        .CreateItem("darkRitual", "Dark Ritual", idleAnim: "Heartbeat2AnimationProfile")
                        .SetSprites("placeholder-item.png", "item-bg.png")
                        .SetDamage(null)
                        .WithCardType("Item")
                        .WithFlavour("\"From void evolved <b>Phyrexia</b>. Great <b>Yawgmoth, Father of Machines</b>, saw its perfection. Thus the <b>Grand Evolution</b> began.\"\n—Phyrexian Scriptures")
                        .WithValue(35)  // Base price in shop: +-6
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.attackEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Instant Gain Zoomlin (To Card In Hand)", 1),
                            };

                            data.traits = new List<CardData.TraitStacks>(2)
                            {
                                TStack("Zoomlin", 1),
                            };
                            data.textInsert = "<keyword=zoomlin>";
                            data.canPlayOnBoard = false;
                            data.canPlayOnHand = true;
                            data.uses = 1;
                        })
                        );

                    // Ancestral Recall (no art!)
                    assets.Add(new CardDataBuilder(this)
                        .CreateItem("ancestralRecall", "Ancestral Recall", idleAnim: "FloatAnimationProfile")
                        .SetDamage(null)
                        .WithFlavour("Dwell longest on the thoughts that shine brightest")
                        .SetSprites("placeholder-item.png", "item-bg.png")
                        .WithValue(10)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("On Card Played Draw Cards (Not Boostable)", 3),
                            };
                            data.needsTarget = false;
                        })
                        );

                    // Healing Salve (no art!)
                    assets.Add(new CardDataBuilder(this)
                        .CreateItem("healingSalve", "Healing Salve", idleAnim: "ShakeAnimationProfile")
                        .WithFlavour("\"<b>Xantcha</b> is recovering. The medicine is slow, but my magic would have killed her\"\n<b>—Serra</b>, to <b>Urza</b>")
                        .SetDamage(null)
                        .SetSprites("placeholder-item.png", "item-bg.png")
                        .WithValue(10)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.traits = new List<CardData.TraitStacks>
                            {
                                TStack("Noomlin", 1),
                                TStack("Combo", 1),
                            };
                            data.attackEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Heal", 3),
                            };
                        })
                        );

                    // Disdainful Stroke (no art!)
                    assets.Add(new CardDataBuilder(this)
                        .CreateItem("disdainfulStroke", "Disdainful Stroke", idleAnim:"FloatAnimationProfile")
                        .WithFlavour("\"You are beneath contempt. Your lineage will be forgotten\"")
                        .SetDamage(0)
                        .SetSprites("placeholder-item.png", "item-bg.png")
                        .WithValue(10)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Hit All Enemies With 4 Or Greater Counter", 1)
                            };
                            data.attackEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Snow", 3)
                            };
                        })
                        );

                    // Counterspell (no art!)
                    assets.Add(new CardDataBuilder(this)
                        .CreateItem("counterspell", "Counterspell", idleAnim: "FloatAnimationProfile")
                        .WithFlavour("\"It was probably a lousy spell in the first place\"<b>\n—Ertai, wizard adept</b>")
                        .SetDamage(0)
                        .SetSprites("placeholder-item.png", "item-bg.png")
                        .WithValue(30)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.traits = new List<CardData.TraitStacks>
                            {
                                TStack("Noomlin", 1),
                            };
                            data.attackEffects = new CardData.StatusEffectStacks[]
                            {
                                new CardData.StatusEffectStacks(Get<StatusEffectData>("Snow"), 3),
                            };
                        })
                        );

                    // Rampant Growth (no art!)
                    assets.Add(new CardDataBuilder(this)
                        .CreateItem("rampantGrowth", "Rampant Growth", idleAnim: "ShakeAnimationProfile")
                        .SetDamage(null)
                        .SetSprites("placeholder-item.png", "item-bg.png")
                        .WithFlavour("Nature grows solutions to its problems")
                        .WithValue(55)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                new CardData.StatusEffectStacks(Get<StatusEffectData>("On Card Played Gain Noomlin To Random Card In Hand"), 1),
                            };
                            data.canPlayOnBoard = false;
                            data.needsTarget = false;
                        })
                        );

                    // Time Walk (no art!)
                    assets.Add(
                        new CardDataBuilder(this)
                        .CreateItem("timeWalk", "Time Walk", idleAnim: "FloatAnimationProfile")
                        .SetSprites("placeholder-item.png", "item-bg.png")
                        .SetDamage(null)
                        .WithFlavour("Time is a marvelous plaything")
                        .WithValue(40)  // Base price in shop: +-6
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("On Card Played Apply Snow To All Enemies", 2)
                            };
                            data.traits = new List<CardData.TraitStacks>()
                            {
                                TStack("Consume", 1),
                            };
                            data.needsTarget = false;
                            data.uses = 1;
                        })
                        );

                    // An Offer You Can't Refuse (no art!)
                    assets.Add(
                        new CardDataBuilder(this)
                        .CreateItem("anOfferYouCantRefuse", "An Offer You Can't Refuse", idleAnim: "FloatAnimationProfile")
                        .SetSprites("placeholder-item.png", "item-bg.png")
                        .SetDamage(0)
                        .WithFlavour("\"I think you'll find my terms quite agreeable, if you know what’s good for you\"")
                        .WithValue(40)  // Base price in shop: +-6
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.attackEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Snow", 3),
                            };
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("On Card Played Add Treasure To Hand", 1),
                            };
                            data.traits = new List<CardData.TraitStacks>()
                            {
                                TStack("Consume", 1),
                            };
                            data.uses = 1;
                        })
                        );

                }  // Items

            }  // Generic Stuff

            {  // Dragon deck stuff
                {  // Effects
                    {  // Companion Effects

                        // Wall of omens: When deployed draw 1
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXWhenDeployed>("When Deployed Draw")
                            .WithText("<keyword=draw> <{a}> when deployed")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDeployed>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectInstantDraw>("Instant Draw");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                            })
                            );

                        // Juggernaut: take 1 damage
                        assets.Add(new StatusEffectDataBuilder(this)
                          .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Damage To Self")
                          .WithText("Take <{a}> damage")
                          .WithStackable(true)
                          .WithCanBeBoosted(true)
                          .WithDoesDamage(true)     // Its entity can activate "On kill" effects with this effect, eg for Bling Charm
                          .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                          {
                              data.desc = "Deal <{0}> damage to self and allies in the row";
                              data.dealDamage = true;
                              data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                              data.countsAsHit = true;
                              data.doPing = false;
                          })
                          );

                        // Adding Treasure: Summon Treasure
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectSummon>("Summon Treasure")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectSummon>(data =>
                            {
                                data.eventPriority = 99999;
                                data.summonCard = TryGet<CardData>("treasure");
                                data.effectPrefabRef = new UnityEngine.AddressableAssets.AssetReference("FlipCreateCard");
                            })
                            );

                        // Adding Treasure: Instant Summon Treasure
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectInstantSummon>("Instant Summon Treasure In Hand")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectInstantSummon>(data =>
                            {
                                data.eventPriority = 99999;
                                data.canSummonMultiple = true;
                                data.targetSummon = TryGet<StatusEffectSummon>("Summon Treasure");
                                data.summonPosition = StatusEffectInstantSummon.Position.Hand;
                            })
                            );

                        // Add Treasure on trigger
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Add Treasure To Hand")
                            .WithText("Add <{a}> {0} to your hand")
                            .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.treasure>")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectInstantSummon>("Instant Summon Treasure In Hand");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                                data.queue = true;
                                data.separateActions = true;
                                data.doPing = false;
                            })
                            );

                        // Add Treasure on destroy
                        assets.Add(new StatusEffectDataBuilder(this)
                                .Create<StatusEffectApplyXWhenDestroyed>("When Destroyed Add Treasure To Hand")
                                .WithText("When destroyed, add <{a}> {0} to your hand")
                                .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.treasure>")  // Any {0} in the line above is replaced with the text insert. The html tag must be of the form <card=[GUID name].[card name]>. No spaces around the equal sign. This creates the card pop-up.
                                .WithStackable(true)
                                .WithCanBeBoosted(true)
                                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDestroyed>(data =>
                                {
                                    data.eventPriority = 99999;
                                    data.effectToApply = TryGet<StatusEffectData>("Instant Summon Treasure In Hand");
                                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                                    data.doPing = false;
                                    data.targetMustBeAlive = false;
                                })
                                );

                        // Draw on destroy
                        assets.Add(new StatusEffectDataBuilder(this)
                                .Create<StatusEffectApplyXWhenDestroyed>("When Destroyed Draw")
                                .WithText("When destroyed, <Draw {a}>")
                                .WithStackable(true)
                                .WithCanBeBoosted(true)
                                .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDestroyed>(data =>
                                {
                                    data.eventPriority = 99999;
                                    data.effectToApply = TryGet<StatusEffectInstantDraw>("Instant Draw");
                                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                                    data.doPing = false;
                                    data.targetMustBeAlive = false;
                                })
                                );

                        // Summon Dragon Token
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectSummon>("Summon Dragon Token")
                            .WithText("Summon {0}")
                            .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.dragonToken>")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectSummon>(data =>
                            {
                                data.eventPriority = 99999;
                                data.summonCard = TryGet<CardData>("dragonToken");
                                data.gainTrait = TryGet<StatusEffectTemporaryTrait>("Temporary Summoned");
                                data.setCardType = TryGet<CardType>("Summoned");
                                data.effectPrefabRef = new UnityEngine.AddressableAssets.AssetReference("SummonCreateCard");
                            })
                            );

                        // Instant summon Dragon Token
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectInstantSummon>("Instant Summon Dragon Token")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectInstantSummon>(data =>
                            {
                                data.eventPriority = 99999;
                                data.targetSummon = TryGet<StatusEffectSummon>("Summon Dragon Token");
                                data.summonPosition = StatusEffectInstantSummon.Position.InFrontOfOrOtherRow;
                            })
                            );

                        // Summon Dragon Token with Spark
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectSummon>("Summon Spark Dragon Token")
                            .WithText("Summon {0}")
                            .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.dragonTokenSpark>")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectSummon>(data =>
                            {
                                data.eventPriority = 99999;
                                data.summonCard = TryGet<CardData>("dragonTokenSpark");
                                data.gainTrait = TryGet<StatusEffectTemporaryTrait>("Temporary Summoned");
                                data.setCardType = TryGet<CardType>("Summoned");
                                data.effectPrefabRef = new UnityEngine.AddressableAssets.AssetReference("SummonCreateCard");
                            })
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
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectWhileActiveX>("While Active Decrease Counter Of Flying Allies")
                            .WithText("While active, reduce max <keyword=counter> of {0} allies by 1")
                            .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
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
                                data.wantedTrait = TryGet<TraitData>("Flying");
                            })
                            );

                        // Captain Lannery Storm: Gain attack when Treasure is played
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
                            .Create<StatusEffectApplyXOnCertainCardPlayed>("Trigger On First Treasure")
                            .WithText("Trigger the first time you play a {0} each turn")
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
                                data.allowedTimes = 1;
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
                                data.dealDamage = true;
                                data.effectToApply = TryGet<StatusEffectSnow>("Snow");  // Crashes when I give it no effect O_O
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
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectWhileActiveXUpdatesOnTrait>("While Active Reduce Counter By Allies With Flying")
                            .WithText("While active, reduce own <keyword=counter> by the number of {0} allies")
                            .WithTextInsert($"<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveXUpdatesOnTrait>(data =>
                            {
                                data.alsoActivate = TryGet<TraitData>("Flying");
                                ScriptableTargetsOnBoard scriptAmount = ScriptableTargetsOnBoard.CreateInstance<ScriptableTargetsOnBoard>();
                                scriptAmount.allies = true;
                                scriptAmount.hasTrait = TryGet<TraitData>("Flying");
                                data.scriptableAmount = scriptAmount;
                                data.applyEqualAmount = true;
                                data.effectToApply = TryGet<StatusEffectOngoingCounter>("Ongoing Decrease Counter Stackable");
                                data.affectsSelf = true;
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                            })
                            );

                        // Ojutai, Soul of Winter: When self or Flying ally attacks, apply 1 Snow to a random enemy
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXOnCertainCardPlayed>("On Card Played Apply Snow To Random Enemy")
                            .WithText("Apply <{a}> <keyword=snow> to a random enemy whenever self or {0} ally attacks")
                            .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCertainCardPlayed>(data =>
                            {
                                data.allowedTraits = new TraitData[1] { TryGet<TraitData>("whycats.wildfrost.wildfrostthegathering.Flying") };
                                data.countsSelf = true;
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.RandomEnemy;
                                data.effectToApply = TryGet<StatusEffectSnow>("Snow");
                                data.whileActive = true;
                            })
                            );

                        // Professional Face-Breaker: Recycle Treasure to Draw on attack
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Destroy Right Treasure In Hand And Draw")
                            .WithText("Destroy rightmost {0} in hand to <keyword=draw {a}>")
                            .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.treasure>")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectInstantDestroyNumCardsInHandAndApplyXForEach>("Instant Destroy Treasure In Hand And Draw For Each");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                            })
                            );

                        // Professional Face-Breaker: Recycle Treasure to Draw
                        assets.Add(new StatusEffectDataBuilder(this)
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
                                    TryGet<CardData>("treasure"),
                                };
                            }),
                                };
                                data.destroyCardEffect = TryGet<StatusEffectInstantKill>("Kill");
                                data.effectToApply = TryGet<StatusEffectInstantDraw>("Instant Draw");
                            })
                            );

                        // Shivan Dragon: Apply Spice to applier
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXInstant>("Instant Apply Spice To Applier")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectSpice>("Spice");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Applier;
                                data.doPing = false;
                                data.targetMustBeAlive = false;
                            })
                            );

                        // Shivan Dragon: Count Zoomlin cards in hand and gain Spice (I don't know why I did it this way leave me alone)
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXPreTurn>("Pre Turn Count Zoomlin In Hand & Gain Spice For Each")
                            .WithText("Before attacking, gain <{a}> <keyword=spice> for each card with <keyword=zoomlin> in hand")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXPreTurn>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectApplyXInstant>("Instant Apply Spice To Applier");
                                data.applyConstraints = new TargetConstraint[]
                                {
                            new Scriptable<TargetConstraintHasTrait>(tcht =>
                            {
                                tcht.trait = TryGet<TraitData>("Zoomlin");
                                tcht.ignoreSilenced = false;
                            }),
                                };
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Hand;
                            })
                            );

                        // Manaform Hellkite: Instant Summon Dragon Token on Item played
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXEqualToAttackOnCertainCardPlayed>("Summon Dragon Token On Item Played")
                            .WithText("Summon a {0} with <keyword=attack> equal to the <keyword=attack> of items you play")
                            .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.dragonTokenSpark>")
                            .WithStackable(true)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXEqualToAttackOnCertainCardPlayed>(data =>
                            {
                                data.allowedCardType = new CardType { item = true, name = "Item" };
                                data.hasAttack = true;
                                data.effectToApply = TryGet<StatusEffectInstantSummon>("Instant Summon Spark Dragon Token With X Health and Attack");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                                data.applyEqualAmount = true;
                            })
                            );

                        // Manaform Hellkite: Instant summon Dragon Token with equal health
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectInstantSummon>("Instant Summon Spark Dragon Token With X Health and Attack")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectInstantSummon>(data =>
                            {
                                data.eventPriority = 99999;
                                data.targetSummon = TryGet<StatusEffectSummon>("Summon Spark Dragon Token");
                                data.summonPosition = StatusEffectInstantSummon.Position.InFrontOfOrOtherRow;
                                data.withEffects = new StatusEffectData[]
                                {
                                TryGet<StatusEffectInstantSetAttack>("Set Attack"),
                                };
                            })
                            );

                        // Change the target mode to "fireball"
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectChangeTargetMode>("Random Enemy For Zoomlin")
                            .SubscribeToAfterAllBuildEvent<StatusEffectChangeTargetMode>(data =>
                            {
                                data.targetMode = new Scriptable<TargetModeFireball>();
                            })
                            );

                        // Voracious Hydra: When deployed, gain attack equal to zoomlined cards in hand
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXWhenDeployed>("When Deployed Apply Attack And Health To Self Equal To Zoomlin")
                            .WithText("When deployed, gain <keyword=attack> and <keyword=health> equal to cards with <keyword=zoomlin> in hand")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDeployed>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectInstantMultiple>("Increase Attack & Health (No Constraints)");
                                ScriptableTargetsInHand scriptAmount = ScriptableTargetsInHand.CreateInstance<ScriptableTargetsInHand>();
                                scriptAmount.hasTrait = TryGet<TraitData>("Zoomlin");
                                data.scriptableAmount = scriptAmount;
                                data.applyEqualAmount = true;
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                            })
                            );

                    }  // Companion Effects 

                    {  // Item Effects

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

                        // Treasure: Add Zoomlin to self
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXInstant>("Instant Gain Zoomlin (To Card In Hand)")
                            .WithText("Add {0} to a card in your hand")
                            .WithTextInsert("<keyword=zoomlin>")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
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
                                    new Scriptable<TargetConstraintHasStatus>(tchs =>
                                {
                                    tchs.not = true;
                                    tchs.status = TryGet<StatusEffectFreeAction>("Free Action");
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

                        // Delayed Blast Fireball: Gain Spice
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Apply Spice To Self")
                            .WithText("Gain <{a}><keyword=spice>")
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectSpice>("Spice");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                                data.waitForAnimationEnd = true;
                            })
                            );

                        // Gain Zoomlin when drawn if 2+ allies have Flying
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXWhenDrawn>("Gain Zoomlin When Drawn If 2 Allies With Flying")
                            .WithText("Gain <keyword=zoomlin> when drawn if 2 allies have {0}")
                            .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDrawn>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectSafeTemporaryTrait>("Safe Temporary Zoomlin");
                                data.applyConstraints = new TargetConstraint[]
                                {
                                new Scriptable<TargetConstraintIsFeatureOnBoard>(tcifob =>
                                {
                                    tcifob.allies = true;
                                    tcifob.hasTrait = TryGet<TraitData>("Flying");
                                    tcifob.requiredAmount = 2;
                                }),
                                };
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                            })
                            );

                        // Increase Effects when drawn if 2+ allies have Flying
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXWhenDrawn>("Increase Effects By 2 When Drawn If 2 Allies With Flying")
                            .WithText("Increase effects by 2 when drawn if 2 allies have {0}")
                            .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                            .WithStackable(true)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDrawn>(data =>
                            {
                                ScriptableFixedAmount scriptAmount = ScriptableFixedAmount.CreateInstance<ScriptableFixedAmount>();
                                scriptAmount.amount = 2;
                                data.effectToApply = TryGet<StatusEffectInstantIncreaseEffects>("Increase Effects");
                                data.applyConstraints = new TargetConstraint[]
                                {
                                new Scriptable<TargetConstraintIsFeatureOnBoard>(tcifob =>
                                {
                                    tcifob.allies = true;
                                    tcifob.hasTrait = TryGet<TraitData>("Flying");
                                    tcifob.requiredAmount = 2;
                                }),
                                };
                                data.scriptableAmount = scriptAmount;
                                data.applyEqualAmount = true;
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                            })
                            );

                        // Add Treasure equal to counter
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXInstant>("Instant Add Treasure To Hand Equal To Target Counter")
                            .WithText("Add {0} to your hand equal to the target's <keyword=counter>")
                            .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.treasure>")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                            {
                                ScriptableEqualToCounter scriptAmount = ScriptableEqualToCounter.CreateInstance<ScriptableEqualToCounter>();
                                data.effectToApply = TryGet<StatusEffectInstantSummon>("Instant Summon Treasure In Hand");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Target;
                                data.applyEqualAmount = true;
                                data.scriptableAmount = scriptAmount;
                                data.doPing = false;
                            })
                            );

                        // Fireball: damage equal to zoomlined cards
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectBonusDamageEqualToCardsWithTrait>("Bonus Damage Equal To Zoomlin In Hand")
                            .WithText("Deal additional damage equal to the number of cards with <keyword=zoomlin> in hand")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectBonusDamageEqualToCardsWithTrait>(data =>
                            {
                                data.doCheckName = false;
                                data.checkTrait = TryGet<TraitData>("Zoomlin");
                            })
                            );

                        // Shared Animosity: Apply Sice equal to Flying allies to all Flying allies
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXOnCardPlayed>("Apply Spice To Flying Allies Equal To Twice Flying Allies")
                            .WithText("Apply <keyword=spice> to {0} allies equal to twice the number of {0} allies")
                            .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                            .WithStackable(true)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectSpice>("Spice");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies;
                                data.applyConstraints = new TargetConstraint[]
                                {
                                new Scriptable<TargetConstraintHasTrait>(tcht =>
                                {
                                    tcht.trait = TryGet<TraitData>("Flying");
                                    tcht.ignoreSilenced = false;
                                }),
                                };
                                ScriptableTargetsOnBoard scriptAmount = ScriptableTargetsOnBoard.CreateInstance<ScriptableTargetsOnBoard>();
                                scriptAmount.allies = true;
                                scriptAmount.hasTrait = TryGet<TraitData>("Flying");
                                scriptAmount.mult = 2;
                                data.scriptableAmount = scriptAmount;
                                data.applyEqualAmount = true;
                            })
                            );

                        // Bottle-Cap Blast: Instant gain treasures equal to health
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXInstant>("Instant Add Treasure To Hand Equal To Target Health Below Zero")
                            .WithText("Add {0} to your hand equal to the excess damage done")
                            .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.treasure>")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectInstantSummon>("Instant Summon Treasure In Hand");
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
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Gain Zoomlin To X Random Cards In Hand")
                            .WithText("Add <keyword=zoomlin> to <{a}> random cards in hand")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectApplyXMultipleInstant>("Instant Gain Zoomlin To Random Card In Hand");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                            })
                            );

                        // Explosive Vegetation: Add Zoomlin to a random card in hand
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXMultipleInstant>("Instant Gain Zoomlin To Random Card In Hand")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXMultipleInstant>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectSafeTemporaryTrait>("Safe Temporary Zoomlin");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.RandomCardInHand;
                                data.applyConstraints = new TargetConstraint[]
                                {
                                new Scriptable<TargetConstraintHasTrait>(tcht =>
                                {
                                    tcht.not = true;
                                    tcht.trait = TryGet<TraitData>("Noomlin");
                                    tcht.ignoreSilenced = false;
                                }),
                                new Scriptable<TargetConstraintHasStatus>(tchs =>
                                {
                                    tchs.not = true;
                                    tchs.status = TryGet<StatusEffectFreeAction>("Free Action");
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
                                    tchs.status = TryGet<StatusEffectFreeAction>("Free Action (Zoomlin)");
                                }),
                                };
                            })
                            );

                    }  // Item Effects 

                    {  // Clunker Effects

                        // Sol Ring: When destroyed, counter leader down by X
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXWhenDestroyed>("When Destroyed Count Down Leader By X")
                            .WithCanBeBoosted(true)
                            .WithStackable(true)
                            .WithText("When destroyed, count down your leader by <{a}>")
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDestroyed>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectInstantReduceCounter>("Reduce Counter");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies;
                                data.applyConstraints = new TargetConstraint[]
                                {
                                new Scriptable<TargetConstraintIsLeader>(),
                                };
                                data.targetMustBeAlive = false;
                            })
                            );

                        // Bootlegger's Stash: While active, add "Add a treasure to your hand" to allies in row
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectWhileActiveX>("While Active Treasure To AlliesInRow")
                            .WithText("While active, add \"Add <{a}> {0} to your hand\" to allies in row")
                            .WithTextInsert($"<card=whycats.wildfrost.wildfrostthegathering.treasure>")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveX>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectApplyXOnCardPlayedUpdateDesc>("On Card Played Add Treasure To Hand");
                                data.affectsSelf = false;
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.AlliesInRow;
                                data.hiddenKeywords = new KeywordData[]
                                {
                                TryGet<KeywordData>("Active"),
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
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXOnCardPlayedUpdateDesc>("On Card Played Add Treasure To Hand")
                            .WithText("Add <{a}> {0} to your hand")
                            .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.treasure>")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayedUpdateDesc>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectInstantSummon>("Instant Summon Treasure In Hand");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                                data.queue = true;
                                data.separateActions = true;
                                data.doPing = true;
                            })
                            );

                        // Rites of Flourishing: Count frontmost enemy down by 1
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Reduce Counter To Front Enemy")
                            .WithText("Count down frontmost enemy's <sprite name=counter> by <{a}>")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectInstantReduceCounter>("Reduce Counter");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.FrontEnemy;
                            })
                            );

                        // Rites of Flourishing: Count allies in row down by 1
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Reduce Counter To Allies In Row")
                            .WithText("Count down <sprite name=counter> of allies in row by <{a}>")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectInstantReduceCounter>("Reduce Counter");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.AlliesInRow;
                            })
                            );

                        // Mox Jasper: When destroyed, add Zoomlin to Flying allies in hand
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXWhenDestroyed>("When Destroyed Gain Zoomlin To Flying Allies In Hand")
                            .WithCanBeBoosted(true)
                            .WithStackable(true)
                            .WithText("When destroyed, add <keyword=zoomlin> to {0} allies in hand")
                            .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDestroyed>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectTemporaryTrait>("Temporary Zoomlin");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Hand;
                                data.applyConstraints = new TargetConstraint[]
                                {
                                new Scriptable<TargetConstraintIsUnit>(),
                                new Scriptable<TargetConstraintHasTrait>(tcht =>
                                {
                                    tcht.trait = TryGet<TraitData>("Flying");
                                    tcht.ignoreSilenced = false;
                                }),
                                };
                                data.targetMustBeAlive = false;
                            })
                            );

                        // Tempting Contract: count down random enemy by 1
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Reduce Counter To Random Enemy")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .WithText("Count down a random enemy's <keyword=counter> by <{a}>")
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectInstantReduceCounter>("Reduce Counter");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.RandomEnemy;
                            })
                            );

                        // Gravitational Shift: While active, add attack to flying allies
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectWhileActiveX>("While Active Increase Attack To Flying Allies")
                            .WithText("While active, add <+{a}><keyword=attack> to all {0} allies")
                            .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveX>(data =>
                            {
                                data.hiddenKeywords = new KeywordData[]
                                {
                                TryGet<KeywordData>("Active"),
                                };
                                data.eventPriority = 10;
                                data.effectToApply = TryGet<StatusEffectOngoingAttack>("Ongoing Increase Attack");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies;
                                data.applyConstraints = new TargetConstraint[]
                                {
                                new Scriptable<TargetConstraintHasTrait>(tcht =>
                                {
                                    tcht.trait = TryGet<TraitData>("Flying");
                                    tcht.ignoreSilenced = false;
                                }),
                                };
                            })
                            );

                        // Gravitational Shift: While active, reduce attack to non Flying
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectWhileActiveX>("While Active Reduce Attack To Non Flying")
                            .WithText("And everyone else gets <-{a}><keyword=attack>")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveX>(data =>
                            {
                                data.hiddenKeywords = new KeywordData[]
                                {
                                TryGet<KeywordData>("Active"),
                                };
                                data.eventPriority = 10;
                                data.effectToApply = TryGet<StatusEffectOngoingAttack>("Ongoing Reduce Attack");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies | StatusEffectApplyX.ApplyToFlags.Enemies;
                                data.applyConstraints = new TargetConstraint[]
                                {
                                new Scriptable<TargetConstraintHasTrait>(tcht =>
                                {
                                    tcht.not = true;
                                    tcht.trait = TryGet<TraitData>("Flying");
                                    tcht.ignoreSilenced = false;
                                }),
                                };
                            })
                            );

                        // Revel in Riches: Gain 1 treasure when an enemy is killed
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXWhenUnitIsKilled>("Add Treasure To Hand When Enemy Killed")
                            .WithText("Add <{a}> {0} to your hand when an enemy is killed")
                            .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.treasure>")
                            .WithStackable(true)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenUnitIsKilled>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectInstantSummon>("Instant Summon Treasure In Hand");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                                data.queue = true;
                                data.enemy = true;
                                data.ally = false;
                            })
                            );

                        // Revel in Riches: Gain Spice when Treasure is played
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXOnCertainCardPlayed>("Gain Spice On Treasure")
                            .WithText("Gain <{a}><keyword=spice> whenever a {0} is played")
                            .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.treasure>")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCertainCardPlayed>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectSpice>("Spice");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                                data.allowedCards = new CardData[1] { TryGet<CardData>("treasure") };
                            })
                            );

                        // Windcrag Siege: While active add 1 frenzy to ally behind
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectWhileActiveXUpdatesOnOtherMove>("While Active Frenzy To AllyBehind")
                            .WithText("While active, add <x{a}><keyword=frenzy> to ally behind")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveXUpdatesOnOtherMove>(data =>
                            {
                                data.hiddenKeywords = new KeywordData[]
                                {
                                TryGet<KeywordData>("Active"),
                                };
                                data.eventPriority = 10;
                                data.effectToApply = TryGet<StatusEffectMultiHit>("MultiHit");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.AllyBehind;
                            })
                            );

                    }  // Clunker Effects 

                    {  // Leader Effects

                        // The Ur-Dragon: While active, Flying allies gain zoomlin when drawn
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectWhileActiveX>("While Active Zoomlin When Drawn To Flying Allies In Hand")
                            .WithText("While active, {0} allies gain <keyword=zoomlin> when drawn")
                            .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectWhileActiveX>(data =>
                            {
                                data.eventPriority = 10;
                                data.effectToApply = TryGet<StatusEffectApplyXWhenDrawn>("When Drawn Gain Zoomlin");
                                data.applyConstraints = new TargetConstraint[]
                                {
                                new Scriptable<TargetConstraintHasTrait>(tcht =>
                                {
                                    tcht.trait = TryGet<TraitData>("Flying");
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
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXInstant>("Instant Gain Fragile")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXInstant>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectTemporaryTrait>("Temporary Fragile");
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
                        assets.Add(new StatusEffectDataBuilder(this)
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
                                data.trait = TryGet<TraitData>("Fragile");
                            })
                            );

                        // Miirym: Summon copy of ally ahead with 1 health and fragile
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectInstantSummon>("Instant Summon Copy With X Health And Fragile")
                            .WithStackable(true)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectInstantSummon>(data =>
                            {
                                data.hiddenKeywords = new KeywordData[]
                                {
                                TryGet<KeywordData>("Copy"),
                                };
                                data.eventPriority = 99999;
                                data.targetSummon = TryGet<StatusEffectSummon>("Summon Plep");
                                data.summonCopy = true;
                                data.withEffects = new StatusEffectData[]
                                {
                                TryGet<StatusEffectInstantSetHealth>("Set Health"),
                                TryGet<StatusEffectApplyXInstant>("Instant Gain Fragile")
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
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Summon Copy Of Ally Ahead With X Health And Fragile")
                            .WithText("Summon a copy of ally ahead with <{a}><keyword=health> and <keyword=fragile>")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectInstantSummon>("Instant Summon Copy With X Health And Fragile");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.AllyInFrontOf;

                            })
                            );

                        // Lathliss: Summon Dragon Token when Flying ally deployed
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXWhenDeployedNoHandIfTrait>("When Flying Ally Deployed Summon Big Dragon Token With X Health")
                            .WithText("Summon a <card=whycats.wildfrost.wildfrostthegathering.bigDragonToken> with <{a}><keyword=health> whenever a {0} ally is deployed")
                            .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDeployedNoHandIfTrait>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectInstantSummon>("Instant Summon Big Dragon Token With X Health");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                                data.whenSelfDeployed = false;
                                data.whenAllyDeployed = true;
                                data.wantedTrait = TryGet<TraitData>("Flying");
                                data.excludedCards = new List<CardData> { TryGet<CardData>("bigDragonToken") };
                            })
                            );

                        // Lathliss: Instant summon Dragon Token With X Health
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectInstantSummon>("Instant Summon Big Dragon Token With X Health")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectInstantSummon>(data =>
                            {
                                data.eventPriority = 99999;
                                data.targetSummon = TryGet<StatusEffectSummon>("Summon Big Dragon Token");
                                data.summonPosition = StatusEffectInstantSummon.Position.InFrontOfOrOtherRow;
                                data.withEffects = new StatusEffectData[]
                                {
                                TryGet<StatusEffectInstantSetHealth>("Set Health"),
                                };
                            })
                            );

                        // Lathliss: Dragon Token with 4 health
                        assets.Add(
                            new CardDataBuilder(this).CreateUnit("bigDragonToken", "Dragon Token", idleAnim: "FloatSquishAnimationProfile")
                            .SetSprites("dragon-token-kyanner.png", "companion-bg.png")
                            .SetStats(4, 4, 3)
                            .WithCardType("Summoned")
                            .WithFlavour("rawr!")
                            .WithValue(25)
                            .SubscribeToAfterAllBuildEvent(data =>
                            {
                                data.traits = new List<CardData.TraitStacks>()
                                {
                            new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                                };
                                data.greetMessages = new string[1] { "Woah a token in the companion pool? That\'s not supposed to happen" };
                            })
                            );

                        // Summon Big Dragon Token
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectSummon>("Summon Big Dragon Token")
                            .WithText("Summon {0}")
                            .WithTextInsert("<card=whycats.wildfrost.wildfrostthegathering.bigDragonToken>")
                            .WithStackable(false)
                            .WithCanBeBoosted(false)
                            .SubscribeToAfterAllBuildEvent<StatusEffectSummon>(data =>
                            {
                                data.eventPriority = 99999;
                                data.summonCard = TryGet<CardData>("bigDragonToken");
                                data.gainTrait = TryGet<StatusEffectTemporaryTrait>("Temporary Summoned");
                                data.setCardType = TryGet<CardType>("Summoned");
                                data.effectPrefabRef = new UnityEngine.AddressableAssets.AssetReference("SummonCreateCard");
                            })
                            );

                        // Old Gnawbone: When Flying ally attacks, gain 1 treasure
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXOnCertainCardPlayed>("On Flying Card Played Add Treasure To Hand")
                            .WithText("Add <{a}> <card=whycats.wildfrost.wildfrostthegathering.treasure> to your hand whenever a {0} ally attacks")
                            .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCertainCardPlayed>(data =>
                            {
                                data.allowedTraits = new TraitData[1] { TryGet<TraitData>("whycats.wildfrost.wildfrostthegathering.Flying") };
                                data.countsSelf = false;
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                                data.effectToApply = TryGet<StatusEffectInstantSummon>("Instant Summon Treasure In Hand");
                                data.whileActive = true;
                            })
                            );

                        // Ganax: Trigger when Flying ally deployed
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXWhenDeployedNoHandIfTrait>("When Flying Ally Deployed Add Treasure To Hand")
                            .WithText("Add <{a}> <card=whycats.wildfrost.wildfrostthegathering.treasure> to your hand whenever a {0} ally is deployed")
                            .WithTextInsert("<keyword=whycats.wildfrost.wildfrostthegathering.flying>")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXWhenDeployedNoHandIfTrait>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectInstantSummon>("Instant Summon Treasure In Hand");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                                data.whenSelfDeployed = false;
                                data.whenAllyDeployed = true;
                                data.wantedTrait = TryGet<TraitData>("Flying");
                            })
                            );

                        // Drakuseth: On card played, gain "Deal X damage to frontmost enemy twice"
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXPostAttack>("On Card Played Deal X Damage To Frontmost Enemy Twice")
                            .WithText("Deal <{a}> damage to frontmost enemy twice")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXPostAttack>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectApplyXMultipleInstant>("Intstant Damage Frontmost Enemy Twice");
                                data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                            })
                            );

                        // Drakuseth: Damage Front enemy twice
                        assets.Add(new StatusEffectDataBuilder(this)
                            .Create<StatusEffectApplyXMultipleInstant>("Intstant Damage Frontmost Enemy Twice")
                            .WithStackable(true)
                            .WithCanBeBoosted(true)
                            .SubscribeToAfterAllBuildEvent<StatusEffectApplyXMultipleInstant>(data =>
                            {
                                data.effectToApply = TryGet<StatusEffectSnow>("Snow");
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

                    }  // Leader Effects
                }  // Effects

                {  // Companions

                    {  // Pets

                        // Wall of Omens
                        assets.Add(new CardDataBuilder(this)
                            .CreateUnit("wallOfOmens", "Wall Of Omens", idleAnim: "SwayAnimationProfile")
                            .SetStats(7, null, 0)
                            .IsPet((ChallengeData)null, true)
                            .SetSprites("wall-of-omensjpaick.png", "companion-bg.png")
                            .WithValue(50)
                            .SubscribeToAfterAllBuildEvent(data =>
                            {
                                data.startWithEffects = new CardData.StatusEffectStacks[]
                                {
                                    SStack("When Deployed Draw", 1),
                                };
                            })
                            );

                        // Air Elemental
                        assets.Add(new CardDataBuilder(this)
                            .CreateUnit("airElemental", "Air Elemental", idleAnim: "FloatAnimationProfile")
                            .SetStats(6, 4, 4)
                            .IsPet((ChallengeData)null, true)
                            .SetSprites("air-elemental-kwalker.png", "companion-bg.png")
                            .WithValue(50)
                            .SubscribeToAfterAllBuildEvent(data =>
                            {
                                data.traits = new List<CardData.TraitStacks>
                                {
                                    TStack("Flying", 1),
                                };
                            })
                            );

                        // Ball Lightning
                        assets.Add(new CardDataBuilder(this)
                            .CreateUnit("ballLightning", "Ball Lightning")
                            .SetStats(1, 6, 4)
                            .IsPet((ChallengeData)null, true)
                            .SetSprites("ball-lightning-tclaxton.png", "companion-bg.png")
                            .WithValue(50)
                            .SubscribeToAfterAllBuildEvent(data =>
                            {
                                data.traits = new List<CardData.TraitStacks>
                                {
                                    TStack("Spark", 1),
                                };
                            })
                            );

                        // Wall of Frost
                        assets.Add(new CardDataBuilder(this)
                            .CreateUnit("wallOfFrost", "Wall of Frost", idleAnim: "")  // Bad strings freeze it
                            .SetStats(6, null, 0)
                            .IsPet((ChallengeData)null, true)
                            .SetSprites("wall-of-frost-mbierek.png", "companion-bg.png")
                            .WithValue(50)
                            .SubscribeToAfterAllBuildEvent(data =>
                            {
                                data.startWithEffects = new CardData.StatusEffectStacks[]
                                {
                                    SStack("When Hit Apply Snow To Attacker", 1),
                                };
                            })
                            );

                        // Atog
                        assets.Add(new CardDataBuilder(this)
                            .CreateUnit("atog", "Atog")
                            .SetStats(5, 3, 3)
                            .IsPet((ChallengeData)null, true)
                            .SetSprites("atog-puddnhead.png", "companion-bg.png")
                            .WithValue(50)
                            .SubscribeToAfterAllBuildEvent(data =>
                            {
                                data.startWithEffects = new CardData.StatusEffectStacks[]
                                {
                                    SStack("When Ally Is Killed Apply Attack To Self", 1),
                                };
                            })
                            );

                        // Llanowar Elves
                        assets.Add(new CardDataBuilder(this)
                            .CreateUnit("llanowarElves", "Llanowar Elves")
                            .SetStats(5, 1, 3)
                            .IsPet((ChallengeData)null, true)
                            .SetSprites("llanowar-elves-kwalker.png", "companion-bg.png")
                            .WithValue(50)
                            .SubscribeToAfterAllBuildEvent(data =>
                            {
                                data.startWithEffects = new CardData.StatusEffectStacks[]
                                {
                                SStack("On Card Played Gain Zoomlin To X Random Cards In Hand", 1),
                                };
                            })
                            );

                        // Grizzly Bears
                        assets.Add(new CardDataBuilder(this)
                            .CreateUnit("grizzlyBears", "Grizzly Bears")
                            .SetStats(2, 2, 2)
                            .IsPet((ChallengeData)null, true)
                            .SetSprites("grizzly-bears-jamenges.png", "companion-bg.png")
                            .WithFlavour("Don\'t try to outrun one of Dominia\'s Grizzlies; it\'ll catch you, knock you down, and eat you. Of course, you could run up a tree. In that case you\'ll get a nice view before it knocks the tree down and eats you.")
                            .WithValue(50)
                            );

                        // Juggernaut
                        assets.Add(new CardDataBuilder(this)
                            .CreateUnit("juggernaut", "Juggernaut")
                            .SetStats(5, 5, 3)
                            .IsPet((ChallengeData)null, true)
                            .SetSprites("juggernaut-kwalker.png", "companion-bg.png")
                            .WithValue(50)
                            .SubscribeToAfterAllBuildEvent(data =>
                            {
                                data.startWithEffects = new CardData.StatusEffectStacks[]
                                {
                                    SStack("On Card Played Damage To Self", 1),
                                };
                            })
                            );

                    }  // Pets

                    // Dragon Token
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("dragonToken", "Dragon Token", idleAnim: "FloatSquishAnimationProfile")
                        .SetSprites("dragon-token-kyanner.png", "companion-bg.png")
                        .SetStats(1, 4, 3)
                        .WithCardType("Summoned")
                        .WithFlavour("rawr!")
                        .WithValue(25)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.traits = new List<CardData.TraitStacks>()
                            {
                                new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                            };
                            data.greetMessages = new string[1] { "Woah a token in the companion pool? That\'s not supposed to happen" };
                        })
                        );

                    // Dragon Token with Spark
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("dragonTokenSpark", "Dragon Illusion Token", idleAnim: "FloatSquishAnimationProfile")
                        .SetSprites("dragon-illusion-token-amar.png", "companion-bg.png")
                        .SetStats(1, 1, 3)
                        .WithCardType("Summoned")
                        .WithFlavour("rawr!")
                        .WithValue(25)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.traits = new List<CardData.TraitStacks>()
                            {
                                new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                                new CardData.TraitStacks(Get<TraitData>("Spark"), 1),
                            };
                            data.greetMessages = new string[1] { "Woah a token in the companion pool? That\'s not supposed to happen" };
                        })
                        );

                    // Goldspan Dragon
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("goldspanDragon", "Goldspan Dragon", idleAnim: "FloatSquishAnimationProfile")
                        .SetSprites("goldspan-dragon-amar.png", "companion-bg.png")
                        .SetStats(7, 4, 4)
                        .WithCardType("Friendly")
                        .WithFlavour("<i>\"You see, most places have mice or mosquitoes...\"</i>")
                        .WithValue(45)
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
                                "<i>\"You see, most places have mice or mosquitoes...\"</i>"};
                        })
                        );

                    // Ancient Copper Dragon
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("ancientCopperDragon", "Ancient Copper Dragon", idleAnim: "GiantAnimationProfile")
                        .SetSprites("ancient-copper-dragon-ajmanzan.png", "companion-bg.png")
                        .SetStats(8, 5, 5)
                        .WithCardType("Friendly")
                        .WithFlavour("<i>You can never have enough gold</i>")
                        .WithValue(45)
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
                            data.greetMessages = new string[1] { "<i>You can never have enough gold</i>" };
                        })
                        );

                    // Atsushi, Blazing Sky
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("atsushiBlazingSky", "Atsushi, the Blazing Sky", idleAnim: "FloatSquishAnimationProfile")
                        .SetSprites("atsushi-blazing-sky-vaminguez.png", "companion-bg.png")
                        .SetStats(7, 1, 3)
                        .WithCardType("Friendly")
                        .WithFlavour("\"<i>The reborn form of <b>Ryusei</b>, protector of <b>Sokenzan</b></i>")
                        .WithValue(45)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("When Destroyed Add Treasure To Hand", 2),
                                SStack("When Destroyed Draw", 2),
                            };
                            data.traits = new List<CardData.TraitStacks>()
                            {
                                new CardData.TraitStacks(Get<TraitData>("Flying"), 1),
                                TStack("Eternal", 1),
                            };
                            data.greetMessages = new string[2] { "<i><b>Ryusei</b> and <b>Jugan</b> sealed themselves and the three other dragon spirits in an egg under <b>Boseiju</b>. They hatched 50 years later, reborn as <b>Ao</b>, <b>Kairi</b>, <b>Junji</b>, <b>Atsushi</b>, and <b>Kura</b></i>",
                                "<i>The reborn form of <b>Ryusei</b>, protector of <b>Sokenzan</b></i>" };
                        })
                        );

                    // Utvara Hellkite
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("utvaraHellkite", "Utvara Hellkite", idleAnim: "Heartbeat2AnimationProfile")
                        .SetSprites("utvara-hellkite-mzug.png", "companion-bg.png")
                        .SetStats(8, 4, 5)
                        .WithCardType("Friendly")
                        .WithFlavour("<i>The fear of dragons is as old and as powerful as the fear of death itself</i>")
                        .WithValue(45)
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
                            data.greetMessages = new string[1] { "<i>The fear of dragons is as old and as powerful as the fear of death itself</i>" };
                        })
                        );

                    // Dragonlord's Servant
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("dragonlordsServant", "Dragonlord's Servant")
                        .SetSprites("dragonlords-servant-sprescott.png", "companion-bg.png")
                        .SetStats(4, 1, 3)
                        .WithCardType("Friendly")
                        .WithFlavour("<i>The tastiest morsels rarely make it to their intended destination</i>")
                        .WithValue(45)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                new CardData.StatusEffectStacks(TryGet<StatusEffectData>("While Active Decrease Counter Of Flying Allies"), 1),
                            };
                            data.greetMessages = new string[2] { "<i>Atarka serving-goblins coat themselves with grease imbued with noxious herbs, hoping to discourage their ravenous masters from adding them to the meal</i>",
                                "<i>The tastiest morsels rarely make it to their intended destination</i>" };
                        })
                        );

                    // Terror of the Peaks
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("terrorOfThePeaks", "Terror of the Peaks", idleAnim: "Heartbeat2AnimationProfile")
                        .SetSprites("terror-of-the-peaks-jraphael.png", "companion-bg.png")
                        .SetStats(6, 5, 5)
                        .WithCardType("Friendly")
                        .WithFlavour("<i>If it comes for you, die boldly or die swiftly—for die you will</i>")
                        .WithValue(45)
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
                            data.greetMessages = new string[1] { "<i>If it comes for you, die boldly or die swiftly — for die you will</i>" };
                        })
                        );

                    // Captain Lannery Storm
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("captainLanneryStorm", "Captain Lannery Storm", idleAnim: "ShakeAnimationProfile")
                        .SetSprites("captain-lannery-storm-crallis.png", "companion-bg.png")
                        .SetStats(3, 2, 3)
                        .WithCardType("Friendly")
                        .WithFlavour("\"I believe in love at first shine\"")
                        .WithValue(45)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Gain Attack On Treasure", 1),
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
                        new CardDataBuilder(this).CreateUnit("academyManufactor", "Academy Manufactor")
                        .SetSprites("academy-manufactor-cwhite.png", "companion-bg.png")
                        .SetStats(4, 1, 0)
                        .WithCardType("Friendly")
                        .WithFlavour("<i>Automated systems at the <b>Tolarian Academy</b> sort new acquisitions for optimal use, determining which should be studied, eaten, or sold</i>")
                        .WithValue(45)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Trigger On First Treasure", 1),
                            };
                            data.traits = new List<CardData.TraitStacks>()
                            {
                                TStack("Draw", 1)
                            };
                            data.greetMessages = new string[2] { "<i>Automated systems at the <b>Tolarian Academy</b> sort new acquisitions for optimal use, determining which should be studied, eaten, or sold</i>",
                            "<i>It shapes wonders beyond our wildest dreams...</i>\n<i>Like sandwiches!</i>"};
                        })
                        );

                    // Glorybringer
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("glorybringer", "Glorybringer", idleAnim: "ShakeAnimationProfile")
                        .SetSprites("glorybringer-sburley.png", "companion-bg.png")
                        .SetStats(5, 3, 3)
                        .WithCardType("Friendly")
                        .WithFlavour("<i>What the initiates face in the final trial is completely at <b>Hazoret's</b> discretion</i>")
                        .WithValue(45)
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
                            data.greetMessages = new string[1] { "<i>What the initiates face in the final trial is completely at <b>Hazoret's</b> discretion</i>" };
                        })
                        );

                    // Earthquake Dragon
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("earthquakeDragon", "Earthquake Dragon", idleAnim: "GiantAnimationProfile")
                        .SetSprites("earthquake-dragon-jgrenier.png", "companion-bg.png")
                        .SetStats(10, 10, 8)
                        .WithCardType("Friendly")
                        .WithFlavour("<i>An empire can take centuries to build but mere moments to destroy</i>")
                        .WithValue(45)
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
                            data.greetMessages = new string[2] { "<i>An empire can take centuries to build but mere moments to destroy</i>",
                                "*The ground rumbles beneath your feet*" };
                        })
                        );

                    // Ojutai, Soul of Winter
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("ojutaiSoulOfWinter", "Ojutai, Soul of Winter", idleAnim: "FloatAnimationProfile")
                        .SetSprites("ojutai-soul-of-winter-cstone.png", "companion-bg.png")
                        .SetStats(9, 2, 4)
                        .WithCardType("Friendly")
                        .WithFlavour("<i>\"Human enlightenment is a firefly that sparks in the night. Dragon enlightenment is a beacon that disperses all darkness.\"</i>")
                        .WithValue(45)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.traits = new List<CardData.TraitStacks>()
                            {
                                TStack("Flying", 1),
                            };
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("On Card Played Apply Snow To Random Enemy", 1),
                            };
                            data.greetMessages = new string[1] { "<i>\"Human enlightenment is a firefly that sparks in the night. Dragon enlightenment is a beacon that disperses all darkness.\"</i>" };
                        })
                        );

                    // Professional Face-Breaker
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("professionalFaceBreaker", "Professional Face-Breaker")
                        .SetSprites("professional-face-breaker-dscott.png", "companion-bg.png")
                        .SetStats(5, 2, 3)
                        .WithCardType("Friendly")
                        .WithFlavour("<i>When you lose <b>Jetmir's</b> trust, his family makes sure you lose everything else</i>")
                        .WithValue(45)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("On Card Played Destroy Right Treasure In Hand And Draw", 1),
                                SStack("MultiHit", 1),
                            };
                            data.greetMessages = new string[1] { "<i>When you lose <b>Jetmir's</b> trust, his family makes sure you lose everything else</i>" };
                        })
                        );

                    // Shivan Dragon
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("shivanDragon", "Shivan Dragon", idleAnim: "FloatSquishAnimationProfile")
                        .SetSprites("shivan-dragon-dgiancola.png", "companion-bg.png")
                        .SetStats(7, 4, 3)
                        .WithCardType("Friendly")
                        .WithFlavour("<i>The undisputed master of the mountains of Shiv</i>")
                        .WithValue(45)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Pre Turn Count Zoomlin In Hand & Gain Spice For Each", 1),
                            };
                            data.traits = new List<CardData.TraitStacks>()
                            {
                                TStack("Flying", 1),
                            };
                            data.greetMessages = new string[2] { "<b>*Breathes Fire Loudly*</b>\n<i>How was it stuck in ice?</i>",
                                "<i>The undisputed master of the mountains of Shiv</i>"};
                        })
                        );

                    // Manaform Hellkite
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("manaformHellkite", "Manaform Hellkite", idleAnim: "FloatSquishAnimationProfile")
                        .SetSprites("manaform-hellkite-amar.png", "companion-bg.png")
                        .SetStats(5, 2, 4)
                        .WithCardType("Friendly")
                        .WithFlavour("<i>Just because it's a big, strong, unthinking beast of the sky intent on burning your house doesn't mean it can't use magic<i>")
                        .WithValue(45)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Summon Dragon Token On Item Played", 1),
                            };
                            data.traits = new List<CardData.TraitStacks>()
                            {
                                TStack("Flying", 1),
                            };
                            data.greetMessages = new string[1] { "<i>Just because it's a big, strong, unthinking beast of the sky intent on burning your house doesn't mean it can't use magic</i>" };
                        })
                        );

                    // Voracious Hydra
                    assets.Add(
                        new CardDataBuilder(this).CreateUnit("voraciousHydra", "Voracious Hydra", idleAnim: "Heartbeat2AnimationProfile")
                        .SetSprites("voracious-hydra-wreynolds.png", "companion-bg.png")
                        .SetStats(2, 1, 2)
                        .WithCardType("Friendly")
                        .WithFlavour("<i>Even baloths fear its feeding time<i>")
                        .WithValue(45)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("When Deployed Apply Attack And Health To Self Equal To Zoomlin", 4),
                            };
                            data.traits = new List<CardData.TraitStacks>()
                            {
                                TStack("Fireball", 1),
                            };
                            data.greetMessages = new string[1] { "<i>Even baloths fear its feeding time</i>" };
                        })
                        );

                    // Fear of Sleep Paralysis (no art!)
                    assets.Add(new CardDataBuilder(this)
                        .CreateUnit("fearOfSleepParalysis", "Fear of Sleep Paralysis", idleAnim: "SwayAnimationProfile")
                        .SetStats(7, 2, 3)
                        .SetSprites("dragon-baby.png", "companion-bg.png")
                        .WithValue(50)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.traits = new List<CardData.TraitStacks>
                            {
                                    TStack("Flying", 1),
                            };
                            data.attackEffects = new CardData.StatusEffectStacks[]
                            {
                                    SStack("Snow", 2),
                            };
                        })
                        );

                }  // Companions

                {  // Items

                    // Shock
                    assets.Add(new CardDataBuilder(this)
                            .CreateItem("shock", "Shock", idleAnim: "ShakeAnimationProfile")
                            .WithFlavour("\"The beauty of it is they never see it coming. Ever.\"\n<b>-Razzix, sparkmage</b>")
                            .SetDamage(2)
                            .SetSprites("shock-jfoster.png", "item-bg.png")
                            .WithValue(10)
                        );

                    // Cancel
                    assets.Add(new CardDataBuilder(this)
                        .CreateItem("cancel", "Cancel", idleAnim: "FloatAnimationProfile")
                        .WithFlavour("\"Even the greatest inferno begins as a spark. And anyone can snuff out a spark.\"\n<b>-Chanyi, mistfire sage</b>")
                        .SetDamage(0)
                        .SetSprites("cancel-dpalumb.png", "item-bg.png")
                        .WithValue(30)  // Base price in shop: 19-31
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.traits = new List<CardData.TraitStacks>
                            {
                                TStack("Zoomlin", 1),
                            };
                            data.attackEffects = new CardData.StatusEffectStacks[]
                            {
                                new CardData.StatusEffectStacks(Get<StatusEffectData>("Snow"), 2),
                            };
                        })
                        );

                    // Swiftfoot Boots
                    assets.Add(new CardDataBuilder(this)
                        .CreateItem("swiftfootBoots", "Swiftfoot Boots", idleAnim: "FloatAnimationProfile")
                        .SetDamage(null)
                        .SetSprites("swiftfoot-boots-svelinov.png", "item-bg.png")
                        .WithValue(50)  // Base price in shop: 35-55
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.traits = new List<CardData.TraitStacks>
                            {
                                TStack("Combo", 1)
                            };
                            data.attackEffects = new CardData.StatusEffectStacks[]
                            {
                                new CardData.StatusEffectStacks(Get<StatusEffectData>("Reduce Counter"), 1),
                            };
                        })
                        );

                    // Treasure
                    assets.Add(
                        new CardDataBuilder(this)
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

                    // Dragon's Fire
                    assets.Add(
                        new CardDataBuilder(this)
                        .CreateItem("dragonsFire", "Dragon's Fire", idleAnim: "ShakeAnimationProfile")
                        .SetSprites("dragons-fire-cwhite.png", "item-bg.png")
                        .WithText("Trigger a <keyword=whycats.wildfrost.wildfrostthegathering.flying> ally")
                        .SetDamage(2)
                        .WithCardType("Item")
                        .WithFlavour("Very hot... It hurts to look")
                        .WithValue(50)  // Base price in shop: +-6
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.attackEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Trigger", 1)
                            };
                            data.targetConstraints = new TargetConstraint[]
                            {
                                new Scriptable<TargetConstraintHasTrait>(tcht =>
                                {
                                    tcht.trait = TryGet<TraitData>("Flying");
                                    tcht.ignoreSilenced = false;
                                }),
                            };
                        })
                        );

                    // Delayed Blast Fireball
                    assets.Add(
                        new CardDataBuilder(this)
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
                                SStack("On Card Played Apply Spice To Self", 3),
                            };
                            data.traits = new List<CardData.TraitStacks>()
                            {
                                TStack("Barrage", 1),
                            };
                        })
                        );

                    // Spit Flame
                    assets.Add(
                        new CardDataBuilder(this)
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
                                SStack("Gain Zoomlin When Drawn If 2 Allies With Flying", 1),
                                SStack("On Card Played Apply Attack To Self", 2)
                            };
                        })
                        );

                    // Draconic Lore
                    assets.Add(
                        new CardDataBuilder(this)
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
                                SStack("Gain Zoomlin When Drawn If 2 Allies With Flying", 1),
                            };
                            data.traits = new List<CardData.TraitStacks>()
                            {
                                TStack("Draw", 2),
                            };
                            data.needsTarget = false;
                        })
                        );

                    // Lofty Denial
                    assets.Add(
                        new CardDataBuilder(this)
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
                                SStack("Increase Effects By 2 When Drawn If 2 Allies With Flying", 1),
                            };
                            data.attackEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Snow", 1)
                            };
                        })
                        );

                    // Spell Swindle
                    assets.Add(new CardDataBuilder(this)
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
                                SStack("Snow", 1),
                                SStack("Instant Add Treasure To Hand Equal To Target Counter", 1),
                            };
                        })
                        );

                    // Fireball
                    assets.Add(new CardDataBuilder(this)
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
                                SStack("Bonus Damage Equal To Zoomlin In Hand", 1),
                                SStack("MultiHit", 2)
                            };
                            data.traits = new List<CardData.TraitStacks>
                            {
                                TStack("Aimless", 1)
                            };
                        })
                        );

                    // Shared Animosity
                    assets.Add(
                        new CardDataBuilder(this)
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
                                SStack("Apply Spice To Flying Allies Equal To Twice Flying Allies", 1),
                            };
                            data.needsTarget = false;
                        })
                        );

                    // Bottle-Cap Blast
                    assets.Add(new CardDataBuilder(this)
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
                                SStack("Instant Add Treasure To Hand Equal To Target Health Below Zero", 1),
                            };
                        })
                        );

                    // Explosive Vegetation
                    assets.Add(
                        new CardDataBuilder(this)
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
                                SStack("On Card Played Gain Zoomlin To X Random Cards In Hand", 2),
                            };
                            data.needsTarget = false;
                        })
                        );

                }  // Items 

                {  // Clunkers

                    // Lotus Petal
                    assets.Add(new CardDataBuilder(this)
                        .CreateUnit("lotusPetal", "Lotus Petal", idleAnim: "ShakeAnimationProfile")
                        .WithCardType("Clunker")
                        .SetStats(null, null, 0)
                        .SetSprites("lotus-petal-alee.png", "clunker-bg.png")
                        .WithValue(50)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                new CardData.StatusEffectStacks(Get<StatusEffectData>("Scrap"), 1),
                                SStack("When Destroyed Count Down Leader By X", 1),
                            };
                        })
                        );

                    // Bootlegger's Stash
                    assets.Add(new CardDataBuilder(this)
                        .CreateUnit("bootleggersStash", "Bootlegger's Stash", idleAnim: "FloatAnimationProfile")
                        .WithCardType("Clunker")
                        .SetStats(null, null, 0)
                        .SetSprites("bootleggers-stash-aovchinnikova.png", "clunker-bg.png")
                        .WithValue(50)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                new CardData.StatusEffectStacks(Get<StatusEffectData>("Scrap"), 1),
                                SStack("While Active Treasure To AlliesInRow", 1),
                            };
                        })
                        );

                    // Rites of Flourishing
                    assets.Add(new CardDataBuilder(this)
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
                                new CardData.StatusEffectStacks(Get<StatusEffectData>("Scrap"), 3),
                                SStack("On Card Played Reduce Counter To Allies In Row", 1),
                                SStack("On Card Played Reduce Counter To Front Enemy", 1),
                                SStack("On Card Played Lose Scrap To Self", 1),
                            };
                        })
                        );

                    // Mox Jasper
                    assets.Add(new CardDataBuilder(this)
                        .CreateUnit("moxJasper", "Mox Jasper", idleAnim: "FloatAnimationProfile")
                        .WithCardType("Clunker")
                        .SetStats(null, null, 0)
                        .SetSprites("mox-jasper-sbelledin.png", "clunker-bg.png")
                        .WithValue(50)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.traits = new List<CardData.TraitStacks>
                            {
                                TStack("Zoomlin", 1)
                            };
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                new CardData.StatusEffectStacks(Get<StatusEffectData>("Scrap"), 1),
                                SStack("When Destroyed Gain Zoomlin To Flying Allies In Hand", 1)
                            };
                        })
                        );

                    // Tempting Contract
                    assets.Add(new CardDataBuilder(this)
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
                                SStack("Scrap", 1),
                                SStack("On Card Played Add Treasure To Hand", 1),
                                SStack("On Card Played Reduce Counter To Random Enemy", 1)
                            };
                        })
                        );

                    // Gravitational Shift
                    assets.Add(new CardDataBuilder(this)
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
                                SStack("Scrap", 1),
                                SStack("While Active Increase Attack To Flying Allies", 1),
                                SStack("While Active Reduce Attack To Non Flying", 1)
                            };
                        })
                        );

                    // Revel in Riches
                    assets.Add(new CardDataBuilder(this)
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
                                TStack("Barrage", 1)
                            };
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Scrap", 2),
                                SStack("Gain Spice On Treasure", 1),
                                SStack("Add Treasure To Hand When Enemy Killed", 1),
                                SStack("When Spice X Applied To Self Trigger To Self", 5),
                            };
                        })
                        );

                    // Dragon's Hoard
                    assets.Add(new CardDataBuilder(this)
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
                                TStack("Draw", 1)
                            };
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Scrap", 1),
                                SStack("When Flying Ally Deployed Trigger Self", 1),
                            };
                        })
                        );

                    // Windcrag Siege
                    assets.Add(new CardDataBuilder(this)
                        .CreateUnit("windcragSiege", "Windcrag Siege", idleAnim: "Heartbeat2AnimationProfile")
                        .WithCardType("Clunker")
                        .SetStats(null, null, 0)
                        .SetSprites("windcrag-siege-noleal.png", "clunker-bg.png")
                        .WithFlavour("\"We are the swift, the strong, the blade's sharp shriek! Fear nothing, and strike!\"")
                        .WithValue(50)
                        .SubscribeToAfterAllBuildEvent(data =>
                        {
                            data.startWithEffects = new CardData.StatusEffectStacks[]
                            {
                                SStack("Scrap", 1),
                                SStack("While Active Frenzy To AllyBehind", 1),
                            };
                        })
                        );


                }  // Clunkers 

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

                    // Fireball targeting mode
                    assets.Add(
                        new KeywordDataBuilder(this)
                        .Create("fireball")
                        .WithTitle("Hits an enemy for each card with Zoomlin in hand")  // The in-game name for the upgrade.
                        .WithShowName(true)  // Shows name in Keyword box (as opposed to a nonexistant icon).
                        .WithShow(false)
                        //.WithShowIcon(false)
                        .WithDescription("Hits an enemy for each card with Zoomlin in hand|Hits none if there are none") //Format is body|note.
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

                    // Eternal
                    assets.Add(
                        new KeywordDataBuilder(this)
                        .Create("eternal")
                        .WithTitle("Eternal")  // The in-game name for the upgrade.
                        .WithShowName(true)  // Shows name in Keyword box (as opposed to a nonexistant icon).
                        .WithDescription("Cannot be Injured") //Format is body|note.
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

                    // Fireball
                    assets.Add(
                        new TraitDataBuilder(this)
                        .Create("Fireball")
                        .WithOverrides(Get<TraitData>("Aimless"), Get<TraitData>("Longshot"), Get<TraitData>("Barrage"), Get<TraitData>("Flying"))
                        .SubscribeToAfterAllBuildEvent((trait) =>
                        {
                            trait.keyword = Get<KeywordData>("fireball");
                            trait.effects = new StatusEffectData[] { Get<StatusEffectData>("Random Enemy For Zoomlin") };
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

                    // Eternal
                    assets.Add(
                        new TraitDataBuilder(this)
                        .Create("Eternal")
                        .SubscribeToAfterAllBuildEvent((trait) =>
                        {
                            trait.keyword = TryGet<KeywordData>("eternal");
                            trait.effects = new StatusEffectData[] { };
                        })
                        );

                }  // Traits

                {  // Charms

                }  // Charms
            }  // Dragon deck stuff
            preLoaded = true;
        }
        public override void Load()
        {
            if (!preLoaded) { CreateModAssets(); }  // preLoaded makes sure that the builders are not made again on the 2nd load.
            base.Load();  // Actual loading and adding assets.
            GameMode gameMode = TryGet<GameMode>("GameModeNormal");  // GameModeNormal is the standard game mode. 
            gameMode.classes = gameMode.classes.Append(TryGet<ClassData>("Dragon")).ToArray();
            Events.OnEntityCreated += FixImage;
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
                    if (pool == null) { continue; };  // Skip null pools

                    pool.list.RemoveAllWhere((item) => item == null || item.ModAdded == this);  // Find and remove everything that needs to be removed.
                }
            }
        }
        public override void Unload()
        {
            base.Unload();

            GameMode gameMode = TryGet<GameMode>("GameModeNormal");
            gameMode.classes = RemoveNulls(gameMode.classes);  // Without this, a non-restarted game would crash on tribe selection
            UnloadFromClasses();
            Events.OnEntityCreated -= FixImage;
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
