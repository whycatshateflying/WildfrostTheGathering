using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static WildfrostTheGathering.WildfrostTheGathering;
namespace WildfrostTheGathering.Generic
{
    internal class Leaders
    {
        internal static void Load(List<object> assets, WildfrostTheGathering wtg)
        {
            Debug.Log("[WTG] Generic leaders loading!");


            // Helga leader
            assets.Add(
            new CardDataBuilder(wtg)
            .CreateUnit("helgaSkittishSeerLeader", "Helga, Skittish Seer", idleAnim: "SwayAnimationProfile")
            .WithCardType("Leader")
            .SetSprites("placeholder-companion.png", "companion-bg.png")
            .SetStats(7, 2, 3)  // 5-9, 2, 3
            .WithFlavour("\"The kings in the dark will return, the mage in blue will bring about the end\"")
            .WithValue(25)
            .SubscribeToAfterAllBuildEvent(data =>
            {
                data.startWithEffects = new CardData.StatusEffectStacks[]
                {
                    wtg.SStack("Gain Attack And Health And Draw When Ally with 4 Counter Is Deployed", 1)
                };
                data.greetMessages = new string[] { "\"The kings in the dark will return, the mage in blue will bring about the end\"" };
                data.createScripts = new CardScript[]
                {
                    GiveUpgrade(),
                    AddRandomHealth(-2,2),
                };
            })
            );

            Debug.Log("[WTG] Generic leaders loaded!");
        }
    }
}
