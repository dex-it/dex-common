using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Inbox.Interfaces;

/// <summary>
/// Типизированный вызов обработчика для типа сообщения, известного только в рантайме.
/// </summary>
/// <remarks>
/// Существует, чтобы не искать метод <c>Process</c> рефлексией. Поиск по имени неоднозначен, если один
/// класс обрабатывает несколько типов сообщений, не находит явную реализацию интерфейса и заворачивает
/// синхронно брошенные исключения в <see cref="System.Reflection.TargetInvocationException"/>, ломая
/// разбор отмены по типу исключения.
/// </remarks>
internal interface IInboxMessageInvoker
{
    /// <summary>
    /// Вызвать <see cref="IInboxMessageHandler{TMessage}.Process"/> через контракт интерфейса.
    /// </summary>
    Task InvokeAsync(object handler, object message, CancellationToken cancellationToken);
}