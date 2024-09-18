using Sharpbench.Core;

namespace SharpbenchCore;

public record JobMessage(string JobId, JobMessageType Type, byte[] Data);

record LogMessage(string Type, string JobId, string LogSource, string Message);
record JobCompleteMessage(string Type, string JobId, Job Job);

record JobTitleGeneratedMessage(string Type, string JobId, string Title);

record JobResultsAnalysisGeneratedMessage(string Type, string JobId, string Analysis);

public enum JobMessageType
{
    /// <summary>
    /// A log message from stdout or stderr
    /// </summary>
    Log,
    /// <summary>
    /// A status update
    /// </summary>
    Status,
    /// <summary>
    /// Notification that title was auto-generated
    /// </summary>
    TitleGenerated,
    /// <summary>
    /// Notification that analysis was auto-generated
    /// </summary>
    ResultsAnalysisGenerated,
}
