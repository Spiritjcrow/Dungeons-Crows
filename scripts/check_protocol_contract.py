#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]

TS_PROTOCOL = ROOT / 'WebPrototype/backend/protocol.ts'
TS_INDEX = ROOT / 'WebPrototype/backend/index.ts'
TS_GAME = ROOT / 'WebPrototype/backend/game.ts'
CS_PROTOCOL = ROOT / 'UnityProject/Assets/Crows/Scripts/Online/OnlineProtocol.cs'
CS_CLIENT = ROOT / 'UnityProject/Assets/Crows/Scripts/Online/OnlineGameClient.cs'
CS_CAMPAIGN = ROOT / 'UnityProject/Assets/Crows/Scripts/Online/OnlineCampaignController.cs'

EXPECTED_VERSION = 'dc-turn/3.0'
EXPECTED_ACTIONS = ['attack', 'defend', 'interact', 'invoke', 'speak']

REQUIRED_SESSION_FIELDS = [
    'chapter',
    'campaignStatus',
    'rookeryPurified',
    'crownBroken',
    'relics',
    'ritualAttempts',
    'score',
]

REQUIRED_COMBATANT_FIELDS = [
    'essence',
    'maxEssence',
]


def require(pattern: str, text: str, label: str) -> str:
    match = re.search(pattern, text, re.DOTALL)
    if not match:
        raise AssertionError(f'Missing {label}')
    return match.group(1)


def main() -> int:
    ts = TS_PROTOCOL.read_text(encoding='utf-8')
    ts_index = TS_INDEX.read_text(encoding='utf-8')
    ts_game = TS_GAME.read_text(encoding='utf-8')
    cs = CS_PROTOCOL.read_text(encoding='utf-8')
    cs_client = CS_CLIENT.read_text(encoding='utf-8')
    cs_campaign = CS_CAMPAIGN.read_text(encoding='utf-8')

    errors: list[str] = []

    try:
        ts_version = require(
            r"PROTOCOL_VERSION\s*=\s*'([^']+)'",
            ts,
            'TypeScript protocol version',
        )
        cs_version = require(
            r'public const string Version\s*=\s*"([^"]+)"',
            cs,
            'C# protocol version',
        )

        if ts_version != cs_version:
            errors.append(
                f'Protocol version mismatch: web={ts_version}, unity={cs_version}'
            )

        if ts_version != EXPECTED_VERSION:
            errors.append(
                f'Expected staged protocol {EXPECTED_VERSION}, got {ts_version}'
            )

        ts_entity = require(
            r"sessionEntityType:\s*'([^']+)'",
            ts,
            'TypeScript session entity type',
        )
        cs_entity = require(
            r'public const string SessionEntityType\s*=\s*"([^"]+)"',
            cs,
            'C# session entity type',
        )

        if ts_entity != cs_entity:
            errors.append(
                f'Entity type mismatch: web={ts_entity}, unity={cs_entity}'
            )

        ts_actions_block = require(
            r'actions:\s*\[(.*?)\]',
            ts,
            'TypeScript actions',
        )
        cs_actions_block = require(
            r'public static readonly string\[\] Actions\s*=\s*\{(.*?)\};',
            cs,
            'C# actions',
        )

        ts_actions = re.findall(r"'([^']+)'", ts_actions_block)
        cs_actions = re.findall(r'"([^"]+)"', cs_actions_block)

        if ts_actions != cs_actions:
            errors.append(
                f'Action mismatch: web={ts_actions}, unity={cs_actions}'
            )

        if ts_actions != EXPECTED_ACTIONS:
            errors.append(
                f'Expected actions={EXPECTED_ACTIONS}, got {ts_actions}'
            )

        limits = {
            'playerName': 'MaxPlayerNameLength',
            'freeformText': 'MaxFreeformTextLength',
            'partySize': 'MaxPartySize',
            'chronicleEntries': 'MaxChronicleEntries',
        }

        for ts_name, cs_name in limits.items():
            ts_value = int(
                require(
                    rf'{ts_name}:\s*(\d+)',
                    ts,
                    f'TypeScript limit {ts_name}',
                )
            )
            cs_value = int(
                require(
                    rf'public const int {cs_name}\s*=\s*(\d+)',
                    cs,
                    f'C# limit {cs_name}',
                )
            )

            if ts_value != cs_value:
                errors.append(
                    f'Limit mismatch {ts_name}: web={ts_value}, unity={cs_value}'
                )

        for field in REQUIRED_SESSION_FIELDS:
            if not re.search(rf"'({re.escape(field)})'", ts):
                errors.append(
                    f'Web protocol missing required session field: {field}'
                )

            if not re.search(
                rf'public\s+(?:int|string|bool|string\[\])\s+{re.escape(field)}\s*;',
                cs,
            ):
                errors.append(
                    f'Unity DTO missing required session field: {field}'
                )

        combatant_block = require(
            r'combatantFields:\s*\[(.*?)\]',
            ts,
            'TypeScript combatant fields',
        )
        ts_combatant_fields = re.findall(r"'([^']+)'", combatant_block)

        for field in REQUIRED_COMBATANT_FIELDS:
            if field not in ts_combatant_fields:
                errors.append(
                    f'Web protocol missing combatant field: {field}'
                )

            if not re.search(
                rf'public\s+int\s+{re.escape(field)}\s*;',
                cs,
            ):
                errors.append(
                    f'Unity CombatantDto missing field: {field}'
                )

            if not re.search(
                rf'\b{re.escape(field)}:\s*number;',
                ts_game,
            ):
                errors.append(
                    f'Web Combatant type missing field: {field}'
                )

        if 'chapters: [1, 2, 3]' not in ts:
            errors.append(
                'Web protocol does not declare three campaign chapters'
            )

        if 'public int[] chapters;' not in cs:
            errors.append(
                'Unity protocol descriptor missing chapters'
            )

        if (
            'campaignStatuses' not in ts
            or 'public string[] campaignStatuses;' not in cs
        ):
            errors.append(
                'Campaign status descriptor is not mirrored across web and Unity'
            )

        if 'public string[] combatantFields;' not in cs:
            errors.append(
                'Unity protocol descriptor missing combatantFields'
            )

        if "'resolvedActionType'" not in ts:
            errors.append(
                'Web protocol descriptor missing resolvedActionType turn field'
            )

        if 'resolvedActionType: action' not in ts_index:
            errors.append(
                'Web turn response does not return resolvedActionType'
            )

        if 'public string resolvedActionType;' not in cs:
            errors.append(
                'Unity TurnEnvelope does not parse resolvedActionType'
            )

        if "'invoke'" not in ts_game:
            errors.append(
                'Web game engine does not declare Invoke'
            )

        if "intent.actionType === 'invoke'" not in ts_game:
            errors.append(
                'Web game engine does not resolve Invoke'
            )

        if 'actor.essence -= 2' not in ts_game:
            errors.append(
                'Invoke does not spend two Crow Essence'
            )

        if 'actor.essence + 1' not in ts_game:
            errors.append(
                'Defend does not restore Crow Essence'
            )

        if "public void Invoke() => Submit("invoke"" not in cs_campaign:
            errors.append(
                'Unity campaign controller does not submit Invoke'
            )

        if "'GET /api/protocol'" not in ts_index:
            errors.append(
                'Web backend does not expose GET /api/protocol'
            )

        if 'withProtocol(' not in ts_index:
            errors.append(
                'Web backend responses are not protocol-tagged'
            )

        if '"/api/protocol"' not in cs_client:
            errors.append(
                'Unity client does not verify /api/protocol'
            )

        if 'ProtocolCompatibility.IsCompatible' not in cs_client:
            errors.append(
                'Unity client does not enforce protocol compatibility'
            )

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
    print(f' combatant_fields={",".join(REQUIRED_COMBATANT_FIELDS)}')
    print(f' campaign_fields={",".join(REQUIRED_SESSION_FIELDS)}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
