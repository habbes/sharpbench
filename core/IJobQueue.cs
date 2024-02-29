using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharpbench.Core;

public interface IJobQueue
{
    Task SubmitJob(string jobId);
    IAsyncEnumerable<string> ListenForJobs(CancellationToken cancellationToken);
}
