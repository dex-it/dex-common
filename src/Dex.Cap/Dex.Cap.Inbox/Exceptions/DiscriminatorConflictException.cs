using System;

namespace Dex.Cap.Inbox.Exceptions;

/// <summary>
/// Один дискриминатор объявлен несколькими типами сообщений.
/// </summary>
/// <remarks>
/// Ошибка конфигурации, а не данных: при коллизии невозможно однозначно восстановить тип
/// сохранённого сообщения, поэтому реестр не строится и сервис не стартует.
/// </remarks>
public class DiscriminatorConflictException : InboxException
{
    public DiscriminatorConflictException()
    {
    }

    public DiscriminatorConflictException(string message) : base(message)
    {
    }

    public DiscriminatorConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}