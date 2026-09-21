import { ai } from '@appdeploy/sdk';
import type { LogEntry, TurnResolution } from './game';

export async function narrate(resolution: TurnResolution, recentLog: LogEntry[]): Promise<string> {
  const context =
    'Chapter: ' + resolution.state.chapter +
    '\nCampaign status: ' + resolution.state.campaignStatus +
    '\nResolved mechanics: ' + resolution.deterministicResult +
    '\nRecent chronicle:\n' +
    recentLog.slice(-8).map(entry => entry.kind + ': ' + entry.text).join('\n');
  try {
    const result = await ai.generate({
      system:
        'You are the Dungeon Master for Dungeons & Crows, an original gothic turn-based RPG. Narrate only the already-resolved outcome. Use 2–4 vivid sentences. Never alter hit points, dice results, chapter progression, score, relics, turn order, inventory, ownership, victory, defeat, or any mechanical effect that was not resolved.',
      prompt: context,
      maxTokens: 220,
      temperature: 0.7,
      thinkingMode: 'FAST',
    });
    const text = result.text.trim();
    if (text) return text;
  } catch (err) {
    console.warn('DM narration fallback', err);
  }
  return 'The crows record the result without embellishment: ' + resolution.deterministicResult;
}
