// <copyright file="DebuggerExpressionLanguageTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AgileObjects.ReadableExpressions;
using Datadog.Trace.Debugger.Configurations.Models;
using Datadog.Trace.Debugger.Expressions;
using Datadog.Trace.Debugger.Models;
using Datadog.Trace.Vendors.Newtonsoft.Json;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace Datadog.Trace.Tests.Debugger
{
    [UsesVerify]
    public class DebuggerExpressionLanguageTests
    {
        private const string DefaultDslTemplate = @"{""dsl"": ""Ignore""}";

        private const string DefaultJsonTemplate = @"{""json"": {""Ignore"": ""Ignore""}}";

        private const string ConditionsFolder = "Conditions";

        private const string TemplatesFolder = "Templates";

        public DebuggerExpressionLanguageTests()
        {
            Test = new TestStruct
            {
                Collection = new List<string> { "hello", "1st Item", "2nd item", "3rd item" },
                IntNumber = 42,
                DoubleNumber = 3.14159,
                String = "Hello world!",
                Nested = new TestStruct.NestedObject() { NestedString = "Hello from nested object" }
            };
        }

        internal TestStruct Test { get; set; }

        public static IEnumerable<object[]> TemplatesResources()
        {
            var sourceFilePath = GetSourceFilePath();
            var path = Path.Combine(sourceFilePath, "..", "ProbeExpressionsResources", TemplatesFolder);
            return Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly).Select(file => new object[] { file });
        }

        public static IEnumerable<object[]> ConditionsResources()
        {
            var sourceFilePath = GetSourceFilePath();
            var path = Path.Combine(sourceFilePath, "..", "ProbeExpressionsResources", ConditionsFolder);
            return Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly).Select(file => new object[] { file });
        }

        public static string GetSourceFilePath([CallerFilePath] string sourceFilePath = null)
        {
            return sourceFilePath ?? throw new InvalidOperationException("Can't obtain source file path");
        }

        [Theory]
        [MemberData(nameof(TemplatesResources))]
        public async Task TestTemplates(string expressionTestFilePath)
        {
            // Arrange
            var evaluator = GetEvaluator(expressionTestFilePath);
            var settings = ConfigureVerifySettings(expressionTestFilePath);

            // Act
            var result = Evaluate(evaluator);

            // Assert
            Assert.NotNull(result.Template);
            Assert.True(result.Condition.HasValue);
            Assert.True(evaluator.CompiledTemplates.Value.Length > 0);
            var toVerify = GetStringToVerify(evaluator, result);
            await Verifier.Verify(toVerify, settings);
        }

        [Theory]
        [MemberData(nameof(ConditionsResources))]
        public async Task TestConditions(string expressionTestFilePath)
        {
            // Arrange
            var evaluator = GetEvaluator(expressionTestFilePath);
            var settings = ConfigureVerifySettings(expressionTestFilePath);

            // Act
            var result = Evaluate(evaluator);

            // Assert
            Assert.NotNull(result.Template);
            Assert.True(result.Condition.HasValue);
            Assert.True(evaluator.CompiledTemplates.Value.Length > 0);
            var toVerify = GetStringToVerify(evaluator, result);
            await Verifier.Verify(toVerify, settings);
        }

        private ProbeExpressionEvaluator GetEvaluator(string expressionTestFilePath)
        {
            var jsonExpression = File.ReadAllText(expressionTestFilePath);
            var dsl = GetDsl(jsonExpression);
            var scopeMembers = CreateScopeMembers();
            DebuggerExpression? condition = null;
            DebuggerExpression[] templates;
            if (new DirectoryInfo(Path.GetDirectoryName(expressionTestFilePath)).Name == ConditionsFolder)
            {
                condition = new DebuggerExpression(dsl, jsonExpression, null);
                templates = new DebuggerExpression[] { new(DefaultDslTemplate, DefaultJsonTemplate, string.Empty) };
            }
            else
            {
                templates = new DebuggerExpression[] { new(dsl, jsonExpression, string.Empty) };
            }

            return new ProbeExpressionEvaluator(templates, condition, null, scopeMembers);
        }

        private VerifySettings ConfigureVerifySettings(string expressionTestFilePath)
        {
            var settings = new VerifySettings();
            settings.UseFileName($"{nameof(DebuggerExpressionLanguageTests)}.{Path.GetFileNameWithoutExtension(expressionTestFilePath)}");
            settings.DisableRequireUniquePrefix();
            VerifierSettings.DerivePathInfo(
                (sourceFile, _, _, _) => new PathInfo(directory: Path.Combine(sourceFile, "..", "ProbeExpressionsResources", "Approvals")));
            return settings;
        }

        private string GetDsl(string expressionJson)
        {
            var reader = new JsonTextReader(new StringReader(expressionJson));

            while (reader.Read())
            {
                if (reader.TokenType != JsonToken.PropertyName)
                {
                    continue;
                }

                if (reader.Value?.ToString() == "dsl")
                {
                    reader.Read();
                    return reader.Value?.ToString();
                }
            }

            throw new InvalidOperationException("DSL part not found in the json file");
        }

        private MethodScopeMembers CreateScopeMembers()
        {
            var scope = new MethodScopeMembers(5, 5);

            // Add locals
            scope.AddMember(new ScopeMember("IntLocal", Test.IntNumber.GetType(), Test.IntNumber, ScopeMemberKind.Local));
            scope.AddMember(new ScopeMember("DoubleLocal", Test.DoubleNumber.GetType(), Test.DoubleNumber, ScopeMemberKind.Local));
            scope.AddMember(new ScopeMember("StringLocal", Test.String.GetType(), Test.String, ScopeMemberKind.Local));
            scope.AddMember(new ScopeMember("CollectionLocal", Test.Collection.GetType(), Test.Collection, ScopeMemberKind.Local));
            scope.AddMember(new ScopeMember("NestedObjectLocal", Test.Nested.GetType(), Test.Nested, ScopeMemberKind.Local));

            // Add arguments
            scope.AddMember(new ScopeMember("IntArg", Test.IntNumber.GetType(), Test.IntNumber, ScopeMemberKind.Argument));
            scope.AddMember(new ScopeMember("DoubleArg", Test.DoubleNumber.GetType(), Test.DoubleNumber, ScopeMemberKind.Argument));
            scope.AddMember(new ScopeMember("StringArg", Test.String.GetType(), Test.String, ScopeMemberKind.Argument));
            scope.AddMember(new ScopeMember("CollectionArg", Test.Collection.GetType(), Test.Collection, ScopeMemberKind.Argument));
            scope.AddMember(new ScopeMember("NestedObjectArg", Test.Nested.GetType(), Test.Nested, ScopeMemberKind.Argument));

            // Add "this" member
            scope.InvocationTarget = new ScopeMember("this", Test.GetType(), Test, ScopeMemberKind.This);

            return scope;
        }

        private (string Template, bool? Condition, List<EvaluationError> Errors) Evaluate(ProbeExpressionEvaluator evaluator)
        {
            var result = evaluator.Evaluate();
            return (result.Template, result.Condition, result.Errors);
        }

        private string GetStringToVerify(ProbeExpressionEvaluator evaluator, (string Template, bool? Condition, List<EvaluationError> Errors) evaluationResult)
        {
            var builder = new StringBuilder();
            if (evaluationResult.Condition.HasValue)
            {
                builder.AppendLine("Condition:");
                builder.AppendLine($"Json:{evaluator.Condition.Value.Json}");
                builder.AppendLine($"Expression: {evaluator.CompiledCondition.Value.Value.ParsedExpression.ToReadableString()}");
                builder.AppendLine($"Result: {evaluationResult.Condition}");
            }

            if (evaluator.Templates.Any(t => t.Dsl != DefaultDslTemplate))
            {
                builder.AppendLine("Template:");
                builder.AppendLine($"Segments: {string.Join(Environment.NewLine, evaluator.Templates.Select(t => t.Json))}");
                builder.AppendLine($"Expressions: {evaluator.CompiledTemplates.Value.Select(t => t.ParsedExpression.ToReadableString())}");
                builder.AppendLine($"Result: {evaluationResult.Template}");
            }

            if (evaluationResult.Errors is { Count: > 0 })
            {
                builder.AppendLine("Errors:");
                builder.AppendLine($"{string.Join(Environment.NewLine, evaluationResult.Errors)}");
            }

            return builder.ToString();
        }

        internal struct TestStruct
        {
            public int IntNumber;

            public List<string> Collection;

            public double DoubleNumber;

            public string String;

            public NestedObject Nested;

            internal class NestedObject
            {
                public string NestedString { get; set; }

                public NestedObject Nested { get; set; }
            }
        }
    }
}
