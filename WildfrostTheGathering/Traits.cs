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

            // Flying
            assets.Add(
                new TraitDataBuilder(wtg)
                .Create("Flying")
                .SubscribeToAfterAllBuildEvent((trait) =>
                {
                    trait.keyword = wtg.Get<KeywordData>("flying");
                    trait.effects = new StatusEffectData[] { wtg.Get<StatusEffectData>("Prioritize Bosses") };
                    trait.overrides = new TraitData[]
                    {
                        TryGet <TraitData>("Aimless"),
                        TryGet <TraitData>("Barrage"),
                        TryGet <TraitData>("Longshot"),
                        TryGet <TraitData>("Fireball")
                    };
                    TraitData aimless = TryGet<TraitData>("Aimless");
                    aimless.overrides = aimless.overrides.With(trait);
                    TraitData barrage = TryGet<TraitData>("Barrage");
                    barrage.overrides = barrage.overrides.With(trait);
                    TraitData longshot = TryGet<TraitData>("Longshot");
                    longshot.overrides = longshot.overrides.With(trait);
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
                    trait.overrides = new TraitData[]
                    {
                        TryGet <TraitData>("Aimless"),
                        TryGet <TraitData>("Longshot"),
                        TryGet <TraitData>("Barrage"),
                        TryGet <TraitData>("Flying")
                    };
                    TraitData aimless = TryGet<TraitData>("Aimless");
                    aimless.overrides = aimless.overrides.With(trait);
                    TraitData barrage = TryGet<TraitData>("Barrage");
                    barrage.overrides = barrage.overrides.With(trait);
                    TraitData longshot = TryGet<TraitData>("Longshot");
                    longshot.overrides = longshot.overrides.With(trait);
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

            Debug.Log("[WTG] Traits loaded!");
        }
    }
}
