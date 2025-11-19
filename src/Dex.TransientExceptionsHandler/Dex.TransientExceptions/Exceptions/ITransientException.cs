using System.Diagnostics.CodeAnalysis;

namespace Dex.TransientExceptions.Exceptions;

/// <summary>
/// Метка для безусловно трансиентных ошибок
/// </summary>
[SuppressMessage("Design", "CA1040:Не используйте пустые интерфейсы")]
[SuppressMessage("Naming", "CA1711:Идентификаторы не должны иметь неправильных суффиксов")]
public interface ITransientException;