using System.Collections.Generic;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Nomination.Managers;
using MapChooserSharpMS.Shared.Nomination.Services;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Nomination;

/// <summary>
/// API for nomination module
/// </summary>
public interface IMcsNominationController
{
    /// <summary>
    /// Nomination Service
    /// </summary>
    IMapNominationService NominationService { get; }
    
    /// <summary>
    /// Validation service
    /// </summary>
    INominationValidateService NominationValidateService { get; }
    
    /// <summary>
    /// Nomination Manager
    /// </summary>
    INominationManager NominationManager { get; }
    
    /// <summary>
    /// Installs event listener
    /// </summary>
    void InstallEventListener(INominationEventListener listener);

    /// <summary>
    /// Remove event listener
    /// </summary>
    void RemoveEventListener(INominationEventListener listener);
    
    /// <summary>
    /// Show nomination menu to client, this method is show maps in given config list
    /// </summary>
    /// <param name="client">Client</param>
    /// <param name="configs">Map configs to show</param>
    public void ShowNominationMenu(IGameClient client, List<IMapConfig> configs);

    /// <summary>
    /// Show nomination menu to client, this method is shows all maps in map config provider
    /// </summary>
    /// <param name="client">Client</param>
    public void ShowNominationMenu(IGameClient client);


    /// <summary>
    /// Show nomination menu to client, this method is show maps in given config list
    /// </summary>
    /// <param name="client">Client</param>
    /// <param name="configs">Map configs to show</param>
    public void ShowAdminNominationMenu(IGameClient client, List<IMapConfig> configs);

    /// <summary>
    /// Show nomination menu to client, this method is shows all maps in map config provider
    /// </summary>
    /// <param name="client">Client</param>
    public void ShowAdminNominationMenu(IGameClient client);

    /// <summary>
    /// Show remove nomination menu to client with given config list<br/>
    /// You can show this menu to any client, but non admin client is cannot execute the command.
    /// </summary>
    /// <param name="client">Client</param>
    /// <param name="nominationData">Nomination Data to show</param>
    public void ShowRemoveNominationMenu(IGameClient client, List<IMcsNominationData> nominationData);

    /// <summary>
    /// Show remove nomination menu to client with all nominated maps<br/>
    /// You can show this menu to any client, but non admin client is cannot execute the command.
    /// </summary>
    /// <param name="client">Client</param>
    public void ShowRemoveNominationMenu(IGameClient client);
}