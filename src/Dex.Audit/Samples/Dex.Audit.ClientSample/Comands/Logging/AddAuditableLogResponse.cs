using Dex.Audit.MediatR.Responses;

namespace Dex.Audit.ClientSample.Comands.Logging;

public record AddAuditableLogResponse(bool Result) : IAuditResponse;