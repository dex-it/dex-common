using Dex.Audit.ClientSample.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dex.Audit.ClientSample.Queries.GetUserQuery;

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