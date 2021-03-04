﻿using System;
using DynamicDiagnosticSourceBindings.Demo;

namespace Demo.Slimple.NetFx45
{
    class Program
    {
        // This demo shows how to use the stub APIs directly, without protecting against dynamic invocation exceptions.
        // The corresponding Net Core demo shows one of several possible ways for dealing with such exceptions.
        // Other demos show other approaches for dealing with these exceptions.

        static void Main(string[] args)
        {
            ConsoleWrite.Line(typeof(Program).FullName);

            UseDiagnosticSourceStub.Run();

            ConsoleWrite.Line("Done. Press enter.");
            Console.ReadLine();
        }
    }
}
