﻿//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// Params.cs - Copyright (c) 2019 Németh Péter
//-----------------------------------------------------------------------
// MicroCoin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MicroCoin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU General Public License for more details.
//-----------------------------------------------------------------------
// You should have received a copy of the GNU General Public License
// along with MicroCoin. If not, see <http://www.gnu.org/licenses/>.
//-----------------------------------------------------------------------
using MicroCoin.Cryptography;
using MicroCoin.Types;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;

namespace MicroCoin
{
    public static class Params
    {
        public static ushort ServerPort { get; set; } = 4005;
        public static ByteString GenesisPayload { get; set; } = "(c) Peter Nemeth - Okes rendben okes";
        public static uint NetworkPacketMagic { get; internal set; } = 0x0A043580;
        public static ushort NetworkProtocolVersion { get; set; } = 6;
        public static ushort NetworkProtocolAvailable { get; set; } = 6;
        public static ECKeyPair NodeKey { get; set; } = ECKeyPair.CreateNew();
        public static string ProgramVersion
        {
            get
            {
                var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                return assemblyVersion + ".NET DEV";
            }
        }
        public static ICollection<IPEndPoint> FixedSeedServers { get; set; } = new HashSet<IPEndPoint>()
        {
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4004),
            new IPEndPoint(IPAddress.Parse("80.211.211.48"), 4004),
            new IPEndPoint(IPAddress.Parse("94.177.237.196"), 4004),
            new IPEndPoint(IPAddress.Parse("5.189.143.76"), 4004),
            new IPEndPoint(IPAddress.Parse("194.182.64.181"), 4004),
            new IPEndPoint(IPAddress.Parse("80.211.200.121"), 4004)
        };
    }
}
