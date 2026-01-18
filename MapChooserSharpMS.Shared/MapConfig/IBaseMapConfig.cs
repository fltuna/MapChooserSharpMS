using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.MapConfig;

public interface IBaseMapConfig
{
    /// <summary>
    /// Is this config data disabled?
    /// </summary>
    public bool IsDisabled { get; }
    
    /// <summary>
    /// How many extends available in this map?
    /// </summary>
    public int MaxExtends { get; }
    
    /// <summary>
    /// How many times allow the !ext command in this map?
    /// </summary>
    public int MaxExtCommandUses { get; }
    
    /// <summary>
    /// Map's default mp_timelimit value
    /// </summary>
    public int MapTime { get; }
    
    /// <summary>
    /// How many minutes extended in per extend?
    /// </summary>
    public int ExtendTimePerExtends { get; }
    
    /// <summary>
    /// Map's default mp_maxround
    /// </summary>
    public int MapRounds { get; }
    
    /// <summary>
    /// How many rounds extended in per extend?
    /// </summary>
    public int ExtendRoundsPerExtends { get; }
    
    /// <summary>
    /// Random pick settings
    /// </summary>
    public IRandomPickConfig RandomPickConfig { get; }
    
    /// <summary>
    /// Nomination settings
    /// </summary>
    public INominationConfig NominationConfig { get; }
    
    /// <summary>
    /// Cooldown things
    /// </summary>
    public ICooldownConfig CooldownConfig { get; }
    
    /// <summary>
    /// This is for API developers to define custom value like integrate with shop plugin or any other custom plugin. <br/>
    /// <br/>
    /// If defined in config like this: <br/>
    ///
    /// [ze_xxxxx] <br/>
    /// description = "ze xxxxx map!" <br/>
    /// MapNameAlias = "ze xxxxx" <br/>
    /// <br/>
    /// [ze_xxxxxx.extra.shop] <br/>
    /// cost = 10 <br/>
    /// <br/>
    /// Then you can access the value like this: <br/>
    /// <br/>
    /// string cost = ExtraConfiguration["shop"]["cost"] <br/>
    /// 
    /// 
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> ExtraConfiguration { get; }
}