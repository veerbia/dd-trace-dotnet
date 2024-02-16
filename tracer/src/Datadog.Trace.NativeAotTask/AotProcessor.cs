// <copyright file="AotProcessor.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Datadog.Trace.NativeAotTask;

#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class AotProcessor
{
    private readonly IReadOnlyList<AssemblyDefinition> _assemblies;
    private readonly AssemblyDefinition _datadogAssembly;
    private readonly TypeDefinition _duckCopyInterface;
    private readonly AssemblyDefinition _entryPointAssembly;

    private AotProcessor(IReadOnlyList<string> assemblyPaths, Action<string> progress)
    {
        Log = progress;
        _assemblies = assemblyPaths.Select(ReadAssembly).Where(a => a != null).ToList()!;
        _datadogAssembly = _assemblies.First(a => Path.GetFileName(a.MainModule.FileName) == "Datadog.Trace.dll");
        _entryPointAssembly = _assemblies.First();

        _duckCopyInterface = _datadogAssembly.MainModule.GetType("Datadog.Trace.DuckTyping.IDuckCopy`1");

        if (_duckCopyInterface == null)
        {
            throw new InvalidOperationException("IDuckCopy`1 not found in Datadog.Trace.dll");
        }
    }

    private Action<string> Log { get; }

    private static AssemblyDefinition? ReadAssembly(string path)
    {
        try
        {
            var readerParameters = new ReaderParameters
            {
                ReadingMode = ReadingMode.Immediate,
                ReadWrite = true,
                InMemory = true
            };

            var assembly = AssemblyDefinition.ReadAssembly(path, readerParameters);

            assembly.MainModule.Attributes |= ModuleAttributes.ILOnly;

            return assembly;
        }
        catch
        {
            return null;
        }
    }

    public static void Invoke(IReadOnlyList<string> assemblyPaths, Action<string> progress)
    {
        var aotProcessor = new AotProcessor(assemblyPaths, progress);

        aotProcessor.Invoke();
    }

    internal void Invoke()
    {
        _entryPointAssembly.MainModule.AssemblyReferences.Add(_datadogAssembly.Name);

        PatchEntryPoint();
        GenerateDuckTypes();
        // GenerateEntryPointReverseDuckTypes();
        InstrumentCallTargetMethods();

        Log("Writing assemblies");

        foreach (var assembly in _assemblies)
        {
            Log($"Writing {Path.GetFullPath(assembly.MainModule.FileName)}");

            try
            {
                assembly.Write(Path.GetFullPath(assembly.MainModule.FileName));
            }
            catch (IOException e)
            {
                Log($"Error writing {Path.GetFileName(assembly.MainModule.FileName)}: {e.Message}");
            }

            assembly.Dispose();
        }
    }

    private bool IsDuckType(TypeDefinition type)
    {
        return type.CustomAttributes
            .Where(attribute => attribute.ConstructorArguments.Count == 2)
            .Any(attribute => attribute.AttributeType.Name == "DuckTypeAttribute" || attribute.AttributeType.Name == "DuckCopyAttribute");
    }

    private IEnumerable<TypeDefinition> GetDuckChains(TypeDefinition type)
    {
        foreach (var attribute in type.CustomAttributes)
        {
            if (attribute.ConstructorArguments.Count != 2)
            {
                continue;
            }

            if (attribute.AttributeType.Name == "DuckTypeAttribute" || attribute.AttributeType.Name == "DuckCopyAttribute")
            {
                foreach (var method in type.Methods)
                {
                    var returnType = method.ReturnType.Resolve();

                    if (IsDuckType(returnType))
                    {
                        yield return returnType;
                    }

                    foreach (var parameter in method.Parameters)
                    {
                        var parameterType = parameter.ParameterType.Resolve();

                        if (IsDuckType(parameterType))
                        {
                            yield return parameterType;
                        }
                    }
                }

                foreach (var property in type.Properties)
                {
                    var propertyType = property.PropertyType.Resolve();

                    if (IsDuckType(propertyType))
                    {
                        yield return propertyType;
                    }
                }

                foreach (var field in type.Fields)
                {
                    var fieldType = field.FieldType.Resolve();

                    if (IsDuckType(fieldType))
                    {
                        yield return fieldType;
                    }
                }
            }
        }
    }

    private void ImplementIDuckType(TypeDefinition duckTypeInterface, TypeDefinition targetType)
    {
        var targetModule = targetType.Module;

        var interfaces = new Queue<TypeDefinition>();
        var processedInterfaces = new HashSet<TypeDefinition>();

        interfaces.Enqueue(duckTypeInterface);

        while (interfaces.Count > 0)
        {
            var currentInterface = interfaces.Dequeue();

            if (!processedInterfaces.Add(currentInterface))
            {
                continue;
            }

            foreach (var parentInterface in currentInterface.Interfaces)
            {
                interfaces.Enqueue(parentInterface.InterfaceType.Resolve());
            }

            Log($"Injecting {currentInterface.Name} into target {targetType.Name} in {targetModule.Assembly.Name}");

            currentInterface.IsPublic = true;

            var currentInterfaceReference = targetModule.ImportReference(currentInterface);

            targetType.Interfaces.Add(new InterfaceImplementation(currentInterfaceReference));

            foreach (var method in currentInterface.Methods)
            {
                if (method.IsSpecialName)
                {
                    continue;
                }

                var newMethod = new MethodDefinition($"{currentInterface.FullName}.{method.Name}", default, targetModule.ImportReference(method.ReturnType))
                {
                    IsNewSlot = true,
                    IsVirtual = true,
                    IsSpecialName = true,
                    IsFinal = true,
                    IsPrivate = true,
                    IsHideBySig = true
                };

                newMethod.Overrides.Add(targetModule.ImportReference(method));

                foreach (var parameter in method.Parameters)
                {
                    newMethod.Parameters.Add(new ParameterDefinition(targetModule.ImportReference(parameter.ParameterType)));
                }

                var ilProcessor = newMethod.Body.GetILProcessor();

                ilProcessor.Append(Instruction.Create(OpCodes.Ldarg_0));

                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    // TODO: 🦆-chaining
                    ilProcessor.Append(Instruction.Create(OpCodes.Ldarg_S, method.Parameters[i]));
                }

                ilProcessor.Append(Instruction.Create(OpCodes.Call, FindMethod(targetType, method.Name, method.Parameters)));

                // TODO: 🦆-chaining
                var returnType = method.ReturnType.Resolve();

                if (IsDuckType(returnType))
                {
                    // If it's an interface then it should "just work", TODO: verify this
                    // If it's a duckcopy, we need to call the right IDuckCopy<T>.DuckCopy method
                    var duckCopyAttribute = returnType.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "DuckCopyAttribute");

                    if (duckCopyAttribute != null)
                    {
                        var duckCopyTarget = duckCopyAttribute.ConstructorArguments[0].Value.ToString();
                        var duckCopyTargetAssembly = duckCopyAttribute.ConstructorArguments[1].Value.ToString();

                        var duckCopyTargetAssemblyDefinition = _assemblies.FirstOrDefault(a => a.Name.Name == duckCopyTargetAssembly);

                        if (duckCopyTargetAssemblyDefinition == null)
                        {
                            Log($"DuckCopyAttribute on {returnType.Name} has invalid target assembly {duckCopyTargetAssembly}");
                        }
                        else
                        {
                            var duckCopyTargetType = duckCopyTargetAssemblyDefinition.MainModule.GetType(duckCopyTarget);

                            if (duckCopyTargetType == null)
                            {
                                Log($"DuckCopyAttribute on {returnType.Name} has invalid target type {duckCopyTarget}");
                            }
                            else
                            {
                                var duckCopyMethodName = GetDuckCopyMethodName(returnType);
                                var duckCopyMethod = duckCopyTargetType.Methods.Single(m => m.Name == duckCopyMethodName);

                                ilProcessor.Append(Instruction.Create(OpCodes.Callvirt, targetModule.ImportReference(duckCopyMethod)));
                            }
                        }
                    }
                }

                ilProcessor.Append(Instruction.Create(OpCodes.Ret));

                targetType.Methods.Add(newMethod);
            }

            foreach (var property in currentInterface.Properties)
            {
                var newProperty = new PropertyDefinition($"{currentInterface.FullName}.{property.Name}", PropertyAttributes.None, targetModule.ImportReference(property.PropertyType));

                if (property.GetMethod != null)
                {
                    var getMethod = new MethodDefinition($"get_{property.Name}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual, targetModule.ImportReference(property.PropertyType));
                    newProperty.GetMethod = getMethod;
                    targetType.Methods.Add(getMethod);

                    var getMethodIlProcessor = getMethod.Body.GetILProcessor();

                    if (currentInterface.Module == _datadogAssembly.MainModule && currentInterface.FullName == "Datadog.Trace.DuckTyping.IDuckType")
                    {
                        if (property.Name == "Instance")
                        {
                            getMethodIlProcessor.Append(Instruction.Create(OpCodes.Ldarg_0));
                            getMethodIlProcessor.Append(Instruction.Create(OpCodes.Ret));
                        }
                        else if (property.Name == "Type")
                        {
                            getMethodIlProcessor.Append(Instruction.Create(OpCodes.Ldarg_0));

                            var obj = property.PropertyType.Module.TypeSystem.Object;
                            var objType = obj.Resolve();

                            // Call object.GetType()
                            getMethodIlProcessor.Append(Instruction.Create(OpCodes.Callvirt, targetType.Module.ImportReference(objType.Methods.First(m => m.Name == "GetType"))));
                            getMethodIlProcessor.Append(Instruction.Create(OpCodes.Ret));
                        }
                    }
                    else
                    {
                        getMethodIlProcessor.Append(Instruction.Create(OpCodes.Ldarg_0));
                        getMethodIlProcessor.Append(Instruction.Create(OpCodes.Call, targetType.Methods.First(p => p.Name == $"get_{property.Name}")));

                        // Duckchaining
                        var returnType = property.PropertyType.Resolve();

                        if (IsDuckType(returnType))
                        {
                            // If it's an interface then it should "just work", TODO: verify this
                            // If it's a duckcopy, we need to call the right IDuckCopy<T>.DuckCopy method
                            var duckCopyAttribute = returnType.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "DuckCopyAttribute");

                            if (duckCopyAttribute != null)
                            {
                                var duckCopyTarget = duckCopyAttribute.ConstructorArguments[0].Value.ToString();
                                var duckCopyTargetAssembly = duckCopyAttribute.ConstructorArguments[1].Value.ToString();

                                var duckCopyTargetAssemblyDefinition = _assemblies.FirstOrDefault(a => a.Name.Name == duckCopyTargetAssembly);

                                if (duckCopyTargetAssemblyDefinition == null)
                                {
                                    Log($"DuckCopyAttribute on {returnType.Name} has invalid target assembly {duckCopyTargetAssembly}");
                                }
                                else
                                {
                                    var duckCopyTargetType = duckCopyTargetAssemblyDefinition.MainModule.GetType(duckCopyTarget);

                                    if (duckCopyTargetType == null)
                                    {
                                        Log($"DuckCopyAttribute on {returnType.Name} has invalid target type {duckCopyTarget}");
                                    }
                                    else
                                    {
                                        var duckCopyMethodName = GetDuckCopyMethodName(returnType);
                                        var duckCopyMethod = duckCopyTargetType.Methods.Single(m => m.Name == duckCopyMethodName);

                                        getMethodIlProcessor.Append(Instruction.Create(OpCodes.Callvirt, targetModule.ImportReference(duckCopyMethod)));
                                    }
                                }
                            }
                        }

                        getMethodIlProcessor.Append(Instruction.Create(OpCodes.Ret));
                    }
                }

                if (property.SetMethod != null)
                {
                    var setMethod = new MethodDefinition($"set_{property.Name}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual, targetModule.ImportReference(typeof(void)));
                    setMethod.Parameters.Add(new ParameterDefinition(targetModule.ImportReference(property.PropertyType)));
                    newProperty.SetMethod = setMethod;
                    targetType.Methods.Add(setMethod);

                    var setMethodIlProcessor = setMethod.Body.GetILProcessor();

                    setMethodIlProcessor.Append(Instruction.Create(OpCodes.Ldarg_0));
                    setMethodIlProcessor.Append(Instruction.Create(OpCodes.Ldarg_1));
                    setMethodIlProcessor.Append(Instruction.Create(OpCodes.Call, targetType.Methods.First(p => p.Name == $"set_{property.Name}")));
                    setMethodIlProcessor.Append(Instruction.Create(OpCodes.Ret));
                }

                targetType.Properties.Add(newProperty);
            }
        }
    }

    private PropertyDefinition? FindProperty(TypeDefinition targetType, string name)
    {
        // TODO: Check the type of the property?
        var type = targetType;

        while (type != null)
        {
            foreach (var property in type.Properties.Where(p => p.Name == name))
            {
                return property;
            }

            type = type.BaseType?.Resolve();
        }

        return null;
    }

    private MethodDefinition? FindMethod(TypeDefinition targetType, string name, IList<ParameterDefinition> parameters)
    {
        var type = targetType;

        while (type != null)
        {
            foreach (var method in type.Methods.Where(m => m.Name == name && m.Parameters.Count == parameters.Count))
            {
                // Check if the parameters match
                bool parametersMatch = true;

                for (int i = 0; i < parameters.Count; i++)
                {
                    // TODO: 🦆-chaining
                    if (method.Parameters[i].ParameterType.FullName != parameters[i].ParameterType.FullName)
                    {
                        parametersMatch = false;
                        break;
                    }
                }

                if (parametersMatch)
                {
                    return method;
                }
            }

            type = type.BaseType?.Resolve();
        }

        return null;
    }

    private string GetDuckCopyMethodName(TypeDefinition duckTypeName)
    {
        return $"Datadog.Trace.DuckTyping.IDuckCopy<{duckTypeName.FullName}>.DuckCopy";
    }

    private void ImplementDuckCopy(TypeDefinition duckType, TypeDefinition targetType)
    {
        var targetModule = targetType.Module;

        Log($"Implementing IDuckCopy into target {targetType.Name} in {targetModule.Assembly.Name} to return {duckType.Name} ");

        duckType.IsPublic = true;

        var duckTypeReference = targetModule.ImportReference(duckType);

        var genericDuckCopyInterface = _duckCopyInterface.MakeGenericInstanceType(duckTypeReference);
        var duckCopyInterfaceReference = targetModule.ImportReference(genericDuckCopyInterface);

        targetType.Interfaces.Add(new InterfaceImplementation(duckCopyInterfaceReference));

        var newMethod = new MethodDefinition(GetDuckCopyMethodName(duckType), default, duckTypeReference)
        {
            IsNewSlot = true,
            IsVirtual = true,
            IsFinal = true,
            IsPrivate = true,
            IsHideBySig = true
        };

        var duckCopyMethod = _duckCopyInterface.Methods.Single();

        var duckCopyMethodGenericReference = new MethodReference(duckCopyMethod.Name, duckCopyMethod.ReturnType, genericDuckCopyInterface)
        {
            CallingConvention = duckCopyMethod.CallingConvention,
            HasThis = duckCopyMethod.HasThis,
            ExplicitThis = duckCopyMethod.ExplicitThis,
        };

        newMethod.Overrides.Add(targetModule.ImportReference(duckCopyMethodGenericReference));
        newMethod.Body.Variables.Add(new VariableDefinition(duckTypeReference));

        var ilProcessor = newMethod.Body.GetILProcessor();

        ilProcessor.Append(Instruction.Create(OpCodes.Ldloca_S, newMethod.Body.Variables[0]));
        ilProcessor.Append(Instruction.Create(OpCodes.Initobj, duckTypeReference));

        foreach (var field in duckType.Fields)
        {
            var targetProperty = FindProperty(targetType, field.Name);

            if (targetProperty == null)
            {
                Log($"Property {field.Name} not found in {targetType.Name}");
                continue;
            }

            ilProcessor.Append(Instruction.Create(OpCodes.Ldloca_S, newMethod.Body.Variables[0]));
            ilProcessor.Append(Instruction.Create(OpCodes.Ldarg_0));
            ilProcessor.Append(Instruction.Create(OpCodes.Call, targetProperty.GetMethod));
            ilProcessor.Append(Instruction.Create(OpCodes.Stfld, targetModule.ImportReference(field)));
        }

        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc_0));
        ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));

        targetType.Methods.Add(newMethod);
    }

    private void GenerateDuckTypes()
    {
        var processedTypes = new HashSet<TypeDefinition>();
        var typesToProcess = new Stack<TypeDefinition>();

        foreach (var type in _datadogAssembly.MainModule.Types)
        {
            var duckTypeAttribute = type.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "DuckTypeAttribute");

            if (duckTypeAttribute != null)
            {
                if (duckTypeAttribute.ConstructorArguments.Count != 2)
                {
                    Log($"DuckTypeAttribute on {type.Name} has invalid number of arguments");
                }
                else
                {
                    typesToProcess.Push(type);
                }
            }

            var duckCopyAttribute = type.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "DuckCopyAttribute");

            if (duckCopyAttribute == null)
            {
                continue;
            }

            if (duckCopyAttribute.ConstructorArguments.Count != 2)
            {
                Log($"DuckCopyAttribute on {type.Name} has invalid number of arguments");
            }
            else
            {
                typesToProcess.Push(type);
            }
        }

        while (typesToProcess.Count > 0)
        {
            var type = typesToProcess.Peek();

            Log($"Processing {type.Name}");

            if (processedTypes.Contains(type))
            {
                typesToProcess.Pop();
                continue;
            }

            // Check if it depends on another type (duck-chaining)
            bool canProcess = true;

            foreach (var dependency in GetDuckChains(type))
            {
                if (!processedTypes.Contains(dependency))
                {
                    Log($"Type {type.Name} depends on {dependency.Name}, processing first");
                    typesToProcess.Push(dependency);
                    canProcess = false;
                }
            }

            if (!canProcess)
            {
                continue;
            }

            foreach (var attribute in type.CustomAttributes)
            {
                if (attribute.ConstructorArguments.Count != 2)
                {
                    continue;
                }

                if (attribute.AttributeType.Name == "DuckTypeAttribute" || attribute.AttributeType.Name == "DuckCopyAttribute")
                {
                    var targetTypeName = attribute.ConstructorArguments[0].Value.ToString();
                    var targetAssemblyName = attribute.ConstructorArguments[1].Value.ToString();

                    var targetAssembly = _assemblies.FirstOrDefault(a => a.Name.Name == targetAssemblyName);

                    if (targetAssembly == null)
                    {
                        Log($"Target assembly {targetAssemblyName} for ducktype {type} not found");
                        continue;
                    }

                    var targetType = targetAssembly.MainModule.GetType(targetTypeName);

                    if (attribute.AttributeType.Name == "DuckTypeAttribute")
                    {
                        ImplementIDuckType(type, targetType);
                    }
                    else
                    {
                        ImplementDuckCopy(type, targetType);
                    }
                }
            }

            processedTypes.Add(type);
            _ = typesToProcess.Pop();
        }
    }

    private void GenerateEntryPointReverseDuckTypes()
    {
        foreach (var duckTypeInterface in _datadogAssembly.MainModule.Types)
        {
            var duckTypeAttribute = duckTypeInterface.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "ReverseDuckTypeAttribute");

            if (duckTypeAttribute == null)
            {
                continue;
            }

            if (duckTypeAttribute.ConstructorArguments.Count != 2)
            {
                Log($"DuckTypeAttribute on {duckTypeInterface.Name} has invalid number of arguments");
                continue;
            }

            var targetTypeName = duckTypeAttribute.ConstructorArguments[0].Value.ToString();
            var targetAssemblyName = duckTypeAttribute.ConstructorArguments[1].Value.ToString();

            if (targetAssemblyName != _entryPointAssembly.Name.Name)
            {
                continue;
            }

            Log($"Creating proxy {duckTypeInterface.Name} for target {targetTypeName} in {targetAssemblyName}");

            var targetType = _entryPointAssembly.MainModule.GetType(targetTypeName);

            var reverseDuckTypeType = new TypeDefinition(
                duckTypeInterface.Namespace,
                "<>Proxy",
                TypeAttributes.AnsiClass | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit | TypeAttributes.NestedPublic,
                _datadogAssembly.MainModule.ImportReference(targetType));

            var instanceField = new FieldDefinition("_instance", FieldAttributes.Private, duckTypeInterface);
            reverseDuckTypeType.Fields.Add(instanceField);

            var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, _datadogAssembly.MainModule.TypeSystem.Void);
            ctor.Parameters.Add(new ParameterDefinition(duckTypeInterface));

            var ctorIlProcessor = ctor.Body.GetILProcessor();
            ctorIlProcessor.Append(Instruction.Create(OpCodes.Ldarg_0));
            ctorIlProcessor.Append(Instruction.Create(OpCodes.Call, _datadogAssembly.MainModule.ImportReference(_datadogAssembly.MainModule.TypeSystem.Object.Resolve().GetConstructors().First())));
            ctorIlProcessor.Append(Instruction.Create(OpCodes.Ldarg_0));
            ctorIlProcessor.Append(Instruction.Create(OpCodes.Ldarg_1));
            ctorIlProcessor.Append(Instruction.Create(OpCodes.Stfld, instanceField));
            ctorIlProcessor.Append(Instruction.Create(OpCodes.Ret));

            reverseDuckTypeType.Methods.Add(ctor);

            foreach (var method in duckTypeInterface.Methods)
            {
                if (!method.CustomAttributes.Any(a => a.AttributeType.Name == "DuckReverseMethodAttribute"))
                {
                    continue;
                }

                var methodToOverride = _datadogAssembly.MainModule.ImportReference(targetType.Methods.First(p => p.Name == method.Name));

                var newMethod = new MethodDefinition($"{method.Name}", method.Attributes, method.ReturnType)
                {
                    IsVirtual = true,
                    IsHideBySig = true
                };

                foreach (var parameter in method.Parameters)
                {
                    newMethod.Parameters.Add(new ParameterDefinition(_entryPointAssembly.MainModule.ImportReference(parameter.ParameterType)));
                }

                newMethod.Overrides.Add(methodToOverride);

                var ilProcessor = newMethod.Body.GetILProcessor();

                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    ilProcessor.Append(Instruction.Create(OpCodes.Ldarg, i + 1));
                }

                ilProcessor.Append(Instruction.Create(OpCodes.Ldarg_0));
                ilProcessor.Append(Instruction.Create(OpCodes.Ldfld, instanceField));
                ilProcessor.Append(Instruction.Create(OpCodes.Callvirt, method));

                ilProcessor.Append(Instruction.Create(OpCodes.Ret));

                reverseDuckTypeType.Methods.Add(newMethod);
            }

            duckTypeInterface.NestedTypes.Add(reverseDuckTypeType);

            // Implement IReverseDuckType
            var reverseDuckTypeInterface = _datadogAssembly.MainModule.GetType("Datadog.IReverseDuckType");

            var createProxyMethod = new MethodDefinition("CreateProxy", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, _datadogAssembly.MainModule.TypeSystem.Object);
            createProxyMethod.Parameters.Add(new ParameterDefinition(_datadogAssembly.MainModule.ImportReference(typeof(Type))));

            var createProxyIlProcessor = createProxyMethod.Body.GetILProcessor();

            // In the real case, we would check the type and branch to the right constructor
            createProxyIlProcessor.Append(createProxyIlProcessor.Create(OpCodes.Ldarg_0));
            createProxyIlProcessor.Append(createProxyIlProcessor.Create(OpCodes.Newobj, ctor));
            createProxyIlProcessor.Append(createProxyIlProcessor.Create(OpCodes.Ret));

            duckTypeInterface.Methods.Add(createProxyMethod);

            duckTypeInterface.Interfaces.Add(new InterfaceImplementation(reverseDuckTypeInterface));
        }
    }

    private void InstrumentCallTargetMethods()
    {
        foreach (var instrumentationType in _datadogAssembly.MainModule.Types
            .Where(t => t.CustomAttributes.Any(a => a.AttributeType.Name == "InstrumentMethodAttribute")))
        {
            foreach (var instrumentMethodAttribute in instrumentationType.CustomAttributes.Where(a => a.AttributeType.Name == "InstrumentMethodAttribute"))
            {
                // Retrieve the values
                string[] assemblyNames;

                if (instrumentMethodAttribute.Properties.Any(p => p.Name == "AssemblyNames"))
                {
                    assemblyNames = instrumentMethodAttribute.Properties.First(p => p.Name == "AssemblyNames").Argument.Value as string[] ?? Array.Empty<string>();
                }
                else
                {
                    assemblyNames = [instrumentMethodAttribute.Properties.First(p => p.Name == "AssemblyName").Argument.Value.ToString()!];
                }

                string[] typeNames;

                if (instrumentMethodAttribute.Properties.Any(p => p.Name == "TypeNames"))
                {
                    typeNames = instrumentMethodAttribute.Properties.First(p => p.Name == "TypeNames").Argument.Value as string[] ?? Array.Empty<string>();
                }
                else
                {
                    typeNames = [instrumentMethodAttribute.Properties.First(p => p.Name == "TypeName").Argument.Value.ToString()!];
                }

                var methodName = instrumentMethodAttribute.Properties.First(p => p.Name == "MethodName").Argument.Value.ToString();
                var returnTypeName = instrumentMethodAttribute.Properties.First(p => p.Name == "ReturnTypeName").Argument.Value.ToString();
                var parameterTypeNames = instrumentMethodAttribute.Properties.FirstOrDefault(p => p.Name == "ParameterTypeNames").Argument.Value as CustomAttributeArgument[] ?? Array.Empty<CustomAttributeArgument>();

                var minimumVersion = instrumentMethodAttribute.Properties.First(p => p.Name == "MinimumVersion").Argument.Value.ToString()!;
                var maximumVersion = instrumentMethodAttribute.Properties.First(p => p.Name == "MaximumVersion").Argument.Value.ToString()!;

                var integrationName = instrumentMethodAttribute.Properties.First(p => p.Name == "IntegrationName").Argument.Value.ToString();

                if (integrationName != "WebRequest")
                {
                    continue;
                }

                foreach (var assemblyName in assemblyNames)
                {
                    var assembly = _assemblies.FirstOrDefault(a => a.Name.Name == assemblyName);

                    if (assembly == null)
                    {
                        Log($"Skipping {assemblyName} for instrumentation {instrumentationType.Name} because the assembly was not found");
                        continue;
                    }

                    // TODO: Check the version
                    // if (assembly.Name.Version < new Version(minimumVersion) || assembly.Name.Version > new Version(maximumVersion))
                    // {
                    //     Log($"Skipping {assemblyName} for instrumentation {instrumentationType.Name} because the version {assembly.Name.Version} is not in the range {minimumVersion} - {maximumVersion}");
                    //     continue;
                    // }

                    foreach (var typeName in typeNames)
                    {
                        var type = assembly.MainModule.GetType(typeName);

                        if (type == null)
                        {
                            Log($"Skipping {typeName} in {assemblyName} for instrumentation {instrumentationType.Name} because the type was not found");
                            continue;
                        }

                        foreach (var method in type.Methods.Where(m => m.Name == methodName))
                        {
                            if (method.Parameters.Count != parameterTypeNames.Length)
                            {
                                Log($"Skipping {method.Name} in {typeName} for instrumentation {instrumentationType.Name} because the number of parameters does not match");
                                continue;
                            }

                            // TODO: Check argument types and return type

                            Log($"Found method {method.Name} in {typeName} for instrumentation {instrumentationType.Name}");
                            InstrumentCallTargetMethod(instrumentationType, method);
                        }
                    }
                }
            }
        }
    }

    private void InstrumentCallTargetMethod(TypeDefinition instrumentationType, MethodDefinition method)
    {
        // TODO: See if/how OnMethodEndAsync needs to be wired up
        var ilProcessor = method.Body.GetILProcessor();
        var onMethodBeginMethodOpen = instrumentationType.Methods.FirstOrDefault(m => m.Name == "OnMethodBegin");
        var onMethodEndMethodOpen = instrumentationType.Methods.FirstOrDefault(m => m.Name == "OnMethodEnd");
        var onAsyncMethodEndMethodOpen = instrumentationType.Methods.FirstOrDefault(m => m.Name == "OnAsyncMethodEnd");

        var callTargetStateRef = method.Module.ImportReference(_datadogAssembly.MainModule.GetType("Datadog.Trace.ClrProfiler.CallTarget.CallTargetState"));
        var callTargetStateVariable = new VariableDefinition(callTargetStateRef);

        var callTargetReturnOpenType = _datadogAssembly.MainModule.GetType("Datadog.Trace.ClrProfiler.CallTarget.CallTargetReturn`1");

        // Specialize generic type
        var callTargetReturnType = callTargetReturnOpenType.MakeGenericInstanceType(method.ReturnType);
        var callTargetReturnRef = method.Module.ImportReference(callTargetReturnType);
        var callTargetReturnVariable = new VariableDefinition(callTargetReturnRef);

        var exceptionVariable = new VariableDefinition(method.Module.ImportReference(typeof(Exception)));

        var returnVariable = new VariableDefinition(method.ReturnType);

        var start = ilProcessor.Body.Instructions[0];

        if (onMethodBeginMethodOpen != null)
        {
            onMethodBeginMethodOpen.IsPublic = true;

            if (method.IsStatic)
            {
                if (method.DeclaringType.IsValueType)
                {
                    Log($"Skipping {method.Name} because it's a static method on a value type");
                    return;
                }

                ilProcessor.InsertBefore(start, Instruction.Create(OpCodes.Ldnull));
            }
            else
            {
                ilProcessor.InsertBefore(start, Instruction.Create(OpCodes.Ldarg_0));

                if (method.DeclaringType.IsValueType)
                {
                    ilProcessor.InsertBefore(start, Instruction.Create(OpCodes.Ldobj, method.DeclaringType));
                }
            }

            foreach (var parameter in method.Parameters)
            {
                ilProcessor.InsertBefore(start, Instruction.Create(OpCodes.Ldarg, parameter));
            }

            var onMethodBeginMethod = new GenericInstanceMethod(onMethodBeginMethodOpen);
            onMethodBeginMethod.GenericArguments.Add(method.DeclaringType);

            var onMethodBeginMethodRef = method.Module.ImportReference(onMethodBeginMethod);
            ilProcessor.InsertBefore(start, Instruction.Create(OpCodes.Call, onMethodBeginMethodRef));
            ilProcessor.InsertBefore(start, Instruction.Create(OpCodes.Stloc, callTargetStateVariable));
        }

        ilProcessor.Body.Variables.Add(callTargetStateVariable);
        ilProcessor.Body.Variables.Add(callTargetReturnVariable);
        ilProcessor.Body.Variables.Add(exceptionVariable);
        ilProcessor.Body.Variables.Add(returnVariable);

        if (onMethodEndMethodOpen != null || onAsyncMethodEndMethodOpen != null)
        {
            Instruction newEnd;

            if (onMethodEndMethodOpen != null)
            {
                onMethodEndMethodOpen.IsPublic = true;

                newEnd = Instruction.Create(OpCodes.Ldarg_0);
                ilProcessor.Append(newEnd);
                ilProcessor.Append(Instruction.Create(OpCodes.Ldloc, returnVariable));
                ilProcessor.Append(Instruction.Create(OpCodes.Ldnull));
                ilProcessor.Append(Instruction.Create(OpCodes.Ldloca, callTargetStateVariable));

                var onMethodEndMethod = new GenericInstanceMethod(onMethodEndMethodOpen);
                onMethodEndMethod.GenericArguments.Add(method.DeclaringType);
                onMethodEndMethod.GenericArguments.Add(method.ReturnType);

                var onMethodEndMethodRef = method.Module.ImportReference(onMethodEndMethod);
                ilProcessor.Append(Instruction.Create(OpCodes.Call, onMethodEndMethodRef));
                ilProcessor.Append(Instruction.Create(OpCodes.Stloc, callTargetReturnVariable));

                var getReturnValueMethod = callTargetReturnType.Resolve().Methods.First(m => m.Name == "GetReturnValue");

                var getReturnValueMethodGenericReference = new MethodReference(getReturnValueMethod.Name, getReturnValueMethod.ReturnType, callTargetReturnType)
                {
                    CallingConvention = getReturnValueMethod.CallingConvention,
                    HasThis = getReturnValueMethod.HasThis,
                    ExplicitThis = getReturnValueMethod.ExplicitThis,
                };

                ilProcessor.Append(Instruction.Create(OpCodes.Ldloca, callTargetReturnVariable));
                ilProcessor.Append(Instruction.Create(OpCodes.Call, method.Module.ImportReference(getReturnValueMethodGenericReference)));
                ilProcessor.Append(Instruction.Create(OpCodes.Ret));
            }
            else
            {
                onAsyncMethodEndMethodOpen!.IsPublic = true;


            }

            // Change all returns to LEAVE_S
            foreach (var instruction in ilProcessor.Body.Instructions.ToList())
            {
                if (instruction == newEnd)
                {
                    break;
                }

                if (instruction.OpCode == OpCodes.Ret)
                {
                    ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Stloc, returnVariable));
                    ilProcessor.Replace(instruction, Instruction.Create(OpCodes.Leave_S, newEnd));
                }
            }
        }
    }

    private void PatchEntryPoint()
    {
        var ilProcessor = _entryPointAssembly.EntryPoint.Body.GetILProcessor();

        var agentType = _datadogAssembly.MainModule.GetType("Datadog.Trace.ClrProfiler.Instrumentation");
        agentType.IsPublic = true;

        var runMethod = agentType.Methods.First(m => m.Name == "InitializeNoNativeParts");
        runMethod.IsPublic = true;

        var runMethodRef = _entryPointAssembly.MainModule.ImportReference(runMethod);

        var start = ilProcessor.Body.Instructions[0];

        ilProcessor.InsertBefore(start, Instruction.Create(OpCodes.Ldnull));
        ilProcessor.InsertBefore(start, Instruction.Create(OpCodes.Call, runMethodRef));
    }
}