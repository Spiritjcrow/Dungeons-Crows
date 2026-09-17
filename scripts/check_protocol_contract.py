#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
TS_PROTOCOL = ROOT / 'WebPrototype/backend/protocol.ts'
TS_INDEX = ROOT / 'WebPrototype/backend/index.ts'
CS_PROTOCOL = ROOT / 'UnityProject/Assets/Crows/Scripts/Online/OnlineProtocol.cs'
CS_CLIENT = ROOT / 'UnityProject/Assets/Crows/Scripts/Online/OnlineGameClient.cs'


def require(pattern: str, text: str, label: str) -> str:
    match = re.search(pattern, text, re.DOTALL)
    if not match:
        raise AssertionError(f'Missing {label}')
    return match.group(1)


def main() -> int:
    ts = TS_PROTOCOL.read_text(encoding='utf-8')
    ts_index = TS_INDEX.read_text(encoding='utf-8')
    cs = CS_PROTOCOL.read_text(encoding='utf-8')
    cs_client = CS_CLIENT.read_text(encoding='utf-8')

    errors: list[str] = []

    try:
        ts_version = require(r"PROTOCOL_VERSION\s*=\s*'([^']+)'", ts, 'TypeScript protocol version')
        cs_version = require(r'public const string Version\s*=\s*"([^"]+)"', cs, 'C# protocol version')
        if ts_version != cs_version:
            errors.append(f'Protocol version mismatch: web={ts_version}, unity={cs_version}')

        ts_entity = require(r"sessionEntityType:\s*'([^']+)'", ts, 'TypeScript session entity type')
        cs_entity = require(r'public const string SessionEntityType\s*=\s*"([^"]+)"', cs, 'C# session entity type')
        if ts_entity != cs_entity:
            errors.append(f'Entity type mismatch: web={ts_entity}, unity={cs_entity}')

        ts_actions_block = require(r'actions:\s*\[(.*?)\]', ts, 'TypeScript actions')
        cs_actions_block = require(r'public static readonly string\[\] Actions\s*=\s*\{(.*?)\};', cs, 'C# actions')
        ts_actions = re.findall(r"'([^']+)'", ts_actions_block)
        cs_actions = re.findall(r'"([^"]+)"', cs_actions_block)
        if ts_actions != cs_actions:
            errors.append(f'Action mismatch: web={ts_actions}, unity={cs_actions}')

        limits = {
            'playerName': 'MaxPlayerNameLength',
            'freeformText': 'MaxFreeformTextLength',
            'partySize': 'MaxPartySize',
            'chronicleEntries': 'MaxChronicleEntries',
        }
        for ts_name, cs_name in limits.items():
            ts_value = int(require(rf'{ts_name}:\s*(\d+)', ts, f'TypeScript limit {ts_name}'))
            cs_value = int(require(rf'public const int {cs_name}\s*=\s*(\d+)', cs, f'C# limit {cs_name}'))
            if ts_value != cs_value:
                errors.append(f'Limit mismatch {ts_name}: web={ts_value}, unity={cs_value}')

        if "'GET /api/protocol'" not in ts_index:
            errors.append('Web backend does not expose GET /api/protocol')
        if 'withProtocol(' not in ts_index:
            errors.append('Web backend responses are not protocol-tagged')
        if '"/api/protocol"' not in cs_client:
            errors.append('Unity client does not verify /api/protocol')
        if 'ProtocolCompatibility.IsCompatible' not in cs_client:
            errors.append('Unity client does not enforce protocol compatibility')

    except AssertionError as exc:
        errors.append(str(exc))

    if errors:
        print('Dungeons & Crows protocol contract FAILED:')
        for error in errors:
            print(f' - {error}')
        return 1

    print('Dungeons & Crows protocol contract OK')
    print(f' version={ts_version}')
    print(f' entity={ts_entity}')
    print(f' actions={",".join(ts_actions)}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
