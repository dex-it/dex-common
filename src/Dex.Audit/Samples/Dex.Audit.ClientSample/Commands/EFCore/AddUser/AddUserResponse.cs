using Dex.Audit.MediatR.Responses;

namespace Dex.Audit.ClientSample.Commands.EFCore.AddUser;

public class AddUserResponse(int Id) : IAuditResponse;