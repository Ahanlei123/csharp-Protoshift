// Below is human-written, acting as an generated code example.
// 
// ------------------------------------------------------------
//
// <auto-generated>
//     Generated by csharp-Protoshift.HandlerGenerator. 
// </auto-generated>

#nullable enable
#region Designer Generated Code
using csharp_Protoshift.ProtoHotPatch;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using System.Collections.Generic;

namespace csharp_Protoshift.Enhanced.Handlers.GeneratedCode
{
    public class HandlerExampleProto2 
        : HandlerBase<NewProtos.ExampleProto2, OldProtos.ExampleProto2>
    {
        #region Base Type
        MessageParser<NewProtos.ExampleProto2> newproto_parser_base = NewProtos.ExampleProto2.Parser;
        MessageParser<OldProtos.ExampleProto2> oldproto_parser_base = OldProtos.ExampleProto2.Parser;
        #endregion
        #region Import Types
        HandlerExampleInnerEnum handler_ExampleInnerEnum = HandlerExampleInnerEnum.GlobalInstance;
        HandlerExampleInnerProto handler_ExampleInnerProto = HandlerExampleInnerProto.GlobalInstance;
        [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
        public static string[] ImportedHandlers = new string[] {
            "ExampleInnerEnum",
            "ExampleInnerProto",
        };
        #endregion

        #region Protocol shift
        [System.Diagnostics.DebuggerNonUserCode]
        [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
        public override OldProtos.ExampleProto2? NewShiftToOld(NewProtos.ExampleProto2? newprotocol)
        {
            if (newprotocol == null) return null;
            OldProtos.ExampleProto2 oldprotocol = new();
            oldprotocol.ExBytes = newprotocol.ExBytes;
            // foreach (var element_list_str in newprotocol.ListStr)
            // {
            //     oldprotocol.ListStr.Add(element_list_str);
            // }
            oldprotocol.ListStr.AddRange(newprotocol.ListStr);
            return oldprotocol;
        }

        [System.Diagnostics.DebuggerNonUserCode]
        [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
        public override NewProtos.ExampleProto2? OldShiftToNew(OldProtos.ExampleProto2? oldprotocol)
        {
            if (oldprotocol == null) return null;
            NewProtos.ExampleProto2 newprotocol = new();
            newprotocol.ExBytes = oldprotocol.ExBytes;
            // foreach (var element_list_str in oldprotocol.ListStr)
            // {
            //     newprotocol.ListStr.Add(element_list_str);
            // }
            newprotocol.ListStr.AddRange(oldprotocol.ListStr);
            return newprotocol;
        }
        #endregion

        public bool HasSkillIssue = true;

        #region Outer bytes invoke
        [System.Diagnostics.DebuggerNonUserCode]
        [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
        public override byte[] NewShiftToOld(byte[] arr, int offset, int length)
        {
            var rtn = NewShiftToOld(newproto_parser_base.ParseFrom(arr, offset, length));
            return rtn == null ? Array.Empty<byte>() : rtn.ToByteArray();
        }
        [System.Diagnostics.DebuggerNonUserCode]
        [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
        public override byte[] NewShiftToOld(ReadOnlySpan<byte> span)
        {
            var rtn = NewShiftToOld(newproto_parser_base.ParseFrom(span));
            return rtn == null ? Array.Empty<byte>() : rtn.ToByteArray();
        }
        [System.Diagnostics.DebuggerNonUserCode]
        [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
        public override ByteString NewShiftToOld(ByteString bytes)
        {
            var rtn = NewShiftToOld(newproto_parser_base.ParseFrom(bytes));
            return rtn == null ? ByteString.Empty : rtn.ToByteString();
        }
        [System.Diagnostics.DebuggerNonUserCode]
        [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
        public override byte[] OldShiftToNew(byte[] arr, int offset, int length)
        {
            var rtn = OldShiftToNew(oldproto_parser_base.ParseFrom(arr, offset, length));
            return rtn == null ? Array.Empty<byte>() : rtn.ToByteArray();
        }
        [System.Diagnostics.DebuggerNonUserCode]
        [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
        public override byte[] OldShiftToNew(ReadOnlySpan<byte> span)
        {
            var rtn = OldShiftToNew(oldproto_parser_base.ParseFrom(span));
            return rtn == null ? Array.Empty<byte>() : rtn.ToByteArray();
        }
        [System.Diagnostics.DebuggerNonUserCode]
        [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
        public override ByteString OldShiftToNew(ByteString bytes)
        {
            var rtn = OldShiftToNew(oldproto_parser_base.ParseFrom(bytes));
            return rtn == null ? ByteString.Empty : rtn.ToByteString();
        }
        #endregion

        private static HandlerExampleProto2 _globalOnlyInstance = new HandlerExampleProto2();
        [System.Diagnostics.DebuggerNonUserCode]
        [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
        public static HandlerExampleProto2 GlobalInstance => _globalOnlyInstance;

        #region Inner Message
        public class HandlerExampleInnerProto
            : HandlerBase<NewProtos.ExampleProto2.Types.ExampleInnerProto, OldProtos.ExampleProto2.Types.ExampleInnerProto>
        {
            #region Base Type
            MessageParser<NewProtos.ExampleProto2.Types.ExampleInnerProto> newproto_parser_base = NewProtos.ExampleProto2.Types.ExampleInnerProto.Parser;
            MessageParser<OldProtos.ExampleProto2.Types.ExampleInnerProto> oldproto_parser_base = OldProtos.ExampleProto2.Types.ExampleInnerProto.Parser;
            #endregion
            #region Import Types
            [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
            public static string[] ImportedHandlers = new string[] {
            };
            #endregion

            #region Protocol shift
            [System.Diagnostics.DebuggerNonUserCode]
            [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
            public override OldProtos.ExampleProto2.Types.ExampleInnerProto? NewShiftToOld(NewProtos.ExampleProto2.Types.ExampleInnerProto? newprotocol)
            {
                if (newprotocol == null) return null;
                OldProtos.ExampleProto2.Types.ExampleInnerProto oldprotocol = new();
                oldprotocol.InnerCode = newprotocol.InnerCode;
                return oldprotocol;
            }

            [System.Diagnostics.DebuggerNonUserCode]
            [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
            public override NewProtos.ExampleProto2.Types.ExampleInnerProto? OldShiftToNew(OldProtos.ExampleProto2.Types.ExampleInnerProto? oldprotocol)
            {
                if (oldprotocol == null) return null;
                NewProtos.ExampleProto2.Types.ExampleInnerProto newprotocol = new();
                newprotocol.InnerCode = oldprotocol.InnerCode;
                return newprotocol;
            }
            #endregion

            public bool HasSkillIssue = true;

            #region Outer bytes invoke
            [System.Diagnostics.DebuggerNonUserCode]
            [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
            public override byte[] NewShiftToOld(byte[] arr, int offset, int length)
            {
                var rtn = NewShiftToOld(newproto_parser_base.ParseFrom(arr, offset, length));
                return rtn == null ? Array.Empty<byte>() : rtn.ToByteArray();
            }
            [System.Diagnostics.DebuggerNonUserCode]
            [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
            public override byte[] NewShiftToOld(ReadOnlySpan<byte> span)
            {
                var rtn = NewShiftToOld(newproto_parser_base.ParseFrom(span));
                return rtn == null ? Array.Empty<byte>() : rtn.ToByteArray();
            }
            [System.Diagnostics.DebuggerNonUserCode]
            [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
            public override ByteString NewShiftToOld(ByteString bytes)
            {
                var rtn = NewShiftToOld(newproto_parser_base.ParseFrom(bytes));
                return rtn == null ? ByteString.Empty : rtn.ToByteString();
            }
            [System.Diagnostics.DebuggerNonUserCode]
            [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
            public override byte[] OldShiftToNew(byte[] arr, int offset, int length)
            {
                var rtn = OldShiftToNew(oldproto_parser_base.ParseFrom(arr, offset, length));
                return rtn == null ? Array.Empty<byte>() : rtn.ToByteArray();
            }
            [System.Diagnostics.DebuggerNonUserCode]
            [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
            public override byte[] OldShiftToNew(ReadOnlySpan<byte> span)
            {
                var rtn = OldShiftToNew(oldproto_parser_base.ParseFrom(span));
                return rtn == null ? Array.Empty<byte>() : rtn.ToByteArray();
            }
            [System.Diagnostics.DebuggerNonUserCode]
            [System.CodeDom.Compiler.GeneratedCode("YYHEggEgg/csharp_Protoshift.HandlerGenerator", "1.0.0.0")]
            public override ByteString OldShiftToNew(ByteString bytes)
            {
                var rtn = OldShiftToNew(oldproto_parser_base.ParseFrom(bytes));
                return rtn == null ? ByteString.Empty : rtn.ToByteString();
            }
            #endregion

            private static HandlerExampleInnerProto _globalOnlyInstance = new HandlerExampleInnerProto();
            public static HandlerExampleInnerProto GlobalInstance => _globalOnlyInstance;
        }
        #endregion

        #region Inner Enums
        public class HandlerExampleInnerEnum
            : HandlerEnumBase<NewProtos.ExampleProto2.Types.ExampleInnerEnum, OldProtos.ExampleProto2.Types.ExampleInnerEnum>
        {
            public override OldProtos.ExampleProto2.Types.ExampleInnerEnum NewShiftToOld(NewProtos.ExampleProto2.Types.ExampleInnerEnum newprotocol)
            {
                switch (newprotocol)
                {
                    case NewProtos.ExampleProto2.Types.ExampleInnerEnum.None:
                        return OldProtos.ExampleProto2.Types.ExampleInnerEnum.None;
                    case NewProtos.ExampleProto2.Types.ExampleInnerEnum.Someobj:
                        return OldProtos.ExampleProto2.Types.ExampleInnerEnum.Someobj;
                    case NewProtos.ExampleProto2.Types.ExampleInnerEnum.Otherobj:
                        return OldProtos.ExampleProto2.Types.ExampleInnerEnum.Otherobj;
                    default:
                        return OldProtos.ExampleProto2.Types.ExampleInnerEnum.None;
                }
            }

            public override NewProtos.ExampleProto2.Types.ExampleInnerEnum OldShiftToNew(OldProtos.ExampleProto2.Types.ExampleInnerEnum oldprotocol)
            {
                switch (oldprotocol)
                {
                    case OldProtos.ExampleProto2.Types.ExampleInnerEnum.None:
                        return NewProtos.ExampleProto2.Types.ExampleInnerEnum.None;
                    case OldProtos.ExampleProto2.Types.ExampleInnerEnum.Someobj:
                        return NewProtos.ExampleProto2.Types.ExampleInnerEnum.Someobj;
                    case OldProtos.ExampleProto2.Types.ExampleInnerEnum.Otherobj:
                        return NewProtos.ExampleProto2.Types.ExampleInnerEnum.Otherobj;
                    default:
                        return NewProtos.ExampleProto2.Types.ExampleInnerEnum.None;
                }
            }

            private static HandlerExampleInnerEnum _globalOnlyInstance = new HandlerExampleInnerEnum();
            public static HandlerExampleInnerEnum GlobalInstance => _globalOnlyInstance;
        }
        #endregion
    }
}
#endregion Designer generated code