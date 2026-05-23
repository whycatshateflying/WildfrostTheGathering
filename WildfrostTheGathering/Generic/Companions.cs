using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WildfrostTheGathering.Generic
{
    internal class Companions
    {
        internal static void Load(List<object> assets, WildfrostTheGathering wtg)
        {
            Debug.Log("[WTG] Generic companions loading!");
            // Fear of Sleep Paralysis
            assets.Add(new CardDataBuilder(wtg)
                .CreateUnit("fearOfSleepParalysis", "Fear of Sleep Paralysis", idleAnim: "FloatSquishAnimationProfile")
                .SetStats(7, 2, 3)
                .SetSprites("fear-of-sleep-paralysis-jraphael.png", "companion-bg.png")
                .WithValue(50)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                                    wtg.TStack("Flying", 1),
                    };
                    data.attackEffects = new CardData.StatusEffectStacks[]
                    {
                                    wtg.SStack("Snow", 2),
                    };
                    data.greetMessages = new string[2] { "Don’t fear the darkness. Fear what it hides",
                                                        "Sleep doesn’t always mean rest"};
                })
                );
            Debug.Log("[WTG] Generic companions loaded!");
        }
    }
}
