using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
                                wtg.TryGet<TraitData>("Aimless"),
                                wtg.TryGet < TraitData >("Barrage"),
                                wtg.TryGet < TraitData >("Longshot"),
                                wtg.TryGet < TraitData >("Fireball")
                    };
                    TraitData aimless = wtg.TryGet<TraitData>("Aimless");
                    aimless.overrides = aimless.overrides.With(trait);
                    TraitData barrage = wtg.TryGet<TraitData>("Barrage");
                    barrage.overrides = barrage.overrides.With(trait);
                    TraitData longshot = wtg.TryGet<TraitData>("Longshot");
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
                                wtg.TryGet < TraitData >("Aimless"),
                                wtg.TryGet < TraitData >("Longshot"),
                                wtg.TryGet < TraitData >("Barrage"),
                                wtg.TryGet < TraitData >("Flying")
                    };
                    TraitData aimless = wtg.TryGet<TraitData>("Aimless");
                    aimless.overrides = aimless.overrides.With(trait);
                    TraitData barrage = wtg.TryGet<TraitData>("Barrage");
                    barrage.overrides = barrage.overrides.With(trait);
                    TraitData longshot = wtg.TryGet<TraitData>("Longshot");
                    longshot.overrides = longshot.overrides.With(trait);
                })
                );

            // Unplayable
            assets.Add(
                new TraitDataBuilder(wtg)
                .Create("Unplayable")
                .SubscribeToAfterAllBuildEvent((trait) =>
                {
                    trait.keyword = wtg.TryGet<KeywordData>("unplayable");
                    trait.effects = new StatusEffectData[] { wtg.TryGet<WildfrostTheGathering.StatusEffectUnplayable>("Unplayable") };
                })
                );

            // Eternal
            assets.Add(
                new TraitDataBuilder(wtg)
                .Create("Eternal")
                .SubscribeToAfterAllBuildEvent((trait) =>
                {
                    trait.keyword = wtg.TryGet<KeywordData>("eternal");
                    trait.effects = new StatusEffectData[] { };
                })
                );
            Debug.Log("[WTG] Traits loaded!");
        }
    }
}
