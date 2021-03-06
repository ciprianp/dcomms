﻿using Dcomms.DRP.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dcomms.DRP
{

    /// <summary>
    /// contains recent RDRs;  RAM-based database
    /// is used to prioritize requests in case of DoS
    /// 50 neighbors, 1 req per second, 1 hour: 180K records: 2.6MB
    /// </summary>
    class RequestDetailsRecordsHistory
    {
        LinkedList<RequestDetailsRecord> Records; // newest first
    }
    class RequestDetailsRecord
    {
        RegistrationId Sender;
        RegistrationId Receiver;
        RegistrationId Requester;
        RegistrationId Responder;
        DateTime RequestCreatedTimeUTC;
        DateTime RequestFinishedTimeUTC;
        ResponseOrFailureCode Status;
    }
    enum RequestType
    {
        invite,
        register
    }

}
