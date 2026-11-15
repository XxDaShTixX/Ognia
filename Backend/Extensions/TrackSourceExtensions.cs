using Ognia.Backend.Enums;

namespace Ognia.Backend.Extensions
{
    public static class TrackSourceExtensions
    {
        /// <summary>Converts the enum to the canonical string stored in the DB and JSON.</summary>
        public static string ToWireName(this TrackSource source) => source switch
        {
            TrackSource.YouTube => "youtube",
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unhandled source.")
        };
    }
}
