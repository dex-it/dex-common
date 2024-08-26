using Dex.Audit.MediatR.PipelineBehaviours;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.MediatR.Extensions;

/// <summary>
/// Статический класс, который содержит методы расширения для конфигурации поведения конвейера MeiatR.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Добавить в конвейер аудируемость.
    /// </summary>
    /// <param name="configuration"><see cref="MediatRServiceConfiguration"/></param>
    public static void AddPipelineAuditBehavior(this MediatRServiceConfiguration configuration)
    {
        configuration.AddBehavior(typeof(IPipelineBehavior<,>),typeof(AuditBehavior<,>));
    }
}