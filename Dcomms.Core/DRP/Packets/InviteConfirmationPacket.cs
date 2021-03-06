﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dcomms.DRP.Packets
{
    class InviteConfirmationPacket
    {
        // byte Flags;
        const byte FlagsMask_MustBeZero = 0b11110000;

        /// <summary>
        /// authorizes peer that sends the packet
        /// </summary>
        public NeighborToken32 NeighborToken32;

        public uint ReqTimestamp32S;
        public RegistrationId RequesterRegistrationId; // A public key 
        public RegistrationId ResponderRegistrationId; // B public key

        public RegistrationSignature ResponderRegistrationSignature;

        public RequestP2pSequenceNumber16 ReqP2pSeq16;

        /// <summary>
        /// authorizes peer that sends the packet
        /// </summary>
        public HMAC NeighborHMAC;

        public byte[] Encode_SetP2pFields(ConnectionToNeighbor transmitToNeighbor)
        {
            BinaryProcedures.CreateBinaryWriter(out var ms, out var w);
            w.Write((byte)PacketTypes.InviteCfm);
            byte flags = 0;
            w.Write(flags);

            NeighborToken32 = transmitToNeighbor.RemoteNeighborToken32;
            NeighborToken32.Encode(w);

            ReqP2pSeq16 = transmitToNeighbor.GetNewRequestP2pSeq16_P2P();

            GetSignedFieldsForNeighborHMAC(w);

            NeighborHMAC = transmitToNeighbor.GetNeighborHMAC(GetSignedFieldsForNeighborHMAC);
            NeighborHMAC.Encode(w);

            return ms.ToArray();
        }

        internal void GetSharedSignedFields(BinaryWriter w)
        {
            w.Write(ReqTimestamp32S);
            RequesterRegistrationId.Encode(w);
            ResponderRegistrationId.Encode(w);
        }
        internal void GetSignedFieldsForNeighborHMAC(BinaryWriter w)
        {
            GetSharedSignedFields(w);
            ResponderRegistrationSignature.Encode(w);
            ReqP2pSeq16.Encode(w);
        }

        internal byte[] DecodedUdpPayloadData;
        public static InviteConfirmationPacket Decode(byte[] udpData)
        {
            var r = new InviteConfirmationPacket();
            r.DecodedUdpPayloadData = udpData;
            var reader = BinaryProcedures.CreateBinaryReader(udpData, 1);
            var flags = reader.ReadByte();
            if ((flags & FlagsMask_MustBeZero) != 0)
                throw new NotImplementedException();

            r.NeighborToken32 = NeighborToken32.Decode(reader);

            r.ReqTimestamp32S = reader.ReadUInt32();
            r.RequesterRegistrationId = RegistrationId.Decode(reader);
            r.ResponderRegistrationId = RegistrationId.Decode(reader);
            r.ResponderRegistrationSignature = RegistrationSignature.Decode(reader);
            r.ReqP2pSeq16 = RequestP2pSequenceNumber16.Decode(reader);

            r.NeighborHMAC = HMAC.Decode(reader);

            return r;
        }



        /// <summary>
        /// creates a scanner that finds CFM that matches to REQ
        /// the scanner will verify CFM.NeighborHMAC
        /// </summary>
        /// <param name="connectionToNeighbor">
        /// peer that responds with CFM
        /// </param>
        public static LowLevelUdpResponseScanner GetScanner(Logger logger, InviteRequestPacket req, ConnectionToNeighbor connectionToNeighbor)
        {
            BinaryProcedures.CreateBinaryWriter(out var ms, out var w);
            w.Write((byte)PacketTypes.InviteCfm);
            w.Write((byte)0); // flags

            connectionToNeighbor.LocalNeighborToken32.Encode(w);

            w.Write(req.ReqTimestamp32S);
            req.RequesterRegistrationId.Encode(w);
            req.ResponderRegistrationId.Encode(w);

            var r = new LowLevelUdpResponseScanner
            {
                ResponseFirstBytes = ms.ToArray(),
                IgnoredByteAtOffset1 = 1 // ignore flags
            };

            r.OptionalFilter = (responseData) =>
            {
                if (connectionToNeighbor.IsDisposed)
                {
                    logger.WriteToLog_needsAttention("ignoring CFM: connection is disposed");
                    return false;
                }
                var cfm = Decode(responseData);
                if (cfm.NeighborHMAC.Equals(connectionToNeighbor.GetNeighborHMAC(cfm.GetSignedFieldsForNeighborHMAC)) == false)
                {
                    logger.WriteToLog_attacks("ignoring CFM: received NeighborHMAC is invalid");
                    return false;
                }
                return true;
            };

            return r;
        }

    }
}
