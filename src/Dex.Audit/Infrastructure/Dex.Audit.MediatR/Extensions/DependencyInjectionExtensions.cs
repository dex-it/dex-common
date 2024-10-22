using Dex.Audit.MediatR.PipelineBehaviours;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.MediatR.Extensions;

/// <summary>
/// A static class that contains extension methods for configuring dependencies.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Add audibility to the pipeline.
    /// </summary>
    /// <param name="configuration"><see cref="MediatRServiceConfiguration"/></param>
    public static void AddPipelineAuditBehavior(this MediatRServiceConfiguration configuration)
    {
        configuration.AddBehavior(typeof(IPipelineBehavior<,>),typeof(AuditBehavior<,>));
    }
}