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
    internal class Traits
    {
        internal static void Load(List<object> assets, WildfrostTheGathering wtg)
        {
            Debug.Log("[WTG] Traits loading!");

            List<TraitData> targetingModes = [];
            targetingModes.Add(TryGet<TraitData>("Aimless"));
            targetingModes.Add(TryGet<TraitData>("Barrage"));
            targetingModes.Add(TryGet<TraitData>("Longshot"));

            // Ongoing Barrage
            assets.Add(new TraitDataBuilder(wtg)
                .Create("OngoingBarrage")
                .SubscribeToAfterAllBuildEvent((trait) =>
                {
                    trait.keyword = wtg.Get<KeywordData>("ongoingbarrage");
                    trait.effects = new StatusEffectData[] { wtg.Get<StatusEffectData>("Hit All Enemies In Row") };
                    trait.overrides = new TraitData[] { };
                    targetingModes.Add(trait);
                })
                );

            // Ongoing Flying
            assets.Add(new TraitDataBuilder(wtg)
                .Create("OngoingFlying")
                .SubscribeToAfterAllBuildEvent((trait) =>
                {
                    trait.keyword = wtg.Get<KeywordData>("ongoingflying");
                    trait.effects = new StatusEffectData[] { wtg.Get<StatusEffectData>("Prioritize Bosses") };
                    trait.overrides = new TraitData[] { };
                    targetingModes.Add(trait);
                })
                );

            // Flying
            assets.Add(new TraitDataBuilder(wtg)
                .Create("Flying")
                .SubscribeToAfterAllBuildEvent((trait) =>
                {
                    trait.keyword = wtg.Get<KeywordData>("flying");
                    trait.effects = new StatusEffectData[] { wtg.Get<StatusEffectData>("Prioritize Bosses") };
                    trait.overrides = new TraitData[] { };
                    targetingModes.Add(trait);
                })
                );

            // Counts As Flying
            assets.Add(
                new TraitDataBuilder(wtg)
                .Create("CountsAsFlying")
                .SubscribeToAfterAllBuildEvent((trait) =>
                {
                    trait.keyword = wtg.Get<KeywordData>("invisible");
                })
                );

            // Fireball
            assets.Add(
                new TraitDataBuilder(wtg)
                .Create("Fireball")
                .SubscribeToAfterAllBuildEvent((trait) =>
                {
                    trait.keyword = wtg.Get<KeywordData>("fireball");
                    trait.effects = new StatusEffectData[] { wtg.Get<StatusEffectData>("Random Enemy For Zoomlin") };
                    trait.overrides = new TraitData[] { };
                    targetingModes.Add(trait);
                })
                );

            // Unplayable
            assets.Add(
                new TraitDataBuilder(wtg)
                .Create("Unplayable")
                .SubscribeToAfterAllBuildEvent((trait) =>
                {
                    trait.keyword = TryGet<KeywordData>("unplayable");
                    trait.effects = new StatusEffectData[] { TryGet<WildfrostTheGathering.StatusEffectUnplayable>("Unplayable") };
                })
                );

            // Trample
            assets.Add(
                new TraitDataBuilder(wtg)
                .Create("Trample")
                .SubscribeToAfterAllBuildEvent((trait) =>
                {
                    trait.keyword = TryGet<KeywordData>("trample");
                    trait.effects = new StatusEffectData[] { TryGet<WildfrostTheGathering.StatusEffectTrample>("Trample") };
                    TraitData barrage = TryGet<TraitData>("Barrage");
                    barrage.overrides = barrage.overrides.With(trait);
                })
                );

            // Eternal
            assets.Add(
                new TraitDataBuilder(wtg)
                .Create("Eternal")
                .SubscribeToAfterAllBuildEvent((trait) =>
                {
                    trait.keyword = TryGet<KeywordData>("eternal");
                    trait.effects = new StatusEffectData[] { };
                })
                );

            // Conspiracy
            assets.Add(
                new TraitDataBuilder(wtg)
                .Create("Conspiracy")
                .SubscribeToAfterAllBuildEvent((trait) =>
                {
                    trait.keyword = TryGet<KeywordData>("conspiracy");
                    trait.effects = new StatusEffectData[] { };
                })
                );

            // Prey
            assets.Add(
                new TraitDataBuilder(wtg)
                .Create("Prey")
                .SubscribeToAfterAllBuildEvent((trait) =>
                {
                    trait.keyword = TryGet<KeywordData>("prey");
                    trait.effects = new StatusEffectData[] { };
                })
                );

            // Suspected
            assets.Add(
                new TraitDataBuilder(wtg)
                .Create("Suspected")
                .SubscribeToAfterAllBuildEvent((trait) =>
                {
                    trait.keyword = TryGet<KeywordData>("suspected");
                    trait.effects = new StatusEffectData[] { TryGet<WildfrostTheGathering.StatusEffectSuspected>("Suspected") };
                })
                );

            // UNUSED (I can't be bothered to figure out how to hook something directly onto AfterAllBuildEvent
            assets.Add(new TraitDataBuilder(wtg)
                .Create("UNUSED")
                .SubscribeToAfterAllBuildEvent((bleh) =>
                {
                    foreach (TraitData trait in targetingModes)
                    {
                        foreach (TraitData otherTrait in targetingModes)
                        {
                            if (trait == otherTrait)
                            {
                                continue;
                            }
                            trait.overrides = trait.overrides.With(otherTrait);
                        }
                    }
                })
                );

            Debug.Log("[WTG] Traits loaded!");
        }
    }
}
