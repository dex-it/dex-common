using Dex.Audit.MediatR.Responses;

namespace Dex.Audit.ClientSample.Comands.EFCore.UpdateUser;

public class UpdateUserResponse(int Id) : IAuditResponse;