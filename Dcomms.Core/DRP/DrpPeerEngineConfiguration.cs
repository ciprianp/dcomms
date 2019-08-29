﻿using Dcomms.Vision;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Dcomms.DRP
{
    public class DrpPeerEngineConfiguration
    {
        public ushort? LocalPort;
        public IPAddress LocalForcedPublicIpForRegistration;
        public TimeSpan PingRequestsInterval = TimeSpan.FromSeconds(2);
        public double PingRetransmissionInterval_RttRatio = 2.0; // "how much time to wait until sending another ping request?" - coefficient, relative to previously measured RTT
        public TimeSpan ConnectedPeersRemovalTimeout => PingRequestsInterval + TimeSpan.FromSeconds(2);

        public uint RegisterPow1_RecentUniqueDataResetPeriodS = 10 * 60;
        public int RegisterPow1_MaxTimeDifferenceS = 20 * 60;
        public bool RespondToRegisterPow1Errors = false;

        public TimeSpan Pow2RequestStatesTablePeriod = TimeSpan.FromSeconds(5);
        public int Pow2RequestStatesTableMaxSize = 100000;
        public int Timestamp32S_MaxDifferenceToAccept = 20 * 60;

        public TimeSpan PendingRegisterRequestsTimeout = TimeSpan.FromSeconds(20);

        public double UdpLowLevelRequests_ExpirationTimeoutS = 2;
        public double UdpLowLevelRequests_InitialRetransmissionTimeoutS = 0.2;
        public double UdpLowLevelRequests_RetransmissionTimeoutIncrement = 1.5;
        public double RegSynAckRequesterSideTimoutS = 10;

        public double InitialPingRequests_ExpirationTimeoutS = 5;
        public double InitialPingRequests_InitialRetransmissionTimeoutS = 0.1;
        public double InitialPingRequests_RetransmissionTimeoutIncrement = 1.05;
        public TimeSpan ResponderToRetransmittedRequestsTimeout = TimeSpan.FromSeconds(30);

        public VisionChannel VisionChannel;
        public string VisionChannelSourceId;
    }
    public class DrpPeerRegistrationConfiguration
    {
        public IPEndPoint[] EntryPeerEndpoints; // in case when local peer IP = entry peer IP, it is skipped
        public RegistrationPublicKey LocalPeerRegistrationPublicKey;
        public RegistrationPrivateKey LocalPeerRegistrationPrivateKey;
        public int? NumberOfNeighborsToKeep;
    }
}