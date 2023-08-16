using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ParticipantOrderMapping {
    private Dictionary<ulong, ParticipantOrder> _ClientToOrder;
    private Dictionary<ParticipantOrder, ulong> _OrderToClient;

    private void LogErrorNotInit() {
        Debug.LogError("Class not correctly initialized!");
    }

    private void initDicts() {
        _OrderToClient = new Dictionary<ParticipantOrder, ulong>();
        _ClientToOrder = new Dictionary<ulong, ParticipantOrder>();
    }

    public bool AddParticipant(ParticipantOrder or, ulong id) {
        if (_OrderToClient == null) {
            LogErrorNotInit();
            return false;
        }

        if (!_OrderToClient.ContainsKey(or)) {
            _OrderToClient.Add(or, id);
            _ClientToOrder.Add(id, or);

            return true;
        }

        return false;
    }

    public bool RemoveParticipant(ulong id) {
        var outVal = GetOrder(id, out var or);
        if (outVal && _OrderToClient.ContainsKey(or) && _ClientToOrder.ContainsKey(id)) {
            _OrderToClient.Remove(or);
            _ClientToOrder.Remove(id);
        }

        return outVal;
    }


    public bool CheckOrder(ParticipantOrder or) {
        if (_OrderToClient == null) {
            LogErrorNotInit();
            return false;
        }

        return _OrderToClient.ContainsKey(or);
    }

    public bool CheckClientID(ulong id) {
        if (_OrderToClient == null) {
            LogErrorNotInit();
            return false;
        }

        return _ClientToOrder.ContainsKey(id);
    }


    /*
     *
     * Returns Success state if found, out
     */
    public bool GetClientID(ParticipantOrder or, out ulong outVal) {
        if (_OrderToClient == null) LogErrorNotInit();

        if (CheckOrder(or)) {
            outVal = _OrderToClient[or];
            return true;
        }

        outVal = 0;
        return false;
    }

    public bool GetOrder(ulong id, out ParticipantOrder outVal) {
        if (_OrderToClient == null) {
            LogErrorNotInit();
            outVal = ParticipantOrder.None;
            return false;
        }

        if (CheckClientID(id)) {
            outVal = _ClientToOrder[id];
            return true;
        }

        outVal = ParticipantOrder.None;
        return false;
    }

    public int GetParticipantCount() {
        if (_ClientToOrder == null || _OrderToClient == null) {
            LogErrorNotInit();
            return -1;
        }

        if (_ClientToOrder.Count == _OrderToClient.Count) return _ClientToOrder.Count;

        Debug.LogError(
            "Our Participant Connection has become inconsistent. This is bad. Please restart and tell david!");
        return -1;
    }

    public ParticipantOrder[] GetAllConnectedParticipants() {
        if (_ClientToOrder == null || _OrderToClient == null) LogErrorNotInit();

        return _OrderToClient.Keys.ToArray();
    }

    public ulong[] GetAllConnectedClients() {
        if (_ClientToOrder == null || _OrderToClient == null) LogErrorNotInit();

        return _ClientToOrder.Keys.ToArray();
    }
}