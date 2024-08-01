using Dex.Audit.MediatR.Requests;
using Dex.Audit.Sample.Domain.Enums;

namespace Dex.Audit.ClientSample.Queries.GetUserQuery;

public class GetUserQuery : AuditRequest<GetUserQueryResponse>
{
    public override string EventType { get; } = AuditEventType.ObjectRead.ToString();
    public override string EventObject { get; } = nameof(GetUserQuery);
    public override string Message { get; } = "Getting User by Id";

    public int Id { get; set; }
}