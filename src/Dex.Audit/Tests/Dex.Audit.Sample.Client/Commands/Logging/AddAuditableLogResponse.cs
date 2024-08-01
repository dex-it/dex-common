using Dex.Audit.MediatR.Responses;

namespace Dex.Audit.ClientSample.Commands.Logging;

public record AddAuditableLogResponse(bool Result) : IAuditResponse;