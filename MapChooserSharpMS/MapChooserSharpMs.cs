using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using TnmsPluginFoundation;

namespace MapChooserSharpMS;

public sealed class MapChooserSharpMs(
    ISharedSystem sharedSystem,
    string dllPath,
    string sharpPath,
    Version? version,
    IConfiguration coreConfiguration,
    bool hotReload)
    : TnmsPlugin(sharedSystem, dllPath, sharpPath, version, coreConfiguration, hotReload)
{
    public override string DisplayName => "MapChooserSharp - ModSharp";
    public override string DisplayAuthor => "faketuna A.K.A fltuna or tuna, Spitice, uru";
    public override string BaseCfgDirectoryPath => ModuleDirectory;
    public override string ConVarConfigPath => "";
    public override string PluginPrefix => "Plugin.Prefix";
    public override bool UseTranslationKeyInPluginPrefix => true;


    protected override void TnmsOnPluginLoad(bool hotReload)
    {
        RegisterModulesUnderNamespace("MapChooserSharpMS", true);
        AddTnmsCommandsUnderNamespace("MapChooserSharpMS", true);
    }
}
