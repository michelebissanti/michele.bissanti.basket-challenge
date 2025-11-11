using UnityEngine;

/// <summary>
/// Generic Singleton pattern implementation for MonoBehaviour classes.
/// Ensures only one instance of the derived class exists throughout the application lifecycle.
/// Automatically handles duplicate instances and persists across scene loads.
/// </summary>
/// <typeparam name="T">The type of the Singleton, must be a Component.</typeparam>
public class Singleton<T> : MonoBehaviour where T : Component
{
    #region Private Fields

    /// <summary>The single instance of the Singleton.</summary>
    private static T instance;

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the singleton instance. Creates a new instance if one doesn't exist.
    /// Thread-safe lazy initialization.
    /// </summary>
    public static T Instance
    {
        get
        {
            // Check if instance exists
            if (instance == null)
            {
                // Try to find existing instance in scene
                instance = (T)FindObjectOfType(typeof(T));

                // If still not found, create new instance
                if (instance == null)
                {
                    SetupInstance();
                }
            }
            return instance;
        }
    }

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Ensures no duplicate instances exist in the scene.
    /// </summary>
    public virtual void Awake()
    {
        RemoveDuplicates();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates and sets up a new singleton instance.
    /// If no instance exists in the scene, creates a new GameObject with the component.
    /// Marks the GameObject as DontDestroyOnLoad to persist across scenes.
    /// </summary>
    private static void SetupInstance()
    {
        // Try to find existing instance one more time
        instance = (T)FindObjectOfType(typeof(T));

        // If no instance exists, create new GameObject with the component
        if (instance == null)
        {
            GameObject gameObj = new GameObject();
            gameObj.name = typeof(T).Name;
            instance = gameObj.AddComponent<T>();

            // Prevent destruction when loading new scenes
            DontDestroyOnLoad(gameObj);
        }
    }

    /// <summary>
    /// Removes duplicate instances of the singleton.
    /// If this is the first instance, it becomes the singleton and persists across scenes.
    /// If an instance already exists, this GameObject is destroyed to maintain singleton pattern.
    /// </summary>
    private void RemoveDuplicates()
    {
        // If no instance exists yet, this becomes the instance
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Duplicate detected - destroy this instance
            Destroy(gameObject);
        }
    }

    #endregion
}
