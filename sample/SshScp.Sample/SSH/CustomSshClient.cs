using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DevTunnels.Ssh;
using Microsoft.DevTunnels.Ssh.Messages;

namespace SshScp.Sample.SSH;

public class CustomSshClient : ICustomSshClient
{
  public async Task<string> ExecuteCommand(SshChannel channel, string command)
  {
    // Open a channel, send a command, and read the command result.
    string result;
    bool commandAuthorized = await channel.RequestAsync(new CommandRequestMessage(command));
    if (commandAuthorized)
    {
      using (var channelStream = new SshStream(channel))
      {
        result = await new StreamReader(channelStream).ReadToEndAsync();
      }
    }
    else
    {
      throw new Exception("Unauthorized access");
    }
    await channel.CloseAsync();
    return result;
  }

  public async Task<List<string>> GetDirectories(SshChannel channel, string command, OperatingSystems os)
  {
    var result = await ExecuteCommand(channel, command);

    var normalizedLines = result.ReplaceLineEndings();
    return new List<string>(normalizedLines.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList());
  }
}
