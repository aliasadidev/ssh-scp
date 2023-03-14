using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SshScp.Sample;
public class SshSettings
{
  public string ServerUrl { get; set; }
  public int Port { get; set; }
  public string User { get; set; }
  public string PrivateKeyPath { get; set; }
  public Dictionary<CommandNames, Dictionary<OperatingSystems, string>> ListCommand { get; set; }
  public int MaxConnections { get; set; }
}

public enum CommandNames
{
  ls_only_file,
  ls_only_dir,
  file_exists
}

public enum OperatingSystems
{
  win32,
  linux
}
