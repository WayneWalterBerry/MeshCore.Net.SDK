// <copyright file="RfPayloadType.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Protocol
{
    /// <summary>
    /// Represents the payload type within an RF log data frame
    /// </summary>
    public enum RfPayloadType : byte
    {
        /// <summary>Request message</summary>
        Request = 0x00,

        /// <summary>Response message</summary>
        Response = 0x01,

        /// <summary>Text message</summary>
        TextMessage = 0x02,

        /// <summary>Acknowledgment</summary>
        Ack = 0x03,

        /// <summary>Advertisement</summary>
        Advert = 0x04,

        /// <summary>Group text/channel message</summary>
        GroupText = 0x05,

        /// <summary>Group data message</summary>
        GroupData = 0x06,

        /// <summary>Anonymous request</summary>
        AnonymousRequest = 0x07,

        /// <summary>Path message</summary>
        Path = 0x08,

        /// <summary>Trace message</summary>
        Trace = 0x09,

        /// <summary>Multipart message</summary>
        Multipart = 0x0A,

        /// <summary>Control message</summary>
        Control = 0x0B
    }
}
