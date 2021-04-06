using System;

namespace Dex.MassTransit.Extensions
{
    /// <summary>
    /// Позволяет получить LogId для всех инстансов ILogger в скоупе
    /// </summary>
    public interface ILogId
    {
        Guid LogId { get; }
        Uri BaseOrigin { get; }
    }
}