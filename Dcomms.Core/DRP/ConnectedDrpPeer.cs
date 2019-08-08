﻿using Dcomms.Cryptography;
using Dcomms.DRP.Packets;
using Dcomms.DSP;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dcomms.DRP
{
    public enum ConnectedDrpPeerInitiatedBy
    {
        localPeer, // local peer connected to remote peer via REGISTER procedure
        remotePeer // remote peer connected to local peer via REGISTER procedure
    }
    public class ConnectedDrpPeer
    {
        public ConnectedDrpPeerInitiatedBy InitiatedBy;
        public P2pStreamParameters TxParameters;
        public RegistrationPublicKey RemotePeerPublicKey;
        public P2pConnectionToken32 LocalRxToken32; // is generated by local peer


        ConnectedDrpPeerRating Rating;

        IirFilterCounter RxInviteRateRps;
        IirFilterCounter RxRegisterRateRps;

        float MaxTxInviteRateRps, MaxTxRegiserRaateRps; // sent by remote peer via ping
        IirFilterCounter TxInviteRateRps, TxRegisterRateRps;
        List<TxRegisterRequestState> PendingTxRegisterRequests;
        List<TxInviteRequestState> PendingTxInviteRequests;

        public ConnectedDrpPeer(ConnectedDrpPeerInitiatedBy initiatedBy)
        {
            InitiatedBy = initiatedBy;
        }
        public PingRequestPacket GetPingRequestPacket(ICryptoLibrary cryptoLibrary)
        {
            var r = new PingRequestPacket
            {
                SenderToken32 = TxParameters.RemotePeerToken32,
                MaxRxInviteRateRps = 10,
                MaxRxRegisterRateRps = 10,
                Timestamp = x,
            };
            r.SenderHMAC = txParameters.GetLocalSenderHmac(_cryptoLibrary, );
            return r;
        }
    }
    class ConnectedDrpPeerRating
    {
        IirFilterAverage PingRttMs;
        TimeSpan Age => throw new NotImplementedException();
        float RecentRegisterRequestsSuccessRate => throw new NotImplementedException(); // target of sybil-looped-traffic attack
        float RecentInviteRequestsSuccessRate => throw new NotImplementedException(); // target of sybil-looped-traffic attack
    }
}
