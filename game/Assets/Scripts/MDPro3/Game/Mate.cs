using MDPro3.Servant;
using MDPro3.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Willow;

namespace MDPro3
{
    public class Mate : MonoBehaviour
    {
        public enum MateType
        {
            MasterDuel,
            CrossDuel
        }

        public enum MateAction
        {
            Initial,
            Entry,
            Tap,
            BattlePhase,
            Attack,
            GetDamage,
            Victory,
            Defeat,
            Random
        }

        public MateType type;
        public bool huge;
        public Transform parent;
        public int code;

        public PlayableDirector directorA;//a_summon    A
        public PlayableDirector directorB;//b_destroy      BD BOSS站立被破坏
        public PlayableDirector directorD;//d_attack        D 直接攻击
        public PlayableDirector directorE;//e_attack         
        public PlayableDirector directorI;//i_spSummon   SP
        public PlayableDirector directorJ;//j_destroy        JD BOSS倒地被破坏
        public PlayableDirector directorK;//k_down          KD BOSS倒地
        public PlayableDirector directorM;//m_bossAttack   BA BOSS重攻击 另有 m_attack 艾克佐迪亞
        public PlayableDirector directorN;//n_attack        NA BOSS范围攻击
        public PlayableDirector directorO;//o_attack        NA BOSS轻攻击
        BoxCollider m_collider;
        SkinnedMeshRenderer mesh;
        private readonly Dictionary<Animator, HashSet<string>> masterDuelTriggersByAnimator = new();
        private Animator[] masterDuelAnimators = Array.Empty<Animator>();
        private Animator masterDuelPrimaryAnimator;
        private bool masterDuelInitialized;
        private bool masterDuelHasAnyTriggers;
        private static readonly string[] masterDuelPrimaryTriggerHints =
        {
            "Entry", "Attack", "Damage", "Victory", "Defeat",
            "Tap", "Tap1", "Tap2", "Random1", "Random2",
            "Wait2", "Wait3", "Change", "Change1", "Change2", "ChangeBack", "ChangePreHide"
        };
        private static readonly string[] masterDuelTapFallbackStates = { "Tap", "Tap1", "Tap2" };
        private static readonly string[] masterDuelRandomFallbackStates = { "Random1", "Random2", "Wait2", "Wait3", "Normal" };
        private static readonly string[] masterDuelChangeFallbackStates = { "Change", "Change1", "Change2", "ChangeBack", "ChangePreHide" };
        private static readonly string[] ipEntryVoice = { "SE_M14676_c", "SE_M14676_d" };
        private static readonly string[] ipTapVoice = { "SE_M14676_i", "SE_M14676_j", "SE_M14676_k", "SE_M14676_d" };
        private static readonly string[] ipAttackVoice = { "SE_M14676_e", "SE_M14676_f", "SE_M14676_d1", "SE_M14676_d" };
        private static readonly string[] ipDamageVoice = { "SE_M14676_h2", "SE_M14676_h1", "SE_M14676_h", "SE_M14676_d1", "SE_M14676_d" };
        private static readonly string[] ipRandomVoice = { "SE_M14676_f", "SE_M14676_g", "SE_M14676_e", "SE_M14676_j" };
        private static readonly string[] ipVictoryVoice = { "SE_M14676_k", "SE_M14676_g", "SE_M14676_i" };
        private static readonly string[] ipDefeatVoice = { "SE_M14676_d1", "SE_M14676_h2", "SE_M14676_d" };
        private static readonly string[] spEntryVoice = { "SE_M15702_c", "SE_M15702_f" };
        private static readonly string[] spTapVoice = { "SE_M15702_i", "SE_M15702_j", "SE_M15702_k" };
        private static readonly string[] spAttackVoice = { "SE_M15702_d", "SE_M15702_d1", "SE_M15702_g" };
        private static readonly string[] spDamageVoice = { "SE_M15702_h2", "SE_M15702_h1", "SE_M15702_h" };
        private static readonly string[] spRandomVoice = { "SE_M15702_f", "SE_M15702_g", "SE_M15702_e", "SE_M15702_j" };
        private static readonly string[] spVictoryVoice = { "SE_M15702_k", "SE_M15702_i", "SE_M15702_e" };
        private static readonly string[] spDefeatVoice = { "SE_M15702_d1", "SE_M15702_h2", "SE_M15702_d" };
        private Coroutine masterDuelVoiceFallbackCoroutine;
        private float masterDuelVoiceFallbackStartTime;

        private bool Playing()
        {
            if (directorA != null && directorA.state == PlayState.Playing)
                return true;
            if (directorB != null && directorB.state == PlayState.Playing)
                return true;
            if (directorD != null && directorD.state == PlayState.Playing)
                return true;
            if (directorE != null && directorE.state == PlayState.Playing)
                return true;
            if (directorI != null && directorI.state == PlayState.Playing)
                return true;

            if (directorJ != null && directorJ.state == PlayState.Playing)
                return true;
            if (directorK != null && directorK.state == PlayState.Playing)
                return true;
            if (directorM != null && directorM.state == PlayState.Playing)
                return true;
            if (directorN != null && directorN.state == PlayState.Playing)
                return true;
            if (directorO != null && directorO.state == PlayState.Playing)
                return true;

            return false;
        }

        private void EnsureMasterDuelInitialized(bool forceRefresh = false)
        {
            if (type != MateType.MasterDuel)
                return;
            if (forceRefresh)
                masterDuelInitialized = false;
            if (masterDuelInitialized)
                return;

            if (m_collider == null)
            {
                m_collider = gameObject.GetComponent<BoxCollider>();
                if (m_collider == null)
                    m_collider = gameObject.AddComponent<BoxCollider>();
            }
            m_collider.size = new Vector3(10, 10, 10);
            m_collider.center = new Vector3(0, 5, 0);

            masterDuelAnimators = transform.GetComponentsInChildren<Animator>(true);
            masterDuelTriggersByAnimator.Clear();
            masterDuelPrimaryAnimator = null;
            masterDuelHasAnyTriggers = false;
            var bestScore = -1;
            foreach (var animator in masterDuelAnimators)
            {
                if (animator == null)
                    continue;

                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
                KeepAnimatorStateOnDisable(animator);
                if (animator.gameObject.GetComponent<EventSEPlayer>() == null)
                    animator.gameObject.AddComponent<EventSEPlayer>();

                var triggerSet = new HashSet<string>(StringComparer.Ordinal);
                foreach (var parameter in animator.parameters)
                    if (parameter.type == AnimatorControllerParameterType.Trigger)
                        triggerSet.Add(parameter.name);
                masterDuelTriggersByAnimator[animator] = triggerSet;
                if (triggerSet.Count > 0)
                    masterDuelHasAnyTriggers = true;

                var score = 0;
                for (var i = 0; i < masterDuelPrimaryTriggerHints.Length; i++)
                    if (triggerSet.Contains(masterDuelPrimaryTriggerHints[i]))
                        score++;
                if (score > bestScore)
                {
                    bestScore = score;
                    masterDuelPrimaryAnimator = animator;
                }
            }

            if (masterDuelPrimaryAnimator == null && masterDuelAnimators.Length > 0)
                masterDuelPrimaryAnimator = masterDuelAnimators[0];

            masterDuelInitialized = true;

        }

        private void RefreshMasterDuelAnimatorCacheIfNeeded()
        {
            if (type != MateType.MasterDuel)
                return;

            if (!masterDuelInitialized)
            {
                EnsureMasterDuelInitialized();
                return;
            }

            var stale = masterDuelAnimators == null || masterDuelAnimators.Length == 0;
            if (!stale)
            {
                for (var i = 0; i < masterDuelAnimators.Length; i++)
                {
                    if (masterDuelAnimators[i] != null)
                        continue;
                    stale = true;
                    break;
                }
            }

            if (!stale && (masterDuelPrimaryAnimator == null || !masterDuelTriggersByAnimator.ContainsKey(masterDuelPrimaryAnimator)))
                stale = true;
            if (!stale)
                return;

            EnsureMasterDuelInitialized(forceRefresh: true);
        }

        public void PrepareForPremiumSwapActivation()
        {
            if (type != MateType.MasterDuel)
                return;

            RefreshMasterDuelAnimatorCacheIfNeeded();
            EnsureMasterDuelInitialized();
            if (masterDuelAnimators == null || masterDuelAnimators.Length == 0)
                return;

            for (var i = 0; i < masterDuelAnimators.Length; i++)
            {
                var animator = masterDuelAnimators[i];
                if (animator == null)
                    continue;

                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
                KeepAnimatorStateOnDisable(animator);
                if (!animator.gameObject.activeInHierarchy)
                    continue;

                animator.Rebind();
                animator.Update(0f);
            }
        }

        private void Start()
        {
            if (type == MateType.MasterDuel)
            {
                EnsureMasterDuelInitialized();
            }
            else
            {
                Tools.SetPlayableDirectorUnscaledGameTime(transform);
                transform.localEulerAngles = Vector3.zero;
                mesh = transform.Find("Mesh").GetComponent<SkinnedMeshRenderer>();
                mesh.updateWhenOffscreen = false;
                var bounds = mesh.bounds;
                bounds.extents = new Vector3(1000, 1000, 1000);
                bounds.center = Vector3.zero;
                m_collider = gameObject.AddComponent<BoxCollider>();
                m_collider.size = new Vector3(2, 2, 2);
                m_collider.center = new Vector3(0, 1, 0);

                if (Program.instance.currentServant == Program.instance.ocgcore)
                    Tools.ChangeLayer(gameObject, "Default");
                for (int i = 0; i < transform.childCount; i++)
                {
                    CustomTimelineController controller;
                    if ((controller = transform.GetChild(i).GetComponent<CustomTimelineController>()) != null)
                    {
                        var director = controller.currentDirector;
                        if (director.name.ToLower().Contains("_a_"))
                            directorA = director;
                        if (director.name.ToLower().Contains("_b_"))
                            directorB = director;
                        if (director.name.ToLower().Contains("_d_"))
                            directorD = director;
                        if (director.name.ToLower().Contains("_e_"))
                            directorE = director;
                        if (director.name.ToLower().Contains("_i_"))
                            directorI = director;
                        if (director.name.ToLower().Contains("_j_"))
                            directorJ = director;
                        if (director.name.ToLower().Contains("_k_"))
                            directorK = director;
                        if (director.name.ToLower().Contains("_m_"))
                        {
                            directorM = director;
                            huge = true;
                        }
                        if (director.name.ToLower().Contains("_n_"))
                            directorN = director;
                        if (director.name.ToLower().Contains("_o_"))
                            directorO = director;
                    }
                }
                transform.localScale = Vector3.one * 5;
                if(huge)
                    transform.localScale = Vector3.one * 4f;

                var animator = GetComponent<Animator>();
                if (animator != null)
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        public void Play(MateAction action)
        {
            //Debug.Log($"Mate {code} Play {action}.");

            switch (type)
            {
                case (MateType.MasterDuel):
                    if (action != MateAction.Entry && !gameObject.activeInHierarchy)
                        break;
                    switch (action)
                    {
                        case MateAction.Entry:
                            transform.SetParent(parent, false);
                            if (TryPlayMasterDuelTrigger("Entry"))
                                QueueMasterDuelVoiceFallback(MateAction.Entry);
                            break;
                        case MateAction.Tap:
                            var tapPriority = new[] { "Tap", "Tap1", "Tap2" };
                            var tapStartIndex = UnityEngine.Random.Range(0, tapPriority.Length);
                            var tapped = false;
                            for (var tapTry = 0; tapTry < tapPriority.Length; tapTry++)
                            {
                                var triggerName = tapPriority[(tapStartIndex + tapTry) % tapPriority.Length];
                                var logMissing = tapTry == tapPriority.Length - 1;
                                if (!TryPlayMasterDuelTrigger(triggerName, logMissing))
                                    continue;

                                tapped = true;
                                break;
                            }
                            if (tapped)
                                QueueMasterDuelVoiceFallback(MateAction.Tap);
                            break;
                        case MateAction.BattlePhase:

                            break;
                        case MateAction.Attack:
                            if (TryPlayMasterDuelTrigger("Attack"))
                                QueueMasterDuelVoiceFallback(MateAction.Attack);
                            break;
                        case MateAction.GetDamage:
                            if (TryPlayMasterDuelTrigger("Damage"))
                                QueueMasterDuelVoiceFallback(MateAction.GetDamage);
                            break;
                        case MateAction.Victory:
                            if (TryPlayMasterDuelTrigger("Victory"))
                                QueueMasterDuelVoiceFallback(MateAction.Victory);
                            break;
                        case MateAction.Defeat:
                            if (TryPlayMasterDuelTrigger("Defeat"))
                                QueueMasterDuelVoiceFallback(MateAction.Defeat);
                            break;
                        case MateAction.Random:
                            var preferWait3 = UnityEngine.Random.value < 0.2f;
                            var played = preferWait3
                                ? TryPlayMasterDuelTrigger("Wait3", false) || TryPlayMasterDuelTrigger("Wait2", false)
                                : TryPlayMasterDuelTrigger("Wait2", false) || TryPlayMasterDuelTrigger("Wait3", false);
                            if (!played)
                            {
                                var preferRandom2 = UnityEngine.Random.value < 0.5f;
                                played = preferRandom2
                                    ? TryPlayMasterDuelTrigger("Random2", false) || TryPlayMasterDuelTrigger("Random1", false)
                                    : TryPlayMasterDuelTrigger("Random1", false) || TryPlayMasterDuelTrigger("Random2", false);
                            }
                            if (!played)
                                played = TryPlayMasterDuelTrigger("Normal");
                            if (played)
                                QueueMasterDuelVoiceFallback(MateAction.Random);
                            break;
                    }
                    break;
                case (MateType.CrossDuel):
                    var animator = GetComponent<Animator>();
                    if (animator.GetBool("IsKnockDown"))
                        break;
                    switch (action)
                    {
                        case MateAction.Initial:

                            animator.SetBool("IsVisible", true);
                            animator.SetBool("IsFaceUp", true);
                            animator.SetBool("IsAttackPosition", true);
                            animator.SetTrigger("Update");
                            break;
                        case MateAction.Entry:
                            transform.SetParent(parent, false);
                            animator.SetBool("IsVisible", true);
                            animator.SetBool("IsFaceUp", true);
                            animator.SetBool("IsAttackPosition", true);
                            animator.SetTrigger("Update");
                            if (directorI != null)
                            {
                                directorI.Play();
                                MateViewer.PlayCrossDuelSe(directorI.name.Replace("(Clone)", string.Empty));
                                mesh.updateWhenOffscreen = true;
                            }
                            else if (directorA != null)
                            {
                                directorA.Play();
                                MateViewer.PlayCrossDuelSe(directorA.name.Replace("(Clone)", string.Empty));
                            }
                            break;
                        case MateAction.Tap:
                            if (Playing())
                                break;
                            if (directorM != null && directorN != null && directorO != null)
                            {
                                var random = UnityEngine.Random.Range(0, 3);
                                switch (random)
                                {
                                    case 0:
                                        directorM.Play();
                                        MateViewer.PlayCrossDuelSe(directorM.name.Replace("(Clone)", string.Empty));
                                        break;
                                    case 1:
                                        directorN.Play();
                                        MateViewer.PlayCrossDuelSe(directorN.name.Replace("(Clone)", string.Empty));
                                        break;
                                    case 2:
                                        directorO.Play();
                                        MateViewer.PlayCrossDuelSe(directorO.name.Replace("(Clone)", string.Empty));
                                        break;
                                }
                            }
                            else if (directorM != null)//艾克佐迪亞
                            {
                                directorM.Play();
                                MateViewer.PlayCrossDuelSe(directorM.name.Replace("(Clone)", string.Empty));
                            }
                            else if (directorD != null)
                            {
                                directorD.Play();
                                MateViewer.PlayCrossDuelSe(directorD.name.Replace("(Clone)", string.Empty));
                            }
                            break;
                        case MateAction.BattlePhase:
                            break;
                        case MateAction.Attack:
                            if (directorE != null)
                            {
                                directorE.Play();
                                MateViewer.PlayCrossDuelSe(directorE.name.Replace("(Clone)", string.Empty));
                            }
                            break;
                        case MateAction.GetDamage:
                            //animator.SetTrigger("Damage");
                            break;
                        case MateAction.Victory:
                            break;
                        case MateAction.Defeat:
                            if (directorB != null) 
                            { 
                                directorB.Play();
                                MateViewer.PlayCrossDuelSe(directorB.name.Replace("(Clone)", string.Empty));
                                animator.SetBool("IsKnockDown", true);
                                animator.SetBool("IsVisible", false);
                            }
                            else
                            {
                                animator.SetBool("IsAttackPosition", false);
                            }
                            break;
                    }
                    break;
            }
        }

        private void QueueMasterDuelVoiceFallback(MateAction action)
        {
            if (type != MateType.MasterDuel)
                return;
            if (!isActiveAndEnabled)
                return;
            if (!TryGetMasterDuelVoiceCandidates(action, out var candidates))
                return;

            if (masterDuelVoiceFallbackCoroutine != null)
                StopCoroutine(masterDuelVoiceFallbackCoroutine);

            masterDuelVoiceFallbackStartTime = Time.unscaledTime;
            masterDuelVoiceFallbackCoroutine = StartCoroutine(PlayMasterDuelVoiceFallbackDelayed(candidates, GetMasterDuelVoicePrefix(), masterDuelVoiceFallbackStartTime));
        }

        private IEnumerator PlayMasterDuelVoiceFallbackDelayed(IReadOnlyList<string> candidates, string expectedPrefix, float startTime)
        {
            yield return null;
            yield return null;

            masterDuelVoiceFallbackCoroutine = null;
            if (startTime != masterDuelVoiceFallbackStartTime)
                yield break;
            if (EventSEPlayer.HasRecentEvent(expectedPrefix, startTime))
                yield break;
            if (candidates == null || candidates.Count == 0)
                yield break;

            var startIndex = UnityEngine.Random.Range(0, candidates.Count);
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[(startIndex + i) % candidates.Count];
                if (string.IsNullOrEmpty(candidate))
                    continue;

                AudioManager.PlaySE(candidate, 0.4f);
                yield break;
            }
        }

        private string GetMasterDuelVoicePrefix()
        {
            return code switch
            {
                1003005 => "SE_M14676_",
                1003105 => "SE_M15702_",
                _ => string.Empty
            };
        }

        private bool TryGetMasterDuelVoiceCandidates(MateAction action, out IReadOnlyList<string> candidates)
        {
            candidates = null;
            if (code == 1003005)
            {
                candidates = action switch
                {
                    MateAction.Entry => ipEntryVoice,
                    MateAction.Tap => ipTapVoice,
                    MateAction.Attack => ipAttackVoice,
                    MateAction.GetDamage => ipDamageVoice,
                    MateAction.Random => ipRandomVoice,
                    MateAction.Victory => ipVictoryVoice,
                    MateAction.Defeat => ipDefeatVoice,
                    _ => null
                };
                return candidates != null;
            }

            if (code == 1003105)
            {
                candidates = action switch
                {
                    MateAction.Entry => spEntryVoice,
                    MateAction.Tap => spTapVoice,
                    MateAction.Attack => spAttackVoice,
                    MateAction.GetDamage => spDamageVoice,
                    MateAction.Random => spRandomVoice,
                    MateAction.Victory => spVictoryVoice,
                    MateAction.Defeat => spDefeatVoice,
                    _ => null
                };
                return candidates != null;
            }

            return false;
        }

        public void ActiveCamera(MateAction action, int layerMask)
        {
            if (action == MateAction.Entry)
            {
                if (directorI != null)
                {
                    foreach (var camera in directorI.transform.parent.GetComponentsInChildren<Camera>(true))
                    {
                        if (camera.name == "UIEffect_Camera")
                            continue;
                        camera.enabled = true;
                        camera.cullingMask = 1 << layerMask;
                        //camera.clearFlags = CameraClearFlags.Nothing;
                        CameraManager.DuelOverlay2DMinus();
                        var mono = camera.gameObject.AddComponent<DoWhenDisabled>();
                        mono.action = () =>
                        {
                            CameraManager.DuelOverlay2DPlus();
                        };
                    }
                    foreach (var light in directorI.transform.parent.GetComponentsInChildren<Light>(true))
                    {
                        light.enabled = true;
                        light.cullingMask = 1 << layerMask;
                    }
                }
            }
            if (action == MateAction.Tap)
            {
                if (directorM != null)
                {
                    foreach (var camera in directorM.transform.parent.GetComponentsInChildren<Camera>(true))
                    {
                        if (camera.name == "UIEffect_Camera")
                            continue;
                        camera.enabled = true;
                        camera.cullingMask = 1 << layerMask;
                    }
                    foreach (var light in directorM.transform.parent.GetComponentsInChildren<Light>(true))
                    {
                        light.enabled = true;
                        light.cullingMask = 1 << layerMask;
                    }
                }
            }
        }

        public void SetTimeScale(float timeScale)
        {
            if (type == MateType.MasterDuel)
            {
                Tools.SetAnimatorTimescale(transform, timeScale);
            }
        }

        public bool PlayChangeTransition(bool toSubForm)
        {
            if (type != MateType.MasterDuel)
                return false;

            if (toSubForm)
            {
                return PlayChangeTransition(new[] { "Change", "Change1", "ChangePreHide", "Change2" });
            }

            return PlayChangeTransition(new[] { "Change2", "ChangeBack", "ChangePreHide", "Change" });
        }

        public bool PlayChangeTransition(IReadOnlyList<string> triggerPriority)
        {
            return PlayChangeTransition(triggerPriority, out _);
        }

        public bool PlayChangeTransition(IReadOnlyList<string> triggerPriority, out float suggestedDelaySeconds)
        {
            //foreach (var trigger in triggerPriority)
            //    Debug.Log($"PlayChangeTransition: {trigger}");

            suggestedDelaySeconds = 0f;
            if (type != MateType.MasterDuel)
                return false;
            if (triggerPriority == null || triggerPriority.Count == 0)
                return false;

            for (var i = 0; i < triggerPriority.Count; i++)
            {
                var triggerName = triggerPriority[i];
                if (string.IsNullOrEmpty(triggerName))
                    continue;
                if (TryPlayMasterDuelTrigger(triggerName, out suggestedDelaySeconds, false))
                    return true;
            }
            return false;
        }

        public bool HasMasterDuelTriggerParameter(string triggerName)
        {
            if (type != MateType.MasterDuel || string.IsNullOrEmpty(triggerName))
                return false;

            RefreshMasterDuelAnimatorCacheIfNeeded();
            EnsureMasterDuelInitialized();
            if (!masterDuelHasAnyTriggers || masterDuelAnimators == null || masterDuelAnimators.Length == 0)
                return false;

            if (masterDuelPrimaryAnimator != null
                && masterDuelTriggersByAnimator.TryGetValue(masterDuelPrimaryAnimator, out var primaryTriggers)
                && primaryTriggers.Contains(triggerName))
                return true;

            for (var i = 0; i < masterDuelAnimators.Length; i++)
            {
                var animator = masterDuelAnimators[i];
                if (animator == null)
                    continue;
                if (masterDuelTriggersByAnimator.TryGetValue(animator, out var triggers) && triggers.Contains(triggerName))
                    return true;
            }

            return false;
        }

        public bool TryDescribeMasterDuelTransitionCandidates(IReadOnlyList<string> triggerPriority, out string summary)
        {
            summary = "none";
            if (type != MateType.MasterDuel)
                return false;
            if (triggerPriority == null || triggerPriority.Count == 0)
                return false;

            RefreshMasterDuelAnimatorCacheIfNeeded();
            EnsureMasterDuelInitialized();

            var parts = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var hasAnyCandidate = false;
            for (var i = 0; i < triggerPriority.Count; i++)
            {
                var triggerName = triggerPriority[i];
                if (string.IsNullOrEmpty(triggerName))
                    continue;
                if (!seen.Add(triggerName))
                    continue;

                var hasParameter = HasMasterDuelTriggerParameter(triggerName);
                var hasStateFallback = HasMasterDuelStateFallbackCandidate(triggerName, out var stateCandidate);
                var hasClipFallback = HasMasterDuelClipFallbackCandidate(triggerName, out var clipCandidate);
                if (hasParameter || hasStateFallback || hasClipFallback)
                    hasAnyCandidate = true;

                var stateSummary = hasStateFallback ? stateCandidate : "-";
                var clipSummary = hasClipFallback ? clipCandidate : "-";
                parts.Add($"{triggerName}[param={(hasParameter ? 1 : 0)},state={stateSummary},clip={clipSummary}]");
            }

            if (parts.Count > 0)
                summary = string.Join(";", parts);
            return hasAnyCandidate;
        }

        public bool TryGetMasterDuelPrimaryAnimatorSnapshot(out int stateHash, out float normalizedTime, out float stateLength, out bool inTransition, out bool activeInHierarchy)
        {
            stateHash = 0;
            normalizedTime = 0f;
            stateLength = 0f;
            inTransition = false;
            activeInHierarchy = false;

            if (type != MateType.MasterDuel)
                return false;

            RefreshMasterDuelAnimatorCacheIfNeeded();
            EnsureMasterDuelInitialized();
            if (masterDuelPrimaryAnimator == null)
                return false;

            activeInHierarchy = masterDuelPrimaryAnimator.gameObject != null && masterDuelPrimaryAnimator.gameObject.activeInHierarchy;
            if (masterDuelPrimaryAnimator.layerCount <= 0)
                return true;

            var stateInfo = masterDuelPrimaryAnimator.GetCurrentAnimatorStateInfo(0);
            stateHash = stateInfo.shortNameHash;
            normalizedTime = stateInfo.normalizedTime;
            stateLength = stateInfo.length;
            inTransition = masterDuelPrimaryAnimator.IsInTransition(0);
            return true;
        }

        private bool TryPlayMasterDuelTrigger(string triggerName)
        {
            return TryPlayMasterDuelTrigger(triggerName, out _);
        }

        private bool TryPlayMasterDuelTrigger(string triggerName, bool logMissing)
        {
            return TryPlayMasterDuelTrigger(triggerName, out _, logMissing);
        }

        private bool TryPlayMasterDuelTrigger(string triggerName, out float suggestedDelaySeconds)
        {
            return TryPlayMasterDuelTrigger(triggerName, out suggestedDelaySeconds, true);
        }

        private bool TryPlayMasterDuelTrigger(string triggerName, out float suggestedDelaySeconds, bool logMissing)
        {
            suggestedDelaySeconds = 0f;
            if (type != MateType.MasterDuel)
                return false;
            RefreshMasterDuelAnimatorCacheIfNeeded();
            EnsureMasterDuelInitialized();
            if (masterDuelAnimators == null || masterDuelAnimators.Length == 0)
                return false;
            if (!masterDuelHasAnyTriggers)
            {
                if (TryPlayMasterDuelStateFallback(triggerName, out suggestedDelaySeconds))
                    return true;
                if (TryPlayMasterDuelClipFallback(triggerName, out suggestedDelaySeconds))
                    return true;
                return false;
            }

            if (masterDuelPrimaryAnimator != null
                && CanDriveAnimator(masterDuelPrimaryAnimator)
                && masterDuelTriggersByAnimator.TryGetValue(masterDuelPrimaryAnimator, out var primaryTriggers)
                && primaryTriggers.Contains(triggerName))
            {
                masterDuelPrimaryAnimator.SetTrigger(triggerName);
                suggestedDelaySeconds = EstimateTriggerDuration(masterDuelPrimaryAnimator, triggerName);
                return true;
            }

            foreach (var animator in masterDuelAnimators)
            {
                if (!CanDriveAnimator(animator))
                    continue;
                if (!masterDuelTriggersByAnimator.TryGetValue(animator, out var triggers))
                    continue;
                if (!triggers.Contains(triggerName))
                    continue;

                animator.SetTrigger(triggerName);
                suggestedDelaySeconds = EstimateTriggerDuration(animator, triggerName);
                return true;
            }

            if (TryPlayMasterDuelStateFallback(triggerName, out suggestedDelaySeconds))
                return true;
            if (TryPlayMasterDuelClipFallback(triggerName, out suggestedDelaySeconds))
                return true;

            return false;
        }

        private bool TryPlayMasterDuelStateFallback(string triggerName, out float suggestedDelaySeconds)
        {
            suggestedDelaySeconds = 0f;
            if (string.IsNullOrEmpty(triggerName) || masterDuelAnimators == null || masterDuelAnimators.Length == 0)
                return false;

            var candidates = GetMasterDuelStateFallbackCandidates(triggerName);
            for (var i = 0; i < masterDuelAnimators.Length; i++)
            {
                var animator = masterDuelAnimators[i];
                if (!CanDriveAnimator(animator))
                    continue;

                for (var layer = 0; layer < animator.layerCount; layer++)
                {
                    var layerName = animator.GetLayerName(layer);
                    for (var j = 0; j < candidates.Length; j++)
                    {
                        var candidate = candidates[j];
                        if (string.IsNullOrEmpty(candidate))
                            continue;

                        var shortHash = Animator.StringToHash(candidate);
                        var fullPathHash = Animator.StringToHash($"{layerName}.{candidate}");
                        if (!animator.HasState(layer, shortHash) && !animator.HasState(layer, fullPathHash))
                            continue;

                        if (animator.HasState(layer, fullPathHash))
                            animator.Play(fullPathHash, layer, 0f);
                        else
                            animator.Play(shortHash, layer, 0f);

                        suggestedDelaySeconds = EstimateTriggerDuration(animator, candidate);
                        if (suggestedDelaySeconds <= 0f)
                            suggestedDelaySeconds = EstimateTriggerDuration(animator, triggerName);
                        if (suggestedDelaySeconds <= 0f)
                        {
                            var stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
                            if (stateInfo.length > 0f)
                                suggestedDelaySeconds = stateInfo.length;
                        }
                        if (suggestedDelaySeconds <= 0f && candidate.StartsWith("Change", StringComparison.OrdinalIgnoreCase))
                            suggestedDelaySeconds = 1.35f;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasMasterDuelStateFallbackCandidate(string triggerName, out string candidateSummary)
        {
            candidateSummary = null;
            if (string.IsNullOrEmpty(triggerName) || masterDuelAnimators == null || masterDuelAnimators.Length == 0)
                return false;

            var candidates = GetMasterDuelStateFallbackCandidates(triggerName);
            for (var i = 0; i < masterDuelAnimators.Length; i++)
            {
                var animator = masterDuelAnimators[i];
                if (animator == null)
                    continue;

                for (var layer = 0; layer < animator.layerCount; layer++)
                {
                    var layerName = animator.GetLayerName(layer);
                    for (var j = 0; j < candidates.Length; j++)
                    {
                        var candidate = candidates[j];
                        if (string.IsNullOrEmpty(candidate))
                            continue;

                        var shortHash = Animator.StringToHash(candidate);
                        var fullPathHash = Animator.StringToHash($"{layerName}.{candidate}");
                        if (!animator.HasState(layer, shortHash) && !animator.HasState(layer, fullPathHash))
                            continue;

                        candidateSummary = $"{candidate}@{animator.name}:L{layer}";
                        return true;
                    }
                }
            }

            return false;
        }

        private static string[] GetMasterDuelStateFallbackCandidates(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName))
                return Array.Empty<string>();

            var triggerLower = triggerName.ToLowerInvariant();
            if (triggerLower == "tap" || triggerLower == "tap1" || triggerLower == "tap2")
                return masterDuelTapFallbackStates;
            if (triggerLower == "wait2" || triggerLower == "wait3" || triggerLower == "random1" || triggerLower == "random2" || triggerLower == "normal")
                return masterDuelRandomFallbackStates;
            if (triggerLower.StartsWith("change", StringComparison.Ordinal))
            {
                var ordered = new List<string>(masterDuelChangeFallbackStates.Length + 1);
                ordered.Add(triggerName);
                for (var i = 0; i < masterDuelChangeFallbackStates.Length; i++)
                {
                    var candidate = masterDuelChangeFallbackStates[i];
                    if (string.Equals(candidate, triggerName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    ordered.Add(candidate);
                }

                return ordered.ToArray();
            }
            return new[] { triggerName };
        }

        private bool TryPlayMasterDuelClipFallback(string triggerName, out float suggestedDelaySeconds)
        {
            suggestedDelaySeconds = 0f;
            if (string.IsNullOrEmpty(triggerName))
                return false;

            var allowGenericFallback = triggerName.Equals("Tap", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Tap1", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Tap2", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Random1", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Random2", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Wait2", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Wait3", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Normal", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Entry", StringComparison.OrdinalIgnoreCase);

            for (var i = 0; i < masterDuelAnimators.Length; i++)
            {
                var animator = masterDuelAnimators[i];
                if (!CanDriveAnimator(animator))
                    continue;

                var controller = animator.runtimeAnimatorController;
                if (controller == null)
                    continue;

                AnimationClip bestClip = null;
                AnimationClip genericClip = null;
                var clips = controller.animationClips;
                var triggerLower = triggerName.ToLowerInvariant();
                for (var j = 0; j < clips.Length; j++)
                {
                    var clip = clips[j];
                    if (clip == null)
                        continue;

                    var clipName = clip.name.ToLowerInvariant();
                    if (allowGenericFallback)
                    {
                        var looksIdle = clipName.Contains("idle") || clipName.Contains("wait");
                        if (!looksIdle && (genericClip == null || clip.length > genericClip.length))
                            genericClip = clip;
                    }
                    var matches = clipName.Contains(triggerLower)
                        || ((triggerLower == "tap" || triggerLower == "tap1" || triggerLower == "tap2") && clipName.Contains("tap"))
                        || ((triggerLower == "wait2" || triggerLower == "wait3" || triggerLower == "random1" || triggerLower == "random2") && (clipName.Contains("wait") || clipName.Contains("random")))
                        || (triggerLower.StartsWith("change", StringComparison.Ordinal) && clipName.Contains("change"));
                    if (!matches)
                        continue;

                    if (bestClip == null || clip.length > bestClip.length)
                        bestClip = clip;
                }

                if (bestClip == null)
                    bestClip = genericClip;
                if (bestClip == null)
                    continue;

                animator.Play(bestClip.name, 0, 0f);
                suggestedDelaySeconds = bestClip.length;
                return true;
            }

            return false;
        }

        private bool HasMasterDuelClipFallbackCandidate(string triggerName, out string clipName)
        {
            clipName = null;
            if (string.IsNullOrEmpty(triggerName) || masterDuelAnimators == null || masterDuelAnimators.Length == 0)
                return false;

            var allowGenericFallback = triggerName.Equals("Tap", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Tap1", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Tap2", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Random1", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Random2", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Wait2", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Wait3", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Normal", StringComparison.OrdinalIgnoreCase)
                || triggerName.Equals("Entry", StringComparison.OrdinalIgnoreCase);

            var triggerLower = triggerName.ToLowerInvariant();
            for (var i = 0; i < masterDuelAnimators.Length; i++)
            {
                var animator = masterDuelAnimators[i];
                if (animator == null)
                    continue;

                var controller = animator.runtimeAnimatorController;
                if (controller == null)
                    continue;

                AnimationClip bestClip = null;
                AnimationClip genericClip = null;
                var clips = controller.animationClips;
                for (var j = 0; j < clips.Length; j++)
                {
                    var clip = clips[j];
                    if (clip == null)
                        continue;

                    var clipLower = clip.name.ToLowerInvariant();
                    if (allowGenericFallback)
                    {
                        var looksIdle = clipLower.Contains("idle") || clipLower.Contains("wait");
                        if (!looksIdle && (genericClip == null || clip.length > genericClip.length))
                            genericClip = clip;
                    }

                    var matches = clipLower.Contains(triggerLower)
                        || ((triggerLower == "tap" || triggerLower == "tap1" || triggerLower == "tap2") && clipLower.Contains("tap"))
                        || ((triggerLower == "wait2" || triggerLower == "wait3" || triggerLower == "random1" || triggerLower == "random2")
                            && (clipLower.Contains("wait") || clipLower.Contains("random")))
                        || (triggerLower.StartsWith("change", StringComparison.Ordinal) && clipLower.Contains("change"));
                    if (!matches)
                        continue;

                    if (bestClip == null || clip.length > bestClip.length)
                        bestClip = clip;
                }

                if (bestClip == null)
                    bestClip = genericClip;
                if (bestClip == null)
                    continue;

                clipName = bestClip.name;
                return true;
            }

            return false;
        }

        private static bool CanDriveAnimator(Animator animator)
        {
            return animator != null
                && animator.enabled
                && animator.gameObject != null
                && animator.gameObject.activeInHierarchy;
        }

        private static float EstimateTriggerDuration(Animator animator, string triggerName)
        {
            if (animator == null || string.IsNullOrEmpty(triggerName))
                return 0f;
            var controller = animator.runtimeAnimatorController;
            if (controller == null)
                return 0f;

            var triggerLower = triggerName.ToLowerInvariant();
            var best = 0f;
            var clips = controller.animationClips;
            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip == null)
                    continue;

                var clipName = clip.name.ToLowerInvariant();
                var matches = clipName.Contains(triggerLower)
                    || (triggerLower.StartsWith("change", StringComparison.Ordinal) && clipName.Contains("change"))
                    || (triggerLower == "wait2" && (clipName.Contains("wait2") || clipName.Contains("random1")))
                    || (triggerLower == "wait3" && (clipName.Contains("wait3") || clipName.Contains("random2")));
                if (!matches)
                    continue;

                if (clip.length > best)
                    best = clip.length;
            }
            if (best <= 0f && triggerLower.StartsWith("change", StringComparison.Ordinal))
                return 1.35f;
            return best;
        }

        private static void KeepAnimatorStateOnDisable(Animator animator)
        {
            var type = typeof(Animator);
            var keepController = type.GetProperty("keepAnimatorControllerStateOnDisable");
            if (keepController != null && keepController.PropertyType == typeof(bool) && keepController.CanWrite)
            {
                keepController.SetValue(animator, true, null);
                return;
            }

            var keepState = type.GetProperty("keepAnimatorStateOnDisable");
            if (keepState != null && keepState.PropertyType == typeof(bool) && keepState.CanWrite)
                keepState.SetValue(animator, true, null);
        }
    }
}
