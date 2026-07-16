using System;

namespace Dex.Cap.Inbox.Exceptions;

/// <summary>
/// Аренда сообщения потеряна до того, как обработчик успел зафиксировать успех.
/// </summary>
/// <remarks>
/// Internal: бросается и ловится только внутри библиотеки, поймать её потребитель не может.
/// Бросается только на пути успеха и только для того, чтобы откатить транзакцию обработчика.
/// Без этого изменения обработчика закоммитились бы, а статус сообщения остался бы прежним,
/// и следующий владелец аренды применил бы эффект второй раз.
/// </remarks>
internal sealed class InboxLeaseLostException : InboxException
{
    public InboxLeaseLostException()
    {
    }

    public InboxLeaseLostException(string message) : base(message)
    {
    }

    public InboxLeaseLostException(string message, Exception innerException) : base(message, innerException)
    {
    }
}