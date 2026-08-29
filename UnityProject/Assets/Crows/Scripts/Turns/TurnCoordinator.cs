using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace DungeonsCrows.Turns
{
    public enum TurnPhase : byte
    {
        AwaitingPlayers,
        CollectingIntent,
        ResolvingRules,
        ResolvingNarrative,
        Animating,
        CommittingWorld,
        Completed
    }

    /// <summary>
    /// Network-authoritative turn state. The LLM never advances this directly.
    /// Server-side rules and persistence services decide when a phase is complete.
    /// </summary>
    public sealed class TurnCoordinator : NetworkBehaviour
    {
        public NetworkVariable<ulong> TurnNumber = new(1);
        public NetworkVariable<TurnPhase> Phase = new(TurnPhase.AwaitingPlayers);

        private readonly HashSet<ulong> _readyClients = new();

        public event Action<ulong, TurnPhase> TurnStateChanged;

        public override void OnNetworkSpawn()
        {
            TurnNumber.OnValueChanged += (_, value) => RaiseChanged(value, Phase.Value);
            Phase.OnValueChanged += (_, value) => RaiseChanged(TurnNumber.Value, value);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetReadyServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer || Phase.Value != TurnPhase.AwaitingPlayers) return;
            _readyClients.Add(rpcParams.Receive.SenderClientId);

            int connected = NetworkManager.Singleton.ConnectedClientsIds.Count;
            if (connected > 0 && _readyClients.Count >= connected)
            {
                _readyClients.Clear();
                Phase.Value = TurnPhase.CollectingIntent;
            }
        }

        public bool TryAdvanceServer(TurnPhase expected, TurnPhase next)
        {
            if (!IsServer || Phase.Value != expected) return false;
            Phase.Value = next;
            return true;
        }

        public void CommitTurnServer()
        {
            if (!IsServer || Phase.Value != TurnPhase.CommittingWorld) return;
            TurnNumber.Value++;
            Phase.Value = TurnPhase.AwaitingPlayers;
        }

        private void RaiseChanged(ulong turn, TurnPhase phase)
        {
            TurnStateChanged?.Invoke(turn, phase);
            Debug.Log($"[Dungeons&Crows] Turn {turn}: {phase}");
        }
    }
}
