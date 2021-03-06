﻿using Dcomms.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dcomms.DRP.Packets
{
    public class PingPacket
    {
        /// <summary>
        /// comes from ConnectionToNeighbor.RemoteNeighborToken32
        /// </summary>
        public NeighborToken32 NeighborToken32;
        public uint PingRequestId32; // is used to avoid mismatch between delayed responses and requests // is used as salt also
        public byte Flags; // is signed by HMAC
        const byte FlagsMask_MustBeZero = 0b11110000;
        public const byte Flags_RegistrationConfirmationSignatureRequested = 0b00000001;
        /// <summary>
        /// initiates "connection teardown" state which lasts 10 seconds before destroying P2P connection, at both sides - "requester, responder"
        /// </summary>
        public const byte Flags_ConnectionTeardown = 0b00000010;
        public const double ConnectionTeardownStateDurationS = 15;
        
        public string VisionName { get; set; } // goes into VisionChannel, for developer, to debug the system, see what happens

        /// <summary>
        /// flags, 1 is set if there is a connected neighbor in specific sector of the 8D regID space 
        /// only 9 LSB bits are used now 
        /// MSB1 (in udp data only) = Requester_AnotherNeighborToSameSectorExists
        /// </summary>
        public ushort RequesterNeighborsBusySectorIds;
        public bool Requester_AnotherNeighborToSameSectorExists;
        public ushort RequesterNeighborsBusySectorIds__AnotherNeighborToSameSectorExists_Combined
        {
            get => (ushort)(RequesterNeighborsBusySectorIds | (Requester_AnotherNeighborToSameSectorExists ? 0x8000 : 0x0000));
            set
            {
                RequesterNeighborsBusySectorIds = (ushort)(value & 0x7FFF);
                Requester_AnotherNeighborToSameSectorExists = (value & 0x8000) != 0;
            }
        }

        public float? MaxRxInviteRateRps;   // zero means NULL // signal from sender "how much I can receive via this p2p connection"
        public float? MaxRxRegisterRateRps; // zero means NULL // signal from sender "how much I can receive via this p2p connection"
        public HMAC NeighborHMAC; // signs fields { DrpDmpPacketTypes.PingRequestPacket,NeighborToken32,Flags,PingRequestId32,RequesterNeighborsBusySectorIds,MaxRxInviteRateRps,MaxRxRegisterRateRps  }, to authenticate the request

        static ushort RpsToUint16(float? rps) // resolution=0.01 RPS    max value=0.65K RPS
        {
            return (ushort)Math.Round(Math.Min(65535, (rps ?? 0) * 100));
        }
        static float? RpsFromUint16(ushort v)
        {
            return v != 0 ? (float?)((float)v * 0.01) : null;
        }
        public void GetSignedFieldsForNeighborHMAC(BinaryWriter writer)
        {
            writer.Write((byte)PacketTypes.Ping);
            NeighborToken32.Encode(writer);
            writer.Write(PingRequestId32);
            writer.Write(Flags);
            BinaryProcedures.EncodeString1ASCII(writer, VisionName);
            writer.Write(RequesterNeighborsBusySectorIds__AnotherNeighborToSameSectorExists_Combined);
            writer.Write(RpsToUint16(MaxRxInviteRateRps));
            writer.Write(RpsToUint16(MaxRxRegisterRateRps));
        }
        public byte[] Encode()
        {
            BinaryProcedures.CreateBinaryWriter(out var ms, out var writer);            
            GetSignedFieldsForNeighborHMAC(writer);
            NeighborHMAC.Encode(writer);
            return ms.ToArray();
        }

        public static ushort DecodeNeighborToken16(byte[] udpData)
        { // first byte is packet type. then 4 bytes are NeighborToken32
            return (ushort)(udpData[1] | (udpData[2] << 8));
        }

        public static PingPacket DecodeAndVerify(byte[] udpData, ConnectionToNeighbor connectedPeerWhoSentTheRequest)
        {
            var reader = BinaryProcedures.CreateBinaryReader(udpData, 1);

            var r = new PingPacket();
            r.NeighborToken32 = NeighborToken32.Decode(reader);
            r.PingRequestId32 = reader.ReadUInt32();
            r.Flags = reader.ReadByte();
            if ((r.Flags & FlagsMask_MustBeZero) != 0) throw new NotImplementedException();
            r.VisionName = BinaryProcedures.DecodeString1ASCII(reader);            
            r.RequesterNeighborsBusySectorIds__AnotherNeighborToSameSectorExists_Combined = reader.ReadUInt16();
            r.MaxRxInviteRateRps = RpsFromUint16(reader.ReadUInt16());
            r.MaxRxRegisterRateRps = RpsFromUint16(reader.ReadUInt16());
            r.NeighborHMAC = HMAC.Decode(reader);
            
            // verify NeighborToken32
            if (!r.NeighborToken32.Equals(connectedPeerWhoSentTheRequest.LocalNeighborToken32))
                throw new BadSignatureException("invalid PING NeighborToken32 23478");

            // verify NeighborHMAC
            if (r.NeighborHMAC.Equals(
                connectedPeerWhoSentTheRequest.GetNeighborHMAC(r.GetSignedFieldsForNeighborHMAC)
                ) == false)
                throw new BadSignatureException("invalid PING NeighborHMAC 4701");

            return r;
        }
    }
    public class PongPacket
    {
        /// <summary>
        /// authenticates sender peer at receiver side
        /// comes from ConnectionToNeighbor.RemoteNeighborToken32
        /// </summary>
        /// </summary>
        public NeighborToken32 NeighborToken32;
        public uint PingRequestId32;  // must match to request
       // byte Flags;
        const byte Flags_ResponderRegistrationConfirmationSignatureExists = 0x01;
        const byte FlagsMask_MustBeZero = 0b11110000;
        /// <summary>
        /// comes from responder neighbor when connection is set up; in other cases it is NULL
        /// signs fields: 
        /// { 
        ///    REQ shared fields,
        ///    ACK1 shared fields,
        ///    ACK2 shared fields,
        ///    ResponderRegistrationConfirmationSignature_MagicNumber
        /// } by responder's reg. private key
        /// is verified by EP, X to update rating of responder neighbor
        /// </summary>
        public RegistrationSignature ResponderRegistrationConfirmationSignature;

        public HMAC NeighborHMAC; // signs { NeighborToken32,PingRequestId32,(optional)ResponderRegistrationConfirmationSignature }

        /// <param name="reader">is positioned after first byte = packet type</param>
        public static PongPacket DecodeAndVerify(ICryptoLibrary cryptoLibrary,
            byte[] udpData, PingPacket optionalPingRequestPacketToCheckRequestId32, 
            ConnectionToNeighbor connectedPeerWhoSentTheResponse, bool requireSignature
            )
        {
            connectedPeerWhoSentTheResponse.AssertIsNotDisposed();
            var reader = BinaryProcedures.CreateBinaryReader(udpData, 1);
            var r = new PongPacket();
            r.NeighborToken32 = NeighborToken32.Decode(reader);
            r.PingRequestId32 = reader.ReadUInt32();
            var flags = reader.ReadByte();
            if ((flags & FlagsMask_MustBeZero) != 0) throw new NotImplementedException();
 
            // verify signature of N
            if ((flags & Flags_ResponderRegistrationConfirmationSignatureExists) != 0)
                r.ResponderRegistrationConfirmationSignature = RegistrationSignature.DecodeAndVerify(reader, cryptoLibrary, 
                    w => connectedPeerWhoSentTheResponse.GetResponderRegistrationConfirmationSignatureFields(w), 
                    connectedPeerWhoSentTheResponse.RemoteRegistrationId);
            else
            {
                if (requireSignature) throw new UnmatchedFieldsException();
            }

            r.NeighborHMAC = HMAC.Decode(reader);

            if (optionalPingRequestPacketToCheckRequestId32 != null)
            {
                // verify PingRequestId32
                if (r.PingRequestId32 != optionalPingRequestPacketToCheckRequestId32.PingRequestId32)
                    throw new UnmatchedFieldsException();
            }

            // verify NeighborToken32
            if (!r.NeighborToken32.Equals(connectedPeerWhoSentTheResponse.LocalNeighborToken32))
                throw new UnmatchedFieldsException();

            // verify NeighborHMAC
            var expectedHMAC = connectedPeerWhoSentTheResponse.GetNeighborHMAC(r.GetSignedFieldsForNeighborHMAC);
            if (r.NeighborHMAC.Equals(expectedHMAC) == false)
            {
                connectedPeerWhoSentTheResponse.Engine.WriteToLog_p2p_detail(connectedPeerWhoSentTheResponse, $"incorrect sender HMAC in ping response: {r.NeighborHMAC}. expected: {expectedHMAC}", null);
                throw new BadSignatureException("invalid PONG NeighborHMAC 23494");
            }
          
            return r;
        }
        public static ushort DecodeNeighborToken16(byte[] udpData)
        { // first byte is packet type. then 4 bytes are NeighborToken32
            return (ushort)(udpData[1] | (udpData[2] << 8));
        }

        public static LowLevelUdpResponseScanner GetScanner(NeighborToken32 senderToken32, uint pingRequestId32)
        {
            BinaryProcedures.CreateBinaryWriter(out var ms, out var w);
            GetHeaderFields(w, senderToken32, pingRequestId32);
            return new LowLevelUdpResponseScanner { ResponseFirstBytes = ms.ToArray() };
        }
        static void GetHeaderFields(BinaryWriter writer, NeighborToken32 senderToken32, uint pingRequestId32)
        {
            writer.Write((byte)PacketTypes.Pong);
            senderToken32.Encode(writer);
            writer.Write(pingRequestId32);
        }

        public void GetSignedFieldsForNeighborHMAC(BinaryWriter writer)
        {
            GetHeaderFields(writer, NeighborToken32, PingRequestId32);
            if (ResponderRegistrationConfirmationSignature != null)
                ResponderRegistrationConfirmationSignature.Encode(writer);
        }
                   
        public byte[] Encode()
        {
            BinaryProcedures.CreateBinaryWriter(out var ms, out var writer);
            GetHeaderFields(writer, NeighborToken32, PingRequestId32);           
            byte flags = 0;
            if (ResponderRegistrationConfirmationSignature != null) flags |= Flags_ResponderRegistrationConfirmationSignatureExists;
            writer.Write(flags);
            if (ResponderRegistrationConfirmationSignature != null) ResponderRegistrationConfirmationSignature.Encode(writer);
            NeighborHMAC.Encode(writer);
            return ms.ToArray();
        }
    }
}
