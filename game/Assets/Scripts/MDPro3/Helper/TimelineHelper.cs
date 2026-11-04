using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.Utility;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using YgomSystem.ElementSystem;


namespace MDPro3
{
    public static class TimelineHelper
    {
        private static Transform dummyCard;
        private static int code;
        private static Card data;
        private static List<GameCard> materials;
        private static uint reason;
        private static GameObject unitCards;
        private static bool useSpecialUnitCard;

        public static async UniTask PlaySummonTimelineAsync()
        {
            if (OcgCore.summonCard == null)
                return;

            code = OcgCore.summonCard.GetData().Id;
            data = CardsManager.Get(code);
            materials = OcgCore.materialCards;
            reason = materials[0].p.reason;

            await CutinViewer.CacheCutin(code);
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
            UnityEngine.Object.Destroy(unitCards);

            await director.WaitToTimeAsync(startCardTime, false);
            await StartCard();
        }

        private static async UniTask CacheUnitCardsAsync()
        {
            // Super Polymerization
            if (data.HasType(CardType.Fusion)
                && OcgCore.chainSolvingCard != null
                && OcgCore.chainSolvingCard.GetData().GetOriginalID() == 48130397)
            {
                if (materials.Count > 8)
                    await ABLoader.LoadFromFolderAsync<PlayableDirector>
                        ("MasterDuel/Timeline/Summon/SummonFusion/SummonFusion07445ShowUnitCard08", true, false);
                else
                    await ABLoader.LoadFromFolderAsync<PlayableDirector>
                        ("MasterDuel/Timeline/Summon/SummonFusion/SummonFusion07445ShowUnitCard0" + materials.Count, true, false);
            }
            // Invocation
            else if (data.HasType(CardType.Fusion)
                && OcgCore.chainSolvingCard != null
                && OcgCore.chainSolvingCard.GetData().GetOriginalID() == 74063034)
            {
                var path = "MasterDuel/Timeline/Summon/SummonFusion/SummonFusion12852ShowUnitCard08";
                if (materials.Count < 8)
                    path = "MasterDuel/Timeline/Summon/SummonFusion/SummonFusion12852ShowUnitCard0" + materials.Count;
                if (OcgCore.chainSolvingCard.GetData().Id == 74063035)
                    path = path.Replace("12852", "03432");
                await ABLoader.LoadFromFolderAsync<PlayableDirector>(path, true, false);
            }
        }

        private static async UniTaskVoid ShowUnitCardsAsync()
        {
            useSpecialUnitCard = false;
            int maxCount = 8;

            // Super Polymerization
            if (data.HasType(CardType.Fusion)
                && OcgCore.chainSolvingCard != null
                && OcgCore.chainSolvingCard.GetData().GetOriginalID() == 48130397)
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
            else if (data.HasType(CardType.Fusion)
                && OcgCore.chainSolvingCard != null
                && OcgCore.chainSolvingCard.GetData().GetOriginalID() == 74063034)
            {
                useSpecialUnitCard = true;

                var path = "MasterDuel/Timeline/Summon/SummonFusion/SummonFusion12852ShowUnitCard08";
                if (materials.Count < 8)
                    path = "MasterDuel/Timeline/Summon/SummonFusion/SummonFusion12852ShowUnitCard0" + materials.Count;
                if (OcgCore.chainSolvingCard.GetData().Id == 74063035)
                    path = path.Replace("12852", "03432");
                unitCards = await ABLoader.LoadFromFolderAsync<PlayableDirector>(path, true, true);
            }
            else if (data.HasType(CardType.Fusion))
            {
                if (materials.Count > 8)
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonFusionShowUnitCard08");
                else
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonFusionShowUnitCard0" + materials.Count);
            }
            else if (data.HasType(CardType.Synchro))
            {
                if (materials.Count > 6)
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonSynchroShowUnitCard06");
                else
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonSynchroShowUnitCard0" + materials.Count);
                maxCount = 6;
            }
            else if (data.HasType(CardType.Xyz))
            {
                if (materials.Count > 6)
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonXYZShowUnitCard06");
                else
                    unitCards = ABLoader.LoadMasterDuelGameObject("SummonXYZShowUnitCard0" + materials.Count);
                maxCount = 6;
            }
            else if (data.HasType(CardType.Link))
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

            var manager = unitCards.GetComponent<ElementObjectManager>();
            for (int i = 0; i < Mathf.Min(materials.Count, maxCount); i++)
            {
                var dummyCard = manager.GetElement<ElementObjectManager>("DummyCard" + (i + 1).ToString("00"));
                var renderer = dummyCard.GetElement<MeshRenderer>("DummyCardModel_front");
                var code = materials[i].GetData().Id;
                if (code == 0)
                    code = materials[i].GetValidData().Id;
                _ = RefreshCardFace(renderer, code);

                if (data.HasType(CardType.Synchro))
                {
                    var mData = materials[i].GetData();
                    if (mData.HasType(CardType.Tuner))
                        UnityEngine.Object.Destroy(manager.GetElement($"Synchro01Card0{i + 1}"));
                    else
                        UnityEngine.Object.Destroy(manager.GetElement($"Synchro00Card0{i + 1}"));
                }
            }

            Program.instance.ocgcore.AllGameObjects.Add(unitCards);
            var director = unitCards.GetComponent<PlayableDirector>();
            await director.AutoDestroy();
        }

        private static PlayableDirector GetPlayableDirector()
        {
            if(data.HasType(CardType.Fusion))
                return GetFusionDirector();
            else if (data.HasType(CardType.Synchro))
                return GetSynchroDirector();
            else if (data.HasType(CardType.Xyz))
                return GetXyzDirector();
            else if (data.HasType(CardType.Link))
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

            var manager = go.GetComponent<ElementObjectManager>();
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

            var manager = go.GetComponent<ElementObjectManager>();
            var subManager = manager.GetElement<ElementObjectManager>("SummonSynchroPostSynchro");
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
                    UnityEngine.Object.Destroy(manager.GetElement("SynchroCircle02"));
                    UnityEngine.Object.Destroy(manager.GetElement("SynchroCircle03"));
                }
                else if (level < 9)
                {
                    UnityEngine.Object.Destroy(manager.GetElement("SynchroCircle01"));
                    UnityEngine.Object.Destroy(manager.GetElement("SynchroCircle03"));
                }
                else
                {
                    UnityEngine.Object.Destroy(manager.GetElement("SynchroCircle01"));
                    UnityEngine.Object.Destroy(manager.GetElement("SynchroCircle02"));
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

            var manager = go.GetComponent<ElementObjectManager>();
            dummyCard = manager.GetElement<Transform>("DummyCardXYZ");
            var cardFace = manager.GetNestedElement<Renderer>("DummyCardXYZ/DummyCardModel_front");
            _ = RefreshCardFace(cardFace, code);

            if (DeviceInfo.OnAndroid())
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

            var manager = go.GetComponent<ElementObjectManager>();
            var subManager = manager.GetElement<ElementObjectManager>("SummonLinkPostLink");
            dummyCard = subManager.GetElement<Transform>("DummyCardLink");
            var cardFace = subManager.GetNestedElement<Renderer>("DummyCardLink/DummyCardModel_front");
            _ = RefreshCardFace(cardFace, code);
            var postAdd = subManager.GetElement<Renderer>("DummyCardLinkAdd");
            _ = RefreshCardFace(postAdd, code, true);

            var linkMarkers = data.LinkMarker;
            var trail1 = manager.GetElement<ElementObjectManager>("LinkTrailIn01");
            linkMarkers = DestroyLinkTrail(trail1, linkMarkers, linkCount > 5 ? 2 : 1);
            if (linkCount > 1)
            {
                var trail2 = manager.GetElement<ElementObjectManager>("LinkTrailIn02");
                linkMarkers = DestroyLinkTrail(trail2, linkMarkers, linkCount > 4 ? 2 : 1);
            }
            if (linkCount > 2)
            {
                var trail3 = manager.GetElement<ElementObjectManager>("LinkTrailIn03");
                DestroyLinkTrail(trail3, linkMarkers, linkCount > 3 ? 2 : 1);
            }

            if(DeviceInfo.OnAndroid())
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

            var manager = go.GetComponent<ElementObjectManager>();
            var subManager = manager.GetElement<ElementObjectManager>("SummonRitualPostRitual");
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
            var texture = await CardImageLoader.LoadCardAsync(code, false, face.GetCancellationTokenOnDestroy());
            if(!post)
                face.material = MaterialLoader.GetCardMaterial(code, true);
            face.material.mainTexture = texture;
        }

        private static async UniTask RefreshCardFrame(Renderer face, int code)
        {
            var frame = await CardImageLoader.LoadCardAsync(code, false, face.GetCancellationTokenOnDestroy());
            face.material.SetTexture("_CardFrameA", frame);
        }

        private static async UniTask RefreshCardFrame(Renderer face, int count, int order)
        {
            for (int i = 0; i < count; i++)
            {
                var code = materials[i + order].GetData().Id;
                if (code == 0)
                    code = materials[i + order].GetValidData().Id;

                var cardTex = await CardImageLoader.LoadCardAsync(code, false, face.GetCancellationTokenOnDestroy());
                face.material.SetTexture("_CardFrame" + (char)('A' + i), cardTex);
            }
        }

        private static int GetTunerLevel()
        {
            int tunerLevel = 0;

            bool levelForSelect1 = false;
            foreach (var material in materials)
                tunerLevel += material.levelForSelect_1;
            if (tunerLevel == data.Level)
                levelForSelect1 = true;

            tunerLevel = 0;
            foreach (var material in materials)
            {
                if (material.GetData().HasType(CardType.Tuner))
                {
                    if (levelForSelect1)
                        tunerLevel += material.levelForSelect_1;
                    else
                        tunerLevel += material.levelForSelect_2;
                }
            }
            if (tunerLevel == 0)
            {
                foreach (var material in materials)
                {
                    var data = material.GetValidData();
                    if (data.HasType(CardType.Tuner))
                        tunerLevel += data.Level;
                }
                if (tunerLevel == 0)
                    tunerLevel = materials[0].GetValidData().Level;
            }
            return tunerLevel;
        }

        private static int DestroyLinkTrail(ElementObjectManager manager, int linkMarkers, int need)
        {
            int foundMarker = 0;
            int foundMarkerCount = 0;
            var parent = manager.transform.parent.GetComponent<ElementObjectManager>();
            if ((linkMarkers & (int)CardLinkMarker.Top) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)CardLinkMarker.Top;
            }
            else
            {
                UnityEngine.Object.Destroy(manager.GetElement("LinkTrailG02"));
                UnityEngine.Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_02"));
            }
            if (foundMarkerCount < need && (linkMarkers & (int)CardLinkMarker.TopLeft) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)CardLinkMarker.TopLeft;
            }
            else
            {
                UnityEngine.Object.Destroy(manager.GetElement("LinkTrailG01"));
                UnityEngine.Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_01"));
            }
            if (foundMarkerCount < need && (linkMarkers & (int)CardLinkMarker.Left) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)CardLinkMarker.Left;
            }
            else
            {
                UnityEngine.Object.Destroy(manager.GetElement("LinkTrailG04"));
                UnityEngine.Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_04"));
            }
            if (foundMarkerCount < need && (linkMarkers & (int)CardLinkMarker.BottomLeft) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)CardLinkMarker.BottomLeft;
            }
            else
            {
                UnityEngine.Object.Destroy(manager.GetElement("LinkTrailG06"));
                UnityEngine.Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_06"));
            }
            if (foundMarkerCount < need && (linkMarkers & (int)CardLinkMarker.Bottom) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)CardLinkMarker.Bottom;
            }
            else
            {
                UnityEngine.Object.Destroy(manager.GetElement("LinkTrailG07"));
                UnityEngine.Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_07"));
            }
            if (foundMarkerCount < need && (linkMarkers & (int)CardLinkMarker.BottomRight) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)CardLinkMarker.BottomRight;
            }
            else
            {
                UnityEngine.Object.Destroy(manager.GetElement("LinkTrailG08"));
                UnityEngine.Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_08"));
            }
            if (foundMarkerCount < need && (linkMarkers & (int)CardLinkMarker.Right) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)CardLinkMarker.Right;
            }
            else
            {
                UnityEngine.Object.Destroy(manager.GetElement("LinkTrailG05"));
                UnityEngine.Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_05"));
            }
            if (foundMarkerCount < need && (linkMarkers & (int)CardLinkMarker.TopRight) > 0)
            {
                foundMarkerCount++;
                foundMarker += (int)CardLinkMarker.TopRight;
            }
            else
            {
                UnityEngine.Object.Destroy(manager.GetElement("LinkTrailG03"));
                UnityEngine.Object.Destroy(parent.GetElement("Marker" + manager.name.Substring(manager.name.Length - 2, 2) + "_03"));
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
            if (OcgCore.summonCard == null)
                return;

            var position = dummyCard.position;
            var angle = dummyCard.eulerAngles;
            angle = new Vector3(-angle.x, angle.y + 180f, -angle.z);

            OcgCore.summonCard.UpdateExDeckTop();

            var interval = 0.5f;
            if (CutinViewer.CutinExist(code))
                interval = 1f;
            await OcgCore.summonCard.StartCardSequence(position, angle, interval).WaitAsync();
            dummyCard = null;
            OcgCore.summonCard = null;
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