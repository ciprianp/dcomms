﻿using Dcomms.Cryptography;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Dcomms.DRP
{
    /// <summary>
    /// "contact point" of local user in the regID space
    /// can be "registered" or "registering"
    /// </summary>
    public partial class LocalDrpPeer: IDisposable
    {
        /// <summary>
        /// is used for:
        /// - PoW1
        /// - EpEndpoint vlidateion at responder
        /// - RequesterEndpoint validation at requester
        /// </summary>
        public IPAddress PublicIpApiProviderResponse;

        readonly DrpPeerRegistrationConfiguration _registrationConfiguration;
        public DrpPeerRegistrationConfiguration RegistrationConfiguration => _registrationConfiguration;
        readonly IDrpRegisteredPeerApp _drpPeerApp;
        internal readonly DrpPeerEngine Engine;
        internal ICryptoLibrary CryptoLibrary => Engine.CryptoLibrary;
        public LocalDrpPeer(DrpPeerEngine engine, DrpPeerRegistrationConfiguration registrationConfiguration, IDrpRegisteredPeerApp drpPeerApp)
        {
            Engine = engine;
            _registrationConfiguration = registrationConfiguration;
            _drpPeerApp = drpPeerApp;
        }
        public List<ConnectionToNeighbor> ConnectedNeighbors = new List<ConnectionToNeighbor>();
       
        public void Dispose() // unregisters
        {

        }
        public override string ToString() => _registrationConfiguration.LocalPeerRegistrationId.ToString();
    }
}
