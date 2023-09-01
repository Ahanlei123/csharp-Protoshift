#if !PROXY_ONLY_SERVER

using AssetLib.Utils;
using csharp_Protoshift.Commands.Windy;
using csharp_Protoshift.Configuration;
using csharp_Protoshift.Enhanced.Handlers.GeneratedCode;
using csharp_Protoshift.resLoader;
using csharp_Protoshift.SkillIssue;
using Google.Protobuf;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using YSFreedom.Common.Util;
using YYHEggEgg.Logger;

namespace csharp_Protoshift.GameSession
{
    public class HandlerSession
    {
        protected byte[] _xorKey;
        /// <summary>
        /// XOR key used to decrypt packet. Usually have a length of 4096.
        /// </summary>
        public byte[] XorKey { get => _xorKey; }
        private uint _sessionId;
        public uint SessionId { get => _sessionId; }
        protected uint _uid = 0;
        public uint Uid { get => _uid; }
        public IPEndPoint? remoteIp { get; set; }
        
        public const int Recommended_Protoshift_maximum_time_ms = 5;

        /// <summary>
        /// Whether records contain PingReq/PingRsp packets. Only apply to packets received after modified.
        /// </summary>
        public bool RecordPingPackets { get; set; }
        /// <summary>
        /// Whether output packets in the console.
        /// </summary>
        public bool Verbose { get; set; }
        public int packetCounts { get; protected set; }

        #region Record Packet Protoshift time cost
        public ConcurrentBag<TimeRecord> TimeRecords { get; } = new();

        public struct TimeRecord
        {
            public string protoname { get; set; }
            public bool fromClient { get; set; }
            public long handle_Milliseconds { get; set; }
            public long handle_interval_nanoseconds { get; set; }
            public int packetSize { get; set; }
        }

        private static readonly Int64 nanosecPerTick = (1000L * 1000L * 1000L) / Stopwatch.Frequency;
        private void SubmitTimeRecord(string protoname, bool isNewCmdid, Int64 handleMilliseconds, Int64 handleTicks, int packetSize)
        {
#if !PROTOSHIFT_BENCHMARK
            TimeRecords.Add(new TimeRecord
            {
                protoname = protoname,
                fromClient = isNewCmdid,
                handle_interval_nanoseconds = handleTicks * nanosecPerTick,
                handle_Milliseconds = handleMilliseconds,
                packetSize = packetSize
            });
#endif
        }

        private static readonly ReadOnlyCollection<string>? excludeLogPackets = null;
        private static readonly bool _globalEnableFullPacketLog;
        private Int64 CalcNanosecFromStopwatchTicks(Int64 handleTicks)
        {
            return handleTicks * nanosecPerTick;
        }
        static HandlerSession()
        {
            #region Unordered cmds
            try
            {
                List<ushort> _unordered_cmds_new = new();
                List<ushort> _unordered_cmds_old = new();
                foreach (var protoname in unordered_messages)
                {
                    _unordered_cmds_new.Add((ushort)NewProtos.AskCmdId.GetCmdIdFromProtoname(protoname));
                    _unordered_cmds_old.Add((ushort)OldProtos.AskCmdId.GetCmdIdFromProtoname(protoname));
                }
                unordered_cmds_new = new(_unordered_cmds_new);
                unordered_cmds_old = new(_unordered_cmds_old);
            }
            catch (Exception ex)
            {
                Log.Erro($"HandlerSession cmdid init failed: {ex}", nameof(HandlerSession));
                unordered_cmds_new = new(new List<ushort>());
                unordered_cmds_old = new(new List<ushort>());
            }
            #endregion
            if (Config.Global.PacketLogExcluding != null)
                excludeLogPackets = new(Config.Global.PacketLogExcluding);
            _globalEnableFullPacketLog = Config.Global.EnableFullPacketLog;
        }

        // Generated by ChatGPT, not tested
        // Need EPPlus nuget reference
        public void ExportXlsxRecord(string filePath)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Records");
                worksheet.Cells.LoadFromCollection(TimeRecords, true);
                package.SaveAs(new FileInfo(filePath));
            }
        }
        #endregion

        /// <summary>
        /// Initializer
        /// </summary>
        /// <param name="packetLimits">The maximum count of packets reserved for analyzing and resending. Default is 5000.</param>
        public HandlerSession(uint sessionId)
        {
            _xorKey = Resources.dispatchKey;
            Debug.Assert(_xorKey.Length == 4096);
            // records = new PacketRecord[packetLimits];
            _sessionId = sessionId;
            client_seed = server_seed = Array.Empty<byte>();
            // Verbose = true;
            Verbose = false;
        }

        public byte[] HandlePacket(byte[] packet, bool isNewCmdid)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            bool fallback = false; // Whether use dispatchKey
            XorDecrypt(ref packet, 0, 2);
            // Reference: https://sdl.moe/post/magic-sniffer/#%E5%B7%B2%E7%9F%A5%E6%98%8E%E6%96%87%E6%94%BB%E5%87%BB
            var magic_start = packet.GetUInt16(0);
            if (magic_start != 0x4567)
            {
                XorDecrypt(ref packet, 2, packet.Length - 2);
                Log.Erro("Invalid Magic Start: Bad packet received from " +
                    $"{(isNewCmdid ? "Client" : "Server")}:---{Convert.ToHexString(packet)}",
                    $"PacketHandler({_sessionId})");
                XorDecrypt(ref packet, 0, packet.Length);
                Log.Info("Fall back to dispatchKey", $"PacketHandler({_sessionId})");
                fallback = true;
                XorDecrypt(ref packet, 0, 2, fallback);
                magic_start = packet.GetUInt16(0);
                if (magic_start != 0x4567)
                {
                    throw new InvalidOperationException();
                }
                else
                {
                    Log.Info("Success decoded after XOR Key Fallback, continue", $"PacketHandler({_sessionId})");
                }
            }

            XorDecrypt(ref packet, 2, 2 + 2 + 4, fallback);
            var cmdid = packet.GetUInt16(2);
            var head_length = packet.GetUInt16(4);
            var body_length = packet.GetUInt32(6);
            int head_offset = 2 + 2 + 2 + 4;
            if (body_length > int.MaxValue)
                throw new InvalidOperationException("Are you downloading anime game through KCP? How in teyvat can you get a 2GB packet?");
            XorDecrypt(ref packet, head_offset, head_length + (int)body_length + 2 + 2, fallback); // read magic start of the next if possible
            int body_offset = head_offset + head_length;
            int magic_end_offset = body_offset + (int)body_length;
            var magic_end = packet.GetUInt16(magic_end_offset);
            if (magic_end != 0x89AB)
            {
                Log.Erro("Invalid Magic End: Bad packet received from " +
                    $"{(isNewCmdid ? "Client" : "Server")}:---{Convert.ToHexString(packet)}",
                    $"PacketHandler({_sessionId})");
                throw new InvalidOperationException();
            }

            if (packet.Length >= body_offset + body_length + 2 + 2)
            {
                int next_magic_start = packet.GetUInt16(body_offset + (int)body_length + 2);
                if (next_magic_start == 0x4567)
                    Log.Warn("Multiple packets detected in one request. Program need optimize.",
                        $"PacketHandler({_sessionId})");
            }

            var rtn = GetPacketResult(packet, cmdid, isNewCmdid, 
                head_offset, head_length, body_offset, body_length);
            Debug.Assert(rtn.GetUInt16(rtn.Length - 2) == 0x89AB);

            if (!isNewCmdid && cmdid == OldProtos.AskCmdId.GetCmdIdFromProtoname("GetPlayerTokenRsp"))
                XorDecrypt(ref rtn, fallbackToDispatchKey: true);
            else XorDecrypt(ref rtn, fallbackToDispatchKey: fallback);
            return rtn;
        }

        // use whitelist now
        #region Ordered Packet List
        // this is disabled now
        /*private static ReadOnlyCollection<string> ordered_cmds = new(new List<string>
        {
            "WorldPlayerRTTNotify", "SceneTransToPointReq", "PlayerEnterSceneNotify",
            "EnterSceneReadyReq", "SceneInitFinishReq", "EnterSceneDoneReq",
            "EnterScenePeerNotify", "SceneEntityDisappearNotify", "SceneTransToPointRsp",
            "EnterSceneReadyRsp", "SceneInitFinishRsp", "EnterSceneDoneRsp",
            "GetPlayerTokenReq", "GetPlayerTokenRsp", "PlayerLoginReq", "PlayerLoginRsp",
            "ScenePlayerLocationNotify"
        });*/

        // this is in use
        private static List<string> unordered_messages = new List<string>
        {
            // "GetPlayerTokenReq", "GetPlayerTokenRsp", "PingReq", "PingRsp", 
            "GetActivityInfoRsp", "AchievementUpdateNotify", "PlayerLoginRsp",
            "BattlePassMissionUpdateNotify", "CombatInvocationsNotify", "ActivityInfoNotify",
            "BattlePassMissionUpdateNotify", "PlayerStoreNotify", "AvatarDataNotify",
            "BattlePassAllDataNotify", "AvatarExpeditionDataNotify", 
            // "UnionCmdNotify", "CombatInvocationsNotify", 
            // "AbilityInvocationFailNotify", "AbilityInvocationsNotify",
            // "ClientAbilitiesInitFinishCombineNotify", "ClientAbilityChangeNotify",
            // "ClientAbilityInitFinishNotify"
        };

        private static ReadOnlyCollection<ushort> unordered_cmds_new, unordered_cmds_old;
        #endregion

        public bool OrderedPacket(byte[] packet, bool isNewCmdid)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            bool fallback = false; // Whether use dispatchKey
            XorDecrypt(ref packet, 0, 2);
            // Reference: https://sdl.moe/post/magic-sniffer/#%E5%B7%B2%E7%9F%A5%E6%98%8E%E6%96%87%E6%94%BB%E5%87%BB
            var magic_start = packet.GetUInt16(0);
            if (magic_start != 0x4567)
            {
                XorDecrypt(ref packet, 0, 2); // recover original encrypted
                fallback = true;
                XorDecrypt(ref packet, 0, 2, fallback); // fallback to dispatchKey
                magic_start = packet.GetUInt16(0);
                if (magic_start != 0x4567)
                {
                    XorDecrypt(ref packet, 0, 2, fallback); // recover original encrypted
                    return false;
                }
            }

            XorDecrypt(ref packet, 2, 2, fallback);
            var cmdid = packet.GetUInt16(2);
            XorDecrypt(ref packet, 0, 4, fallback); // recover original packet

            return !(isNewCmdid ? unordered_cmds_new : unordered_cmds_old).Contains(cmdid);
        }

        #region Packet Handle
#if DEBUG || PROTOSHIFT_BENCHMARK
        public byte[] GetPacketResult(byte[] packet, ushort cmdid, bool isNewCmdid,
            int head_offset, int head_length, int body_offset, uint body_length)
#else
        private byte[] GetPacketResult(byte[] packet, ushort cmdid, bool isNewCmdid,
            int head_offset, int head_length, int body_offset, uint body_length)
#endif
        {
#if !PROTOSHIFT_BENCHMARK
            Stopwatch ProtoshiftWatch = new();
            ProtoshiftWatch.Start();
#endif

            byte[] shifted_body;
            string protoname = isNewCmdid
                    ? NewProtos.AskCmdId.GetProtonameFromCmdId(cmdid)
                    : OldProtos.AskCmdId.GetProtonameFromCmdId(cmdid);
            if (Verbose)
            {
                Log.Info($"Received packet {protoname} ({packet.Length} bytes) with CmdId: {cmdid} from" +
                    (isNewCmdid ? "Client." : "Server."), $"PacketHandler({_sessionId})");
            }
            ushort shifted_cmdid = (ushort)(isNewCmdid ? ShiftCmdId.NewShiftToOld(cmdid) : ShiftCmdId.OldShiftToNew(cmdid));

            if (isNewCmdid) shifted_body = ProtoshiftDispatch.NewShiftToOld(cmdid, 
                packet, head_offset, head_length, packet, body_offset, (int)body_length);
            else shifted_body = ProtoshiftDispatch.OldShiftToNew(cmdid, 
                packet, head_offset, head_length, packet, body_offset, (int)body_length);
            if (isNewCmdid && cmdid == NewProtos.AskCmdId.GetCmdIdFromProtoname("GetPlayerTokenReq"))
                GetPlayerTokenReqNotify(shifted_body);
            if (!isNewCmdid && cmdid == OldProtos.AskCmdId.GetCmdIdFromProtoname("GetPlayerTokenRsp"))
                GetPlayerTokenRspNotify(shifted_body);

            #region Push to Skill issue detect
#if !PROTOSHIFT_BENCHMARK
            if (isNewCmdid)
                SkillIssueDetect.StartHandlePacket(protoname,
                    shifted_body, 0, shifted_body.Length,
                    packet, body_offset, (int)body_length);
            else
                SkillIssueDetect.StartHandlePacket(protoname,
                    packet, body_offset, (int)body_length,
                    shifted_body, 0, shifted_body.Length);
#endif
            #endregion

            #region Build New Packet
            int rtnpacketLength = body_offset + shifted_body.Length + 2;
            byte[] rtn = new byte[rtnpacketLength];
            if (body_offset > 0) Array.Copy(packet, 0, rtn, 0, body_offset);
            rtn.SetUInt16(2, shifted_cmdid);
            rtn.SetUInt32(2 + 2 + 2, (uint)shifted_body.Length);
            Array.Copy(shifted_body, 0, rtn, body_offset, shifted_body.Length);
            rtn.SetUInt16(rtnpacketLength - 2, 0x89AB);
            #endregion

#if !PROTOSHIFT_BENCHMARK
            ProtoshiftWatch.Stop();
            if (ProtoshiftWatch.ElapsedMilliseconds >= Recommended_Protoshift_maximum_time_ms && !unordered_cmds_old.Contains(cmdid))
            {
                Log.Info($"Handling packet: {protoname} ({packet.Length} bytes) exceeded ordered packet required time ({ProtoshiftWatch.ElapsedMilliseconds}ms > {Recommended_Protoshift_maximum_time_ms}ms)", $"PacketHandler({_sessionId})");
            }
            SubmitTimeRecord(protoname, isNewCmdid, ProtoshiftWatch.ElapsedMilliseconds, ProtoshiftWatch.ElapsedTicks, packet.Length);
            if (_globalEnableFullPacketLog && excludeLogPackets?.Contains(protoname) != true)
            {
                Debug.Assert(GameSessionDispatch.PacketLogger != null);
                GameSessionDispatch.PacketLogger.Info(() =>
                    new PacketRecord(Uid, protoname, cmdid, isNewCmdid,
                    packet, head_offset, head_length, body_offset, (int)body_length,
                    CalcNanosecFromStopwatchTicks(ProtoshiftWatch.ElapsedTicks),
                    shifted_body, DateTime.MinValue).ToString(), Uid.ToString());
            }
#endif
            return rtn;
        }

        protected byte[] client_seed;
        protected byte[] server_seed;

        private void GetPlayerTokenReqNotify(byte[] message_oldprotocol)
        {
            var message = OldProtos.GetPlayerTokenReq.Parser.ParseFrom(message_oldprotocol);
            uint key_id = message.KeyId;
            try
            {
                client_seed = Resources.SPri[key_id].RsaDecrypt(
                    Convert.FromBase64String(message.ClientRandKey),
                    RSAEncryptionPadding.Pkcs1)
                    .Fill0(8);
            }
            catch
            {
                NewProtos.GetPlayerTokenRsp rsaFatalRsp = new();
                rsaFatalRsp.Retcode = 42;
                rsaFatalRsp.Msg = "Crypto failure. Please confirm that your program is the right version.";
                GameSessionDispatch.InjectPacketToClient(_sessionId, 
                    nameof(OldProtos.GetPlayerTokenRsp), null, rsaFatalRsp.ToByteArray());
                Program.ProxyServer.KickSession(_sessionId, client_reason: 5);
                return;
            }
            _ = Task.Run(async () =>
            {
                try
                {
                    await GameSessionDispatch.InjectOnlineExecuteWindy(_sessionId);
                    Log.Info($"Successfully sent windy lua: " +
                        Path.GetFileNameWithoutExtension(Config.Global.WindyConfig.OnlineExecWindyLua) +
                        $" to session id: {_sessionId}, IP: {remoteIp}.", "windyOnGetPlayerTokenReq_AsyncTask");
                }
                catch (Exception ex)
                {
                    Log.Warn($"Windy auto-execute failed: {ex}", "windyOnGetPlayerTokenReq_AsyncTask");
                }
            });
        }

        private void GetPlayerTokenRspNotify(byte[] message_newprotocol)
        {
            var message = NewProtos.GetPlayerTokenRsp.Parser.ParseFrom(message_newprotocol);
            uint key_id = message.KeyId;
            server_seed = Resources.CPri[key_id].RsaDecrypt(
                Convert.FromBase64String(message.ServerRandKey),
                RSAEncryptionPadding.Pkcs1)
                .Fill0(8);
            ulong encrypt_seed = server_seed.GetUInt64(0) ^ client_seed.GetUInt64(0);
            _xorKey = Generate4096KeyByMT19937(encrypt_seed);
            _uid = message.Uid;
            Log.Info($"IMPORTANT: New XOR Key built{Environment.NewLine}" +
                $"-----BEGIN HEX New 4096 XOR Key-----{Environment.NewLine}" +
                Convert.ToHexString(_xorKey) +
                $"{Environment.NewLine}-----END HEX New 4096 XOR Key-----", $"HandlerSession({_sessionId})");
            if (GameSessionDispatch.OnlineExecWindyMode == OnlineExecWindyMode_v1_0_0.OnGetPlayerTokenFinish)
            {
                _ = Task.Run(async () =>
                {
                    // GetPlayerTokenRsp MUST BE earlier than WindSeedClientNotify
                    await Task.Delay(1500);
                    try
                    {
                        await GameSessionDispatch.InjectOnlineExecuteWindy(_sessionId);
                        Log.Info($"Successfully sent windy lua: " +
                            Path.GetFileNameWithoutExtension(Config.Global.WindyConfig.OnlineExecWindyLua) +
                            $" to session id: {_sessionId}, IP: {remoteIp}.", "windyOnGetPlayerTokenFinish_AsyncTask");
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"Windy auto-execute failed: {ex}", "windyOnGetPlayerTokenFinish_AsyncTask");
                    }
                });
            }
        }
        #endregion

        #region Packet Create
        public byte[] ConstructPacket(bool isNewCmdid, string protoname, byte[]? packetHead, byte[] packetBody)
        {
            var cmdid = isNewCmdid 
                ? NewProtos.AskCmdId.GetCmdIdFromProtoname(protoname)
                : OldProtos.AskCmdId.GetCmdIdFromProtoname(protoname);
            var head_length = packetHead?.Length ?? 0;
            var body_length = packetBody.Length;
            int head_offset = 2 + 2 + 2 + 4;
            int body_offset = head_offset + head_length;
            int magic_end_offset = body_offset + body_length;
            int rtnpacketLength = magic_end_offset + 2;

            byte[] rtn = new byte[rtnpacketLength];
            rtn.SetUInt16(0, 0x4567);
            rtn.SetUInt16(2, (ushort)cmdid);
            rtn.SetUInt16(4, (ushort)head_length);
            rtn.SetUInt32(6, (uint)body_length);

            if (packetHead != null) Buffer.BlockCopy(packetHead, 0, rtn, head_offset, packetHead.Length);
            Buffer.BlockCopy(packetBody, 0, rtn, body_offset, body_length);
            rtn.SetUInt16(rtnpacketLength - 2, 0x89AB);

            if (Verbose)
            {
                Log.Info($"Create packet {protoname} with " +
                    $"CmdId:{NewProtos.AskCmdId.GetCmdIdFromProtoname(protoname)} " +
                    $"for {(isNewCmdid ? "Client" : "Server")}:---{Convert.ToHexString(rtn)}",
                $"PacketConstructor({_sessionId})");
            }

            XorDecrypt(ref rtn);
            return rtn;
        }
        #endregion

        #region Crypto
        private void XorDecrypt(ref byte[] encrypted, int offset = 0, int length = -1, bool fallbackToDispatchKey = false)
        {
            if (length == -1) length = encrypted.Length;
            else length = Math.Min(length, encrypted.Length - offset);
            for (int i = offset; i < offset + length; i++)
            {
                if (!fallbackToDispatchKey) encrypted[i] = (byte)(encrypted[i] ^ _xorKey[i % _xorKey.Length]);
                else encrypted[i] = (byte)(encrypted[i] ^ Resources.dispatchKey[i % Resources.dispatchKey.Length]);
            }
        }

        static readonly JsonSerializer serializer = new();

        public static string ConvertJsonString(string str)
        {
            //格式化json字符串
            TextReader tr = new StringReader(str);
            JsonTextReader jtr = new(tr);
            object? obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                StringWriter textWriter = new();
                JsonTextWriter jsonWriter = new(textWriter)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                serializer.Serialize(jsonWriter, obj);
                return textWriter.ToString();
            }
            else
            {
                return str;
            }
        }

        public static byte[] Generate4096KeyByMT19937(ulong seed)
        {
            MT19937_64 mt1 = new(seed);
            ulong mt2seed = mt1.Int64();
            MT19937_64 mt2 = new(mt2seed);
            mt2.Int64();
            byte[] key = new byte[4096];
            for (int i = 0; i < key.Length; i += 8)
            {
                ulong newui64 = mt2.Int64();
                key.SetUInt64(i, newui64);
            }
            return key;
        }
        #endregion
    }
}
#endif