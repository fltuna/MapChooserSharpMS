using System;
using System.Collections.Generic;
using MapChooserSharpMS.Shared.Events;

namespace MapChooserSharpMS.Modules.EventManager;

/// <summary>
/// Internal event manager for handling event listeners and firing events
/// </summary>
internal interface IInternalEventManager
{
    /// <summary>
    /// Registers a listener for a specific event type
    /// </summary>
    /// <typeparam name="TListener">Event listener interface type</typeparam>
    /// <param name="listener">Listener instance to register</param>
    void RegisterListener<TListener>(TListener listener)
        where TListener : IEventListenerBase;

    /// <summary>
    /// Removes a registered listener
    /// </summary>
    /// <typeparam name="TListener">Event listener interface type</typeparam>
    /// <param name="listener">Listener instance to remove</param>
    void RemoveListener<TListener>(TListener listener)
        where TListener : IEventListenerBase;

    /// <summary>
    /// Fires an event to all registered listeners (void return type)
    /// </summary>
    /// <typeparam name="TListener">Event listener interface type</typeparam>
    /// <param name="action">Action to execute on each listener</param>
    void Fire<TListener>(Action<TListener> action)
        where TListener : IEventListenerBase;

    /// <summary>
    /// Fires a cancellable event to registered listeners (bool return type)
    /// </summary>
    /// <typeparam name="TListener">Event listener interface type</typeparam>
    /// <param name="predicate">Predicate to execute on each listener. Returns true to cancel the event</param>
    /// <returns>True if the event was cancelled by any listener, false otherwise</returns>
    bool FireCancellable<TListener>(Func<TListener, bool> predicate)
        where TListener : IEventListenerBase;

    /// <summary>
    /// Fires an event and collects results from all registered listeners
    /// </summary>
    /// <typeparam name="TListener">Event listener interface type</typeparam>
    /// <typeparam name="TResult">Result type (can be any type including custom classes)</typeparam>
    /// <param name="selector">Function to execute on each listener that returns a result</param>
    /// <param name="skipNullResults">If true, null results will be excluded from the returned list</param>
    /// <returns>List of collected results from all listeners</returns>
    List<TResult> FireCollect<TListener, TResult>(
        Func<TListener, TResult> selector,
        bool skipNullResults = true)
        where TListener : IEventListenerBase;

    /// <summary>
    /// Fires an event and collects flattened results from all registered listeners
    /// (for listeners that return IEnumerable)
    /// </summary>
    /// <typeparam name="TListener">Event listener interface type</typeparam>
    /// <typeparam name="TResult">Element type in the collection</typeparam>
    /// <param name="selector">Function to execute on each listener that returns an enumerable</param>
    /// <returns>Flattened list of all results from all listeners</returns>
    List<TResult> FireCollectMany<TListener, TResult>(
        Func<TListener, IEnumerable<TResult>> selector)
        where TListener : IEventListenerBase;

    /// <summary>
    /// Fires an event with custom result handling and stop condition
    /// </summary>
    /// <typeparam name="TListener">Event listener interface type</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="selector">Function to execute on each listener that returns a result</param>
    /// <param name="shouldStop">Predicate to determine if iteration should stop based on result</param>
    /// <param name="defaultValue">Default value to return if no listener satisfies the stop condition</param>
    /// <returns>First result that satisfies the stop condition, or defaultValue</returns>
    TResult? FireWithResult<TListener, TResult>(
        Func<TListener, TResult> selector,
        Func<TResult, bool> shouldStop,
        TResult? defaultValue = default)
        where TListener : IEventListenerBase;
}