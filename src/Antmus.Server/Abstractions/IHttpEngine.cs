namespace Antmus.Server;

public interface IHttpEngine
{
    public Task Handle(HttpContext context);
}
