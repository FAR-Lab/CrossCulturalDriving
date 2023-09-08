using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ParticipantOrderMapping {
    private Dictionary<ulong, ParticipantOrder> _clientToOrder;
    private Dictionary<ParticipantOrder, ulong> _orderToClient;
    private Dictionary<ParticipantOrder, SpawnType> _orderToSpawnType;
    private Dictionary<ParticipantOrder, JoinType> _orderToJoinType;

    private bool initDone = false;

    private void LogErrorNotInit() {
        Debug.LogError("Class not correctly initialized!");
    }

    public ParticipantOrderMapping() {
        _orderToClient = new Dictionary<ParticipantOrder, ulong>();
        _clientToOrder = new Dictionary<ulong, ParticipantOrder>();
        _orderToSpawnType = new Dictionary<ParticipantOrder, SpawnType>();
        _orderToJoinType = new Dictionary<ParticipantOrder, JoinType>();
        initDone = true;
    }

    public bool AddParticipant(ParticipantOrder po, ulong id,SpawnType st, JoinType jt) {
        if (!initDone) {
            LogErrorNotInit();
            return false;
        }

        if (!_orderToClient.ContainsKey(po)) {
            _orderToClient.Add(po, id);
            _clientToOrder.Add(id, po);
            _orderToSpawnType.Add(po, st);
            _orderToJoinType.Add(po, jt);

            return true;
        }

        return false;
    }

    
    public void RemoveParticipant(ParticipantOrder po) {
     GetClientID(po, out var id);
     _RemoveParticipant(id,po);

    }


    private void _RemoveParticipant(ulong id, ParticipantOrder po)
    {

        if (_orderToClient[po] == id && _clientToOrder[id] == po)
        {
           
                _orderToClient.Remove(po);
                _clientToOrder.Remove(id);
                _orderToSpawnType.Remove(po);
                _orderToJoinType.Remove(po);
            
        }
        else
        {
            Debug.LogError("This should never happend the internal tracking of participants desyrnozied Fix this class. write test cases But this should never happen (PARTICIPANTORDERMAPPING.CS)!!!");
        }
        
    }
    
    public void RemoveParticipant(ulong id) {
        var outVal = GetOrder(id, out var po);
        _RemoveParticipant(id,po);
    }


    public bool CheckOrder(ParticipantOrder or) {
        if (!initDone) {
            LogErrorNotInit();
            return false;
        }

        return _orderToClient.ContainsKey(or);
    }

    public bool CheckClientID(ulong id) {
        if (!initDone) {
            LogErrorNotInit();
            return false;
        }

        return _clientToOrder.ContainsKey(id);
    }

    public bool GetSpawnType(ulong clientId,out SpawnType st) {
        if (!initDone ||  GetOrder(clientId, out ParticipantOrder po)) {
            LogErrorNotInit();
            st = SpawnType.NONE;
            return false;
        }

       
        return GetSpawnType(po, out st);
    }
    public bool GetSpawnType(ParticipantOrder or,out SpawnType st) {
        if (!initDone || !_orderToSpawnType.ContainsKey(or)) {
            LogErrorNotInit();
            st = SpawnType.NONE;
            return false;
        }

        st = _orderToSpawnType[or];
        return true;
    }
    public bool GetJoinType(ulong clientId,out JoinType st) {
        if (!initDone ||  GetOrder(clientId, out ParticipantOrder po)) {
            LogErrorNotInit();
            st = JoinType.SCREEN;
            return false;
        }
        return GetJoinType(po, out st);
    }
    public bool GetJoinType(ParticipantOrder or,out JoinType st) {
        if (!initDone || !_orderToSpawnType.ContainsKey(or)) {
            LogErrorNotInit();
            st = JoinType.SCREEN;
            return false;
        }

        st = _orderToJoinType[or];
        return true;
    }

    /*
     *
     * Returns Success state if found, out
     */
    public bool GetClientID(ParticipantOrder or, out ulong outVal) {
        if (!initDone) LogErrorNotInit();

        if (CheckOrder(or)) {
            outVal = _orderToClient[or];
            return true;
        }

        outVal = 0;
        return false;
    }

    public bool GetOrder(ulong id, out ParticipantOrder outVal) {
        if (!initDone) {
            LogErrorNotInit();
            outVal = ParticipantOrder.None;
            return false;
        }

        if (CheckClientID(id)) {
            outVal = _clientToOrder[id];
            return true;
        }

        outVal = ParticipantOrder.None;
        return false;
    }

    public int GetParticipantCount() {
        if (!initDone) {
            LogErrorNotInit();
            return -1;
        }

        if (_clientToOrder.Count == _orderToClient.Count) return _clientToOrder.Count;

        Debug.LogError(
            "Our Participant Connection has become inconsistent. This is bad. Please restart and tell david!");
        return -1;
    }

    public ParticipantOrder[] GetAllConnectedParticipants() {
        if ( !initDone) LogErrorNotInit();

        return _orderToClient.Keys.ToArray();
    }

    public ulong[] GetAllConnectedClients() {
        if ( !initDone) LogErrorNotInit();

        return _clientToOrder.Keys.ToArray();
    }
}