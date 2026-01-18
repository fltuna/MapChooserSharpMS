using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MapChooserSharpMS.Shared.Events;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.MapConfig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager;

internal sealed class EventManager(IServiceProvider serviceProvider, bool hotReload) : PluginModuleBase(serviceProvider, hotReload), IInternalEventManager
{
    public override string PluginModuleName => "McsEventManager";
    public override string ModuleChatPrefix => "unused";
    protected override bool UseTranslationKeyInModuleChatPrefix => false;


    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IInternalEventManager>(this);
    }

    protected override void OnInitialize()
    {
        Fire<IMapCycleEventListener>(l => l.OnExtendVoteCancelled(null!));
        var cancelled = FireCancellable<IMapCycleEventListener>(l => l.OnExtCommandExecute(null!));
        var results = FireCollect<IMapVoteEventListener, List<IMapConfig>>(l => l.OnRandomMapPick(null!));
    }


    private readonly Dictionary<Type, List<IEventListenerBase>> _listeners = new();
    private readonly Lock _listenersLock = new();

    #region Listener Management

    /// <summary>
    /// Registers a listener for a specific event type and sorts the list immediately
    /// </summary>
    public void RegisterListener<TListener>(TListener listener)
        where TListener : IEventListenerBase
    {
        lock (_listenersLock)
        {
            Type listenerType = typeof(TListener);

            if (!_listeners.TryGetValue(listenerType, out var list))
            {
                list = new List<IEventListenerBase>();
                _listeners[listenerType] = list;
            }

            list.Add(listener);

            // Sort immediately after adding
            SortListeners(list);

            Logger.LogDebug($"Registered listener: {listenerType.Name} (Priority: {listener.ListenerPriority})");
        }
    }

    /// <summary>
    /// Removes a registered listener and sorts the list immediately
    /// </summary>
    public void RemoveListener<TListener>(TListener listener)
        where TListener : IEventListenerBase
    {
        lock (_listenersLock)
        {
            Type listenerType = typeof(TListener);

            if (!_listeners.TryGetValue(listenerType, out var list))
                return;

            list.Remove(listener);

            if (list.Count == 0)
            {
                _listeners.Remove(listenerType);
            }
            else
            {
                SortListeners(list);
            }

            Logger.LogDebug($"Removed listener: {listenerType.Name}");
        }
    }

    /// <summary>
    /// Sorts listeners by priority (descending: higher priority first)
    /// </summary>
    private static void SortListeners(List<IEventListenerBase> list)
    {
        // Sort by priority (descending: higher priority first)
        // List.Sort is stable, so same priority maintains insertion order
        list.Sort((a, b) => b.ListenerPriority.CompareTo(a.ListenerPriority));
    }

    /// <summary>
    /// Gets listeners (already sorted) for the specified type
    /// </summary>
    private List<IEventListenerBase> GetSortedListeners<TListener>()
        where TListener : IEventListenerBase
    {
        Type listenerType = typeof(TListener);

        lock (_listenersLock)
        {
            // Listeners are already sorted, just return a copy
            if (!_listeners.TryGetValue(listenerType, out var list))
                return [];

            return [..list]; // Return copy for thread safety
        }
    }

    #endregion

    #region Event Firing

    /// <summary>
    /// Fires an event to all registered listeners (void return type)
    /// </summary>
    public void Fire<TListener>(Action<TListener> action)
        where TListener : IEventListenerBase
    {
        var listeners = GetSortedListeners<TListener>();

        foreach (var listener in listeners)
        {
            try
            {
                action((TListener)listener);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    $"Error in event listener {listener.GetType().Name} " +
                    $"(Priority: {listener.ListenerPriority})");
            }
        }
    }

    /// <summary>
    /// Fires a cancellable event to registered listeners (bool return type)
    /// </summary>
    public bool FireCancellable<TListener>(Func<TListener, bool> predicate)
        where TListener : IEventListenerBase
    {
        var listeners = GetSortedListeners<TListener>();

        foreach (var listener in listeners)
        {
            try
            {
                bool shouldCancel = predicate((TListener)listener);
                if (shouldCancel)
                {
                    Logger.LogDebug(
                        $"Event cancelled by {listener.GetType().Name} " +
                        $"(Priority: {listener.ListenerPriority})");
                    return true; // Event cancelled
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    $"Error in event listener {listener.GetType().Name}");
                // Continue to next listener even if exception occurs
            }
        }

        return false; // Not cancelled
    }

    /// <summary>
    /// Fires an event and collects results from all registered listeners
    /// </summary>
    public List<TResult> FireCollect<TListener, TResult>(
        Func<TListener, TResult> selector,
        bool skipNullResults = true)
        where TListener : IEventListenerBase
    {
        var listeners = GetSortedListeners<TListener>();
        var results = new List<TResult>();

        foreach (var listener in listeners)
        {
            try
            {
                var result = selector((TListener)listener);

                // Skip null results if requested
                if (skipNullResults && result == null)
                    continue;

                results.Add(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    $"Error in event listener {listener.GetType().Name}");
            }
        }

        return results;
    }

    /// <summary>
    /// Fires an event and collects flattened results from all registered listeners
    /// </summary>
    public List<TResult> FireCollectMany<TListener, TResult>(
        Func<TListener, IEnumerable<TResult>> selector)
        where TListener : IEventListenerBase
    {
        var listeners = GetSortedListeners<TListener>();
        var results = new List<TResult>();

        foreach (var listener in listeners)
        {
            try
            {
                var listenerResults = selector((TListener)listener);
                results.AddRange(listenerResults);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    $"Error in event listener {listener.GetType().Name}");
            }
        }

        return results;
    }

    /// <summary>
    /// Fires an event with custom result handling and stop condition
    /// </summary>
    public TResult? FireWithResult<TListener, TResult>(
        Func<TListener, TResult> selector,
        Func<TResult, bool> shouldStop,
        TResult? defaultValue = default)
        where TListener : IEventListenerBase
    {
        var listeners = GetSortedListeners<TListener>();

        foreach (var listener in listeners)
        {
            try
            {
                TResult result = selector((TListener)listener);
                if (shouldStop(result))
                {
                    Logger.LogDebug(
                        $"Event stopped by {listener.GetType().Name} with result");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    $"Error in event listener {listener.GetType().Name}");
            }
        }

        return defaultValue;
    }

    #endregion
}
