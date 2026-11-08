#!/usr/bin/env python3
"""Shared read-only card data and duel probes for the pinned MDPro3 core.

The shipping native ABI, script reader, and duel helpers are shared by source
runtime verification and card coverage audits. Importing this module does not
open the native library or any card database.
"""
from __future__ import annotations

import ctypes
import hashlib
from pathlib import Path
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[1]
LIBRARY_HASH = "9ebca5960c34e45e33446ff7c152b11f65d49e9a309998455630ddb439654197"


class CardData(ctypes.Structure):
    # The shipping arm64 DataManager copies 80 bytes. Its core record begins
    # with code/alias followed by 16 16-bit setcodes (CoreWrapper.cs), with the
    # shipping native rule-code extension in the final four bytes.
    _fields_ = [("code", ctypes.c_uint32), ("alias", ctypes.c_uint32),
                ("setcodes", ctypes.c_uint16 * 16), ("type", ctypes.c_uint32),
                ("level", ctypes.c_uint32), ("attribute", ctypes.c_uint32),
                ("race", ctypes.c_uint32), ("atk", ctypes.c_int32),
                ("defense", ctypes.c_int32), ("lscale", ctypes.c_uint32),
                ("rscale", ctypes.c_uint32), ("link_marker", ctypes.c_uint32),
                ("rule_code", ctypes.c_uint32)]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


class NativeCore:
    def __init__(self, client, locale):
        self.client = client
        library = client / "MDPro3.app/Contents/PlugIns/libygoserver.dylib"
        if not library.is_file():
            library = ROOT / "vendor/MDPro3-mac/MDPro3.app/Contents/PlugIns/libygoserver.dylib"
        assert digest(library) == LIBRARY_HASH, "Native library differs from the shipping core"
        self.lib = ctypes.CDLL(str(library))
        self.pointer = ctypes.c_void_p
        self.reads, self.errors, self.buffers = [], [], {}
        self.archive = zipfile.ZipFile(client / "Data/script.zip")
        self.db = client / "Data/locales" / locale / "cards.cdb"
        self.manager = ctypes.addressof(ctypes.c_ubyte.in_dll(self.lib, "_ZN3ygo11dataManagerE"))
        database = self.pointer()
        open_database = self.function("sqlite3_open_v2", ctypes.c_int,
                                      [ctypes.c_char_p, ctypes.POINTER(self.pointer), ctypes.c_int, ctypes.c_char_p])
        assert open_database(str(self.db).encode(), ctypes.byref(database), 1, None) == 0
        try:
            load = self.function("_ZN3ygo11DataManager6ReadDBEP7sqlite3", ctypes.c_bool, [self.pointer, self.pointer])
            assert load(self.manager, database), "Shipping DataManager did not load the patched card database"
        finally:
            self.function("sqlite3_close", ctypes.c_int, [self.pointer])(database)
        self.get_data = self.function("_ZNK3ygo11DataManager7GetDataEjP9card_data", ctypes.c_bool,
                                     [self.pointer, ctypes.c_uint32, ctypes.POINTER(CardData)])
        self.card_reader = self.function("_ZN3ygo11DataManager10CardReaderEjP9card_data", ctypes.c_uint32,
                                        [ctypes.c_uint32, ctypes.POINTER(CardData)])
        self.function("set_card_reader", None, [self.pointer])(ctypes.cast(self.card_reader, self.pointer))
        script_type = ctypes.CFUNCTYPE(self.pointer, ctypes.c_char_p, ctypes.POINTER(ctypes.c_int))

        def script_reader(name, length):
            path = name.decode()
            if path.startswith("./"):
                path = path[2:]
            if path not in self.archive.namelist():
                length[0] = 0
                return None
            data = self.archive.read(path)
            self.reads.append({"path": path, "sha256": hashlib.sha256(data).hexdigest(), "bytes": len(data)})
            buffer = self.buffers.setdefault(path, ctypes.create_string_buffer(data))
            length[0] = len(data)
            return ctypes.addressof(buffer)

        self.script_callback = script_type(script_reader)
        self.function("set_script_reader", None, [script_type])(self.script_callback)
        log = self.function("get_log_message", None, [self.pointer, self.pointer])
        message_type = ctypes.CFUNCTYPE(ctypes.c_uint32, self.pointer, ctypes.c_uint32)

        def error_reader(duel, message_type):
            buffer = ctypes.create_string_buffer(4096)
            log(duel, buffer)
            self.errors.append(buffer.value.decode(errors="replace"))
            return 0

        self.error_callback = message_type(error_reader)
        self.function("set_message_handler", None, [message_type])(self.error_callback)
        self.create = self.function("create_duel", self.pointer, [ctypes.c_uint32])
        self.end = self.function("end_duel", None, [self.pointer])
        self.start = self.function("start_duel", None, [self.pointer, ctypes.c_uint32])
        self.new_card = self.function("new_card", None, [self.pointer, ctypes.c_uint32] + [ctypes.c_uint8] * 5)
        self.player = self.function("set_player_info", None, [self.pointer] + [ctypes.c_int32] * 4)
        self.process = self.function("process", ctypes.c_int32, [self.pointer])
        self.get_message = self.function("get_message", ctypes.c_int32, [self.pointer, self.pointer])
        self.response = self.function("set_responsei", None, [self.pointer, ctypes.c_int32])
        self.response_bytes = self.function("set_responseb", None, [self.pointer, self.pointer])
        self.query = self.function("query_card", ctypes.c_int32,
                                  [self.pointer, ctypes.c_uint8, ctypes.c_uint8, ctypes.c_uint8,
                                   ctypes.c_int32, self.pointer, ctypes.c_int32])
        self.count = self.function("query_field_count", ctypes.c_int32, [self.pointer, ctypes.c_uint8, ctypes.c_uint8])

    def function(self, name, result, arguments):
        function = getattr(self.lib, name)
        function.restype, function.argtypes = result, arguments
        return function

    def data(self, code):
        card = CardData()
        assert self.get_data(self.manager, code, ctypes.byref(card)), "Native card identity missing: " + str(code)
        return {"code": card.code, "alias": card.alias, "type": card.type, "level": card.level,
                "attribute": card.attribute, "race": card.race, "atk": card.atk, "def": card.defense,
                "rule_code": card.rule_code}

    def setup(self, hand, extra=(), field=()):
        duel = self.create(20261101)
        assert duel
        for player in (0, 1):
            self.player(duel, player, 8000, 0, 0)
            for index in range(40):
                self.new_card(duel, 89631139, player, player, 1, index, 8)
        for index, code in enumerate(hand):
            self.new_card(duel, code, 0, 0, 2, index, 8)
        for index, code in enumerate(extra):
            self.new_card(duel, code, 0, 0, 0x40, index, 8)
        for index, code in enumerate(field):
            self.new_card(duel, code, 0, 0, 4, index, 1)
        self.start(duel, 4 << 16)
        return duel

    def step(self, duel):
        state = self.process(duel)
        buffer = ctypes.create_string_buffer(65536)
        length = self.get_message(duel, buffer)
        assert 0 <= length < len(buffer)
        return state, buffer.raw[:length]

    def card_code(self, duel, player, location, sequence):
        buffer = ctypes.create_string_buffer(1024)
        length = self.query(duel, player, location, sequence, 1, buffer, 0)
        assert length == 12 and struct.unpack_from("<I", buffer.raw, 4)[0] == 1
        return struct.unpack_from("<I", buffer.raw, 8)[0]

    def field_code(self, duel, sequence):
        return self.card_code(duel, 0, 4, sequence)

    def location_codes(self, duel, player, location):
        return [self.card_code(duel, player, location, index)
                for index in range(self.count(duel, player, location))]

    def select_place(self, duel, message):
        assert len(message) == 7 and message[0] == 18 and message[2] == 1
        available = ~struct.unpack_from("<I", message, 3)[0] & 0xFFFFFFFF
        assert available
        bit = (available & -available).bit_length() - 1
        assert bit // 16 == 0
        location = 4 if bit % 16 < 8 else 8
        self.response_bytes(duel, ctypes.create_string_buffer(bytes([0, location, bit % 8])))
        return bit % 8

    def idle(self, duel):
        for _ in range(30):
            state, message = self.step(duel)
            assert not message or message[0] != 1, "Core requested a retry"
            if state & 0x10000000:
                assert message[0] == 11, "Unexpected initial duel prompt"
                return message
        raise AssertionError("Core did not reach its Main Phase action prompt")


def tribute_summon(core, code):
    duel = core.setup([code], field=[89631139, 89631139])
    prompts, sequence = [], None
    try:
        idle = core.idle(duel)
        assert idle[2] == 1 and struct.unpack_from("<I", idle, 3)[0] == code
        core.response(duel, 0)
        for _ in range(50):
            state, message = core.step(duel)
            assert not message or message[0] != 1, "Core rejected the tribute-summon response"
            if not state & 0x10000000:
                continue
            prompts.append(message[0])
            if message[0] == 20:
                assert message[3:6] == bytes([2, 2, 2]), "Normal monster must require both tributes"
                core.response_bytes(duel, ctypes.create_string_buffer(bytes([2, 0, 1])))
            elif message[0] == 18:
                sequence = core.select_place(duel, message)
            elif message[0] == 16:
                core.response(duel, -1)
            elif message[0] == 11:
                assert sequence is not None and 20 in prompts
                assert core.field_code(duel, sequence) == code
                assert core.count(duel, 0, 4) == 1 and core.count(duel, 0, 16) == 2
                graveyard = core.location_codes(duel, 0, 16)
                assert graveyard == [89631139, 89631139]
                assert core.count(duel, 0, 1) == core.count(duel, 1, 1) == 40
                return {"code": code, "tributes": 2, "summoned_code": code,
                        "graveyard_codes": graveyard, "prompt_messages": prompts}
            else:
                raise AssertionError("Unexpected tribute summon prompt: " + message.hex())
        raise AssertionError("Tribute summon did not return to the Main Phase")
    finally:
        core.end(duel)
