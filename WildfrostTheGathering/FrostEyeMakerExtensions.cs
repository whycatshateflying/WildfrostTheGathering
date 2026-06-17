using Deadpan.Enums.Engine.Components.Modding;
using System.Linq;
using UnityEngine;
using static WildfrostTheGathering.WildfrostTheGathering;

namespace FrostEyeMakerExtensions
{
    internal static class FrostEyeMakerExtensions
    {
        public static CardDataBuilder AddEye(this CardDataBuilder builder, float positionX, float positionY, float scaleX = 1f, float scaleY = 1f, float rotation = 0f)
        {
            // EyeDataBuilders want the name of the card the eyes are for. We get it from the CardDataBuilder.
            string cardName = builder._data.name;

            // If the most recently made asset is an EyeDataBuilder for the same card, we will add an eye to it.
            EyeDataBuilder eyeBuilder;
            if (assets.LastOrDefault() is EyeDataBuilder builder2 && builder2._data.cardData == cardName)
            {
                eyeBuilder = builder2;
            }
            else
            {
                // Otherwise, we make a new EyeDataBuilder.
                eyeBuilder = new EyeDataBuilder(Instance).Create(cardName.Replace(Instance.GUID + ".", "") + "EyeData").WithCardData(cardName).WithEyes();
                assets.Add(eyeBuilder);
            }

            // We make a new eye with the given position, scale, and rotation.
            EyeData.Eye eye = new EyeData.Eye()
            {
                position = new Vector2(positionX, positionY),
                scale = new Vector2(scaleX, scaleY),
                rotation = rotation
            };

            // We replace the EyeDataBuilder's array of eyes with an array containing all of the eyes it already had, plus our new eye.
            eyeBuilder.FreeModify(data => data.eyes = data.eyes.With(eye).ToArray());
            return builder;
        }
    }
}
