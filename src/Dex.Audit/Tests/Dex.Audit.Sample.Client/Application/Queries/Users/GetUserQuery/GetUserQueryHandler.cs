using Dex.Audit.ClientSample.Infrastructure.Context;
using Dex.Audit.MediatR.Requests;
using Dex.Audit.Sample.Shared.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.ClientSample.Application.Queries.Users.GetUserQuery;

public class GetUserQueryHandler(ClientSampleContext clientSampleContext) : IRequestHandler<GetUserQuery, GetUserQueryResponse>
{
    public async Task<GetUserQueryResponse> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await clientSampleContext
            .Users
            .FirstOrDefaultAsync(user => user.Id == request.Id, cancellationToken);

        if (user != null)
        {
            return new GetUserQueryResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.UserName,
                Fullname = user.Fullname
            };
        }

        return new GetUserQueryResponse();
    }
}

public class GetUserQuery : AuditRequest<GetUserQueryResponse>
{
    public override string EventType { get; } = AuditEventType.ObjectRead.ToString();
    public override string EventObject { get; } = nameof(GetUserQuery);
    public override string Message { get; } = "Getting User by Id";

    public int Id { get; set; }
}

public class GetUserQueryResponse
{
    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ФИО пользователя.
    /// </summary>
    public string? Fullname { get; set; }

    /// <summary>
    /// Имя пользователя.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Адрес e-mail.
    /// </summary>
    public string? Email { get; set; }
}