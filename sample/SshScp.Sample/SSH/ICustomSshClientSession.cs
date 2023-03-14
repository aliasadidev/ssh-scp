using System.Threading.Tasks;
using Microsoft.DevTunnels.Ssh;

namespace SshScp.Sample.SSH;

public interface ICustomSshClientSession
{
    Task<SshClientSession> GetSession();
    Task CloseSession(SshClientSession session);
}

