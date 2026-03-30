using AbsentAvalanche;
using AbsentAvalanche.Builders.Cards.Items;
using AbsentAvalanche.StatusEffectImplementations;
using Deadpan.Enums.Engine.Components.Modding;
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
                    }
                }
                yield return Remove();
            }
            public void RemoveAfterBattle()
            {
                Debug.Log("[WildfrostTheGathering] Is this battle end?");
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
                    }
                }
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

        // Thank you again Abigail for the very yoinkable code! :3
        public class ScriptableTargetsOnBoard : ScriptableAmount
        {
            public bool allies;
            public bool enemies;
            public bool inRow = false;
            public CardType cardType;
            public TraitData hasTrait;

            public override int Get(Entity entity)
            {
                Debug.Log("[WildfrostTheGathering] Am I alive?");
                int num = 0;
                int[] rowIndices = References.Battle.GetRowIndices(entity);
                if (inRow)
                {
                    return InRow(entity, rowIndices);
                }

                if (allies)
                {
                    List<Entity> allies = entity.GetAllies();
                    num += allies.Count((Entity e) => ((object)cardType == null || e.data.cardType == cardType) && (hasTrait == null || e.traits.Any(t => t.data.name.Equals(hasTrait.name))));
                }

                if (enemies)
                {
                    List<Entity> enemies = entity.GetEnemies();
                    num += enemies.Count((Entity e) => ((object)cardType == null || e.data.cardType == cardType) && (hasTrait == null || e.traits.Any(t => t.data.name.Equals(hasTrait.name))));
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
                        List<Entity> allies = entity.GetAlliesInRow(rowIndex);
                        num += allies.Count((Entity e) => ((object)cardType == null || e.data.cardType == cardType) && (hasTrait == null || e.traits.Any(t => t.data.name.Equals(hasTrait.name))));
                    }

                    if (enemies)
                    {
                        List<Entity> enemies = entity.GetEnemiesInRow(rowIndex);
                        num += enemies.Count((Entity e) => ((object)cardType == null || e.data.cardType == cardType) && (hasTrait == null || e.traits.Any(t => t.data.name.Equals(hasTrait.name))));
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

        // ScriptableAmount arbitrary cause I'm le stupid
        public class ScriptableAmountAny : ScriptableAmount
        {
            public int custom = 0;
            public override int Get(Entity entity)
            {
                return custom;
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

        // TODO: Make flying and other targeting modes override each other visually
        private void CreateModAssets()
        {
            {  // Effects
                {  // Companion Effects

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
                            .WithText("When destroyed, draw <{a}>")
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
                            data.applyEqualAmount = true;
                            data.scriptableAmount = scriptAmount;
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

                    // Shivan Dragon: Count Zoomlin cards in hand and gain Spice
                    assets.Add(new StatusEffectDataBuilder(this)
                        .Create<StatusEffectApplyXPreTurn>("Pre Turn Count Zoomlin In Hand & Gain Spice For Each")
                        .WithText("Before attacking, gain {a} <keyword=spice> for each card with <keyword=zoomlin> in hand")
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

                    // Delayed Blast Fireball: Remove Spice
                    assets.Add(new StatusEffectDataBuilder(this)
                        .Create<StatusEffectInstantRemoveSpecificEffect>("Remove Spice")
                        .SubscribeToAfterAllBuildEvent<StatusEffectInstantRemoveSpecificEffect>(data =>
                        {
                            data.effectToClean = "Spice";
                        })
                        );

                    // Delayed Blast Fireball: Remove Spice From Self
                    assets.Add(new StatusEffectDataBuilder(this)
                        .Create<StatusEffectApplyXOnCardPlayed>("On Card Played Clear Own Spice")
                        .SubscribeToAfterAllBuildEvent<StatusEffectApplyXOnCardPlayed>(data =>
                        {
                            data.effectToApply = TryGet<StatusEffectInstantRemoveSpecificEffect>("Remove Spice");
                            data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
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
                            ScriptableAmountAny scriptAmount = ScriptableAmountAny.CreateInstance<ScriptableAmountAny>();
                            scriptAmount.custom = 2;
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

                }  // Item Effects
            }  // Effects

            {  // Companions

                // Dragon Token
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("dragonToken", "Dragon Token")
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
                        data.greetMessages = new string[1] { "Woah a token in the companion pool? That\'s not supposed to happen"};
                    })
                    );

                // Dragon Token with Spark
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("dragonTokenSpark", "Dragon Illusion Token")
                    .SetSprites("dragon-illusion-token-amar.png", "companion-bg.png")
                    .SetStats(1, 4, 3)
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
                    new CardDataBuilder(this).CreateUnit("goldspanDragon", "Goldspan Dragon")
                    .SetSprites("goldspan-dragon-amar.png", "companion-bg.png")
                    .SetStats(7, 4, 4)
                    .WithCardType("Friendly")
                    .WithFlavour("<i>\"You see, most places have mice or mosquitoes...\"</i>")
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
                            "<i>\"You see, most places have mice or mosquitoes...\"</i>"};
                    })
                    );

                // Ancient Copper Dragon
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("ancientCopperDragon", "Ancient Copper Dragon")
                    .SetSprites("ancient-copper-dragon-ajmanzan.png", "companion-bg.png")
                    .SetStats(8, 5, 5)
                    .WithCardType("Friendly")
                    .WithFlavour("<i>You can never have enough gold</i>")
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
                        data.greetMessages = new string[1] { "<i>You can never have enough gold</i>" };
                    })
                    );

                // Atsushi, Blazing Sky
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("atsushiBlazingSky", "Atsushi, the Blazing Sky")
                    .SetSprites("atsushi-blazing-sky-vaminguez.png", "companion-bg.png")
                    .SetStats(7, 4, 3)
                    .WithCardType("Friendly")
                    .WithFlavour("<i>Deep crimson in color and one hundred feet long, she is the reincarnation of the dragon spirit <b>Ryusei</b></i>")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
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
                        };
                        data.greetMessages = new string[3] { "<i>Deep crimson in color and one hundred feet long, she is the reincarnation of the dragon spirit <b>Ryusei</b></i>",
                            "<i><b>Ryusei</b> and <b>Jugan</b> sealed themselves and the three other dragon spirits in an egg under <b>Boseiju</b>. They hatched 50 years later, reborn as <b>Ao</b>, <b>Kairi</b>, <b>Junji</b>, <b>Atsushi</b>, and <b>Kura</b></i>",
                            "<i>The reborn form of <b>Ryusei</b>, protector of <b>Sokenzan</b></i>" };
                    })
                    );

                // Utvara Hellkite
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("utvaraHellkite", "Utvara Hellkite")
                    .SetSprites("utvara-hellkite-mzug.png", "companion-bg.png")
                    .SetStats(8, 4, 5)
                    .WithCardType("Friendly")
                    .WithFlavour("<i>The fear of dragons is as old and as powerful as the fear of death itself</i>")
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
                    .AddPool("MagicUnitPool")
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
                    new CardDataBuilder(this).CreateUnit("terrorOfThePeaks", "Terror of the Peaks")
                    .SetSprites("terror-of-the-peaks-jraphael.png", "companion-bg.png")
                    .SetStats(6, 5, 5)
                    .WithCardType("Friendly")
                    .WithFlavour("<i>If it comes for you, die boldly or die swiftly—for die you will</i>")
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
                        data.greetMessages = new string[1] { "<i>If it comes for you, die boldly or die swiftly — for die you will</i>" };
                    })
                    );

                // Captain Lannery Storm
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("captainLanneryStorm", "Captain Lannery Storm")
                    .SetSprites("captain-lannery-storm-crallis.png", "companion-bg.png")
                    .SetStats(3, 2, 3)
                    .WithCardType("Friendly")
                    .WithFlavour("\"I believe in love at first shine\"")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.startWithEffects = new CardData.StatusEffectStacks[]
                        {
                            SStack("Gain Attack On Treasure", 1),
                        };
                        data.greetMessages = new string[6] { "\"I believe in love at first shine\"",
                        "\"Skim all the gold and magic rocks you want, but if I see one greasy fingerprint on my new boots, you’ll be drinking bilgewater for a month\"",
                        "\"Charge like a red-hot cannonball straight to your target. You slow down, you sink\"",
                        "\"Just imagine what’s waiting around the bend. Adventure. Discovery. Riches for the taking. This is why I sail\"",
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
                        data.greetMessages = new string[2] { "<i>Automated systems at the <b>Tolarian Academy</b> sort new acquisitions for optimal use, determining which should be studied, eaten, or sold</i>",
                        "<i>It shapes wonders beyond our wildest dreams</i>\n<i>Like sandwiches!</i>"};
                    })
                    );

                // Glorybringer
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("glorybringer", "Glorybringer")
                    .SetSprites("glorybringer-sburley.png", "companion-bg.png")
                    .SetStats(5, 3, 3)
                    .WithCardType("Friendly")
                    .WithFlavour("<i>What the initiates face in the final trial is completely at <b>Hazoret's</b> discretion</i>")
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
                        data.greetMessages = new string[1] { "<i>What the initiates face in the final trial is completely at <b>Hazoret's</b> discretion</i>" };
                    })
                    );

                // Earthquake Dragon
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("earthquakeDragon", "Earthquake Dragon")
                    .SetSprites("earthquake-dragon-jgrenier.png", "companion-bg.png")
                    .SetStats(10, 10, 8)
                    .WithCardType("Friendly")
                    .WithFlavour("<i>An empire can take centuries to build but mere moments to destroy</i>")
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
                        data.greetMessages = new string[2] { "<i>An empire can take centuries to build but mere moments to destroy</i>", "*The ground rumbles beneath your feet*" };
                    })
                    );

                // Ojutai, Soul of Winter
                assets.Add(
                    new CardDataBuilder(this).CreateUnit("ojutaiSoulOfWinter", "Ojutai, Soul of Winter")
                    .SetSprites("ojutai-soul-of-winter-cstone.png", "companion-bg.png")
                    .SetStats(9, 2, 4)
                    .WithCardType("Friendly")
                    .WithFlavour("<i>\"Human enlightenment is a firefly that sparks in the night. Dragon enlightenment is a beacon that disperses all darkness.\"</i>")
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
                    .AddPool("MagicUnitPool")
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
                    new CardDataBuilder(this).CreateUnit("shivanDragon", "Shivan Dragon")
                    .SetSprites("shivan-dragon-dgiancola.png", "companion-bg.png")
                    .SetStats(7, 4, 3)
                    .WithCardType("Friendly")
                    .WithFlavour("<i>The undisputed master of the mountains of Shiv</i>")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
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
                    new CardDataBuilder(this).CreateUnit("manaformHellkite", "Manaform Hellkite")
                    .SetSprites("manaform-hellkite-amar.png", "companion-bg.png")
                    .SetStats(5, 2, 4)
                    .WithCardType("Friendly")
                    .WithFlavour("<i>Just because it's a big, strong, unthinking beast of the sky intent on burning your house doesn't mean it can't use magic<i>")
                    .WithValue(45)
                    .AddPool("MagicUnitPool")
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

            }  // Companions

            {  // Items

                // Treasure
                assets.Add(
                    new CardDataBuilder(this)
                    .CreateItem("treasure", "Treasure")
                    .SetSprites("treasure-orichards.png", "item-bg.png")
                    .SetDamage(null)
                    .WithCardType("Item")
                    .WithFlavour("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$")
                    .WithPools("GeneralItemPool")
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

                // Dragon's Fire (no art!)
                assets.Add(
                    new CardDataBuilder(this)
                    .CreateItem("dragonsFire", "Dragon's Fire")
                    .SetSprites("placeholder-item.png", "item-bg.png")
                    .WithText("Trigger a <keyword=whycats.wildfrost.wildfrostthegathering.flying> ally")
                    .SetDamage(2)
                    .WithCardType("Item")
                    .WithFlavour("Very hot... It hurts to look")
                    .WithPools("GeneralItemPool")
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

                // Delayed Blast Fireball (no art!)
                assets.Add(
                    new CardDataBuilder(this)
                    .CreateItem("delayedBlastFireball", "Delayed Blast Fireball")
                    .SetDamage(3)
                    .WithCardType("Item")
                    .WithFlavour("The spell will fall upon a crowd like a dragon, ancient and full of death")
                    .SetSprites("placeholder-item.png", "item-bg.png")
                    .WithPools("GeneralItemPool")
                    .WithValue(50)  // Base price in shop: +-6
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.startWithEffects = new CardData.StatusEffectStacks[]
                        {
                            SStack("On Card Played Clear Own Spice", 1),
                            SStack("On Card Played Apply Spice To Self", 3),
                        };
                        data.traits = new List<CardData.TraitStacks>()
                        {
                            TStack("Barrage", 1),
                        };
                    })
                    );

                // Spit Flame (no art!)
                assets.Add(
                    new CardDataBuilder(this)
                    .CreateItem("spitFlame", "Spit Flame")
                    .SetSprites("placeholder-item.png", "item-bg.png")
                    .SetDamage(2)
                    .WithCardType("Item")
                    .WithFlavour("\"Spread out, you idiots! Spread out!\"\n—<b>Marsden, party leader</b>, last words")
                    .WithPools("GeneralItemPool")
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

                // Draconic Lore (no art!)
                assets.Add(
                    new CardDataBuilder(this)
                    .CreateItem("draconicLore", "Draconic Lore")
                    .SetSprites("placeholder-item.png", "item-bg.png")
                    .SetDamage(null)
                    .WithCardType("Item")
                    .WithFlavour("The wyrmling studied the ancient carvings and dreamed of a day when her own exploits would be immortalized in stone")
                    .WithPools("GeneralItemPool")
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

                // Lofty Denial (no art!)
                assets.Add(
                    new CardDataBuilder(this)
                    .CreateItem("loftyDenial", "Lofty Denial")
                    .SetSprites("placeholder-item.png", "item-bg.png")
                    .SetDamage(0)
                    .WithCardType("Item")
                    .WithFlavour("\"As one, nature lifts its voice to tell you this: \'No.\'\"")
                    .WithPools("GeneralItemPool")
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

                // Spell Swindle (no art!)
                assets.Add(
                    new CardDataBuilder(this)
                    .CreateItem("spellSwindle", "Spell Swindle")
                    .SetSprites("placeholder-item.png", "item-bg.png")
                    .SetDamage(0)
                    .WithCardType("Item")
                    .WithFlavour("Honesty is the first casualty of war")
                    .WithPools("GeneralItemPool")
                    .WithValue(40)  // Base price in shop: +-6
                    .SubscribeToAfterAllBuildEvent(data =>
                    {
                        data.attackEffects = new CardData.StatusEffectStacks[]
                        {
                            SStack("Instant Add Treasure To Hand Equal To Target Counter", 1),
                            SStack("Snow", 2)
                        };
                    })
                    );

            }  // Items

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
