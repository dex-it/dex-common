using Dex.Audit.MediatR.Responses;

namespace Dex.Audit.ClientSample.Comands.EFCore.AddUser;

public class AddUserResponse(int Id) : IAuditResponse;