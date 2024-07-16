using Dex.Audit.MediatR.Responses;

namespace Dex.Audit.ClientSample.Commands.EFCore.AddUser;

public record AddUserResponse(int Id) : IAuditResponse;