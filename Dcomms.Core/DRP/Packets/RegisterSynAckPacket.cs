﻿using Dcomms.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Dcomms.DRP.Packets
{

    /// <summary>
    /// response to REGISTER SYN request, 
    /// is sent from neighbor/responder/N to M, from M to EP, from EP to requester/A
    /// ответ от N к A идет по тому же пути, узлы помнят обратный путь по RequestId
    /// </summary>
    public class RegisterSynAckPacket
    {
        public static byte Flag_EPtoA = 0x01; // set if packet is transmitted from EP to registering A, otherwise it is zero
        public static byte Flag_ipv6 = 0x02;  // set if responder is accessible via ipv6 address. default (0) means ipv4
        public byte Flags;

        P2pConnectionToken32 SenderToken32; // is not sent from EP to A
        public RegistrationPublicKey RequesterPublicKey_RequestID; // public key of requester (A)
        /// <summary>
        /// against flood by this packet in future, without N (against replay attack)
        /// is copied from REGISTER SYN request packet by N and put into the SYN-ACK response
        /// seconds since 2019-01-01 UTC, 32 bits are enough for 136 years
        /// </summary>
        public uint RegisterSynTimestamp32S;
        public DrpResponderStatusCode ResponderStatusCode;
        public EcdhPublicKey ResponderEcdhePublicKey;
        /// <summary>
        /// not null only for (status=connected) (N->X-M-EP-A)
        /// IP address of N with salt, encrypted for A
        /// 16 bytes for ipv4 address of neighbor, 32 bytes for ipv6
        /// </summary>
        public byte[] ToResponderTxParametersEncrypted;


        public RegistrationPublicKey ResponderPublicKey; // public key of responder (neighbor, N)
        /// <summary>
        /// signs fields:
        /// {
        ///   RequesterPublicKey_RequestID,
        ///   RegisterSynTimestamp32S,
        ///   ResponderStatusCode,
        ///   ResponderEcdhePublicKey,
        ///   ToResponderTxParametersEncrypted,
        ///   ResponderPublicKey 
        /// }
        /// </summary>
        public RegistrationSignature ResponderSignature; 

        HMAC SenderHMAC; // is not sent from EP to A
        
        #region is sent only from EP to A
        
        public IPEndPoint RequesterEndpoint; // public IP:port of A, for UDP hole punching  // not encrypted.  IP address is validated at requester side
       
        #endregion

        public NextHopAckSequenceNumber16 NhaSeq16; // is not sent from EP to A (because response to the SYNACK is ACK, not NHACK) // goes into NHACK packet at peer that responds to this packet
        public byte[] OriginalUdpPayloadData;

        /// <summary>
        /// decodes the packet, decrypts ToNeighborTxParametersEncrypted, verifies NeighborSignature, verifies match to register SYN
        /// </summary>
        /// <param name="newConnectionToNeighborAtRequesterNullable">if not null (at requester) - this procedure verifies ResponderSignature</param>
        /// <param name="reader">is positioned after first byte = packet type</param>    
        public static RegisterSynAckPacket DecodeAndOptionallyVerify(byte[] registerSynAckPacketData, RegisterSynPacket synNullable,
            ConnectionToNeighbor newConnectionToNeighborAtRequesterNullable)
        {
            var reader = PacketProcedures.CreateBinaryReader(registerSynAckPacketData, 1);
            var synAck = new RegisterSynAckPacket();
            synAck.OriginalUdpPayloadData = registerSynAckPacketData;
            synAck.Flags = reader.ReadByte();
            if ((synAck.Flags & Flag_EPtoA) == 0) synAck.SenderToken32 = P2pConnectionToken32.Decode(reader);           
            if ((synAck.Flags & Flag_ipv6) != 0) throw new InvalidOperationException();
            synAck.RequesterPublicKey_RequestID = RegistrationPublicKey.Decode(reader);
            synAck.RegisterSynTimestamp32S = reader.ReadUInt32();
            synAck.ResponderStatusCode = (DrpResponderStatusCode)reader.ReadByte();
            synAck.ResponderEcdhePublicKey = EcdhPublicKey.Decode(reader);
            synAck.ToResponderTxParametersEncrypted = reader.ReadBytes(16);
            synAck.ResponderPublicKey = RegistrationPublicKey.Decode(reader);

            if (newConnectionToNeighborAtRequesterNullable != null)
            {
                synAck.ResponderSignature = RegistrationSignature.DecodeAndVerify(
                    reader, newConnectionToNeighborAtRequesterNullable.Engine.CryptoLibrary,
                    w => synAck.GetCommonRequesterProxierResponderFields(w, false, true),
                    synAck.ResponderPublicKey);
            }
            else
            { // at proxy we don't verify responder's signature, to avoid high spending of resources
                synAck.ResponderSignature = RegistrationSignature.Decode(reader);
            }

            if (synNullable != null)
            {
                synAck.AssertMatchToRegisterSyn(synNullable);
                if (newConnectionToNeighborAtRequesterNullable != null)
                {
                    newConnectionToNeighborAtRequesterNullable.DecryptAtRegisterRequester(synNullable, synAck);
                }
            }
            if ((synAck.Flags & Flag_EPtoA) != 0) synAck.RequesterEndpoint = PacketProcedures.DecodeIPEndPoint(reader);
            if ((synAck.Flags & Flag_EPtoA) == 0)
            {
                synAck.NhaSeq16 = NextHopAckSequenceNumber16.Decode(reader);
                synAck.SenderHMAC = HMAC.Decode(reader);
            }
            return synAck;
        }

        void AssertMatchToRegisterSyn(RegisterSynPacket localRegisterSyn)
        {
            if (localRegisterSyn.RequesterPublicKey_RequestID.Equals(this.RequesterPublicKey_RequestID) == false)
                throw new UnmatchedFieldsException();
            if (localRegisterSyn.Timestamp32S != this.RegisterSynTimestamp32S)
                throw new UnmatchedFieldsException();
        }


        /// <summary>
        /// fields for responder signature and for AEAD hash
        /// </summary>
        public void GetCommonRequesterProxierResponderFields(BinaryWriter writer, bool includeSignature, bool includeTxParameters)
        {
            RequesterPublicKey_RequestID.Encode(writer);
            writer.Write(RegisterSynTimestamp32S);
            writer.Write((byte)ResponderStatusCode);
            ResponderEcdhePublicKey.Encode(writer);
            if (includeTxParameters) writer.Write(ToResponderTxParametersEncrypted);
            ResponderPublicKey.Encode(writer);
            if (includeSignature) ResponderSignature.Encode(writer);
        }

        /// <param name="connectionToNeighbor">is not null for packets between registered peers</param>
        public byte[] EncodeAtResponder(ConnectionToNeighbor connectionToNeighbor)
        {
            PacketProcedures.CreateBinaryWriter(out var ms, out var writer);

            writer.Write((byte)DrpPacketType.RegisterSynAck);
            byte flags = 0;
            if (connectionToNeighbor == null) flags |= Flag_EPtoA;
            writer.Write(flags);
            if (connectionToNeighbor != null)
            {
                SenderToken32 = connectionToNeighbor.RemotePeerToken32;
                SenderToken32.Encode(writer);
            }

            GetCommonRequesterProxierResponderFields(writer, true, true);

            if (connectionToNeighbor != null)
            {
                NhaSeq16.Encode(writer);
                this.SenderHMAC = connectionToNeighbor.GetSharedHMAC(this.GetSignedFieldsForSenderHMAC);
                this.SenderHMAC.Encode(writer);
            }
            else
            {
                PacketProcedures.EncodeIPEndPoint(writer, RequesterEndpoint);
            }

            return ms.ToArray();

        }
        internal void GetSignedFieldsForSenderHMAC(BinaryWriter w)
        {
            SenderToken32.Encode(w);
            GetCommonRequesterProxierResponderFields(w, true, true);
            NhaSeq16.Encode(w);
        }

        /// <summary>
        /// creates a scanner that finds SYNACK that matches to SYN
        /// </summary>
        /// <param name="destinationPeer">
        /// peer that responds to SYN with SYNACK
        /// if not null - the scanner will verify SYNACK.SenderHMAC
        /// </param>
        public static LowLevelUdpResponseScanner GetScanner(RegistrationPublicKey requesterPublicKey_RequestID, uint registerSynTimestamp32S, ConnectionToNeighbor destinationPeer = null)
        {
            PacketProcedures.CreateBinaryWriter(out var ms, out var w);
            w.Write((byte)DrpPacketType.RegisterSynAck);
            w.Write((byte)0);
            if (destinationPeer != null)
            {
                destinationPeer.RemotePeerToken32.Encode(w);
            }

            requesterPublicKey_RequestID.Encode(w);
            w.Write(registerSynTimestamp32S);
            
            var r = new LowLevelUdpResponseScanner
            {
                ResponseFirstBytes = ms.ToArray(),
                IgnoredByteAtOffset1 = 1 // ignore flags
            };
            if (destinationPeer != null)
            {
                r.OptionalFilter = (responseData) =>
                {
                    var synack = DecodeAndOptionallyVerify(responseData, null, null);
                    if (synack.SenderHMAC.Equals(destinationPeer.GetSharedHMAC(synack.GetSignedFieldsForSenderHMAC)) == false) return false;
                    return true;
                };
            }
            return r;
        }

    }
}
