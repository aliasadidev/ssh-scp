using Microsoft.DevTunnels.Ssh;

namespace SshScp.Sample.SSH;

public interface ICustomSshClient
{
  Task<string> ExecuteCommand(SshChannel channel, string command);
  Task<List<string>> GetDirectories(SshChannel channel, string command, OperatingSystems os);
}
