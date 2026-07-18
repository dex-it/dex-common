using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dex.Cap.Inbox.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Inbox;

/// <summary>
/// Ищет типы сообщений рефлексией по загруженным сборкам.
/// </summary>
/// <remarks>
/// Сборка, объявляющая тип сообщения, обязана быть загружена к моменту построения реестра.
/// На практике это обеспечивает регистрация обработчика: она заставляет CLR загрузить сборку типа.
/// </remarks>
internal sealed class AppDomainInboxMessageTypeSource(ILogger<AppDomainInboxMessageTypeSource> logger) : IInboxMessageTypeSource
{
    public IEnumerable<Type> GetMessageTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(GetLoadableTypes)
            .Where(t => typeof(IInboxMessage).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false });
    }

    /// <summary>
    /// Прочитать типы сборки.
    /// </summary>
    /// <remarks>
    /// Частично загружаемая сборка не должна валить дискавери целиком: берём то, что загрузилось.
    /// </remarks>
    private IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            logger.LogWarning(e, "Assembly {Assembly} is partially loadable, inbox message types may be incomplete", assembly.FullName);
            return e.Types.Where(t => t is not null)!;
        }
    }
}