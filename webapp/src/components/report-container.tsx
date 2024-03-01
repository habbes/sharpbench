import { Job } from '@/types';
import Markdown from "react-markdown";
import remarkGfm from 'remark-gfm'

export function ReportContainer({ job } : { job : Job }) {
  return (
    <>
    {
      job.status === 'completed' ?
        <Markdown remarkPlugins={[remarkGfm]}>{ job.markdownReport }</Markdown>
      : job.status === 'error' ?
        <div className="font-mono">
          Execution failed with exit code { job.exitCode}. Check out the logs for more details.
        </div>
      :
        <div>Results not ready. Check again after the job is completed.</div>
    }
    </>
  );
}
