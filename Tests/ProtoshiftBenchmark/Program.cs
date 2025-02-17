﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using CommandLine;
using csharp_Protoshift.Configuration;
using csharp_Protoshift.GameSession;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using YYHEggEgg.Logger;

namespace csharp_Protoshift.Enhanced.Benchmark
{
    [Orderer(SummaryOrderPolicy.SlowestToFastest)]
    public class Program
    {
        public const string benchmark_source_file_suffix = "benchmark-source.packet.log";
        public const string benchmark_source_file_shared = $"logs/latest.{benchmark_source_file_suffix}";

        private static void Main(string[] args)
        {
            StartupWorkingDirChanger.ChangeToDotNetRunPath(new LoggerConfig(
                max_Output_Char_Count: 16 * 1024,
                use_Console_Wrapper: false,
                use_Working_Directory: true,
                global_Minimum_LogLevel: LogLevel.Verbose,
                console_Minimum_LogLevel: LogLevel.Information,
                debug_LogWriter_AutoFlush: true
            ));
            if (File.Exists(benchmark_source_file_shared))
            {
                File.Move(benchmark_source_file_shared,
                    $"logs/{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.{benchmark_source_file_suffix}");
            }
            var parser_args = Parser.Default;
            //new Parser(config =>
            // {
                // Set custom ConsoleWriter during construction
            //     config.HelpWriter = TextWriter.Synchronized(new LogTextWriter("CommandLineParser"));
            // });
            BenchmarkOptions global_opt = new BenchmarkOptions();
            parser_args.ParseArguments<BenchmarkOptions>(args)
                .WithNotParsed(errs =>
                {
                    Log.Erro("Unrecognized args detected. Please check your input.");
                    Environment.Exit(0);
                })
                .WithParsed(o => global_opt = o);

            string? sourcefile = global_opt.FilePath;
            if (sourcefile == null)
            {
                Log.Info("Please drag in the latest.packet.log file:");
                sourcefile = Console.ReadLine();
            }
            if (sourcefile == null) throw new Exception("im tired plz give a file ok?");
            SetUpBenchmarkSource(sourcefile, global_opt);

            // Set up and test-run
            var instance = new Program();
            var runs = instance.GetBenchmarkArguments();
            if (runs.Any()) 
                instance.ProtoshiftBenchmark(runs.First());

            BenchmarkRunner.Run<Program>(
                ManualConfig
                .Create(DefaultConfig.Instance)
                .WithSummaryStyle(SummaryStyle.Default
                    .WithMaxParameterColumnWidth(100))
                .WithOption(ConfigOptions.DisableLogFile, true)
                .WithOption(ConfigOptions.JoinSummary, true)
                .WithOption(ConfigOptions.StopOnFirstError, false)
                .WithOption(ConfigOptions.KeepBenchmarkFiles, false)
                .WithArtifactsPath(Path.Combine(Directory.GetCurrentDirectory(), "output_benchmark"))
#if RELEASE
                .AddJob(Job.Default.WithArguments(new[] { new MsBuildArgument($"--property:{global_opt.ExtraMSBuildProperty}") }))
#endif
                );
            Console.ReadLine();
        }

        public static readonly string curdir;
        static Program()
        {
            curdir = Environment.CurrentDirectory;
            while (true)
            {
                if (File.Exists($"{curdir}/ProtoshiftBenchmark.csproj"))
                {
                    break;
                }
                curdir = Directory.GetParent(curdir)?.FullName ?? throw new FileNotFoundException("csproj file not found!");
            }

            Config.InitializeAsync($"{curdir}/../../csharp-Protoshift/config.json").Wait();
            worker = new(1001);
        }

        public const char separateChar = PacketRecord.separateChar;
        public const int PACKET_OVERHEAD = PacketRecord.PACKET_OVERHEAD;

        public class BenchmarkOptions
        {
            [Option("minimum-packet-length", Required = false, Default = 1, HelpText =
                "The minimum packet length that can be included in the benchmark.")]
            public int MinimumPacketLength { get; set; }
            [Option("proto-filters", Required = false, Default = null, HelpText =
                "Give the params to benchmark on only the specified packet protos.")]
            public IEnumerable<string>? ProtoFilters { get; set; }
            [Option("each-proto-packet-limit", Required = false, Default = 1, HelpText =
                "How many packets that will be selected for each proto. " +
                "If running without --single-proto, just leave it default.")]
            public int EachProtoLimit { get; set; }
            [Option("orderby-packet-speed", Required = false, Default = false, HelpText =
                "Order the packets by their actual handle cost time at server runtime. " +
                "If running without --single-proto or the data amount is not huge enough, " +
                "don't enable this because the handle time are often unreliable due to JIT " +
                "cost time and other reasons.")]
            public bool OrderByPacketTime { get; set; }
            [Option("packet-log-file-path", Required = false, Default = null, HelpText =
                "The target packet.log file that contains packet record. " +
                "If don't provide it, the program will prompt you to give it runtime.")]
            public string? FilePath { get; set; }
#if RELEASE
            [Option("extra-property", Required = true, HelpText =
                "The extra property field used when building the program itself. " +
                "It's usually DefineConstants.")]
            public string ExtraMSBuildProperty { get; set; }
#endif
        }

        public static void SetUpBenchmarkSource(string sourcefile, BenchmarkOptions opt)
        {
            List<(string protoname, ushort cmdid, bool sentByClient, byte[] body, int line_id, Int64 handlenanosec)> readres = new();

            var source_lines = File.ReadAllLines(sourcefile);
            for (int i = 0; i < source_lines.Length; i++)
            {
                var line = source_lines[i];
                var values = line.Split(separateChar);
                if (DateTime.TryParse(values[0], out _))
                {
                    string protoname = values[1];
                    ushort cmdid = ushort.Parse(values[2]);
                    bool sentByClient = bool.Parse(values[3]);
                    byte[] data = Convert.FromBase64String(values[5]);
                    readres.Add((protoname, cmdid, sentByClient, data, i, -1));
                }
                else
                {
                    string protoname = values[3];
                    ushort cmdid = ushort.Parse(values[4]);
                    bool sentByClient = bool.Parse(values[5]);
                    byte[] data = Convert.FromBase64String(values[7]);
                    Int64 handlenanosec = Int64.Parse(values[8]);
                    readres.Add((protoname, cmdid, sentByClient, data, i, handlenanosec));
                }
            }

            HashSet<string> proto_filters = new(opt.ProtoFilters ?? Enumerable.Empty<string>());
            IEnumerable<IGrouping<string, (string protoname, ushort cmdid, bool sentByClient, byte[] body, int line_id, Int64 handlenanosec)>> select_res;

            select_res = 
                from record in readres
                orderby (opt.OrderByPacketTime ? record.handlenanosec : record.body.Length) descending
                orderby record.body.Length descending
                group record by record.protoname into gr
                where opt.ProtoFilters == null || proto_filters.Contains(gr.Key)
                select gr;
            StringBuilder sb = new();
            foreach (var proto_gr in select_res)
            {
                int count = opt.EachProtoLimit;
                foreach (var record in proto_gr)
                {
                    if (record.body.Length < opt.MinimumPacketLength) break;
                    if (count-- <= 0) break;
                    sb.AppendLine(source_lines[record.line_id]);
                }
            }

            File.WriteAllText($"{curdir}/{benchmark_source_file_shared}", sb.ToString());
        }

        public IEnumerable<ProtoshiftBenchmarkParamters> GetBenchmarkArguments()
        {
            List<(PacketRecord record, int line)> readres = new();

            var source_lines = File.ReadAllLines($"{curdir}/{benchmark_source_file_shared}");
            for (int i = 0; i < source_lines.Length; i++)
            {
                var line = source_lines[i];
                readres.Add((PacketRecord.Parse(line), i));
            }

            var select_res = from tuple in readres
                             let record = tuple.record
                             orderby record.body_length descending
                             group tuple by record.PacketName into gr
                             select gr;
            foreach (var proto_gr in select_res)
            {
                foreach ((var record, int line) in proto_gr)
                {
                    if (record.body_length == 0) break;
                    yield return new()
                    {
                        record = record,
                        line_packet_log = line
                    };
                }
            }
            yield break;
        }

        public class ProtoshiftBenchmarkParamters
        {
            public PacketRecord record;

            public int line_packet_log;

            public override string ToString()
            {
                return $"{record.PacketName} ({record.body_length} bytes, line: {line_packet_log})";
            }
        }

        public static HandlerSession worker;

        [Benchmark]
        [ArgumentsSource(nameof(GetBenchmarkArguments))]
        public void ProtoshiftBenchmark(ProtoshiftBenchmarkParamters paramters)
        {
            var record = paramters.record;
            worker.GetPacketResult(record.data, (ushort)record.CmdId, record.sentByClient, 
                record.head_offset, record.head_length, record.body_offset, (uint)record.body_length);
        }
    }
}