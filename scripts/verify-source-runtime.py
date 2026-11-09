#!/usr/bin/env python3
"""Check a source-built MDPro3 assembly with shipping Mono and the matching core.

No Unity scene or user data is opened. Managed dependencies are linked into a
temporary directory; Mono and native rule checks run in separate subprocesses.
"""
from __future__ import annotations

import argparse
import ctypes
import hashlib
import json
import os
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import tempfile

from native_core import LIBRARY_HASH, NativeCore, tribute_summon
from client_ui_runtime_metadata import verify_runtime_metadata

ROOT = Path(__file__).resolve().parents[1]
MANAGED = "MDPro3.app/Contents/Resources/Data/Managed"
POLICY = """(version 1)(allow default)(deny network*)
(allow network-outbound (remote ip "localhost:*"))
(allow network-inbound (local ip "localhost:*"))
(allow network-bind (local ip "localhost:*"))"""


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def check(condition, message):
    if not condition:
        raise ValueError(message)


def validate(client: Path, assembly: Path, manifest: Path) -> dict:
    build = json.loads(manifest.read_text())
    check(build.get("ok") is True, "Source build manifest does not report success")
    check(digest(assembly) == build.get("assembly_sha256"),
          "Assembly does not match the source build manifest")
    check(isinstance(build.get("client_ui_runtime_metadata"), dict), "Source build must bind client runtime metadata")
    runtime_metadata = verify_runtime_metadata(ROOT, client, build["client_ui_runtime_metadata"])
    publisher = {}
    for line in (ROOT / "vendor/MDPro3-mac-metadata/checksums.txt").read_text().splitlines():
        value, name = line.split("  ", 1)
        publisher[name] = value
    dependencies = []
    names = ["MDPro3.app/Contents/Frameworks/libmonobdwgc-2.0.dylib",
             "MDPro3.app/Contents/Frameworks/libmono-native.dylib",
             "MDPro3.app/Contents/PlugIns/libygoserver.dylib", "Data/script.zip"]
    names += [path.relative_to(client).as_posix() for path in sorted((client / MANAGED).glob("*.dll"))
              if path.name != "Assembly-CSharp.dll"]
    for name in names:
        actual = digest(client / name)
        check(actual == publisher.get(name), "Unsupported shipping dependency: " + name)
        dependencies.append({"path": name, "sha256": actual})
    return {"assembly_sha256": digest(assembly), "manifest_sha256": digest(manifest),
            "client_ui_runtime_metadata": runtime_metadata,
            "shipping_dependencies_verified": len(dependencies), "dependencies": dependencies}


def managed_check(client: Path, managed: Path) -> dict:
    contents = client / "MDPro3.app/Contents"
    mono = ctypes.CDLL(str(contents / "Frameworks/libmonobdwgc-2.0.dylib"), mode=ctypes.RTLD_GLOBAL)
    pointer, string = ctypes.c_void_p, ctypes.c_char_p

    def api(name, returns, parameters):
        method = getattr(mono, name)
        method.restype, method.argtypes = returns, parameters
        return method

    api("mono_set_dirs", None, [string, string])(
        str(contents / "Frameworks").encode(), str(contents / "MonoBleedingEdge/etc").encode())
    api("mono_set_assemblies_path", None, [string])(str(managed).encode())
    api("mono_config_parse", None, [string])(str(contents / "MonoBleedingEdge/etc/mono/config").encode())
    api("mono_set_signal_chaining", None, [ctypes.c_int])(1)
    domain = api("mono_jit_init_version", pointer, [string, string])(b"MDPro3 source smoke", b"v4.0.30319")
    check(domain, "Shipping Mono initialization failed")
    api("mono_dllmap_insert", None, [pointer, string, string, string, string])(
        None, b"System.Native", None, str(contents / "Frameworks/libmono-native.dylib").encode(), None)
    open_assembly = api("mono_domain_assembly_open", pointer, [pointer, string])
    get_image = api("mono_assembly_get_image", pointer, [pointer])
    klass = api("mono_class_from_name", pointer, [pointer, string, string])
    method = api("mono_class_get_method_from_name", pointer, [pointer, string, ctypes.c_int])
    field = api("mono_class_get_field_from_name", pointer, [pointer, string])
    field_type = api("mono_field_get_type", pointer, [pointer])
    type_name = api("mono_type_get_name", pointer, [pointer])
    free = api("mono_free", None, [pointer])
    read_field = api("mono_field_get_value", None, [pointer, pointer, pointer])
    invoke_native = api("mono_runtime_invoke", pointer, [pointer, pointer, pointer, ctypes.POINTER(pointer)])
    new_object = api("mono_object_new", pointer, [pointer, pointer])
    object_class = api("mono_object_get_class", pointer, [pointer])
    unbox = api("mono_object_unbox", pointer, [pointer])
    pin = api("mono_gchandle_new_v2", ctypes.c_size_t, [pointer, ctypes.c_int])
    target = api("mono_gchandle_get_target_v2", pointer, [ctypes.c_size_t])
    unpin = api("mono_gchandle_free_v2", None, [ctypes.c_size_t])
    handles = []

    def rooted(value):
        check(value, "Managed allocation or invocation returned null")
        handle = pin(value, 0)
        check(handle, "Managed object could not be rooted")
        handles.append(handle)
        return target(handle)

    def invoke(selected, instance=None, parameters=None):
        check(selected, "Expected managed method is missing")
        exception = pointer()
        value = invoke_native(selected, instance, parameters, ctypes.byref(exception))
        check(not exception.value, "Managed smoke invocation raised an exception")
        return value

    try:
        assembly = open_assembly(domain, str(managed / "Assembly-CSharp.dll").encode())
        check(assembly, "Source-built Assembly-CSharp did not load")
        image = get_image(assembly)
        # Exercise the real configuration/options code without opening user data.
        new_string = api("mono_string_new", pointer, [pointer, string])
        string_utf8 = api("mono_string_to_utf8", pointer, [pointer])
        config = klass(image, b"MDPro3", b"Config")
        banlists = klass(image, b"MDPro3.Duel.YGOSharp", b"BanlistManager")
        options_class = klass(image, b"MDPro3.AI", b"ResponsesDuelOptions")
        fight_client = klass(image, b"YuGiOhFight.Client.UI.Client", b"FightClient")

        def managed_string(text):
            return rooted(new_string(domain, text.encode()))

        def text_value(value):
            allocated = string_utf8(rooted(value))
            check(allocated, "Managed string conversion failed")
            try:
                return ctypes.string_at(allocated).decode()
            finally:
                free(allocated)

        def set_config(key, value):
            arguments = (pointer * 2)(managed_string(key), managed_string(value))
            invoke(method(config, b"Set", 2), parameters=arguments)

        def options_name(instance):
            return text_value(invoke(method(options_class, b"get_BanlistName", 0), instance))

        def new_options():
            instance = rooted(new_object(domain, options_class))
            invoke(method(options_class, b".ctor", 0), instance)
            return instance

        def ai_options():
            return rooted(invoke(method(fight_client, b"ReadAiBattleOptions", 0)))

        def server_arguments(instance):
            port = ctypes.c_int(7911)
            arguments = (pointer * 1)(ctypes.cast(ctypes.byref(port), pointer))
            return text_value(invoke(method(options_class, b"ToServerArguments", 1), instance, arguments)).split()

        # Construct the actual List<Banlist> backing field and parse two fixtures.
        # This avoids ResourceManager/Unity initialization and writes no files.
        backing = field(banlists, b"<Banlists>k__BackingField")
        generic_class = api("mono_class_from_mono_type", pointer, [pointer])(field_type(backing))
        lists = rooted(new_object(domain, generic_class))
        invoke(method(generic_class, b".ctor", 0), lists)
        invoke(method(banlists, b"set_Banlists", 1), parameters=(pointer * 1)(lists))
        rooted(invoke(method(banlists, b"get_Banlists", 0)))
        fixtures = managed_string("!Default-A\n89631139 0\n!Default-B\n23995346 1\n!N/A\n")
        invoke(method(banlists, b"InitializeFromText", 1), parameters=(pointer * 1)(fixtures))
        check(options_name(new_options()) == "Default-A", "Unset default did not use the first standard banlist")
        set_config("DefaultBanlist", "Default-B")
        check(options_name(new_options()) == "Default-B", "New duel options ignored the saved global default")
        # Old versions wrote N/A merely by reading the AI settings.
        set_config("AIDuelBanlist", "N/A")
        inherited = ai_options()
        check(options_name(inherited) == "Default-B" and server_arguments(inherited)[1] == "1",
              "AI defaults did not use the selected global list/server index")
        set_config("AIDuelBanlistOverride", "N/A")
        unrestricted = ai_options()
        check(options_name(unrestricted) == "N/A" and server_arguments(unrestricted)[1] == "-1"
              and not invoke(method(options_class, b"ResolveBanlist", 0), unrestricted),
              "Explicit AI N/A did not disable banlist restrictions")
        set_config("DefaultBanlist", "Default-A")
        check(options_name(ai_options()) == "N/A", "Changing the global default overwrote explicit AI N/A")
        set_config("AIDuelBanlistOverride", "")
        restricted = ai_options()
        check(options_name(restricted) == "Default-A", "Returning to inherited AI settings ignored the global default")
        active_list = rooted(invoke(method(options_class, b"ResolveBanlist", 0), restricted))
        card_id = ctypes.c_int(89631139)
        quantity = rooted(invoke(method(object_class(active_list), b"GetQuantity", 1), active_list,
                                (pointer * 1)(ctypes.cast(ctypes.byref(card_id), pointer))))
        check(ctypes.c_int.from_address(unbox(quantity)).value == 0, "Inherited banlist did not prohibit its banned card")
        set_config("DefaultBanlist", "Removed-list")
        set_config("AIDuelBanlistOverride", "Removed-list")
        check(options_name(ai_options()) == "Default-A", "Unavailable lists did not fall back to a valid global default")
        banlist_checks = {"saved_global_default": True, "ai_inherits_global": True,
                          "explicit_ai_none": True, "explicit_none_survives_global_change": True,
                          "restore_ai_inheritance": True, "banned_card_enforced": True,
                          "unavailable_list_fallback": True}
        display_class = klass(image, b"YuGiOhFight.Client.Services", b"RuntimeDisplay")
        unity_image = get_image(open_assembly(domain, str(managed / "UnityEngine.CoreModule.dll").encode()))
        platform_class = klass(unity_image, b"UnityEngine", b"RuntimePlatform")
        screen_mode_class = klass(unity_image, b"UnityEngine", b"FullScreenMode")
        enum_field_value = api("mono_field_get_value_object", pointer, [pointer, pointer, pointer])

        def screen_mode_value(name):
            value = rooted(enum_field_value(domain, field(screen_mode_class, name.encode()), None))
            return ctypes.c_int.from_address(unbox(value)).value

        def resolved_display_mode(mode_name, platform_name):
            enum_object = rooted(enum_field_value(domain, field(platform_class, platform_name.encode()), None))
            platform_value = ctypes.c_int.from_address(unbox(enum_object))
            arguments = (pointer * 2)(managed_string(mode_name), ctypes.cast(ctypes.byref(platform_value), pointer))
            result = rooted(invoke(method(display_class, b"ResolveMode", 2), parameters=arguments))
            return ctypes.c_int.from_address(unbox(result)).value

        check(resolved_display_mode("1", "OSXPlayer") == screen_mode_value("MaximizedWindow"),
              "macOS fullscreen did not use its native maximized window mode")
        check(resolved_display_mode("2", "OSXPlayer") == screen_mode_value("Windowed"), "Windowed mode did not exit fullscreen")
        check(resolved_display_mode("0", "OSXPlayer") == screen_mode_value("MaximizedWindow"),
              "macOS accepted Windows-only exclusive fullscreen")
        check(resolved_display_mode("0", "WindowsPlayer") == screen_mode_value("ExclusiveFullScreen"),
              "Windows exclusive fullscreen changed unexpectedly")

        def fitted_window(width, height):
            numbers = [ctypes.c_int(value) for value in (width, height, 4112, 2580)]
            arguments = (pointer * 4)(*[ctypes.cast(ctypes.byref(value), pointer) for value in numbers])
            result = rooted(invoke(method(display_class, b"FitWindow", 4), parameters=arguments))
            values = (ctypes.c_int * 2).from_address(unbox(result))
            return values[0], values[1]

        fitted_width, fitted_height = fitted_window(4112, 2580)
        check(0 < fitted_width <= 1920 and 0 < fitted_height <= 1080,
              "Leaving fullscreen retained a display-sized window")
        check(fitted_window(1280, 800) == (1280, 800), "Normal window dimensions changed unexpectedly")
        display_checks = {"macos_native_fullscreen": True, "windowed_exit": True,
                          "macos_exclusive_rejected": True, "oversized_window_fitted": True,
                          "normal_window_preserved": True}
        file_selector_resolved = []
        replay_encoder_resolved = []
        if (managed / "libStandaloneFileBrowser.dylib").is_file():
            selector = klass(image, b"SFB", b"StandaloneFileBrowserMac")
            check(selector, "macOS file selector wrapper did not resolve")
            lookup_native = api("mono_lookup_pinvoke_call", pointer, [pointer, ctypes.POINTER(string), ctypes.POINTER(string)])
            for name, count in (("DialogOpenFilePanel", 4), ("DialogOpenFilePanelAsync", 5),
                                ("DialogOpenFolderPanel", 3), ("DialogOpenFolderPanelAsync", 4),
                                ("DialogSaveFilePanel", 4), ("DialogSaveFilePanelAsync", 5)):
                selected = method(selector, name.encode(), count)
                exception_class, exception_message = string(), string()
                check(selected and lookup_native(selected, ctypes.byref(exception_class), ctypes.byref(exception_message)),
                      "Mono could not resolve the packaged file selector: " + name)
                file_selector_resolved.append(name)
        if (managed / "libReplayVideo.dylib").is_file():
            encoder = klass(image, b"YuGiOhFight.Client.Services", b"ReplayVideoExport")
            check(encoder, "macOS replay encoder wrapper did not resolve")
            lookup_encoder = api("mono_lookup_pinvoke_call", pointer, [pointer, ctypes.POINTER(string), ctypes.POINTER(string)])
            for name, count in (("ReplayVideoCreate", 6), ("ReplayVideoAppend", 5),
                                ("ReplayVideoFinish", 1), ("ReplayVideoCancel", 1), ("ReplayVideoError", 1),
                                ("ReplayVideoCancelled", 1), ("ReplayVideoCaptureClock", 0),
                                ("ReplayVideoSetAudioSampleRate", 2)):
                selected = method(encoder, name.encode(), count)
                exception_class, exception_message = string(), string()
                check(selected and lookup_encoder(selected, ctypes.byref(exception_class), ctypes.byref(exception_message)),
                      "Mono could not resolve the packaged replay encoder: " + name)
                replay_encoder_resolved.append(name)
        default_field = {"defaultSelectable": "UnityEngine.UI.Selectable"}
        ui_schemas = {
            "MainMenuUI": default_field,
            "SoloSelectorUI": default_field,
            "DeckSelectorUI": {**default_field, "deckType": "System.String", "buttonLayoutSwitching": "System.Boolean"},
        }
        event_methods = {
            "MainMenuUI": {name: 0 for name in ["OnSolo", "OnOnline", "OnPuzzle", "OnReplay", "OnCutin",
                                                "OnMate", "OnDeck", "OnSetting", "OnExit"]},
            "SoloSelectorUI": {"SelectLastSoloItem": 0, "OnPlay": 0, "OnSelectAIDeck": 0},
            "DeckSelectorUI": {"OnOnlineDeckView": 0, "OnCancel": 0, "Print": 1, "set_PickupShowing": 1,
                               "OnType": 0, "OnConfirm": 0, "OnDelete": 0},
        }
        resolved = []
        for name, fields in ui_schemas.items():
            value = klass(image, b"YuGiOhFight.Client.UI.ServantUI", name.encode())
            check(value, "Serialized UI class is missing: " + name)
            for member, expected in fields.items():
                selected = field(value, member.encode())
                check(selected, "Serialized UI field is missing: " + name + "." + member)
                allocated = type_name(field_type(selected))
                check(allocated, "Serialized field type did not resolve")
                try:
                    actual = ctypes.string_at(allocated).decode()
                    check(actual == expected, "Serialized field type differs: " + name + "." + member + "=" + actual)
                finally:
                    free(allocated)
            for callback, count in event_methods[name].items():
                check(method(value, callback.encode(), count), "Serialized UI callback is missing: " + name + "." + callback)
            resolved.append({"class": "YuGiOhFight.Client.UI.ServantUI." + name,
                             "fields": fields,
                             "event_methods": event_methods[name]})
        for namespace, name in [(b"YuGiOhFight.Client.Presentation", b"Boot"), (b"YuGiOhFight.Client.Duel", b"GameCard"),
                                (b"YuGiOhFight.Client.Controllers", b"OcgCore"), (b"YuGiOhFight.Client.Controllers", b"SoloSelector")]:
            check(klass(image, namespace, name), "Key gameplay type did not resolve: " + name.decode())
        online = klass(image, b"MDPro3.Net", b"OnlineService")
        check(online, "OnlineService class is missing")
        task = rooted(invoke(method(online, b"InitializeAsync", 0)))
        task_class = object_class(task)
        source, token = pointer(1), ctypes.c_int16(1)
        source_field, token_field = field(task_class, b"source"), field(task_class, b"token")
        check(source_field and token_field, "Unexpected shipping UniTask representation")
        read_field(task, source_field, ctypes.byref(source))
        read_field(task, token_field, ctypes.byref(token))
        check(source.value is None and token.value == 0, "Online initialization did not return CompletedTask")
        invoke(method(online, b"Initialize", 0))
        disabled = []
        for name, callback in [(b"MainMenuUI", b"OnOnline")]:
            value = klass(image, b"YuGiOhFight.Client.UI.ServantUI", name)
            instance = rooted(new_object(domain, value))
            invoke(method(value, callback, 0), instance)
            disabled.append(name.decode() + "::" + callback.decode())
        cpu = klass(image, b"YuGiOhFight.Client.Controllers", b"SoloSelector")
        cpu_methods = {"StartWindBot": 6, "LaunchWithConfig": 8,
                       "StartAIForSolo": 2, "StartAIForHandTest": 1}
        for name, count in cpu_methods.items():
            check(method(cpu, name.encode(), count), "CPU entry point did not resolve: " + name)
        compile_method = api("mono_compile_method", pointer, [pointer])
        # The former empty deck-online callback now opens a real academy popup.
        # Headless fake MonoBehaviour objects have no Unity scene or Program;
        # resolve/JIT these callbacks here and execute their UI behavior in GUI QA.
        academy_methods = [(b"YuGiOhFight.Client.UI.ServantUI", b"DeckSelectorUI", b"OnOnlineDeckView", 0),
                           (b"YuGiOhFight.Client.UI.ServantUI", b"DeckEditorUI", b"OnDeckAcademy", 0),
                           (b"YuGiOhFight.Client.UI.ServantUI", b"OcgCoreUI", b"RefreshAcademyHelpButton", 0),
                           (b"YuGiOhFight.Client.Runtime", b"UIManager", b"ShowPopupDeckAcademy", 1),
                           (b"YuGiOhFight.Client.UI.Popup", b"PopupDeckAcademy", b"InitializeSelections", 0)]
        academy_compiled = []
        for namespace, owner, name, count in academy_methods:
            academy_class = klass(image, namespace, owner)
            check(academy_class, "Academy type did not resolve: " + owner.decode())
            selected = method(academy_class, name, count)
            check(selected and compile_method(selected), "Academy UI method did not JIT: " + name.decode())
            academy_compiled.append((namespace.decode() + "." + owner.decode()) + "::" + name.decode())
        animation_methods = [(b"YuGiOhFight.Client.Duel", b"GameCard", name) for name in
                             [b"ShowLabel", b"HideLabel", b"ShowHiddenLabel", b"HideHiddenLabel"]]
        animation_methods += [(b"YuGiOhFight.Client.UI", b"DuelButton", name) for name in [b"Show", b"Hide"]]
        compiled = []
        for namespace, owner, name in animation_methods:
            animation_class = klass(image, namespace, owner)
            check(animation_class, "Animation type did not resolve: " + owner.decode())
            selected = method(animation_class, name, 0)
            check(selected and compile_method(selected), "Animation method did not JIT: " + name.decode())
            compiled.append((namespace.decode() + "." + owner.decode()) + "::" + name.decode())
        return {"ok": True, "assembly_loaded": True, "serialized_ui_classes": resolved,
                "banlist_defaults": banlist_checks,
                "display_modes": display_checks,
                "online_initialize_returns_completed_task": True, "online_initialize_no_exception": True,
                "disabled_navigation_invoked": disabled,
                "cpu_entry_points_resolved": list(cpu_methods), "unity_scene_executed": False,
                "animation_methods_jit_compiled": compiled,
                "academy_ui_methods_jit_compiled": academy_compiled,
                "native_file_selector_entry_points_resolved": file_selector_resolved,
                "native_replay_encoder_entry_points_resolved": replay_encoder_resolved,
                "gc_handles": "pointer-sized Mono v2 APIs"}
    finally:
        for handle in reversed(handles):
            unpin(handle)
        api("mono_jit_cleanup", None, [pointer])(domain)


def native_check(client: Path) -> dict:
    # Reuse the already audited shipping ABI and read-only SQLite initializer.
    core = NativeCore(client, "zh-CN")
    tribute = tribute_summon(core, 89631139)
    ritual = core.setup([5405694], field=[89631139, 89631139])
    try:
        check(core.idle(ritual)[2] == 0, "Ritual monster unexpectedly became normally summonable")
    finally:
        core.end(ritual)
    polymerization = 24094653
    first_read = len(core.reads)
    duel = core.setup([polymerization, 89631139, 89631139, 89631139], [23995346])
    prompts, sequence, choices = [], None, 0
    try:
        idle = core.idle(duel)
        offset = 2
        for group in range(5):
            count = idle[offset]
            check(group == 4 or count == 0, "Unexpected fusion setup action list")
            offset += 1 + count * 7
        check(idle[offset] == 1 and struct.unpack_from("<I", idle, offset + 1)[0] == polymerization,
              "Polymerization was not a legal activation")
        core.response(duel, 5)
        for _ in range(90):
            state, message = core.step(duel)
            check(not message or message[0] != 1, "Core rejected a fusion response")
            if not state & 0x10000000:
                continue
            prompts.append(message[0])
            if message[0] == 18:
                selected = core.select_place(duel, message)
                if choices == 3:
                    sequence = selected
            elif message[0] == 16:
                core.response(duel, -1)
            elif message[0] == 15:
                check(struct.unpack_from("<I", message, 6)[0] == 23995346, "Unexpected fusion monster choice")
                core.response_bytes(duel, ctypes.create_string_buffer(bytes([1, 0])))
            elif message[0] == 26:
                choices += 1
                check(choices <= 3, "Fusion requested too many materials")
                core.response_bytes(duel, ctypes.create_string_buffer(bytes([1, 0])))
            elif message[0] == 19:
                core.response(duel, 1)
            elif message[0] == 11:
                check(choices == 3 and sequence is not None, "Fusion did not consume three materials")
                check(core.field_code(duel, sequence) == 23995346, "Blue-Eyes Ultimate Dragon was not summoned")
                check(core.count(duel, 0, 4) == 1 and core.count(duel, 0, 16) == 4,
                      "Unexpected field or graveyard after fusion")
                check(core.count(duel, 0, 2) == 0 and core.count(duel, 0, 0x40) == 0,
                      "Fusion did not consume hand and extra deck cards")
                graveyard = core.location_codes(duel, 0, 16)
                check(sorted(graveyard) == sorted([polymerization] + [89631139] * 3), "Fusion materials differ")
                break
            else:
                raise ValueError("Unexpected fusion prompt: " + message.hex())
        else:
            raise ValueError("Fusion did not return to Main Phase")
        reads = core.reads[first_read:]
        check(any(item["path"] == "script/c24094653.lua" and item["sha256"] ==
                  "154182328523d598c3a5c0dc7de79550c0d0168a7fb7e2c572f889845c141d41" for item in reads),
              "Matching shipping Polymerization script was not used")
        check(not core.errors, "Shipping core script errors: " + repr(core.errors))
        return {"ok": True, "native_library_sha256": LIBRARY_HASH,
                "database_open_mode": "SQLite READONLY", "tribute_summon": tribute,
                "ritual_normal_summon_rejected": True,
                "fusion": {"spell": polymerization, "monster": 23995346, "materials": [89631139] * 3,
                           "graveyard_codes": graveyard, "prompts": prompts},
                "shipping_script_used": True, "script_errors": core.errors,
                "source_csharp_gameplay_loop_executed": False, "all_cards_dynamically_verified": False}
    finally:
        core.end(duel)
        core.archive.close()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--client", type=Path, required=True)
    parser.add_argument("--assembly", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, default=ROOT / "build/mdpro3-source/manifest.json")
    parser.add_argument("--output", type=Path, default=ROOT / "artifacts/mdpro3-source-runtime.json")
    parser.add_argument("--worker", choices=["managed", "native"], help=argparse.SUPPRESS)
    parser.add_argument("--managed-overlay", type=Path, help=argparse.SUPPRESS)
    args = parser.parse_args()
    client, assembly, manifest = args.client.resolve(), args.assembly.resolve(), args.manifest.resolve()
    if args.worker:
        result = managed_check(client, args.managed_overlay) if args.worker == "managed" else native_check(client)
        print(json.dumps(result, ensure_ascii=False))
        return
    output = args.output.resolve()
    check((ROOT / "vendor").resolve() not in [output, *output.parents], "Do not write reports under vendor")
    check(output not in [assembly, manifest], "Report must not overwrite source build inputs")
    report = {"ok": False, "kind": "source-built-mdpro3-runtime-smoke", "client": str(client),
              "assembly": str(assembly), "ui_fidelity_verified": False, "all_card_rules_verified": False}
    try:
        report["inputs"] = validate(client, assembly, manifest)
        with tempfile.TemporaryDirectory(prefix="mdpro3-source-runtime-") as temporary:
            overlay = Path(temporary) / "Managed"
            overlay.mkdir()
            for dependency in (client / MANAGED).iterdir():
                if dependency.is_file() and dependency.name not in ["Assembly-CSharp.dll", "Assembly-CSharp.pdb"]:
                    (overlay / dependency.name).symlink_to(dependency)
            shutil.copy2(assembly, overlay / "Assembly-CSharp.dll")
            pdb = assembly.with_suffix(".pdb")
            check(pdb.is_file(), "Source build must supply its matching PDB")
            shutil.copy2(pdb, overlay / "Assembly-CSharp.pdb")
            for worker in ("managed", "native"):
                command = ["/usr/bin/sandbox-exec", "-p", POLICY, sys.executable, str(Path(__file__).resolve()),
                           "--client", str(client), "--assembly", str(assembly), "--manifest", str(manifest),
                           "--worker", worker, "--managed-overlay", str(overlay)]
                environment = dict(os.environ, PYTHONDONTWRITEBYTECODE="1")
                # Cold macOS library mapping can wait for a security scan of the
                # full app payload. Keep that assessment bounded and retain the
                # shorter deadline for native rule execution.
                deadline = 300 if worker == "managed" else 90
                result = subprocess.run(command, cwd=overlay, env=environment, capture_output=True, text=True, timeout=deadline)
                check(result.returncode == 0, worker + " worker failed: " + result.stdout[-1000:] + result.stderr[-2000:])
                parsed = json.loads(result.stdout.strip().splitlines()[-1])
                check(parsed.get("ok") is True, worker + " worker did not report success")
                report[worker] = {**parsed, "exit_code": result.returncode, "stderr": result.stderr}
        report["ok"] = True
        report["source_assembly_loaded"] = True
        report["limits"] = ("Shipping Mono loaded the source assembly and invoked offline methods without a Unity scene. "
                            "Native rule scenarios use the matching shipping core directly. This does not prove source UI "
                            "scene execution, visual identity, or every card ruling.")
    except (ValueError, OSError, subprocess.SubprocessError, json.JSONDecodeError) as error:
        report["error"] = str(error)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n")
    print(json.dumps({key: value for key, value in report.items() if key != "inputs"}, ensure_ascii=False))
    raise SystemExit(0 if report["ok"] else 1)


if __name__ == "__main__":
    main()
