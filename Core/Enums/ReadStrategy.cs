namespace CheapSerial.Core.Enums
{
    /// <summary>
    /// Read strategy options for handling the SerialStream async bug
    /// </summary>
    public enum ReadStrategy
    {
        /// <summary>
        /// Always use async reads (fastest, but may hit SerialStream bug)
        /// </summary>
        AsyncOnly,

        /// <summary>
        /// Always use sync reads wrapped in Task.Run (most reliable)
        /// </summary>
        SyncOnly,

        /// <summary>
        /// Try async first, fallback to sync on error (recommended)
        /// </summary>
        AsyncWithSyncFallback,

        /// <summary>
        /// Start with sync, switch to async after successful connections
        /// </summary>
        SyncWithAsyncPromotion
    }
}