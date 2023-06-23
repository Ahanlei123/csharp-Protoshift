using YYHEggEgg.Logger;
using System.Diagnostics;
using csharp_Protoshift.Enhanced.Handlers.Generator;
using System.Collections.Concurrent;
using System.IO;
using csharp_Protoshift.resLoader;
using System.Collections.ObjectModel;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Protoshift Ex v1");
Log.Initialize(new LoggerConfig(
    max_Output_Char_Count: -1,
    use_Console_Wrapper: false,
    use_Working_Directory: true,
#if DEBUG
    console_Minimum_LogLevel: LogLevel.Debug,
    global_Minimum_LogLevel: LogLevel.Verbose,
#else
    console_Minimum_LogLevel: LogLevel.Information,
    global_Minimum_LogLevel: LogLevel.Information,
#endif
    debug_LogWriter_AutoFlush: true
));

Log.Info("It is recommended to invoke this program with dotnet run.", "HandlerGenerator");
Log.Warn("Build is currently only supported on Windows!", "HandlerGenerator");
Log.Warn("PLEASE USE THIS PROGRAM ALONG WITH FULL SOURCE CODE!", "HandlerGenerator");
string workingdir = Environment.CurrentDirectory;
DirectoryInfo _workingdirinfo = new(workingdir);
Log.Dbug($"Startup Current directory is: {workingdir}.");
bool passcheck = true;
#region Find proto2json
if (_workingdirinfo.Name == "csharp-Protoshift"
    && Directory.Exists($"{workingdir}\\HandlerGenerator\\proto2json"))
{
    workingdir = $"{workingdir}\\HandlerGenerator";
}
else if (_workingdirinfo.Name.StartsWith("net"))
{
    try
    {
        string? dbug = Directory.GetParent(workingdir)?.FullName;
        if (dbug != null)
        {
            workingdir = Directory.GetParent(Directory.GetParent(dbug).FullName).FullName ?? "";
        }
    }
    catch { }
}
#region Change working directory
if (!File.Exists($"{workingdir}\\HandlerGenerator.csproj"))
{
    Log.Erro("Can't find source code path! Make sure you have cloned full code!", "ResourcesCheck");
    Log.Erro("Process terminated for false launch. Exit code is 4206.", "ResourcesCheck");
    Console.ReadLine();
    Environment.Exit(4206);
}
Environment.CurrentDirectory = workingdir;
Log.Dbug($"Changed Current directory to: {workingdir}.");
#endregion
string proto2jsondir = $"{workingdir}\\proto2json";
if (!File.Exists($"{proto2jsondir}\\go-proto2json_win32.exe"))
{
    Log.Erro("Proto2json not found! Please make sure you're running program with dotnet run and have comiled Executable!", "ResourcesCheck");
    passcheck = false;
}
#endregion
#region Check Protos
string newprotodir = $"{Directory.GetParent(workingdir)}\\NewProtoHandlers\\Google.Protobuf\\Protos";
string oldprotodir = $"{Directory.GetParent(workingdir)}\\OldProtoHandlers\\Google.Protobuf\\Protos";
if (!Directory.Exists(newprotodir))
{
    Log.Erro("Can't find NewProtos dir! Make sure to use this programs along with full source code!");
    passcheck = false;
}
if (!Directory.Exists(oldprotodir))
{
    Log.Erro("Can't find OldProtos dir! Make sure to use this programs along with full source code!");
    passcheck = false;
}
#endregion
if (!passcheck)
{
    Log.Erro("Process terminated for protos/proto2json executable lost. Exit code is 272574.", "ResourcesCheck");
    Console.ReadLine();
    Environment.Exit(272574);
}
ResourcesLoader.CheckForRequiredResources();
#region Smart Compiling
Stopwatch rebuildWatcher_watch = Stopwatch.StartNew();
RebuildWatcher rebuildWatcher;
if (File.Exists("last_build_record.json"))
{
    Log.Info($"Reading last build record from jsonfile, it'll take some time...", nameof(RebuildWatcher));
    rebuildWatcher = RebuildWatcher.DeserializeFromJson(await File.ReadAllTextAsync("last_build_record.json"));
}
else
{
    Log.Info($"Capturing build status from directories, it'll take some time...", nameof(RebuildWatcher));
    rebuildWatcher = new();
    rebuildWatcher.CaptureDirectory($"./../OldProtoHandlers/Google.Protobuf");
    rebuildWatcher.CaptureDirectory($"./../NewProtoHandlers/Google.Protobuf");
}
rebuildWatcher_watch.Stop();
Log.Info($"Rebuild judge completed (need rebuild: {rebuildWatcher.NeedRebuild}), costed {rebuildWatcher_watch.Elapsed}.", nameof(RebuildWatcher));
if (rebuildWatcher.NeedRebuild)
{
    #region Compile Protos (protoc)
    ProcessStartInfo oldprotos_compile_protoc = new ProcessStartInfo(OuterInvokeConfig.protoc_path,
        "--proto_path=\"OldProtoHandlers\\Google.Protobuf\\Protos\" " +
        "\"OldProtoHandlers\\Google.Protobuf\\Protos\\*.proto\" " +
        "--csharp_out=\"OldProtoHandlers\\Google.Protobuf\\Compiled\"")
    {
        WorkingDirectory = "./.."
    };
    ProcessStartInfo newprotos_compile_protoc = new ProcessStartInfo(OuterInvokeConfig.protoc_path,
        "--proto_path=\"NewProtoHandlers\\Google.Protobuf\\Protos\" " +
        "\"NewProtoHandlers\\Google.Protobuf\\Protos\\*.proto\" " +
        "--csharp_out=\"NewProtoHandlers\\Google.Protobuf\\Compiled\"")
    {
        WorkingDirectory = "./.."
    };
    Process? protoc_oldprotos = Process.Start(oldprotos_compile_protoc);
    Log.Info($"Compiling OldProtos, please wait...", "OuterInvoke");
    await (protoc_oldprotos?.WaitForExitAsync() ?? Task.CompletedTask);
    Process? protoc_newprotos = Process.Start(newprotos_compile_protoc);
    Log.Info($"Compiling NewProtos, please wait...", "OuterInvoke");
    await (protoc_newprotos?.WaitForExitAsync() ?? Task.CompletedTask);
    if (protoc_newprotos?.ExitCode != 0 || protoc_oldprotos?.ExitCode != 0)
    {
        Log.Erro("Protos compiling (invoke protoc) failed. Exit code is 20041. ", "OuterInvoke");
        Console.ReadLine();
        Environment.Exit(20041);
    }
    #endregion
    #region Compile Protos (C#)
    ProcessStartInfo oldprotos_compile_dotnet = new ProcessStartInfo(OuterInvokeConfig.dotnet_path, "build")
    {
        WorkingDirectory = "./../OldProtoHandlers"
    };
    ProcessStartInfo newprotos_compile_dotnet = new ProcessStartInfo(OuterInvokeConfig.dotnet_path, "build")
    {
        WorkingDirectory = "./../NewProtoHandlers"
    };
    Process? dotnet_oldprotos = Process.Start(oldprotos_compile_dotnet);
    Log.Info($"Compiling OldProtos, please wait...", "OuterInvoke");
    await (dotnet_oldprotos?.WaitForExitAsync() ?? Task.CompletedTask);
    Process? dotnet_newprotos = Process.Start(newprotos_compile_dotnet);
    Log.Info($"Compiling NewProtos, please wait...", "OuterInvoke");
    await (dotnet_newprotos?.WaitForExitAsync() ?? Task.CompletedTask);
    if (dotnet_newprotos?.ExitCode != 0 || dotnet_oldprotos?.ExitCode != 0)
    {
        Log.Erro("Protos compiling (invoke dotnet) failed. Exit code is 60. ", "OuterInvoke");
        Console.ReadLine();
        Environment.Exit(60);
    }
    #endregion
    #region Invoke proto2json
    if (Directory.Exists($"{workingdir}\\Proto2json_Output"))
    {
        Log.Info("Detected old output directory, deleting...");
        Directory.Delete($"{workingdir}\\Proto2json_Output", true);
    }
    Log.Info("Start invoking proto2json.exe. Please wait patiently...", "OuterInvoke");
    Stopwatch pinvokewatch = Stopwatch.StartNew();
    #region Split OS
    // Generated by Cursor
    string proto2json_invokestr = "";
    string os = Environment.OSVersion.Platform.ToString();
    string arch = Environment.Is64BitOperatingSystem ? "64" : "32";
    #region Windows
    if (os.StartsWith("Win"))
    {
        Log.Dbug($"Found OS Type: Windows x{arch}.", "OuterInvoke");
        proto2json_invokestr = $"{proto2jsondir}\\go-proto2json_win{arch}.exe";
    }
    #endregion
    #region macOS
    else if (os.StartsWith("Darwin"))
    {
        Log.Dbug($"Found OS Type: macOS x{arch}.", "OuterInvoke");
        proto2json_invokestr = $"{proto2jsondir}\\go-proto2json_mac{arch}";
    }
    #endregion
    #region Linux
    else if (os.StartsWith("Linux"))
    {
        Log.Dbug($"Found OS Type: Linux x{arch}.", "OuterInvoke");
        proto2json_invokestr = $"{proto2jsondir}\\go-proto2json_linux{arch}";
    }
    #endregion
    else
    {
        Log.Erro("Unsupported OS detected! Please report this to Issues of this project.", "OuterInvoke");
        Log.Erro("Process terminated for unsupported OS. Exit code is 100.", "OuterInvoke");
        Environment.Exit(100);
    }
    #endregion
    Log.Dbug($"Using proto2json at path {proto2jsondir}.");
    ProcessStartInfo startInfo = new ProcessStartInfo(proto2json_invokestr)
    {
        // Not setting this will cause runtime error: invalid memory address or nil pointer dereference
        WorkingDirectory = proto2jsondir
    };
    Process p = Process.Start(startInfo);
    p.WaitForExit();
    pinvokewatch.Stop();
    Log.Info($"proto2json exited. Total execute time is {pinvokewatch.Elapsed}.", "OuterInvoke");
    #region Fatal exit handle
    if (p.ExitCode != 0)
    {
        Log.Erro($"proto2json exited with error code {p.ExitCode}. ", "OuterInvoke");
        Log.Erro("Process terminated for proto2json not working properly. Exit code is 3300.", "OuterInvoke");
        Console.ReadLine();
        Environment.Exit(3300);
    }
    #endregion
    #endregion
}
await File.WriteAllTextAsync("last_build_record.json", rebuildWatcher.SerializeToJson());
string newoutputdir = $"{workingdir}\\Proto2json_Output\\new";
string oldoutputdir = $"{workingdir}\\Proto2json_Output\\old";
if (!Directory.Exists(newoutputdir) || !Directory.Exists(oldoutputdir))
{
    Log.Erro("Process terminated for proto2json output directories not found. Exit code is 245.", "OuterInvoke");
    Console.ReadLine();
    Environment.Exit(245);
}
#endregion
ConcurrentBag<MessageResult> oldmessages = new();
ConcurrentBag<MessageResult> newmessages = new();
ConcurrentBag<EnumResult> oldenums = new();
ConcurrentBag<EnumResult> newenums = new();
#region Analyze Output
#region Enumerate Files
var oldprotojsons = Directory.EnumerateFiles(oldoutputdir);
var newprotojsons = Directory.EnumerateFiles(newoutputdir);
var oldenumCollections = new CompiledEnumsStringPoolCollection();
var newenumCollections = new CompiledEnumsStringPoolCollection();
Parallel.ForEach(oldprotojsons, path =>
{
    oldenumCollections.AddCodeFile($"./../OldProtoHandlers/Google.Protobuf/Compiled/" +
        $"{Path.GetFileNameWithoutExtension(path).Replace("_", "")}.cs");
    ProtoJsonResult analyzeResult = JsonAnalyzer.AnalyzeProtoJson(File.ReadAllText(path));
    foreach (var message in analyzeResult.messageBodys)
    {
        oldmessages.Add(message);
    }
    foreach (var enumResult in analyzeResult.enumBodys)
    {
        oldenums.Add(enumResult);
    }
});
Log.Info($"OldProtos: read {oldmessages.Count} messages, {oldenums.Count} enums.");
Parallel.ForEach(newprotojsons, path =>
{
    newenumCollections.AddCodeFile($"./../NewProtoHandlers/Google.Protobuf/Compiled/" +
        $"{Path.GetFileNameWithoutExtension(path).Replace("_", "")}.cs");
    ProtoJsonResult analyzeResult = JsonAnalyzer.AnalyzeProtoJson(File.ReadAllText(path));
    foreach (var message in analyzeResult.messageBodys)
    {
        newmessages.Add(message);
    }
    foreach (var enumResult in analyzeResult.enumBodys)
    {
        newenums.Add(enumResult);
    }
});
Log.Info($"NewProtos: read {newmessages.Count} messages, {newenums.Count} enums.");
#endregion
CollectionResult<MessageResult> messageResults = 
    CollectionHelper.GetCompareResult(oldmessages, newmessages, MessageResult.NameComparer);
CollectionResult<EnumResult> enumResults = 
    CollectionHelper.GetCompareResult(oldenums, newenums, EnumResult.NameComparer);
#region Generate String Pool
ProtocStringPoolManager compiledStringPool = new();
foreach (var oldProtoMessage in oldmessages) compiledStringPool.PushMessageResult(oldProtoMessage);
foreach (var newProtoMessage in newmessages) compiledStringPool.PushMessageResult(newProtoMessage);
await compiledStringPool.Compile();
#endregion
#region Debug Output (no prod!!!)
// var newProtosOutputNames = newProto_compiledStringPool.GetAllNames();
// Log.Verb($"NewProtos names(verb): {newProtosOutputNames.Count} records.");
// foreach (var record in newProtosOutputNames)
// {
//     Log.Verb($"    [ {record.Key} -> {record.Value} ]; ");
// }
// var oldProtosOutputNames = oldProto_compiledStringPool.GetAllNames();
// Log.Verb($"OldProtos names(verb): {oldProtosOutputNames.Count} records.");
// foreach (var record in oldProtosOutputNames)
// {
//     Log.Verb($"    [ {record.Key} -> {record.Value} ]; ");
// }
#endregion
#region Generate Message Handlers
if (Directory.Exists($"./../ProtoshiftHandlers/Generated"))
{
    Log.Info($"Detected old Protoshift handlers, deleting...");
    Directory.Delete($"./../ProtoshiftHandlers/Generated", true);
}
Directory.CreateDirectory($"./../ProtoshiftHandlers/Generated");
Directory.CreateDirectory($"./../ProtoshiftHandlers/ProtoDispatch");
Log.Info("Start generating Protoshift Handlers, please wait patiently...");
foreach (var shiftpair in messageResults.IntersectItems)
{
    string msgName = shiftpair.LeftItem.messageName;
    string filePath = $"./../ProtoshiftHandlers/Generated/Handler{msgName}.cs";
    BasicCodeWriter fi = new(filePath);
    fi.WriteLine("// <auto-generated>");
    fi.WriteLine("//     Generated by csharp-Protoshift.HandlerGenerator. ");
    fi.WriteLine("// </auto-generated>");
    fi.WriteLine();
    fi.WriteLine("#region Designer Generated Code");
    fi.WriteLine("using Google.Protobuf;");
    fi.WriteLine();
    fi.WriteLine("namespace csharp_Protoshift.Enhanced.Handlers.GeneratedCode");
    fi.EnterCodeRegion();
    HandlerCodeWriter.GenerateMessageHandler(ref fi, msgName, shiftpair.LeftItem, shiftpair.RightItem,
        ref compiledStringPool, ref oldenumCollections, ref newenumCollections);
    fi.ExitCodeRegion();
    fi.WriteLine("#endregion Designer generated code");
    fi.Dispose();
}
foreach (var shiftpair in enumResults.IntersectItems)
{
    string enumName = shiftpair.LeftItem.enumName;
    string filePath = $"./../ProtoshiftHandlers/Generated/Handler{enumName}.cs";
    BasicCodeWriter fi = new(filePath);
    fi.WriteLine("// <auto-generated>");
    fi.WriteLine("//     Generated by csharp-Protoshift.HandlerGenerator. ");
    fi.WriteLine("// </auto-generated>");
    fi.WriteLine();
    fi.WriteLine("#region Designer Generated Code");
    fi.WriteLine("using Google.Protobuf;");
    fi.WriteLine();
    fi.WriteLine("namespace csharp_Protoshift.Enhanced.Handlers.GeneratedCode");
    fi.EnterCodeRegion();
    HandlerCodeWriter.GenerateEnumHandler(ref fi, enumName, shiftpair.LeftItem, shiftpair.RightItem,
        oldenumCollections.Query(enumName), newenumCollections.Query(enumName));
    fi.ExitCodeRegion();
    fi.WriteLine("#endregion Designer generated code");
    fi.Dispose();
}
#endregion
#endregion
Log.Info("Conguratulations! Protoshift handlers generated successfully.");
Log.Info("Now generating CmdId related and ProtoshiftDispatch...");
List<(string messageName, int cmdId)> oldcmdids = new();
List<(string messageName, int cmdId)> newcmdids = new();
#region Generate AskCmdId
#region OldProtos
#region Read oldcmdid.csv
try
{
    using (StreamReader sr = new StreamReader("./resources/protobuf/oldcmdid.csv"))
    {
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            string[] values = line.Split(',');
            if (values.Length == 2 && int.TryParse(values[1], out int cmdId))
            {
                oldcmdids.Add((values[0], cmdId));
            }
        }
    }
}
catch (IOException e)
{
    Log.Erro($"The file could not be read: {e}", "AskCmdIdGenerate");
}
#endregion
using (BasicCodeWriter fi_askCmdId = new("./../OldProtoHandlers/AskCmdId.cs"))
{
    fi_askCmdId.WriteLine("// <auto-generated>");
    fi_askCmdId.WriteLine("//     Generated by csharp-Protoshift.HandlerGenerator. DO NOT EDIT!");
    fi_askCmdId.WriteLine("// </auto-generated>");
    fi_askCmdId.WriteLine();
    fi_askCmdId.WriteLine("#region Designer Generated Code");
    fi_askCmdId.WriteLine();
    fi_askCmdId.WriteLine("namespace OldProtos");
    fi_askCmdId.EnterCodeRegion();
    fi_askCmdId.WriteLine("public static class AskCmdId");
    fi_askCmdId.EnterCodeRegion();
    fi_askCmdId.WriteLine("public static uint GetCmdIdFromProtoname(string protoname)");
    fi_askCmdId.EnterCodeRegion();
    fi_askCmdId.WriteLine("switch (protoname)");
    fi_askCmdId.EnterCodeRegion();
    oldcmdids.Sort((l, r) => l.messageName.CompareTo(r.messageName));
    foreach (var cmdPair in oldcmdids)
    {
        fi_askCmdId.WriteLine($"case \"{cmdPair.messageName}\": return {cmdPair.cmdId};");
    }
    fi_askCmdId.ExitCodeRegion();
    fi_askCmdId.ExitCodeRegion();
    fi_askCmdId.WriteLine("public static string GetProtonameFromCmdId(uint cmdid)");
    fi_askCmdId.EnterCodeRegion();
    fi_askCmdId.WriteLine("switch (cmdid)");
    fi_askCmdId.EnterCodeRegion();
    oldcmdids.Sort((l, r) => l.cmdId.CompareTo(r.cmdId));
    foreach (var cmdPair in oldcmdids)
    {
        fi_askCmdId.WriteLine($"case {cmdPair.cmdId}: return \"{cmdPair.messageName}\";");
    }
    fi_askCmdId.ExitCodeRegion();
    fi_askCmdId.ExitCodeRegion();
    fi_askCmdId.ExitCodeRegion();
    fi_askCmdId.ExitCodeRegion();
    fi_askCmdId.WriteLine("#endregion Designer generated code");
}
#endregion
#region NewProtos
#region Read newcmdid.csv
try
{
    using (StreamReader sr = new StreamReader("./resources/protobuf/newcmdid.csv"))
    {
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            string[] values = line.Split(',');
            if (values.Length == 2 && int.TryParse(values[1], out int cmdId))
            {
                newcmdids.Add((values[0], cmdId));
            }
        }
    }
}
catch (IOException e)
{
    Log.Erro($"The file could not be read: {e}", "AskCmdIdGenerate");
}
#endregion
using (BasicCodeWriter fi_askCmdId = new("./../NewProtoHandlers/AskCmdId.cs"))
{
    fi_askCmdId.WriteLine("// <auto-generated>");
    fi_askCmdId.WriteLine("//     Generated by csharp-Protoshift.HandlerGenerator. DO NOT EDIT!");
    fi_askCmdId.WriteLine("// </auto-generated>");
    fi_askCmdId.WriteLine();
    fi_askCmdId.WriteLine("#region Designer Generated Code");
    fi_askCmdId.WriteLine();
    fi_askCmdId.WriteLine("namespace NewProtos");
    fi_askCmdId.EnterCodeRegion();
    fi_askCmdId.WriteLine("public static class AskCmdId");
    fi_askCmdId.EnterCodeRegion();
    fi_askCmdId.WriteLine("public static uint GetCmdIdFromProtoname(string protoname)");
    fi_askCmdId.EnterCodeRegion();
    fi_askCmdId.WriteLine("switch (protoname)");
    fi_askCmdId.EnterCodeRegion();
    newcmdids.Sort((l, r) => l.messageName.CompareTo(r.messageName));
    foreach (var cmdPair in newcmdids)
    {
        fi_askCmdId.WriteLine($"case \"{cmdPair.messageName}\": return {cmdPair.cmdId};");
    }
    fi_askCmdId.ExitCodeRegion();
    fi_askCmdId.ExitCodeRegion();
    fi_askCmdId.WriteLine("public static string GetProtonameFromCmdId(uint cmdid)");
    fi_askCmdId.EnterCodeRegion();
    fi_askCmdId.WriteLine("switch (cmdid)");
    fi_askCmdId.EnterCodeRegion();
    newcmdids.Sort((l, r) => l.cmdId.CompareTo(r.cmdId));
    foreach (var cmdPair in newcmdids)
    {
        fi_askCmdId.WriteLine($"case {cmdPair.cmdId}: return \"{cmdPair.messageName}\";");
    }
    fi_askCmdId.ExitCodeRegion();
    fi_askCmdId.ExitCodeRegion();
    fi_askCmdId.ExitCodeRegion();
    fi_askCmdId.ExitCodeRegion();
    fi_askCmdId.WriteLine("#endregion Designer generated code");
}
#endregion
#endregion
ReadOnlyCollection<string> supportedMessages = new(new List<string>(
    from pair in messageResults.IntersectItems
    select pair.LeftItem.messageName));
IEnumerable<(string messageName, int oldcmdid, int newcmdid)> supportedCmdIds =
    from old in oldcmdids
    join @new in newcmdids
    on old.messageName equals @new.messageName
    select (old.messageName, old.cmdId, @new.cmdId);
#region Generate ShiftCmdId
string shiftCmdId_filePath = "./../ProtoshiftHandlers/ProtoDispatch/ShiftCmdId.cs";
#region Analyze Past file
Dictionary<int, List<string>> cmd_newshiftold_specialHandles = new();
Dictionary<int, List<string>> cmd_oldshiftnew_specialHandles = new();
if (File.Exists(shiftCmdId_filePath))
{
    var lines = File.ReadAllLines(shiftCmdId_filePath);
    int? cur_cmd = null;
    bool? isInNewShiftToOld = null;
    for (int i = 0; i < lines.Length; i++)
    {
        var line = lines[i].Trim();
        if (isInNewShiftToOld == null)
        {
            if (line.StartsWith("public static uint NewShiftToOld")) isInNewShiftToOld = true;
            continue;
        }
        else if (line.StartsWith("public static uint OldShiftToNew")) isInNewShiftToOld = false;
        if (cur_cmd == null)
        {
            if (line.StartsWith("case ") 
                && line.EndsWith("DON'T MODIFY THIS LINE - request special handle"))
            {
                int startIndex = "case ".Length;
                int endIndex = line.IndexOf(':');
                if (int.TryParse(line.Substring(startIndex, endIndex - startIndex), out int cmd_id))
                {
                    cur_cmd = cmd_id;
                    ((bool)isInNewShiftToOld 
                        ? cmd_newshiftold_specialHandles 
                        : cmd_oldshiftnew_specialHandles).Add((int)cur_cmd, new());
                }
                else
                {
                    Log.Warn("Read past file failure: ShiftCmdId.cs. This file will be overwritten.");
                    cmd_newshiftold_specialHandles.Clear();
                    cmd_oldshiftnew_specialHandles.Clear();
                    break;
                }
            }
        }
        else
        {
            if (line.StartsWith("// DON'T MODIFY THIS LINE - end special handle"))
            {
                cur_cmd = null;
                continue;
            }
            else ((bool)isInNewShiftToOld
                ? cmd_newshiftold_specialHandles
                : cmd_oldshiftnew_specialHandles)[(int)cur_cmd].Add(line);
        }
    }
    string backup_shiftCmdId_path = $"./../ProtoshiftHandlers/ProtoDispatch/Backup/ShiftCmdId-{DateTime.Now:yyyy_MM_dd-HH_mm_ss}.cs";
    File.Move("./../ProtoshiftHandlers/ProtoDispatch/ShiftCmdId.cs", backup_shiftCmdId_path);
    Log.Info($"ShiftCmdId backup successfully created at {backup_shiftCmdId_path}", "ShiftCmdId_Generate");
}
#endregion
Directory.CreateDirectory("./../ProtoshiftHandlers/ProtoDispatch/Backup");
using (BasicCodeWriter fi = new(shiftCmdId_filePath))
{
    var cmdlist_order_new = from tuple in supportedCmdIds
                            group tuple by tuple.newcmdid into gr
                            orderby gr.Key
                            select gr;
    var cmdlist_order_old = from tuple in supportedCmdIds
                            group tuple by tuple.oldcmdid into gr
                            orderby gr.Key
                            select gr;
    fi.WriteLine("// <auto-generated>");
    fi.WriteLine("//     Generated by csharp-Protoshift.HandlerGenerator.");
    fi.WriteLine("// </auto-generated>");
    fi.WriteLine();
    fi.WriteLine("#region Designer Generated Code");
    fi.WriteLine("namespace csharp_Protoshift.Enhanced.Handlers.GeneratedCode");
    fi.EnterCodeRegion();
    fi.WriteLine("public static class ShiftCmdId");
    fi.EnterCodeRegion();
    fi.WriteLine("public static uint NewShiftToOld(uint newcmdid, ReadOnlySpan<byte> head)");
    fi.EnterCodeRegion();
    fi.WriteLine("switch (newcmdid)");
    fi.EnterCodeRegion();
    foreach (var grp in cmdlist_order_new)
    {
        if (grp.Count() == 1)
        {
            #region 1. No conflict
            var tuple = grp.First();
            fi.WriteLine($"case {tuple.newcmdid}: return {tuple.oldcmdid}; // {tuple.messageName}");
            #endregion
        }
        else if (grp.Count() == 0)
        {
            Log.Erro("2. FUCK (this line never shows)", "ShiftCmdId_Generate");
        }
        else
        {
            #region 3. Have conflict, but solved before
            if (cmd_newshiftold_specialHandles.ContainsKey(grp.Key))
            {
                fi.WriteLine($"case {grp.Key}: // DON'T MODIFY THIS LINE - request special handle",
                    cmd_newshiftold_specialHandles[grp.Key]);
                Log.Dbug($"Successfully merged past special handle of Cmd: {grp.Key} to NewShiftToOld.", "ShiftCmdId_Generate");
            }
            #endregion
            #region 4. Have conflict, never solved
            else
            {
                var writing_list = new List<string>
                {
                    "// Q: Why I'm seeing this? ",
                    "// A: There's a cmdid conflict in the provided cmdid.csv file.",
                    "//    More simply, there're two proto sharing the same cmdid.",
                    "//    That may be a mistake, but if you think the file is correct,",
                    "//    that's because the two message have a difference in Channel id.",
                    "// ",
                    "//    e.g. all protos use ENET_CHANNEL_ID = 0, ",
                    "//         but DebugNotify use ENET_CHANNEL_ID = 2. ",
                    "//    In this case, you should write a code handling the packet head, ",
                    "//    or just uncomment the line not specifing 'DebugNotify'. ",
                    "// ",
                    "//    Please REMEMBER COMMENT/DELETE the line throwing exception! ",
                    "//    And don't delete 'request special handle' line, ",
                    "//    or your changes will be overwritten during the next build!"
                };
                foreach (var tuple in grp)
                {
                    writing_list.Add($"// return {tuple.oldcmdid}; // {tuple.messageName}");
                }
                writing_list.Add("throw new NotSupportedException(\"The cmdid conflict haven't been solved. Search for 'request special handle' in ProtoshiftHandlers/ProtoDispatch/ShiftCmdId.cs for more information.\");");
                writing_list.Add("// DON'T MODIFY THIS LINE - end special handle");
                fi.WriteLine($"case {grp.Key}: // DON'T MODIFY THIS LINE - request special handle",
                    writing_list);
                Log.Warn($"ShiftCmdId.NewShiftToOld has cmd_id conflict to be solved (affected newCmd: {grp.Key}). Search for 'request special handle' in ProtoshiftHandlers/ProtoDispatch/ShiftCmdId.cs for more information.");
            }
            #endregion
        }
    }
    fi.WriteLine("default: throw new NotSupportedException(\"The input new CmdId is unknown.\");");
    fi.ExitCodeRegion();
    fi.ExitCodeRegion();
    fi.WriteLine("// DON'T INSERT ANY CODE HERE");
    fi.WriteLine("public static uint OldShiftToNew(uint oldcmdid, ReadOnlySpan<byte> head)");
    fi.EnterCodeRegion();
    fi.WriteLine("switch (oldcmdid)");
    fi.EnterCodeRegion();
    foreach (var grp in cmdlist_order_old)
    {
        if (grp.Count() == 1)
        {
            #region 1. No conflict
            var tuple = grp.First();
            fi.WriteLine($"case {tuple.oldcmdid}: return {tuple.newcmdid}; // {tuple.messageName}");
            #endregion
        }
        else if (grp.Count() == 0)
        {
            Log.Erro("2. FUCK (this line never shows)", "ShiftCmdId_Generate");
        }
        else
        {
            #region 3. Have conflict, but solved before
            if (cmd_oldshiftnew_specialHandles.ContainsKey(grp.Key))
            {
                fi.WriteLine($"case {grp.Key}: // DON'T MODIFY THIS LINE - request special handle",
                    cmd_oldshiftnew_specialHandles[grp.Key]);
                Log.Dbug($"Successfully merged past special handle of Cmd: {grp.Key} to OldShiftToNew.", "ShiftCmdId_Generate");
            }
            #endregion
            #region 4. Have conflict, never solved
            else
            {
                var writing_list = new List<string>
                {
                    "// Q: Why I'm seeing this? ",
                    "// A: There's a cmdid conflict in the provided cmdid.csv file.",
                    "//    More simply, there're two proto sharing the same cmdid.",
                    "//    That may be a mistake, but if you think the file is correct,",
                    "//    that's because the two message have a difference in Channel id.",
                    "// ",
                    "//    e.g. all protos use ENET_CHANNEL_ID = 0, ",
                    "//         but DebugNotify use ENET_CHANNEL_ID = 2. ",
                    "//    In this case, you should write a code handling the packet head, ",
                    "//    or just uncomment the line not specifing 'DebugNotify'. ",
                    "// ",
                    "//    Please REMEMBER COMMENT/DELETE the line throwing exception! ",
                    "//    And don't delete 'request special handle' line, ",
                    "//    or your changes will be overwritten during the next build!"
                };
                foreach (var tuple in grp)
                {
                    writing_list.Add($"// return {tuple.newcmdid}; // {tuple.messageName}");
                }
                writing_list.Add("throw new NotSupportedException(\"The cmdid conflict haven't been solved. Search for 'request special handle' in ProtoshiftHandlers/ProtoDispatch/ShiftCmdId.cs for more information.\");");
                writing_list.Add("// DON'T MODIFY THIS LINE - end special handle");
                fi.WriteLine($"case {grp.Key}: // DON'T MODIFY THIS LINE - request special handle",
                    writing_list);
                Log.Warn($"ShiftCmdId.OldShiftToNew has cmd_id conflict to be solved (affected oldCmd: {grp.Key}). Search for 'request special handle' in ProtoshiftHandlers/ProtoDispatch/ShiftCmdId.cs for more information.", "ShiftCmdId_Generate");
            }
            #endregion
        }
    }
    fi.WriteLine("default: throw new NotSupportedException(\"The input old CmdId is unknown.\");");
    fi.ExitCodeRegion();
    fi.ExitCodeRegion();
    fi.ExitCodeRegion();
    fi.ExitCodeRegion();
    fi.WriteLine("#endregion Designer generated code");
}
#endregion
#region Generate Protoshift Dispatch
#endregion
Console.ReadLine();