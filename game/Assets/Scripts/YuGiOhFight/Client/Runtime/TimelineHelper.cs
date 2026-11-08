using Cysharp.Threading.Tasks;


using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


using YuGiOhFight.Client.Presentation.Extensions;


namespace YuGiOhFight.Client.Runtime
{
    public static class TimelineHelper
    {
        private static Transform dummyCard;
        private static int code;
        private static MDPro3.Duel.YGOSharp.Card data;
        private static List<Duel.GameCard> materials;
        private static uint reason;
        private static GameObject unitCards;
        private static bool useSpecialUnitCard;

        public static async UniTask PlaySummonTimelineAsync()
        {
            if (Controllers.OcgCore.summonCard == null)
                return;

            code = Controllers.OcgCore.summonCard.GetData().Id;
            data = MDPro3.Duel.YGOSharp.CardsManager.Get(code);
            materials = Controllers.OcgCore.materialCards;
            reason = materials[0].p.reason;

            await Controllers.CutinViewer.CacheCutin(code);
            await CacheUnitCardsAsync();
            if (materials.Count > 0)
                _ = ShowUnitCardsAsync();

            var director = GetPlayableDirector();
            _ = director.AutoDestroy(false);
            Program.instance.ocgcore.AllGameObjects.Add(director.gameObject);
            var strongSummontime = GetDirectorLabelTime(director, "StrongSummon");
            var startCardTime = GetDirectorLabelTime(director, "StartCard");

            var directorTask = director.WaitToTimeAsync(strongSummontime);
            var inputTask = UniTask.WaitUntil(() => UserInput.MouseLeftDown);

            await UniTask.WhenAny(directorTask, inputTask);
            if(director.time < strongSummontime)
            {
                director.time = strongSummontime;
                AudioManager.ResetSESource();
            }
            Object.Destroy(unitCards);

            await director.WaitToTimeAsync(startCardTime, false);
            await StartCard();
        }

        private static async UniTask CacheUnitCardsAsync()
        {
            // Super Polymerization
            if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Fusion)
                && Controllers.OcgCore.chainSolvingCard != null
                && Controllers.OcgCore.chainSolvingCard.GetData().GetOriginalID() == 48130397)
            {
                if (materials.Count > 8)
                    await ABLoader.LoadFromFolderAsync<PlayableDirector>
                        ("MasterDuel/Timeline/Summon/SummonFusion/SummonFusion07445ShowUnitCard08", true, false);
                else
                    await ABLoader.LoadFromFolderAsync<PlayableDirector>
                        ("MasterDuel/Timeline/Summon/SummonFusion/SummonFusion07445ShowUnitCard0" + materials.Count, true, false);
            }
            // Invocation
            else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Fusion)
                && Controllers.OcgCore.chainSolvingCard != null
                && Controllers.OcgCore.chainSolvingCard.GetData().GetOriginalID() == 74063034)
            {
                var path = "MasterDuel/Timeline/Summon/SummonFusion/SummonFusion12852ShowUnitCard08";
                if (materials.Count < 8)
                    path = "MasterDuel/Timeline/Summon/SummonFusion/SummonFusion12852ShowUnitCard0" + materials.Count;
                if (Controllers.OcgCore.chainSolvingCard.GetData().Id == 74063035)
                    path = path.Replace("12852", "03432");
                await ABLoader.LoadFromFolderAsync<PlayableDirector>(path, true, false);
            }
        }

        private static async UniTaskVoid ShowUnitCardsAsync()
        {
            useSpecialUnitCard = false;
            int maxCount = 8;

            // Super Polymerization
            if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Fusion)
                && Controllers.OcgCore.chainSolvingCard != null
                && Controllers.OcgCore.chainSolvingCard.GetData().GetOriginalID() == 48130397)
            {
                useSpecialUnitCard = true;

                if (materials.Count > 8)
                    unitCards = ABLoader.LoadFromFolder<PlayableDirector>
                        ("MasterDuel/Timeline/Summon/SummonFusion/SummonFusion07445ShowUnitCard08", true, true);
                else
                    unitCards = ABLoader.LoadFromFolder<PlayableDirector>
                        ("MasterDuel/Timeline/Summon/SummonFusion/SummonFusion07445ShowUnitCard0" + materials.Count, true, true);
                //TODO: 如果在await之前触发跳过TL，unitCards无法被立即删除
            }
            // Invocation
            else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Fusion)
                && Controllers.OcgCore.chainSolvingCard != null
                && Controllers.OcgCore.chainSolvingCard.GetData().GetOriginalID() == 74063034)
            {
                useSpecialUnitCard = true;

                var path = "MasterDuel/Timeline/Summon/SummonFusion/SummonFusion12852ShowUnitCard08";
                if (materials.Count < 8)
                    path = "MasterDuel/Timeline/Summon/SummonFusion/SummonFusion12852ShowUnitCard0" + materials.Count;
                if (Controllers.OcgCore.chainSolvingCard.GetData().Id == 74063035)
                    path = path.Replace("12852", "03432");
                unitCards = await ABLoader.LoadFromFolderAsync<PlayableDirector>(path, true, true);
            }
            else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Fusion))
            {
                if (materials.Count > 8)
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonFusionShowUnitCard08");
                else
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonFusionShowUnitCard0" + materials.Count);
            }
            else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Synchro))
            {
                if (materials.Count > 6)
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonSynchroShowUnitCard06");
                else
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonSynchroShowUnitCard0" + materials.Count);
                maxCount = 6;
            }
            else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Xyz))
            {
                if (materials.Count > 6)
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonXYZShowUnitCard06");
                else
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonXYZShowUnitCard0" + materials.Count);
                maxCount = 6;
            }
            else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Link))
            {
                if (materials.Count > 8)
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonLinkShowUnitCard08");
                else
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonLinkShowUnitCard0" + materials.Count);
            }
            else
            {
                if (materials.Count > 6)
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonRitualShowUnitCard06");
                else
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonRitualShowUnitCard0" + materials.Count);
                maxCount = 6;
            }

            Presentation.Elements.ElementObjectManager manager = unitCards.GetComponent<Presentation.Elements.ElementObjectManager>();
            for (int i = 0; i < Mathf.Min(materials.Count, maxCount); i++)
            {
                Presentation.Elements.ElementObjectManager dummyCard = manager.GetElement<Presentation.Elements.ElementObjectManager>("DummyCard" + (i + 1).ToString("00"));
                var renderer = dummyCard.GetElement<MeshRenderer>("DummyCardModel_front");
                var code = materials[i].GetData().Id;
                if (code == 0)
                    code = materials[i].GetValidData().Id;
                _ = RefreshCardFace(renderer, code);

                if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Synchro))
                {
                    MDPro3.Duel.YGOSharp.Card mData = materials[i].GetData();
                    if (mData.HasType(MDPro3.Duel.YGOSharp.CardType.Tuner))
                        Object.Destroy(manager.GetElement($"Synchro01Card0{i + 1}"));
                    else
                        Object.Destroy(manager.GetElement($"Synchro00Card0{i + 1}"));
                }
            }

            Program.instance.ocgcore.AllGameObjects.Add(unitCards);
            var director = unitCards.GetComponent<PlayableDirector>();
            await director.AutoDestroy();
        }

        private static PlayableDirector GetPlayableDirector()
        {
            if(data.HasType(MDPro3.Duel.YGOSharp.CardType.Fusion))
                return GetFusionDirector();
            else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Synchro))
                return GetSynchroDirector();
            else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Xyz))
                return GetXyzDirector();
            else if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Link))
                return GetLinkDirector();
            else
                return GetRitualDirector();
        }

        private static PlayableDirector GetFusionDirector()
        {
            GameObject go;
            if (materials.Count > 5)
                go = ABLoader.LoadMasterDuelGameObject("FusionNum");
            else
                go = ABLoader.LoadMasterDuelGameObject($"SummonFusion0{materials.Count}_01");

            Presentation.Elements.ElementObjectManager manager = go.GetComponent<Presentation.Elements.ElementObjectManager>();
            dummyCard = manager.GetElement<Transform>("PostFusionPosDummy");

            var cardModel = manager.GetElement<Renderer>("CardModel");
            _ = RefreshCardFrame(cardModel, code);
            var postFusion = manager.GetElement<Renderer>("PostFusion");
            _ = RefreshCardFrame(postFusion, code);

            switch (materials.Count)
            {
                case 1:
                    var card01 = manager.GetElement<Renderer>("FusionCard01");
                    _ = RefreshCardFrame(card01, materials.Count, 0);
                    break;
                case 2:
                case 3:
                case 4:
                    card01 = manager.GetElement<Renderer>("FusionCard01");
                    var card02 = manager.GetElement<Renderer>("FusionCard02");
                    _ = RefreshCardFrame(card01, materials.Count, 0);
                    _ = RefreshCardFrame(card02, materials.Count, 0);
                    break;
                case 5:
                    for (int i = 1; i < 6; i++)
                    {
                        var card = manager.GetElement<Renderer>("FusionCard0" + i);
                        _ = RefreshCardFrame(card, 1, i - 1);
                    }
                    var cardAll = manager.GetElement<Renderer>("FusionCardAll");
                    _ = RefreshCardFrame(cardAll, 5, 0);
                    break;
                default:
                    for (int i = 1; i < 7; i++)
                    {
                        var card = manager.GetElement<Renderer>("FusionCard0" + i);
                        _ = RefreshCardFrame(card, 1, i - 1);
                    }
                    break;
            }

            if (useSpecialUnitCard && materials.Count > 0)
            {
                manager.GetElement("BlackNormal").SetActive(false);
                manager.GetElement("BlackSSummon").SetActive(true);
            }

            var director = go.GetComponent<PlayableDirector>();
            return director;
        }

        private static PlayableDirector GetSynchroDirector()
        {
            GameObject go;
            if (materials.Count > 0)
                go = ABLoader.LoadMasterDuelGameObject("SummonSynchro01");
            else
                go = ABLoader.LoadMasterDuelGameObject("SummonSynchro02");

            Presentation.Elements.ElementObjectManager manager = go.GetComponent<Presentation.Elements.ElementObjectManager>();
            Presentation.Elements.ElementObjectManager subManager = manager.GetElement<Presentation.Elements.ElementObjectManager>("SummonSynchroPostSynchro");
            dummyCard = subManager.GetElement<Transform>("DummyCardSynchro");

            var cardFace = subManager.GetNestedElement<Renderer>("DummyCardSynchro/DummyCardModel_front");
            _ = RefreshCardFace(cardFace, code);
            var postAdd = subManager.GetElement<Renderer>("DummyCardSynchroAdd");
            _ = RefreshCardFace(postAdd, code, true);

            if (materials.Count > 0)
            {
                var tunerLevel = GetTunerLevel();
                int level = data.Level;
                int nonTunerLevel = level - tunerLevel;

                for (int i = 1; i < 12; i++)
                    if (i != nonTunerLevel)
                    {
                        manager.GetElement("NumberNonTuner" + i.ToString("00")).SetActive(false);
                        manager.GetElement("SynchroStarLevel" + i.ToString("00")).SetActive(false);
                    }
                for (int i = 1; i < 12; i++)
                    if (i != tunerLevel)
                        manager.GetElement("NumberTuner" + i.ToString("00")).SetActive(false);

                if (level < 5)
                {
                    Object.Destroy(manager.GetElement("SynchroCircle02"));
                    Object.Destroy(manager.GetElement("SynchroCircle03"));
                }
                else if (level < 9)
                {
                    Object.Destroy(manager.GetElement("SynchroCircle01"));
                    Object.Destroy(manager.GetElement("SynchroCircle03"));
                }
                else
                {
                    Object.Destroy(manager.GetElement("SynchroCircle01"));
                    Object.Destroy(manager.GetElement("SynchroCircle02"));
                }
            }

            var director = go.GetComponent<PlayableDirector>();
            return director;
        }

        private static PlayableDirector GetXyzDirector()
        {
            GameObject go;
            if (materials.Count == 0)
                go = ABLoader.LoadMasterDuelGameObject("SummonXYZ00_01");
            else if (materials.Count == 1)
                go = ABLoader.LoadMasterDuelGameObject("SummonXYZ01_01");
            else if (materials.Count == 2)
                go = ABLoader.LoadMasterDuelGameObject("SummonXYZ02_01");
            else
                go = ABLoader.LoadMasterDuelGameObject("SummonXYZ03_01");

            Presentation.Elements.ElementObjectManager manager = go.GetComponent<Presentation.Elements.ElementObjectManager>();
            dummyCard = manager.GetElement<Transform>("DummyCardXYZ");
            var cardFace = manager.GetNestedElement<Renderer>("DummyCardXYZ/DummyCardModel_front");
            _ = RefreshCardFace(cardFace, code);

            if (MDPro3.Utility.DeviceInfo.OnAndroid())
                foreach (var child in go.transform.GetComponentsInChildren<MeshRenderer>(true))
                    if (child.name.StartsWith("XYZInMesh"))
                        child.material.GetTexture("_Texture2D").wrapMode = TextureWrapMode.Clamp;

            var director = go.GetComponent<PlayableDirector>();
            return director;
        }

        private static PlayableDirector GetLinkDirector()
        {
            GameObject go;
            var linkCount = data.GetLinkCount();
            if (linkCount == 1)
                go = ABLoader.LoadMasterDuelGameObject("SummonLink01_01");
            else if (linkCount == 2)
                go = ABLoader.LoadMasterDuelGameObject("SummonLink02_01");
            else
                go = ABLoader.LoadMasterDuelGameObject("SummonLink03_01");

            Presentation.Elements.ElementObjectManager manager = go.GetComponent<Presentation.Elements.ElementObjectManager>();
            Presentation.Elements.ElementObjectManager subManager = manager.GetElement<Presentation.Elements.ElementObjectManager>("SummonLinkPostLink");
            dummyCard = subManager.GetElement<Transform>("DummyCardLink");
            var cardFace = subManager.GetNestedElement<Renderer>("DummyCardLink/DummyCardModel_front");
            _ = RefreshCardFace(cardFace, code);
            var postAdd = subManager.GetElement<Renderer>("DummyCardLinkAdd");
            _ = RefreshCardFace(postAdd, code, true);

            var linkMarkers = data.LinkMarker;
            Presentation.Elements.ElementObjectManager trail1 = manager.GetElement<Presentation.Elements.ElementObjectManager>("LinkTrailIn01");
            linkMarkers = DestroyLinkTrail(trail1, linkMarkers, linkCount > 5 ? 2 : 1);
            if (linkCount > 1)
            {
                Presentation.Elements.ElementObjectManager trail2 = manager.GetElement<Presentation.Elements.ElementObjectManager>("LinkTrailIn02");
                linkMarkers = DestroyLinkTrail(trail2, linkMarkers, linkCount > 4 ? 2 : 1);
            }
            if (linkCount > 2)
            {
                Presentation.Elements.ElementObjectManager trail3 = manager.GetElement<Presentation.Elements.ElementObjectManager>("LinkTrailIn03");
                DestroyLinkTrail(trail3, linkMarkers, linkCount > 3 ? 2 : 1);
            }

            if(MDPro3.Utility.DeviceInfo.OnAndroid())
                foreach (var child in go.transform.GetComponentsInChildren<MeshRenderer>(true))
                    if (child.name.StartsWith("SummonLinkTrail"))
                        child.material.GetTexture("_Texture2D").wrapMode = TextureWrapMode.Clamp;

            var director = go.GetComponent<PlayableDirector>();
            return director;
        }

        private static PlayableDirector GetRitualDirector()
        {
            GameObject go;
            if (materials.Count > 0)
                go = ABLoader.LoadMasterDuelGameObject("SummonRitual01");
            else
                go = ABLoader.LoadMasterDuelGameObject("SummonRitual02");

            Presentation.Elements.ElementObjectManager manager = go.GetComponent<Presentation.Elements.ElementObjectManager>();
            Presentation.Elements.ElementObjectManager subManager = manager.GetElement<Presentation.Elements.ElementObjectManager>("SummonRitualPostRitual");
            dummyCard = subManager.GetElement<Transform>("DummyCardRitual");

            var cardFace = subManager.GetNestedElement<Renderer>("DummyCardRitual/DummyCardModel_front");
            _ = RefreshCardFace(cardFace, code);
            var postAdd = subManager.GetElement<Renderer>("DummyCardRitualAdd");
            _ = RefreshCardFace(postAdd, code, true);

            switch (materials.Count)
            {
                case 0:
                    break;
                case 1:
                    manager.GetElement("RitualTrailIn02").SetActive(false);
                    manager.GetElement("RitualTrailIn03").SetActive(false);
                    break;
                case 2:
                    manager.GetElement("RitualTrailIn01").SetActive(false);
                    manager.GetElement("RitualTrailIn03").SetActive(false);
                    break;
                case 3:
                    manager.GetElement("RitualTrailIn01").SetActive(false);
                    manager.GetElement("RitualTrailIn02").SetActive(false);
                    break;
                case 4:
                    manager.GetElement("RitualTrailIn02").SetActive(false);
                    break;
                case 5:
                    manager.GetElement("RitualTrailIn01").SetActive(false);
                    break;
            }

            var director = go.GetComponent<PlayableDirector>();
            return director;
        }

        private static async UniTask RefreshCardFace(Renderer face, int code, bool post = false)
        {
            var texture = await Presentation.CardImageLoader.LoadCardAsync(code, false, face.GetCancellationTokenOnDestroy());
            if(!post)
                face.material = UI.MaterialLoader.GetCardMaterial(code, true);
            face.material.mainTexture = texture;
        }

        private static async UniTask RefreshCardFrame(Renderer face, int code)
        {
            var frame = await Presentation.CardImageLoader.LoadCardAsync(code, false, face.GetCancellationTokenOnDestroy());
            face.material.SetTexture("_CardFrameA", frame);
        }

        private static async UniTask RefreshCardFrame(Renderer face, int count, int order)
        {
            for (int i = 0; i < count; i++)
            {
                var code = materials[i + order].GetData().Id;
                if (code == 0)
                    code = materials[i + order].GetValidData().Id;

                var cardTex = await Presentation.CardImageLoader.LoadCardAsync(code, false, face.GetCancellationTokenOnDestroy());
                face.material.SetTexture("_CardFrame" + (char)('A' + i), cardTex);
            }
        }

        private static int GetTunerLevel()
        {
            int tunerLevel = 0;

            bool levelForSelect1 = false;
            foreach (Duel.GameCard material in materials)
                tunerLevel += material.levelForSelect_1;
            if (tunerLevel == data.Level)
                levelForSelect1 = true;

            tunerLevel = 0;
            foreach (Duel.GameCard material in materials)
            {
                if (material.GetData().HasType(MDPro3.Duel.YGOSharp.CardType.Tuner))
                {
                    if (levelForSelect1)
                        tunerLevel += material.levelForSelect_1;
                    else
                        tunerLevel += material.levelForSelect_2;
                }
            }
            if (tunerLevel == 0)
            {
                foreach (Duel.GameCard material in materials)
                {
                    MDPro3.Duel.YGOSharp.Card data = material.GetValidData();
                    if (data.HasType(MDPro3.Duel.YGOSharp.CardType.Tuner))
                        tunerLevel += data.Level;
                }
                if (tunerLevel == 0)
                    tunerLevel = materials[0].GetValidData().Level;
            }
            return tunerLevel;
        }

        private static int DestroyLinkTrail(Presentation.Elements.ElementObjectManager manager, int linkMarkers, int need)
        {
            int foundMarker = 0;
            int foundMarkerCount = 0;
            Presentation.Elements.ElementObjectManager parent = manager.transform.parent.GetComponent<Presentation.Elements.ElementObjectManager>();
            if ((linkMarkers & (int)MDPro3.Duel.YGOSharp.CardLinkMarker.Top) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)MDPro3.Duel.YGOSharp.CardLinkMarker.Top;
            }
            else
            {
                Object.Destroy(manager.GetElement("LinkTrailG02"));
                Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_02"));
            }
            if (foundMarkerCount < need && (linkMarkers & (int)MDPro3.Duel.YGOSharp.CardLinkMarker.TopLeft) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)MDPro3.Duel.YGOSharp.CardLinkMarker.TopLeft;
            }
            else
            {
                Object.Destroy(manager.GetElement("LinkTrailG01"));
                Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_01"));
            }
            if (foundMarkerCount < need && (linkMarkers & (int)MDPro3.Duel.YGOSharp.CardLinkMarker.Left) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)MDPro3.Duel.YGOSharp.CardLinkMarker.Left;
            }
            else
            {
                Object.Destroy(manager.GetElement("LinkTrailG04"));
                Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_04"));
            }
            if (foundMarkerCount < need && (linkMarkers & (int)MDPro3.Duel.YGOSharp.CardLinkMarker.BottomLeft) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)MDPro3.Duel.YGOSharp.CardLinkMarker.BottomLeft;
            }
            else
            {
                Object.Destroy(manager.GetElement("LinkTrailG06"));
                Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_06"));
            }
            if (foundMarkerCount < need && (linkMarkers & (int)MDPro3.Duel.YGOSharp.CardLinkMarker.Bottom) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)MDPro3.Duel.YGOSharp.CardLinkMarker.Bottom;
            }
            else
            {
                Object.Destroy(manager.GetElement("LinkTrailG07"));
                Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_07"));
            }
            if (foundMarkerCount < need && (linkMarkers & (int)MDPro3.Duel.YGOSharp.CardLinkMarker.BottomRight) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)MDPro3.Duel.YGOSharp.CardLinkMarker.BottomRight;
            }
            else
            {
                Object.Destroy(manager.GetElement("LinkTrailG08"));
                Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_08"));
            }
            if (foundMarkerCount < need && (linkMarkers & (int)MDPro3.Duel.YGOSharp.CardLinkMarker.Right) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)MDPro3.Duel.YGOSharp.CardLinkMarker.Right;
            }
            else
            {
                Object.Destroy(manager.GetElement("LinkTrailG05"));
                Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_05"));
            }
            if (foundMarkerCount < need && (linkMarkers & (int)MDPro3.Duel.YGOSharp.CardLinkMarker.TopRight) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)MDPro3.Duel.YGOSharp.CardLinkMarker.TopRight;
            }
            else
            {
                Object.Destroy(manager.GetElement("LinkTrailG03"));
                Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_03"));
            }

            return linkMarkers - foundMarker;
        }

        private static async UniTask StartCard()
        {
            if (Program.instance == null)
                return;
            if (Program.instance.currentServant != Program.instance.ocgcore)
                return;
            if (dummyCard == null)
                return;
            if (Controllers.OcgCore.summonCard == null)
                return;

            var position = dummyCard.position;
            var angle = dummyCard.eulerAngles;
            angle = new Vector3(-angle.x, angle.y + 180f, -angle.z);

            Controllers.OcgCore.summonCard.UpdateExDeckTop();

            var interval = 0.5f;
            if (Controllers.CutinViewer.CutinExist(code))
                interval = 1f;
            await Controllers.OcgCore.summonCard.StartCardSequence(position, angle, interval).WaitAsync();
            dummyCard = null;
            Controllers.OcgCore.summonCard = null;
        }

        private static double GetDirectorLabelTime(PlayableDirector director, string label)
        {
            foreach (PlayableBinding pb in director.playableAsset.outputs)
            {
                var track = pb.sourceObject as TrackAsset;
                if (track != null)
                    foreach (TimelineClip clip in track.GetClips())
                        if (clip.displayName == label)
                            return clip.start;
            }

            return 3.5d;
        }

    }
}