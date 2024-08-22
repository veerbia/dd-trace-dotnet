// <copyright file="Program.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.
// </copyright>

using System;
using System.IO;
using System.Reflection.PortableExecutable;

namespace dd_prof_etw_replay
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                ParseCommandLine(args, out string eventsFilename);
                if (eventsFilename == null)
                {
                    throw new ArgumentException("Missing required argument: -f <.bevents file name>");
                }
                Console.WriteLine($"Processing events in {eventsFilename}");

                using (FileStream fs = new FileStream(eventsFilename, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    var dumper = new EventDumper();
                    var RecordReader = new RecordReader(reader, dumper);
                    while (fs.Position < fs.Length)
                    {
                        ReadRecord(reader);
                    }
                }
            }
            catch (Exception x)
            {
                Console.WriteLine("Replay recorded ETW events");
                Console.WriteLine("  -f <.bevents file name>");
                Console.WriteLine("----------------------------------------------------");
                Console.WriteLine($"Error: {x.Message}");
            }
        }

        private static void ReadRecord(BinaryReader reader)
        {
            
        }


        private static void ParseCommandLine(string[] args, out string eventsFilename)
        {
            eventsFilename = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if ("-f".Equals(arg, StringComparison.OrdinalIgnoreCase))
                {
                    // a filename is expected
                    i++;
                    if (i < args.Length)
                    {
                        eventsFilename = args[i];
                    }
                }
            }
        }
    }
}
