namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class GetAllRolesRequest
{
    public int Offset { get; set; }

    public int PageSize { get; set; } = 100;
}
