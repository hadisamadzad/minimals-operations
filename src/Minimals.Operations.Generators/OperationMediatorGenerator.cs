using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Minimals.Operations.Generators;

[Generator]
public sealed class OperationMediatorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var operations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax,
                static (generatorContext, _) => GetOperation(generatorContext))
            .Where(static operation => operation is not null)
            .Select(static (operation, _) => operation!)
            .Collect();

        var eventOperations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax,
                static (generatorContext, _) => GetEventOperation(generatorContext))
            .Where(static operation => operation is not null)
            .Select(static (operation, _) => operation!)
            .Collect();

        context.RegisterSourceOutput(
            operations.Combine(eventOperations),
            static (sourceProductionContext, discoveredTypes) =>
        {
            var uniqueOperations = discoveredTypes.Left
                .Distinct(OperationInfoComparer.Instance)
                .OrderBy(operation => operation.CommandType, StringComparer.Ordinal)
                .ToArray();
            var uniqueEventOperations = discoveredTypes.Right
                .Distinct(EventOperationInfoComparer.Instance)
                .OrderBy(operation => operation.EventType, StringComparer.Ordinal)
                .ThenBy(operation => operation.OperationType, StringComparer.Ordinal)
                .ToArray();

            if (uniqueOperations.Length == 0 && uniqueEventOperations.Length == 0)
            {
                return;
            }

            sourceProductionContext.AddSource(
                "GeneratedOperationMediator.g.cs",
                GenerateSource(uniqueOperations, uniqueEventOperations));
        });
    }

    private static OperationInfo? GetOperation(GeneratorSyntaxContext context)
    {
        if (context.Node is not TypeDeclarationSyntax declaration)
        {
            return null;
        }

        var operationType = context.SemanticModel.GetDeclaredSymbol(declaration) as INamedTypeSymbol;
        if (operationType is null ||
            operationType.TypeKind != TypeKind.Class ||
            operationType.IsAbstract)
        {
            return null;
        }

        var operationInterface = operationType.Interfaces.SingleOrDefault(@interface =>
            @interface.IsGenericType &&
            @interface.Name == "IOperation" &&
            @interface.Arity == 2 &&
            @interface.ContainingNamespace.ToDisplayString() == "Minimals.Operations");

        if (operationInterface is null)
        {
            return null;
        }

        var commandType = operationInterface.TypeArguments[0];
        var resultType = operationInterface.TypeArguments[1];
        var isOperationCommand = commandType.AllInterfaces.Any(@interface =>
            @interface.IsGenericType &&
            @interface.Name == "IOperationCommand" &&
            @interface.Arity == 1 &&
            @interface.ContainingNamespace.ToDisplayString() == "Minimals.Operations" &&
            SymbolEqualityComparer.Default.Equals(@interface.TypeArguments[0], resultType));

        if (!isOperationCommand)
        {
            return null;
        }

        return new OperationInfo(
            operationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            commandType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            resultType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static EventOperationInfo? GetEventOperation(GeneratorSyntaxContext context)
    {
        if (context.Node is not TypeDeclarationSyntax declaration)
        {
            return null;
        }

        var operationType = context.SemanticModel.GetDeclaredSymbol(declaration) as INamedTypeSymbol;
        if (operationType is null ||
            operationType.TypeKind != TypeKind.Class ||
            operationType.IsAbstract)
        {
            return null;
        }

        var operationInterface = operationType.Interfaces.SingleOrDefault(@interface =>
            @interface.IsGenericType &&
            @interface.Name == "IOperation" &&
            @interface.Arity == 1 &&
            @interface.ContainingNamespace.ToDisplayString() == "Minimals.Operations");

        if (operationInterface is null)
        {
            return null;
        }

        var eventType = operationInterface.TypeArguments[0];
        var isOperationEvent = eventType.AllInterfaces.Any(@interface =>
            @interface.Name == "IOperationEvent" &&
            @interface.ContainingNamespace.ToDisplayString() == "Minimals.Operations");

        if (!isOperationEvent)
        {
            return null;
        }

        return new EventOperationInfo(
            operationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            eventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static string GenerateSource(
        IReadOnlyList<OperationInfo> operations,
        IReadOnlyList<EventOperationInfo> eventOperations)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        builder.AppendLine("using System.Linq;");
        builder.AppendLine();
        builder.AppendLine("namespace Minimals.Operations;");
        builder.AppendLine();
        builder.AppendLine("public static class GeneratedOperationMediatorRegistration");
        builder.AppendLine("{");
        AppendRegistration(builder, operations, eventOperations);
        AppendLazyDispatcher(builder, operations, eventOperations);
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void AppendRegistration(
        StringBuilder builder,
        IReadOnlyList<OperationInfo> operations,
        IReadOnlyList<EventOperationInfo> eventOperations)
    {
        builder.Append("    public static IServiceCollection ")
            .Append("AddOperations")
            .AppendLine("(this IServiceCollection services)");
        builder.AppendLine("    {");

        foreach (var operation in operations)
        {
            builder.Append("        services.AddTransient<global::Minimals.Operations.IOperation<")
                .Append(operation.CommandType)
                .Append(", ")
                .Append(operation.ResultType)
                .Append(">, ")
                .Append(operation.OperationType)
                .AppendLine(">();");
        }

        foreach (var eventOperation in eventOperations)
        {
            builder.Append("        services.AddTransient<global::Minimals.Operations.IOperation<")
                .Append(eventOperation.EventType)
                .Append(">, ")
                .Append(eventOperation.OperationType)
                .AppendLine(">();");
        }

        builder.Append("        services.AddScoped<global::Minimals.Operations.IOperationMediator, ")
            .Append("GeneratedOperationMediator")
            .AppendLine(">();");
        builder.AppendLine("        return services;");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendLazyDispatcher(
        StringBuilder builder,
        IReadOnlyList<OperationInfo> operations,
        IReadOnlyList<EventOperationInfo> eventOperations)
    {
        builder.AppendLine("    private sealed class GeneratedOperationMediator : global::Minimals.Operations.IOperationMediator");
        builder.AppendLine("    {");

        for (var index = 0; index < operations.Count; index++)
        {
            var operation = operations[index];
            builder.Append("        private readonly global::System.Lazy<global::Minimals.Operations.IOperation<")
                .Append(operation.CommandType)
                .Append(", ")
                .Append(operation.ResultType)
                .Append(">> _operation")
                .Append(index)
                .AppendLine(";");
        }

        for (var index = 0; index < eventOperations.Count; index++)
        {
            var eventOperation = eventOperations[index];
            builder.Append("        private readonly global::System.Lazy<global::System.Collections.Generic.IReadOnlyList<global::Minimals.Operations.IOperation<")
                .Append(eventOperation.EventType)
                .Append(">>> _eventOperations")
                .Append(index)
                .AppendLine(";");
        }

        builder.AppendLine();
        builder.AppendLine("        public GeneratedOperationMediator(global::System.IServiceProvider serviceProvider)");
        builder.AppendLine("        {");
        for (var index = 0; index < operations.Count; index++)
        {
            var operation = operations[index];
            builder.Append("            _operation")
                .Append(index)
                .Append(" = new global::System.Lazy<global::Minimals.Operations.IOperation<")
                .Append(operation.CommandType)
                .Append(", ")
                .Append(operation.ResultType)
                .Append(">>(() => serviceProvider.GetRequiredService<global::Minimals.Operations.IOperation<")
                .Append(operation.CommandType)
                .Append(", ")
                .Append(operation.ResultType)
                .Append(">>(), global::System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);")
                .AppendLine();
        }

        for (var index = 0; index < eventOperations.Count; index++)
        {
            var eventOperation = eventOperations[index];
            builder.Append("            _eventOperations")
                .Append(index)
                .Append(" = new global::System.Lazy<global::System.Collections.Generic.IReadOnlyList<global::Minimals.Operations.IOperation<")
                .Append(eventOperation.EventType)
                .Append(">>>(() => serviceProvider.GetServices<global::Minimals.Operations.IOperation<")
                .Append(eventOperation.EventType)
                .Append(">>().ToArray(), global::System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);")
                .AppendLine();
        }

        builder.AppendLine("        }");
        AppendExecuteMethod(builder, operations, "        ");
        AppendPublishMethod(builder, eventOperations, "        ");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendExecuteMethod(
        StringBuilder builder,
        IReadOnlyList<OperationInfo> operations,
        string indentation)
    {
        builder.AppendLine();
        builder.Append(indentation)
            .AppendLine("public async global::System.Threading.Tasks.Task<global::Minimals.Operations.OperationResult<TResult>> ExecuteAsync<TResult>(");
        builder.Append(indentation)
            .AppendLine("    global::Minimals.Operations.IOperationCommand<TResult> command,");
        builder.Append(indentation)
            .AppendLine("    global::System.Threading.CancellationToken? cancellation = null)");
        builder.Append(indentation).AppendLine("{");

        for (var index = 0; index < operations.Count; index++)
        {
            var operation = operations[index];
            builder.Append(indentation)
                .Append("    if (command is ")
                .Append(operation.CommandType)
                .Append(" command")
                .Append(index)
                .AppendLine(")");
            builder.Append(indentation).AppendLine("    {");
            builder.Append(indentation)
                .Append("        var operationResult = await _operation")
                .Append(index)
                .Append(".Value")
                .Append(".ExecuteAsync(command")
                .Append(index)
                .AppendLine(", cancellation);");
            builder.Append(indentation)
                .AppendLine("        return (global::Minimals.Operations.OperationResult<TResult>)(object)operationResult;");
            builder.Append(indentation).AppendLine("    }");
        }

        builder.AppendLine();
        builder.Append(indentation).AppendLine("    throw new global::System.InvalidOperationException(");
        builder.Append(indentation)
            .AppendLine("        $\"No generated operation handler exists for command '{command.GetType().FullName}'.\");");
        builder.Append(indentation).AppendLine("}");
    }

    private static void AppendPublishMethod(
        StringBuilder builder,
        IReadOnlyList<EventOperationInfo> eventOperations,
        string indentation)
    {
        builder.AppendLine();
        builder.Append(indentation)
            .AppendLine("public async global::System.Threading.Tasks.Task PublishAsync<TEvent>(");
        builder.Append(indentation)
            .AppendLine("    TEvent @event,");
        builder.Append(indentation)
            .AppendLine("    global::System.Threading.CancellationToken? cancellation = null)");
        builder.Append(indentation)
            .AppendLine("    where TEvent : global::Minimals.Operations.IOperationEvent");
        builder.Append(indentation).AppendLine("{");

        for (var index = 0; index < eventOperations.Count; index++)
        {
            var eventOperation = eventOperations[index];
            builder.Append(indentation)
                .Append("    if (@event is ")
                .Append(eventOperation.EventType)
                .Append(" event")
                .Append(index)
                .AppendLine(")");
            builder.Append(indentation).AppendLine("    {");
            builder.Append(indentation)
                .Append("        foreach (var operation in _eventOperations")
                .Append(index)
                .AppendLine(".Value)");
            builder.Append(indentation).AppendLine("        {");
            builder.Append(indentation)
                .Append("            await operation.ExecuteAsync(event")
                .Append(index)
                .AppendLine(", cancellation);");
            builder.Append(indentation).AppendLine("        }");
            builder.Append(indentation).AppendLine("        return;");
            builder.Append(indentation).AppendLine("    }");
        }

        builder.AppendLine();
        builder.Append(indentation).AppendLine("    throw new global::System.InvalidOperationException(");
        builder.Append(indentation)
            .AppendLine("        $\"No generated event handler exists for event '{@event.GetType().FullName}'.\");");
        builder.Append(indentation).AppendLine("}");
    }

    private sealed class OperationInfo
    {
        public OperationInfo(string operationType, string commandType, string resultType)
        {
            OperationType = operationType;
            CommandType = commandType;
            ResultType = resultType;
        }

        public string OperationType { get; }
        public string CommandType { get; }
        public string ResultType { get; }
    }

    private sealed class EventOperationInfo
    {
        public EventOperationInfo(string operationType, string eventType)
        {
            OperationType = operationType;
            EventType = eventType;
        }

        public string OperationType { get; }
        public string EventType { get; }
    }

    private sealed class OperationInfoComparer : IEqualityComparer<OperationInfo>
    {
        public static readonly OperationInfoComparer Instance = new();

        public bool Equals(OperationInfo? left, OperationInfo? right) =>
            left?.OperationType == right?.OperationType &&
            left?.CommandType == right?.CommandType &&
            left?.ResultType == right?.ResultType;

        public int GetHashCode(OperationInfo operation)
        {
            var hash = StringComparer.Ordinal.GetHashCode(operation.OperationType);
            hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(operation.CommandType);
            return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(operation.ResultType);
        }
    }

    private sealed class EventOperationInfoComparer : IEqualityComparer<EventOperationInfo>
    {
        public static readonly EventOperationInfoComparer Instance = new();

        public bool Equals(EventOperationInfo? left, EventOperationInfo? right) =>
            left?.OperationType == right?.OperationType &&
            left?.EventType == right?.EventType;

        public int GetHashCode(EventOperationInfo operation)
        {
            var hash = StringComparer.Ordinal.GetHashCode(operation.OperationType);
            return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(operation.EventType);
        }
    }
}
