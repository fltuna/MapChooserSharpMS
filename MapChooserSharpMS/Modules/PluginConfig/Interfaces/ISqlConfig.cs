using System.Security;

namespace MapChooserSharpMS.Modules.PluginConfig.Interfaces;

public interface ISqlConfig
{
    internal McsSupportedSqlType DataBaseType { get; }
    
    internal string Host { get; }
    
    internal string Port { get; }
    
    internal string DatabaseName { get; }
    
    internal string UserName { get; }
    
    internal SecureString Password { get; }
    
    internal string GroupSettingsSqlTableName { get; }
    
    internal string MapSettingsSqlTableName { get; }
}