using System;
using System.Collections.Generic;

namespace Dex.Cap.Inbox.Interfaces;

/// <summary>
/// Источник типов сообщений инбокса.
/// </summary>
/// <remarks>
/// Отделён от построения реестра намеренно: дискавери зависит от того, какие сборки загружены в процесс,
/// а построение реестра и проверка дискриминаторов от этого не зависят и должны проверяться отдельно.
/// </remarks>
internal interface IInboxMessageTypeSource
{
    /// <summary>
    /// Вернуть конкретные типы, реализующие <see cref="IInboxMessage"/>.
    /// </summary>
    IEnumerable<Type> GetMessageTypes();
}
