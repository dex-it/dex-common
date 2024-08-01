using Dex.Audit.MediatR.Responses;

namespace Dex.Audit.ClientSample.Commands.EFCore.UpdateUser;

public record UpdateUserResponse(int Id) : IAuditResponse;