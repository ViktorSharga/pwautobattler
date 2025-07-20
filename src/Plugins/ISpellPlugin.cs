using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameAutomation.Models.Spells;

namespace GameAutomation.Plugins
{
    /// <summary>
    /// Interface for spell plugins that can dynamically provide spell implementations
    /// </summary>
    public interface ISpellPlugin
    {
        /// <summary>
        /// The name of the plugin
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The version of the plugin
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// A description of what this plugin provides
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The author of the plugin
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Game classes that this plugin supports
        /// </summary>
        IEnumerable<Models.GameClass> SupportedClasses { get; }

        /// <summary>
        /// Initialize the plugin with the service provider
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency injection</param>
        /// <returns>Task representing the initialization operation</returns>
        Task InitializeAsync(IServiceProvider serviceProvider);

        /// <summary>
        /// Get all spells provided by this plugin
        /// </summary>
        /// <returns>Collection of spells</returns>
        IEnumerable<ISpell> GetSpells();

        /// <summary>
        /// Get spells for a specific game class
        /// </summary>
        /// <param name="gameClass">The game class to get spells for</param>
        /// <returns>Collection of spells for the specified class</returns>
        IEnumerable<ISpell> GetSpellsForClass(Models.GameClass gameClass);

        /// <summary>
        /// Check if the plugin supports a specific game class
        /// </summary>
        /// <param name="gameClass">The game class to check</param>
        /// <returns>True if the plugin supports the class</returns>
        bool SupportsClass(Models.GameClass gameClass);

        /// <summary>
        /// Dispose of any resources used by the plugin
        /// </summary>
        void Dispose();
    }

    /// <summary>
    /// Base class for spell plugins providing common functionality
    /// </summary>
    public abstract class SpellPluginBase : ISpellPlugin
    {
        protected IServiceProvider? ServiceProvider { get; private set; }
        protected bool IsInitialized { get; private set; }

        public abstract string Name { get; }
        public abstract Version Version { get; }
        public abstract string Description { get; }
        public abstract string Author { get; }
        public abstract IEnumerable<Models.GameClass> SupportedClasses { get; }

        public virtual async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            await OnInitializeAsync();
            IsInitialized = true;
        }

        public abstract IEnumerable<ISpell> GetSpells();

        public virtual IEnumerable<ISpell> GetSpellsForClass(Models.GameClass gameClass)
        {
            return GetSpells().Where(spell => 
                spell.Requirements.RequiredClass == gameClass || 
                spell.Requirements.RequiredClass == Models.GameClass.None);
        }

        public virtual bool SupportsClass(Models.GameClass gameClass)
        {
            return SupportedClasses.Contains(gameClass) || SupportedClasses.Contains(Models.GameClass.None);
        }

        public virtual void Dispose()
        {
            OnDispose();
            IsInitialized = false;
        }

        /// <summary>
        /// Override this method to provide custom initialization logic
        /// </summary>
        protected virtual Task OnInitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override this method to provide custom disposal logic
        /// </summary>
        protected virtual void OnDispose()
        {
        }

        protected void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException($"Plugin '{Name}' has not been initialized. Call InitializeAsync first.");
            }
        }
    }
}