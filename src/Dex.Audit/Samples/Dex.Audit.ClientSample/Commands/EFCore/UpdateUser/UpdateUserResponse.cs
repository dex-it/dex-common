using Dex.Audit.MediatR.Responses;

namespace Dex.Audit.ClientSample.Commands.EFCore.UpdateUser;

public class UpdateUserResponse(int Id) : IAuditResponse;