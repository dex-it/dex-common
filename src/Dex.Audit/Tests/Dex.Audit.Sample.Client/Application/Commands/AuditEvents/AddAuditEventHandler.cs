﻿using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Abstractions.Messages;
using MediatR;

namespace Dex.Audit.Sample.Client.Application.Commands.AuditEvents;

public class AddAuditEventHandler(IAuditWriter auditWriter) : IRequestHandler<AddAuditEventCommand>
{
    public Task Handle(AddAuditEventCommand request, CancellationToken cancellationToken)
    {
        return auditWriter.WriteAsync(
            new AuditEventBaseInfo(
                request.EventType,
                request.EventObject,
                request.Message,
                request.IsSuccess),
            cancellationToken);
    }
}

public sealed record AddAuditEventCommand : IRequest
{
    /// <summary>
    /// Тип события аудита
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Объект сообщения аудита
    /// </summary>
    public string? EventObject { get; init; }

    /// <summary>
    /// Текст сообщения о событии
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Результат (успех/отказ) действия
    /// </summary>
    public bool IsSuccess { get; init; }
}