﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dcomms.DRP.Packets
{
    /// <summary>
    /// A = requester
    /// EP = entry server, proxy peer
    /// is sent from A to EP when A connects to the P2P network
    /// protects system against IP spoofing
    /// </summary>
    class RegisterPow1RequestPacket
    {
        public byte Flags; // will include PoW type
        const byte FlagsMask_MustBeZero = 0b11110000;
        public uint Timestamp32S; // seconds since 2019-01-01 UTC, 32 bits are enough for 136 years

        /// <summary>
        /// default PoW type: 64 bytes
        /// sha512(Timestamp32S|ProofOfWork1|requesterPublicIp) has byte[6]=7 
        /// todo: consider PoW's based on CryptoNight, argon2, bcrypt, scrypt:  slow on GPUs.   the SHA512 is fast on GPUs, that could be used by DDoS attackers
        /// </summary>
        public byte[] ProofOfWork1;
        /// <summary>
        /// must be copied by EP into RegisterPow1ResponsePacket
        /// </summary>
        public uint Pow1RequestId;

        public RegisterPow1RequestPacket()
        {
        }
        public byte[] Encode()
        {
            BinaryProcedures.CreateBinaryWriter(out var ms, out var writer);
            Encode(writer);
            return ms.ToArray();
        }
        void Encode(BinaryWriter writer)
        {
            writer.Write((byte)PacketTypes.RegisterPow1Request);
            writer.Write(Flags);
            writer.Write(Timestamp32S);
            if (ProofOfWork1.Length != 64) throw new ArgumentException();
            writer.Write(ProofOfWork1);
            writer.Write(Pow1RequestId);
        }

        /// <param name="reader">positioned after first byte = packet type</param>
        public RegisterPow1RequestPacket(byte[] originalPacketUdpPayload)
        {
            var reader = BinaryProcedures.CreateBinaryReader(originalPacketUdpPayload, 1);
            Flags = reader.ReadByte();
            if ((Flags & FlagsMask_MustBeZero) != 0) throw new NotImplementedException();
            Timestamp32S = reader.ReadUInt32();
            ProofOfWork1 = reader.ReadBytes(64);
            Pow1RequestId = reader.ReadUInt32();
        }
    }
}
