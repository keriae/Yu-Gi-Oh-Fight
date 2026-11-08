using UnityEngine;


namespace YuGiOhFight.Client.Duel
{
    public static class DuelEffectUtil
    {
        private const float dummyDeckHeightScaleFor40Cards = 0.91f;

        public static void SetDeckModelAppearance(Presentation.Elements.ElementObjectManager deckManager, int cardCount, Presentation.Elements.ElementObjectManager fromManager)
        {
            if(cardCount > 0)
            {
                var deckSetOffset = deckManager.GetElement<Transform>("DeckSetOffset");
                deckSetOffset.localScale = new Vector3(1, dummyDeckHeightScaleFor40Cards * cardCount / 40, 1);
                var label = deckManager.GetComponent<Presentation.Elements.ElementObject>().label;
                if (label.Contains("Main") || label == "EnExDeck")
                    deckSetOffset.localEulerAngles = new Vector3(0, 180, 0);
                var protectorMat = fromManager.GetNestedElement<MeshRenderer>("CardShuffleTop/CardModel01_back").material;
                deckManager.GetNestedElement<MeshRenderer>("DummyDeck/DummyCardModel_back").material = protectorMat;
            }
            else
            {
                Object.Destroy(deckManager.GetElement("BaseFog"));
                deckManager.GetElement<Transform>("DeckSetOffset").localScale = Vector3.zero;
            }
        }

    }
}
