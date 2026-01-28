using Microsoft.Extensions.DependencyInjection;

namespace Minimals.Operations;

/// <summary>
/// Extension methods for registering operations with the dependency injection container.
/// </summary>
public static class OperationServiceExtensions
{
    /// <summary>
    /// Registers all implementations of IOperation&lt;TCommand, TResult&gt; found in the application domain.
    /// Operations are registered as transient services.
    /// </summary>
    /// <param name="services">The service collection to add operations to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddOperations(this IServiceCollection services)
    {
        var operationTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .SelectMany(type => type.GetInterfaces().Where(@interface =>
                @interface.IsGenericType &&
                @interface.GetGenericTypeDefinition() == typeof(IOperation<,>))
            .Select(@interface => new { Service = @interface, Implementation = type }));

        foreach (var type in operationTypes)
        {
            services.AddTransient(type.Service, type.Implementation);
        }

        return services;
    }
}
